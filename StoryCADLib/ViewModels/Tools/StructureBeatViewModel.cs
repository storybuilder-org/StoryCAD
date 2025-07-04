﻿using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.ViewModels.Tools;

/// <summary>
///	Model for how the StructureModelTitle Tab works in problem.
/// </summary>
public class StructureBeatViewModel : ObservableObject
{
	public Windowing Windowing;
	public ProblemViewModel ProblemViewModel;

	#region Constructor
	public StructureBeatViewModel()
	{
		Windowing = Ioc.Default.GetRequiredService<Windowing>();
		ProblemViewModel = Ioc.Default.GetRequiredService<ProblemViewModel>();
		PropertyChanged += ProblemViewModel.OnPropertyChanged;
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
	internal StoryElement Element
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
                if (Element.ElementType == StoryItemType.Problem)
                {
                    return ((ProblemModel)Element).StoryQuestion;
                }
                else if (Element.ElementType == StoryItemType.Scene)
                {
                    return ((SceneModel)Element).Remarks;
                }
            }

            return "Assign an element to this beat by clicking the edit button.";
        }
    }

	[JsonIgnore]
    public Symbol ElementIcon
    {
        get
        {
            if (Element != null)
            {
                if (Element.ElementType == StoryItemType.Problem)
                {
                    return Symbol.Help;
                }
                else if (Element.ElementType == StoryItemType.Scene)
                {
                    return Symbol.World;
                }
            }

            return Symbol.Cancel;
        }
    }
    #endregion


}