﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.Models.Tools;

/// <summary>
///	Model for how the Structure Tab works in problem.
/// </summary>
public class StructureBeatsModel : ObservableObject
{

	private string title;
	/// <summary>
	/// Title of beat
	/// </summary>
	public string Title
	{
		get => title;
		set => SetProperty(ref title, value);
	}

	private string description;
	/// <summary>
	/// Description of beat
	/// </summary>
	public string Description
	{
		get => description;
		set => SetProperty(ref description, value);
	}


	private string guid;
	/// <summary>
	/// GUID of problem/scene beat links to.
	/// </summary>
	public string Guid
	{
		get => guid;
		set
		{
			SetProperty(ref guid, value);
			OnPropertyChanged(nameof(ElementName));
		}
	}

	/// <summary>
	/// Link to element
	/// </summary>
	private StoryElement? Element
	{
		get
		{
			if (guid != null)
			{
				return ShellViewModel.GetModel().StoryElements.StoryElementGuids[new Guid(Guid)];
			}
			return null;
		}
	}

	/// <summary>
	/// Name of the element
	/// </summary>
	public string ElementName
	{
		get
		{
			if (Element != null)
			{
				return Element.Name;
			}

			return "Debug no name set";
		}
	}
}