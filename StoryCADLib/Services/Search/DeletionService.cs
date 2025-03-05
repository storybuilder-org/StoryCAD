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
        bool result = false;
        _arg = searchArg;
        StoryElement element = null;
        _elementCollection = model.StoryElements;

        if (model.StoryElements.StoryElementGuids.ContainsKey(node.Uuid)) { element = model.StoryElements.StoryElementGuids[node.Uuid]; }
        if (element == null) { return false; } 
        switch (element.ElementType)
        {
            case StoryItemType.StoryOverview:
                result = SearchStoryOverview(element, delete);
                break;
            case StoryItemType.Problem:
                result = SearchProblem(element, delete);
                break;
            case StoryItemType.Character:
                result = SearchCharacter(element, delete);
                break;
            case StoryItemType.Scene:
                result = SearchScene(element, delete);
                break;
        }
        return result;
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
        if (scene.Protagonist != Guid.Empty) // Searches protagonist
        {
            if (_elementCollection.StoryElementGuids.TryGetValue(scene.Protagonist, out StoryElement protag))
            {
                if (protag.Uuid == _arg)
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
            }
            else
            {
                _logger.Log(LogLevel.Warn, $"Protagonist with GUID {scene.Protagonist} not found.");
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
            //TODO: Test deletion service; this is changed code
            if (problem.Protagonist != Guid.Empty)
            {
                var protagonist = (CharacterModel) _elementCollection.StoryElementGuids[problem.Protagonist];
                if (problem.Uuid == _arg) 
                {
                    if (delete) { problem.Protagonist = Guid.Empty; }
                    else 
                        return true;  
                } //Checks problem name
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
        try
        {
            OverviewModel overview = (OverviewModel)element;
            if (overview.StoryProblem != Guid.Empty)
            {
                var problem = (ProblemModel) _elementCollection.StoryElementGuids[overview.StoryProblem];
                if (problem.Uuid == _arg) 
                {
                    if (delete) { overview.StoryProblem = Guid.Empty; }
                    else 
                        return true;  
                } //Checks problem name
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, $"Failed to search scene for storyoverview got error {ex.Message}");
        }

        return false;
    }
}