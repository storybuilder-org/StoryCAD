using CommunityToolkit.Mvvm.ComponentModel;

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
		set => title = value;
	}

	private string description;
	/// <summary>
	/// Description of beat
	/// </summary>
	public string Description
	{
		get => description;
		set => description = value;
	}


	private string guid;
	/// <summary>
	/// GUID of problem/scene beat links to.
	/// </summary>
	public string Guid
	{
		get => guid;
		set => guid = value;
	}

	/// <summary>
	/// Link to element
	/// </summary>
	private StoryElement? Element
	{
		get
		{
			if (guid == null)
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
				return ElementName;
			}

			return "Debug no name set";
		}
	}
}