using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public static class ToolbarExtender
    {
	    [InitializeOnLoadMethod]
	    private static void SetupExtender()
	    {
		    ToolbarCallback.OnToolbarGUIRight -= OnGUIRight;
		    ToolbarCallback.OnToolbarGUIRight += OnGUIRight;
		    
		    ToolbarCallback.OnToolbarGUILeft -= OnGUILeft;
		    ToolbarCallback.OnToolbarGUILeft += OnGUILeft;
	    }

	    static void OnGUILeft(Rect rect)
	    {
		    if (!ToolbarExtenderConfig.Instance.IsActive)
			    return;
		    
		    DrawButtons(rect, ToolbarExtenderConfig.Instance.LeftButtons, ToolbarExtenderConfig.Instance.LeftPosition);
	    }
	    
	    static void OnGUIRight(Rect rect)
	    {
		    if (!ToolbarExtenderConfig.Instance.IsActive)
			    return;
		    
		    DrawButtons(rect, ToolbarExtenderConfig.Instance.RightButtons, ToolbarExtenderConfig.Instance.RightPosition);
	    }

		private static void DrawButtons(Rect rect, IList<BaseToolbarButton> buttons, ToolbarExtenderConfig.ButtonPositions position)
		{
			if (!ToolbarExtenderConfig.Instance.IsActive)
				return;
			
			if (rect.width > 0 && !buttons.IsNullOrEmpty())
			{
				var widths = buttons.Select(x => x.GetWidth()).ToArray();
				var totalWidth = widths.Sum();

				if (position == ToolbarExtenderConfig.ButtonPositions.Right)
					rect = rect.AlignRight(totalWidth);
				else if (position == ToolbarExtenderConfig.ButtonPositions.Center)
					rect = rect.AlignCenter(totalWidth);

				for (var i = 0; i < buttons.Count; i++)
				{
					var handler = buttons[i];
					var width = widths[i];
					var buttonRect = rect.AlignLeft(width);
					rect.x += buttonRect.width;

					GUILayout.BeginArea(buttonRect);
					handler.Draw(new Rect(0, 0, width, rect.height));
					GUILayout.EndArea();

					EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
				}

				// EditorGUI.DrawRect(leftRect, Color.cyan);
			}
		}
    }
}