using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
//using LogLevel = StoryCAD.Services.Logging.LogLevel;

namespace StoryCAD.Services.Search;

/// <summary>
///     Service responsible for searching and managing references to StoryElements within the StoryModel.
///     Supports both string-based content search and UUID-based reference search with optional deletion.
///     Uses reflection with caching to efficiently search all public properties.
/// </summary>
public class SearchService
{
    /// <summary>
    ///     Cache for reflection metadata to improve performance.
    ///     Stores property information to avoid repeated reflection calls.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private readonly ILogService _logger;

    /// <summary>
    ///     Initializes a new instance of the SearchService class.
    /// </summary>
    public SearchService(ILogService logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Searches a StoryElement for a given string using reflection.
    ///     Performs case-insensitive search across all string properties and referenced element names.
    /// </summary>
    /// <param name="node">The StoryNodeItem whose StoryElement will be searched.</param>
    /// <param name="searchArg">The string to search for (case-insensitive).</param>
    /// <param name="model">The StoryModel containing the StoryElements.</param>
    /// <returns>true if the StoryElement contains the search argument; otherwise, false.</returns>
    public bool SearchString(StoryNodeItem node, string searchArg, StoryModel model)
    {
        // Validate input parameters
        if (string.IsNullOrEmpty(searchArg) || node == null || model?.StoryElements?.StoryElementGuids == null)
        {
            return false;
        }

        // Get the element for this node
        if (!model.StoryElements.StoryElementGuids.TryGetValue(node.Uuid, out var element))
        {
            _logger?.Log(LogLevel.Info, $"No element found for UUID {node.Uuid}");
            return false;
        }

        // Perform string search
        return SearchElementForString(element, searchArg.ToLower(), model.StoryElements);
    }

    /// <summary>
    ///     Searches a StoryElement for references to a given UUID using reflection.
    ///     Optionally removes found references when delete flag is set.
    /// </summary>
    /// <param name="node">The StoryNodeItem whose StoryElement to search.</param>
    /// <param name="searchArg">The UUID to search for.</param>
    /// <param name="model">The StoryModel to search in.</param>
    /// <param name="delete">If true, deletes found references.</param>
    /// <returns>true if the StoryElement contains the UUID reference; otherwise, false.</returns>
    public bool SearchUuid(StoryNodeItem node, Guid searchArg, StoryModel model, bool delete = false)
    {
        // Validate input parameters
        if (node == null || model?.StoryElements?.StoryElementGuids == null)
        {
            return false;
        }

        // Get the element for this node
        if (!model.StoryElements.StoryElementGuids.TryGetValue(node.Uuid, out var element))
        {
            _logger?.Log(LogLevel.Info, $"No element found for UUID {node.Uuid}");
            return false;
        }

        // Perform UUID search/deletion
        return SearchElementForUuid(element, searchArg, delete);
    }

    /// <summary>
    ///     Searches an object for string content using reflection.
    /// </summary>
    /// <param name="obj">The object to search.</param>
    /// <param name="searchString">The lowercase search string.</param>
    /// <param name="elementCollection">The story elements collection for lookups.</param>
    /// <returns>true if the search string is found; otherwise, false.</returns>
    private bool SearchElementForString(object obj, string searchString, StoryElementCollection elementCollection)
    {
        if (obj == null)
        {
            return false;
        }

        var type = obj.GetType();

        // Get cached properties or cache them for performance
        var properties = PropertyCache.GetOrAdd(type,
            t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        // Search all public properties
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);
                if (value == null)
                {
                    continue;
                }

                // Handle string properties
                if (property.PropertyType == typeof(string))
                {
                    var stringValue = (string)value;
                    if (!string.IsNullOrEmpty(stringValue) && stringValue.ToLower().Contains(searchString))
                    {
                        return true;
                    }
                }
                // Handle Guid properties - check their associated element names
                else if (property.PropertyType == typeof(Guid))
                {
                    var guidValue = (Guid)value;
                    if (guidValue != Guid.Empty &&
                        elementCollection.StoryElementGuids.TryGetValue(guidValue, out var element) &&
                        !string.IsNullOrEmpty(element.Name) &&
                        element.Name.ToLower().Contains(searchString))
                    {
                        return true;
                    }
                }
                // Handle collections of Guids
                else if (IsCollectionOfType(property.PropertyType, typeof(Guid)))
                {
                    if (SearchGuidCollectionForString(value, searchString, elementCollection))
                    {
                        return true;
                    }
                }
                // Handle RelationshipModel collections
                else if (IsCollectionOfType(property.PropertyType, "RelationshipModel"))
                {
                    if (SearchRelationshipCollectionForString(value, searchString, elementCollection))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Warn, $"Error searching property {property.Name}: {ex.Message}");
            }
        }

        return false;
    }

    /// <summary>
    ///     Searches an object for UUID references using reflection.
    ///     Optionally removes found references when delete flag is set.
    /// </summary>
    /// <param name="obj">The object to search.</param>
    /// <param name="targetUuid">The UUID to search for.</param>
    /// <param name="delete">Whether to delete found references.</param>
    /// <returns>true if the UUID is found; otherwise, false.</returns>
    private bool SearchElementForUuid(object obj, Guid targetUuid, bool delete)
    {
        if (obj == null)
        {
            return false;
        }

        var found = false;
        var type = obj.GetType();

        // Get cached properties or cache them for performance
        var properties = PropertyCache.GetOrAdd(type,
            t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        // Search all public properties
        foreach (var property in properties)
        {
            try
            {
                // Skip the element's own UUID fields to avoid self-references
                if (property.Name.Equals("Uuid", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("_uuid", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip read-only properties when deleting
                if (delete && !property.CanWrite)
                {
                    continue;
                }

                var value = property.GetValue(obj);
                if (value == null)
                {
                    continue;
                }

                // Check Guid properties
                if (property.PropertyType == typeof(Guid))
                {
                    var guidValue = (Guid)value;
                    if (guidValue == targetUuid)
                    {
                        if (delete)
                        {
                            property.SetValue(obj, Guid.Empty);
                            found = true;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                // Check collections of Guids
                else if (IsCollectionOfType(property.PropertyType, typeof(Guid)))
                {
                    if (delete)
                    {
                        if (RemoveFromGuidCollection(value, targetUuid))
                        {
                            found = true;
                        }
                    }
                    else if (SearchGuidCollectionForUuid(value, targetUuid))
                    {
                        return true;
                    }
                }
                // Check RelationshipModel collections
                else if (IsCollectionOfType(property.PropertyType, "RelationshipModel"))
                {
                    if (delete)
                    {
                        if (RemoveFromRelationshipCollection(value, targetUuid))
                        {
                            found = true;
                        }
                    }
                    else if (SearchRelationshipCollectionForUuid(value, targetUuid))
                    {
                        return true;
                    }
                }
                // Check StructureBeat collections
                else if (IsCollectionOfType(property.PropertyType, "StructureBeat"))
                {
                    if (delete)
                    {
                        if (RemoveFromStructureBeatCollection(value, targetUuid))
                        {
                            found = true;
                        }
                    }
                    else if (SearchStructureBeatCollectionForUuid(value, targetUuid))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Warn, $"Error searching property {property.Name}: {ex.Message}");
            }
        }

        return found;
    }

    /// <summary>
    ///     Checks if a type is a generic collection containing a specific element type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="elementType">The expected element type or type name.</param>
    /// <returns>true if the type is a collection of the target type; otherwise, false.</returns>
    private bool IsCollectionOfType(Type type, object elementType)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericDef = type.GetGenericTypeDefinition();
        if (genericDef != typeof(List<>) && genericDef != typeof(IList<>) &&
            genericDef != typeof(ICollection<>) && genericDef != typeof(ObservableCollection<>))
        {
            return false;
        }

        var genericArg = type.GetGenericArguments()[0];

        return elementType switch
        {
            Type t => genericArg == t,
            string typeName => genericArg.Name == typeName,
            _ => false
        };
    }

    /// <summary>
    ///     Searches a collection of Guids for string matches.
    /// </summary>
    private bool SearchGuidCollectionForString(object collection, string searchString,
        StoryElementCollection elementCollection)
    {
        if (collection is IEnumerable<Guid> guids)
        {
            foreach (var guid in guids)
            {
                if (guid != Guid.Empty &&
                    elementCollection.StoryElementGuids.TryGetValue(guid, out var element) &&
                    !string.IsNullOrEmpty(element.Name) &&
                    element.Name.ToLower().Contains(searchString))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Searches a collection of Guids for UUID matches.
    /// </summary>
    private bool SearchGuidCollectionForUuid(object collection, Guid targetUuid)
    {
        if (collection is IEnumerable<Guid> guids)
        {
            return guids.Contains(targetUuid);
        }

        return false;
    }

    /// <summary>
    ///     Removes matching UUIDs from a Guid collection.
    /// </summary>
    private bool RemoveFromGuidCollection(object collection, Guid targetUuid)
    {
        if (collection is List<Guid> guidList)
        {
            var originalCount = guidList.Count;
            guidList.RemoveAll(g => g == targetUuid);
            return guidList.Count < originalCount;
        }

        return false;
    }

    /// <summary>
    ///     Searches a collection of RelationshipModel objects for string matches.
    /// </summary>
    private bool SearchRelationshipCollectionForString(object collection, string searchString,
        StoryElementCollection elementCollection)
    {
        if (collection is not IEnumerable relationships)
        {
            return false;
        }

        foreach (var item in relationships)
        {
            var partnerProp = item.GetType().GetProperty("PartnerUuid");
            if (partnerProp == null)
            {
                continue;
            }

            var partnerUuid = (Guid)partnerProp.GetValue(item);
            if (partnerUuid != Guid.Empty &&
                elementCollection.StoryElementGuids.TryGetValue(partnerUuid, out var element) &&
                !string.IsNullOrEmpty(element.Name) &&
                element.Name.ToLower().Contains(searchString))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Searches a collection of RelationshipModel objects for UUID matches.
    /// </summary>
    private bool SearchRelationshipCollectionForUuid(object collection, Guid targetUuid)
    {
        if (collection is not IEnumerable relationships)
        {
            return false;
        }

        foreach (var item in relationships)
        {
            var partnerProp = item.GetType().GetProperty("PartnerUuid");
            if (partnerProp != null && (Guid)partnerProp.GetValue(item) == targetUuid)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Removes relationships with matching partner UUIDs from a collection.
    /// </summary>
    private bool RemoveFromRelationshipCollection(object collection, Guid targetUuid)
    {
        if (collection is not IList list)
        {
            return false;
        }

        // Collect items to remove (can't modify collection while iterating)
        List<object> toRemove = new();
        foreach (var item in list)
        {
            var partnerProp = item.GetType().GetProperty("PartnerUuid");
            if (partnerProp != null && (Guid)partnerProp.GetValue(item) == targetUuid)
            {
                toRemove.Add(item);
            }
        }

        // Remove collected items
        foreach (var item in toRemove)
        {
            list.Remove(item);
        }

        return toRemove.Count > 0;
    }

    /// <summary>
    ///     Searches a collection of StructureBeat objects for UUID matches.
    /// </summary>
    private bool SearchStructureBeatCollectionForUuid(object collection, Guid targetUuid)
    {
        if (collection is not IEnumerable beats)
        {
            return false;
        }

        foreach (var beat in beats)
        {
            var guidProp = beat.GetType().GetProperty("Guid");
            if (guidProp != null && (Guid)guidProp.GetValue(beat) == targetUuid)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Clears UUID references in StructureBeat objects within a collection.
    /// </summary>
    private bool RemoveFromStructureBeatCollection(object collection, Guid targetUuid)
    {
        if (collection is not IEnumerable beats)
        {
            return false;
        }

        var found = false;
        foreach (var beat in beats)
        {
            var guidProp = beat.GetType().GetProperty("Guid");
            if (guidProp != null && guidProp.CanWrite && (Guid)guidProp.GetValue(beat) == targetUuid)
            {
                // Clear the reference by setting to Empty
                guidProp.SetValue(beat, Guid.Empty);
                found = true;
            }
        }

        return found;
    }
}
