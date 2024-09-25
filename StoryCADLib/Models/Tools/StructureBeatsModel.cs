using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryCAD.Models.Tools;

/// <summary>
///	Model for how the Structure Tab works in problem.
/// </summary>
public class StructureBeatsModel
{

	public string title;
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
	string Description
	{
		get => description;
		set => description = value;
	}


	private string guid;
	/// <summary>
	/// GUID of problem/scene beat links to.
	/// </summary>
	string Guid
	{
		get => guid;
		set => guid = value;
	}
}