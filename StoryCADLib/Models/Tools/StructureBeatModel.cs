using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.Models.Tools;

/// <summary>
///	Model for how the Structure Tab works in problem.
/// </summary>
public class StructureBeatModel : ObservableObject
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
			OnPropertyChanged(nameof(ElementName));
			OnPropertyChanged(nameof(ElementIcon));
			OnPropertyChanged(nameof(ElementDescription));
			SetProperty(ref guid, value);
		}
	}

	/// <summary>
	/// Link to element
	/// </summary>
	private StoryElement? Element
	{
		get
		{
			if (!string.IsNullOrEmpty(guid))
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

			return "No element Selected";
		}
	}

	/// <summary>
	/// NElement Description
	/// </summary>
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
}