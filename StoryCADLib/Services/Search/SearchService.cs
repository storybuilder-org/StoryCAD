using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
using StoryCADLib.Models;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.Search;

/// <summary>
///     Service responsible for searching and managing references to StoryElements within the StoryModel.
///     Supports both string-based content search and UUID-based reference search with optional deletion.
///     Uses reflection with caching to efficiently search all public properties.
/// </summary>
public class SearchService
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private readonly ILogService _logger;

    public SearchService(ILogService logger)
    {
        _logger = logger;
    }

    #region Public API

    /// <summary>
    ///     Searches a StoryElement for a given string using reflection.
    ///     Performs case-insensitive search across all string properties and referenced element names.
    ///     Also searches for reverse relationships (e.g., Problems that reference this element in StructureBeats).
    /// </summary>
    public bool SearchString(StoryNodeItem node, string searchArg, StoryModel model)
    {
        if (string.IsNullOrEmpty(searchArg) || node == null || model?.StoryElements?.StoryElementGuids == null)
            return false;

        if (!model.StoryElements.StoryElementGuids.TryGetValue(node.Uuid, out var element))
        {
            _logger?.Log(LogLevel.Info, $"No element found for UUID {node.Uuid}");
            return false;
        }

        // Direct search
        if (SearchElementForString(element, searchArg.ToLower(), model.StoryElements))
            return true;

        // Reverse search - find elements that reference this one
        return SearchReverseReferences(element.Uuid, searchArg.ToLower(), model.StoryElements);
    }

    /// <summary>
    ///     Searches a StoryElement for references to a given UUID using reflection.
    ///     Optionally removes found references when delete flag is set.
    /// </summary>
    public bool SearchUuid(StoryNodeItem node, Guid searchArg, StoryModel model, bool delete = false)
    {
        if (node == null || model?.StoryElements?.StoryElementGuids == null)
            return false;

        if (!model.StoryElements.StoryElementGuids.TryGetValue(node.Uuid, out var element))
        {
            _logger?.Log(LogLevel.Info, $"No element found for UUID {node.Uuid}");
            return false;
        }

        return SearchElementForUuid(element, searchArg, delete);
    }

    #endregion

    #region Core Search Logic

    private bool SearchElementForString(object obj, string searchString, StoryElementCollection elementCollection)
    {
        if (obj == null) return false;

        var properties = GetCachedProperties(obj.GetType());

        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);
                if (value == null) continue;

                if (property.PropertyType == typeof(string))
                {
                    if (((string)value).ToLower().Contains(searchString))
                        return true;
                }
                else if (property.PropertyType == typeof(Guid))
                {
                    var guid = (Guid)value;
                    if (guid != Guid.Empty &&
                        elementCollection.StoryElementGuids.TryGetValue(guid, out var element) &&
                        element.Name?.ToLower().Contains(searchString) == true)
                        return true;
                }
                else if (value is IEnumerable && property.PropertyType.IsGenericType &&
                         SearchCollectionForString(value, property.PropertyType, searchString, elementCollection))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Warn, $"Error searching property {property.Name}: {ex.Message}");
            }
        }

        return false;
    }

    private bool SearchElementForUuid(object obj, Guid targetUuid, bool delete)
    {
        if (obj == null) return false;

        var found = false;
        var properties = GetCachedProperties(obj.GetType());

        foreach (var property in properties)
        {
            try
            {
                if (property.Name.Equals("Uuid", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("_uuid", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (delete && !property.CanWrite) continue;

                var value = property.GetValue(obj);
                if (value == null) continue;

                if (property.PropertyType == typeof(Guid) && (Guid)value == targetUuid)
                {
                    if (delete)
                    {
                        property.SetValue(obj, Guid.Empty);
                        found = true;
                    }
                    else
                        return true;
                }
                else if (value is IEnumerable && property.PropertyType.IsGenericType)
                {
                    if (delete)
                    {
                        if (RemoveFromCollection(value, property.PropertyType, targetUuid))
                            found = true;
                    }
                    else if (SearchCollectionForUuid(value, property.PropertyType, targetUuid))
                        return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Warn, $"Error searching property {property.Name}: {ex.Message}");
            }
        }

        return found;
    }

    #endregion

    #region Collection Search (Unified)

    private bool SearchCollectionForString(object collection, Type collectionType, string searchString,
        StoryElementCollection elementCollection)
    {
        var elementType = GetCollectionElementType(collectionType);
        if (elementType == null) return false;

        if (elementType == typeof(Guid))
            return SearchEnumerable<Guid>(collection, guid =>
                guid != Guid.Empty &&
                elementCollection.StoryElementGuids.TryGetValue(guid, out var el) &&
                el.Name?.ToLower().Contains(searchString) == true);

        if (elementType == typeof(StructureBeatViewModel))
            return SearchStructureBeats(collection, searchString, elementCollection);

        if (elementType == typeof(RelationshipModel))
            return SearchRelationships(collection, searchString, elementCollection);

        return false;
    }

    private bool SearchCollectionForUuid(object collection, Type collectionType, Guid targetUuid)
    {
        var elementType = GetCollectionElementType(collectionType);
        if (elementType == null) return false;

        if (elementType == typeof(Guid))
            return SearchEnumerable<Guid>(collection, guid => guid == targetUuid);

        if (elementType == typeof(StructureBeatViewModel))
            return SearchEnumerable(collection, beat =>
                GetPropertyValue<Guid>(beat, "Guid") == targetUuid);

        if (elementType == typeof(RelationshipModel))
            return SearchEnumerable(collection, rel =>
                GetPropertyValue<Guid>(rel, "PartnerUuid") == targetUuid);

        return false;
    }

    private bool RemoveFromCollection(object collection, Type collectionType, Guid targetUuid)
    {
        var elementType = GetCollectionElementType(collectionType);
        if (elementType == null) return false;

        // Guid collections - handle both List<Guid> and ObservableCollection<Guid>
        if (elementType == typeof(Guid))
        {
            if (collection is List<Guid> guidList)
            {
                var count = guidList.Count;
                guidList.RemoveAll(g => g == targetUuid);
                return guidList.Count < count;
            }
            if (collection is ObservableCollection<Guid> obsCollection)
            {
                var toRemove = obsCollection.Where(g => g == targetUuid).ToList();
                foreach (var item in toRemove)
                    obsCollection.Remove(item);
                return toRemove.Count > 0;
            }
            return false;
        }

        // StructureBeat collections - clear Guid property
        if (elementType == typeof(StructureBeatViewModel) && collection is IEnumerable beats)
        {
            var found = false;
            foreach (var beat in beats)
            {
                if (GetPropertyValue<Guid>(beat, "Guid") == targetUuid)
                {
                    SetPropertyValue(beat, "Guid", Guid.Empty);
                    found = true;
                }
            }
            return found;
        }

        // Relationship collections
        if (elementType == typeof(RelationshipModel) && collection is IList list)
        {
            var toRemove = new List<object>();
            foreach (var item in list)
            {
                if (GetPropertyValue<Guid>(item, "PartnerUuid") == targetUuid)
                    toRemove.Add(item);
            }
            foreach (var item in toRemove)
                list.Remove(item);
            return toRemove.Count > 0;
        }

        return false;
    }

    #endregion

    #region Specialized Collection Searches

    private bool SearchStructureBeats(object collection, string searchString, StoryElementCollection elementCollection)
    {
        if (collection is not IEnumerable beats) return false;

        foreach (var beat in beats)
        {
            var title = GetPropertyValue<string>(beat, "Title");
            if (title?.ToLower().Contains(searchString) == true)
                return true;

            var description = GetPropertyValue<string>(beat, "Description");
            if (description?.ToLower().Contains(searchString) == true)
                return true;

            var guid = GetPropertyValue<Guid>(beat, "Guid");
            if (guid != Guid.Empty &&
                elementCollection.StoryElementGuids.TryGetValue(guid, out var element) &&
                element.Name?.ToLower().Contains(searchString) == true)
                return true;
        }

        return false;
    }

    private bool SearchRelationships(object collection, string searchString, StoryElementCollection elementCollection)
    {
        if (collection is not IEnumerable relationships) return false;

        foreach (var rel in relationships)
        {
            var partnerUuid = GetPropertyValue<Guid>(rel, "PartnerUuid");
            if (partnerUuid != Guid.Empty &&
                elementCollection.StoryElementGuids.TryGetValue(partnerUuid, out var element) &&
                element.Name?.ToLower().Contains(searchString) == true)
                return true;
        }

        return false;
    }

    #endregion

    #region Reverse Search

    private bool SearchReverseReferences(Guid targetUuid, string searchString, StoryElementCollection elementCollection)
    {
        foreach (var element in elementCollection.StoryElementGuids.Values.Where(e => ElementReferencesUuid(e, targetUuid)))
        {
            if (element.Name?.ToLower().Contains(searchString) == true)
                return true;

            if (SearchElementForString(element, searchString, elementCollection))
                return true;
        }

        return false;
    }

    private bool ElementReferencesUuid(StoryElement element, Guid targetUuid)
    {
        var properties = GetCachedProperties(element.GetType());

        foreach (var property in properties)
        {
            try
            {
                if (property.Name.Equals("Uuid", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = property.GetValue(element);
                if (value == null) continue;

                if (property.PropertyType == typeof(Guid) && (Guid)value == targetUuid)
                    return true;

                if (value is IEnumerable && property.PropertyType.IsGenericType &&
                    SearchCollectionForUuid(value, property.PropertyType, targetUuid))
                    return true;
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Warn, $"Error checking property {property.Name}: {ex.Message}");
            }
        }

        return false;
    }

    #endregion

    #region Reflection Helpers

    private PropertyInfo[] GetCachedProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }

    private Type GetCollectionElementType(Type collectionType)
    {
        if (!collectionType.IsGenericType) return null;

        var genericDef = collectionType.GetGenericTypeDefinition();
        if (genericDef != typeof(List<>) && genericDef != typeof(IList<>) &&
            genericDef != typeof(ICollection<>) && genericDef != typeof(ObservableCollection<>))
            return null;

        return collectionType.GetGenericArguments()[0];
    }

    private T GetPropertyValue<T>(object obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        if (prop == null) return default;

        var value = prop.GetValue(obj);
        return value is T typedValue ? typedValue : default;
    }

    private void SetPropertyValue(object obj, string propertyName, object value)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        if (prop?.CanWrite == true)
            prop.SetValue(obj, value);
    }

    private bool SearchEnumerable<T>(object collection, Func<T, bool> predicate)
    {
        if (collection is IEnumerable<T> typedCollection)
            return typedCollection.Any(predicate);
        return false;
    }

    private bool SearchEnumerable(object collection, Func<object, bool> predicate)
    {
        if (collection is not IEnumerable enumerable) return false;

        foreach (var item in enumerable)
        {
            if (predicate(item))
                return true;
        }
        return false;
    }

    #endregion
}
