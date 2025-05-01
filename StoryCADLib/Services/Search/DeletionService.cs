using System.Diagnostics;

namespace StoryCAD.Services.Search;

/// <summary>
/// This is a copy of the search service however 
/// It does not search the name of the node and 
/// each search has an optional argument to delete references.
/// </summary>
public class DeletionService
{
    private Guid _arg;
    private StoryElementCollection _elementCollection;
    private LogService _logger = Ioc.Default.GetRequiredService<LogService>();

    /// <summary>
    /// Search a StoryElement for a given string search argument
    /// </summary>
    /// <param name="node">StoryNodeItem whose StoryElement to search</param>
    /// <param name="searchArg">string to search for</param>
    /// <param name="model">model to search in</param>
    /// <returns>true if StoryElement contains search argument</returns>
    public bool SearchStoryElement(StoryNodeItem node, Guid searchArg, StoryModel model, bool delete = false)
    {
        if (model?.StoryElements?.StoryElementGuids == null)
        {
            _logger.Log(LogLevel.Info, "StoryElements or its Guid dictionary is null");
            return false;
        }

        if (node == null)
        {
            return false;
        }

        if (!model.StoryElements.StoryElementGuids.TryGetValue(node.Uuid, out var element)
            || element == null)
        {
            _logger.Log(LogLevel.Info, $"No element found for UUID {node.Uuid}");
            return false;
        }

        _arg = searchArg;
        _elementCollection = model.StoryElements;

        switch (element.ElementType)
        {
            case StoryItemType.StoryOverview:
                return SearchStoryOverview(element, delete);
            case StoryItemType.Problem:
                return SearchProblem(element, delete);
            case StoryItemType.Character:
                return SearchCharacter(element, delete);
            case StoryItemType.Scene:
                return SearchScene(element, delete);
            default:
                _logger.Log(LogLevel.Info, $"Unhandled StoryItemType: {element.ElementType}");
                return false;
        }
    }


    /// <summary>
    /// Searches Cast members, viewpoint character, protagonist name, antagonist name,
    /// and the selected setting within a scene node for the specified GUID.
    /// Optionally deletes references if the <paramref name="delete"/> flag is set.
    /// </summary>
    /// <param name="element">The <see cref="StoryElement"/> representing the scene to search.</param>
    /// <param name="delete">If set to <c>true</c>, deletes references to the search argument.</param>
    /// <returns><c>true</c> if the search argument is found; otherwise, <c>false</c>.</returns>
    private bool SearchScene(StoryElement element, bool delete)
{
    SceneModel scene = (SceneModel)element;

    List<Guid> newCast = new();
    
    foreach (Guid memberGuid in scene.CastMembers) // Searches character in scene
    {
        try
        {
            if (_elementCollection.StoryElementGuids.TryGetValue(memberGuid, out StoryElement model))
            {
                if (model.Uuid == _arg)
                {
                    if (!delete) { return true; }
                }
                else
                {
                    newCast.Add(memberGuid);
                }
            }
            else
            {
                _logger.Log(LogLevel.Warn, $"Cast member with GUID {memberGuid} not found.");
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, $"Error checking scene cast member ({ex.Message})");
        }
    }

    if (delete)
    {
        scene.CastMembers = newCast;
    }

    try
    {
        if (scene.Protagonist == _arg) // Searches protagonist
        {
            if (delete)
            {
                scene.Protagonist = Guid.Empty;
            }
            else
            {
                return true;
            }
        }
        if (scene.Antagonist == _arg) // Searches Antagonist
        {
            if (delete)
            {
                scene.Antagonist = Guid.Empty;
            }
            else
            {
                return true;
            }
        }

        if (scene.ViewpointCharacter == _arg) // Searches ViewpointCharacter
        {
            if (delete)
            {
                scene.ViewpointCharacter = Guid.Empty;
            }
            else
            {
                return true;
            }
        }

        if (scene.Setting == _arg) // Searches ViewpointCharacter
        {
            if (delete)
            {
                scene.Setting = Guid.Empty;
            }
            else
            {
                return true;
            }
        }
    }
    catch (Exception ex)
    {
        _logger.Log(LogLevel.Warn, $"Error checking scene protagonist ({ex.Message})");
    }

    try
    {
        if (scene.Antagonist != Guid.Empty) // Searches Antagonist
        {
            if (_elementCollection.StoryElementGuids.TryGetValue(scene.Antagonist, out StoryElement antag))
            {
                if (antag.Uuid == _arg)
                {
                    if (delete)
                    {
                        scene.Antagonist = Guid.Empty;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            else
            {
                _logger.Log(LogLevel.Warn, $"Antagonist with GUID {scene.Antagonist} not found.");
            }
        }
    }
    catch (Exception ex)
    {
        _logger.Log(LogLevel.Warn, $"Error checking scene antagonist ({ex.Message})");
    }

    try
    {
        if (scene.Setting != Guid.Empty) // Searches Setting
        {
            if (_elementCollection.StoryElementGuids.TryGetValue(scene.Setting, out StoryElement setting))
            {
                if (setting.Uuid == _arg)
                {
                    if (delete)
                    {
                        scene.Setting = Guid.Empty;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            else
            {
                _logger.Log(LogLevel.Warn, $"Setting with GUID {scene.Setting} not found.");
            }
        }
    }
    catch (Exception ex)
    {
        _logger.Log(LogLevel.Warn, $"Error checking scene setting ({ex.Message})");
    }

    return false;
}

    /// <summary>
    /// Searches the name of each character in a relationship and the name of the character.
    /// Optionally deletes relationships if the <paramref name="delete"/> flag is set.
    /// </summary>
    /// <param name="element">The <see cref="StoryElement"/> representing the character to search.</param>
    /// <param name="delete">If set to <c>true</c>, deletes relationships referencing the search argument.</param>
    /// <returns><c>true</c> if the search argument is found; otherwise, <c>false</c>.</returns>
    private bool SearchCharacter(StoryElement element, bool delete)
    {
        CharacterModel characterModel = (CharacterModel)element;

        List<RelationshipModel> newRelationships = new();
        foreach (RelationshipModel partner in characterModel.RelationshipList) //Checks each character in relationship
        {
            try
            {
                StoryElement model = StoryElement.GetByGuid(partner.PartnerUuid);
                if (model.Uuid == _arg)
                {
                    if (!delete) { return true; }
                }
                else { newRelationships.Add(partner); }
            }
            catch (Exception ex) { _logger.Log(LogLevel.Warn,$"Error checking partner in relationship list {ex.Message}"); }

        }
        if (delete) { characterModel.RelationshipList = newRelationships; }
        return false;
    }

    /// <summary>
    /// Searches a problem for the element name, antagonist name, and protagonist name.
    /// Optionally deletes references if the <paramref name="delete"/> flag is set.
    /// </summary>
    /// <param name="element">The <see cref="StoryElement"/> representing the problem to search.</param>
    /// <param name="delete">If set to <c>true</c>, deletes references to the search argument.</param>
    /// <returns><c>true</c> if the search argument is found; otherwise, <c>false</c>.</returns>
    private bool SearchProblem(StoryElement element, bool delete)
    {
        ProblemModel problem = (ProblemModel)element;

        try
        {
            if (problem.Protagonist == _arg)
            {
                if (delete) { problem.Protagonist = Guid.Empty; }
                else
                    return true;
            }

            if (problem.Antagonist == _arg)
            {
                if (delete) { problem.Antagonist = Guid.Empty; }
                else
                    return true;
            }

            if (problem.StructureBeats != null)
            {
                foreach (var beat in problem.StructureBeats)
                {
                    if (beat.Guid == _arg)
                    {
                        if (delete) { beat.Guid = Guid.Empty; }
                        else
                            return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, $"Failed to search scene for protagonist - ({problem.Protagonist}) got error {ex.Message}");
        }

        try
        {
            if (problem.Antagonist != Guid.Empty)
            {
                var antagonist = (CharacterModel) _elementCollection.StoryElementGuids[problem.Antagonist];
                if (problem.Uuid == _arg) 
                {
                    if (delete) { problem.Antagonist = Guid.Empty; }
                    else 
                        return true;  
                } //Checks problem name
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, $"Failed to search scene for antagonist - ({problem.Antagonist}) got error {ex.Message}");
        }


        return false;
    }

    /// <summary>
    /// Searches the overview node for the name and main story problem.
    /// Optionally deletes references if the <paramref name="delete"/> flag is set.
    /// </summary>
    /// <param name="element">The <see cref="StoryElement"/> representing the story overview to search.</param>
    /// <param name="delete">If set to <c>true</c>, deletes references to the search argument.</param>
    /// <returns><c>true</c> if the search argument is found; otherwise, <c>false</c>.</returns>
    private bool SearchStoryOverview(StoryElement element, bool delete)
    {
        if (element is not OverviewModel overview) return false;

        bool found = false;

        if (overview.StoryProblem == _arg)
        {
            found = true;
            if (delete) overview.StoryProblem = Guid.Empty;
        }

        if (overview.ViewpointCharacter == _arg)
        {
            found = true;
            if (delete) overview.ViewpointCharacter = Guid.Empty;
        }

        return found;
    }
}