﻿using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.ViewModels.Tools;

/// <summary>
///	Model for how the StructureModelTitle Tab works in problem.
/// </summary>
public class StructureBeatViewModel : ObservableObject
{
	public Windowing Windowing;

	#region Constructor
	public StructureBeatViewModel()
	{
		Windowing = Ioc.Default.GetRequiredService<Windowing>();
		ProblemViewModel ProblemVM = Ioc.Default.GetRequiredService<ProblemViewModel>();
		PropertyChanged += ProblemVM.OnPropertyChanged;
	}
	#endregion

	#region Properties
	[JsonIgnore]
	private string title;
	/// <summary>
	/// Title of beat
	/// </summary>
	[JsonInclude]
	[JsonPropertyName("Title")]
	public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

	[JsonIgnore]
    private string description;
	/// <summary>
	/// Description of beat
	/// </summary>
	[JsonInclude]
	[JsonPropertyName("Description")]
	public string Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    [JsonInclude]
	private Guid guid;
	/// <summary>
	/// GUID of problem/scene beat links to.
	/// </summary>
	[JsonInclude]
	[JsonPropertyName("BoundGUID")]
	public Guid Guid
    {
        get => guid;
        set
        {
	        SetProperty(ref guid, value);
			OnPropertyChanged(nameof(Element));
			OnPropertyChanged(nameof(ElementName));
            OnPropertyChanged(nameof(ElementDescription));
            OnPropertyChanged(nameof(ElementIcon));
        }
	}

	/// <summary>
	/// Link to element
	/// </summary>
	[JsonIgnore]
	private StoryElement Element
    {
        get
        {
            if (guid != Guid.Empty)
            {
                return Ioc.Default.GetRequiredService<OutlineViewModel>()
                    .StoryModel.StoryElements.StoryElementGuids[guid];
            }

            return new StoryElement();
        }
    }

	/// <summary>
	/// Name of the element
	/// </summary>
	[JsonIgnore]
	public string ElementName  
    {
        get
        {
            if (Element.Uuid == Guid.Empty)
            {
                return "No element Selected";
            }
            return Element.Name;
        }
    }

	/// <summary>
	/// Element Description
	/// </summary>
	[JsonIgnore]
	public string ElementDescription
    {
        get
        {
            if (Element != null)
            {
                if (Element.Type == StoryItemType.Problem)
                {
                    return ((ProblemModel)Element).StoryQuestion;
                }
                else if (Element.Type == StoryItemType.Scene)
                {
                    return ((SceneModel)Element).Remarks;
                }
            }

            return "Select an element by clicking show Problems/Scenes and dragging it here";
        }
    }

	[JsonIgnore]
    public Symbol ElementIcon
    {
        get
        {
            if (Element != null)
            {
                if (Element.Type == StoryItemType.Problem)
                {
                    return Symbol.Help;
                }
                else if (Element.Type == StoryItemType.Scene)
                {
                    return Symbol.World;
                }
            }

            return Symbol.Cancel;
        }
    }
    #endregion
}