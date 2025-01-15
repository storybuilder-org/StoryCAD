using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryCAD.Models;

/// <summary>
/// Node that can be persisted to JSON
/// </summary>
public class PersistableNode
{
	/// <summary>
	/// UUID of node
	/// </summary>
	public Guid Uuid { get; set; }
	
	/// <summary>
	/// UUID of parent node
	/// </summary>
	public Guid? ParentUuid { get; set; }
}