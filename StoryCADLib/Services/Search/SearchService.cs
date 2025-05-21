using System.Collections;
using System.Reflection;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Services.Logging;

namespace StoryCAD.Services.Search;

/// <summary>
/// Provides methods to search <see cref="StoryElement"/> instances for
/// strings or GUID references using reflection.
/// </summary>
public class SearchService
{
    private readonly LogService logger;

    public SearchService()
    {
        logger = Ioc.Default.GetRequiredService<LogService>();
    }

    /// <summary>
    /// Searches a story element identified by <paramref name="node"/> for the
    /// specified text.
    /// </summary>
    /// <param name="node">Node whose element will be scanned.</param>
    /// <param name="searchArg">Text to search for.</param>
    /// <param name="model">Model containing the element.</param>
    /// <returns>true if the element contains the text.</returns>
    public bool SearchForString(StoryNodeItem node, string searchArg, StoryModel model)
    {
        if (string.IsNullOrEmpty(searchArg) || model?.StoryElements == null || node == null)
            return false;

        if (!model.StoryElements.StoryElementGuids.TryGetValue(node.Uuid, out StoryElement element) || element == null)
            return false;

        return SearchObjectForString(element, searchArg.ToLowerInvariant());
    }

    private bool SearchObjectForString(object? obj, string query)
    {
        if (obj == null) return false;
        if (obj is string s)
        {
            return s.ToLowerInvariant().Contains(query);
        }

        if (obj is IEnumerable enumerable && obj is not string)
        {
            foreach (var item in enumerable)
                if (SearchObjectForString(item, query))
                    return true;
            return false;
        }

        foreach (PropertyInfo prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length > 0) continue;
            object? value;
            try { value = prop.GetValue(obj); }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Warn, $"Reflection error: {ex.Message}");
                continue;
            }
            if (SearchObjectForString(value, query))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Searches a story element identified by <paramref name="node"/> for a
    /// reference to <paramref name="searchGuid"/>. The element itself is
    /// ignored if its GUID matches the argument.
    /// </summary>
    /// <param name="node">Node whose element will be scanned.</param>
    /// <param name="searchGuid">GUID to locate.</param>
    /// <param name="model">Model containing the element.</param>
    /// <returns>true if a reference is found.</returns>
    public bool SearchForGuid(StoryNodeItem node, Guid searchGuid, StoryModel model)
    {
        if (searchGuid == Guid.Empty || model?.StoryElements == null || node == null)
            return false;

        if (!model.StoryElements.StoryElementGuids.TryGetValue(node.Uuid, out StoryElement element) || element == null)
            return false;

        if (element.Uuid == searchGuid)
            return false;

        return SearchObjectForGuid(element, searchGuid);
    }

    private bool SearchObjectForGuid(object? obj, Guid query)
    {
        if (obj == null) return false;
        if (obj is Guid g)
            return g == query;
        if (obj is IEnumerable enumerable && obj is not string)
        {
            foreach (var item in enumerable)
                if (SearchObjectForGuid(item, query))
                    return true;
            return false;
        }
        foreach (PropertyInfo prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length > 0) continue;
            object? value;
            try { value = prop.GetValue(obj); }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Warn, $"Reflection error: {ex.Message}");
                continue;
            }
            if (SearchObjectForGuid(value, query))
                return true;
        }
        return false;
    }
}
