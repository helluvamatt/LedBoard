using System;

namespace LedBoard.Models
{
	[AttributeUsage(AttributeTargets.Property)]
	public class EditorForAttribute : Attribute
	{
		public EditorForAttribute(string label, Editors editor)
		{
			Label = label;
			Editor = editor;
		}

		public string Label { get; }

		public Editors Editor { get; }

		public object Parameter { get; set; }
	}

	public enum Editors { Text, Color, LedFont, TimeSpan, TimeSpanAdvanced, FileResource, Dropdown, Alignment }
}
