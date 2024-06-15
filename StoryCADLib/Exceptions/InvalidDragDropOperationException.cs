using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryCAD.Exceptions
{
    public class InvalidDragDropOperationException(string message) : Exception(message);
}
