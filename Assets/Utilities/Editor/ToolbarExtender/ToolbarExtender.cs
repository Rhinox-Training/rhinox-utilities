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
        static int _toolCount;
		static GUIStyle _commandStyle = null;
		
#if UNITY_2019_3_OR_NEWER
		public const float space = 8;
#else
		public const float space = 10;
#endif
		public const float largeSpace = 20;
		public const float buttonWidth = 32;
		public const float dropdownWidth = 80;
#if UNITY_2019_1_OR_NEWER
		public const float playPauseStopWidth = 140;
#else
		public const float playPauseStopWidth = 100;
#endif
	    
	    [InitializeOnLoadMethod]
	    private static void SetupExtender()
	    {
		    _toolCount = ToolbarExtenderUtility.GetToolCount();
	
		    ToolbarCallback.OnToolbarGUI -= OnGUI;
		    ToolbarCallback.OnToolbarGUI += OnGUI;
	    }

		static void OnGUI()
		{
			if (!ToolbarExtenderConfig.Instance.IsActive)
				return;
			
			// Create two containers, left and right
			// Screen is whole toolbar

			if (_commandStyle == null)
			{
				_commandStyle = new GUIStyle("CommandLeft");
			}

			var screenWidth = EditorGUIUtility.currentViewWidth;

			// Following calculations match code reflected from Toolbar.OldOnGUI()
			float playButtonsPosition = Mathf.RoundToInt ((screenWidth - playPauseStopWidth) / 2);

			Rect leftRect = new Rect(0, 0, screenWidth, Screen.height);
			leftRect.xMin += space; // Spacing left
			leftRect.xMin += buttonWidth * _toolCount; // Tool buttons
#if UNITY_2019_3_OR_NEWER
			leftRect.xMin += space; // Spacing between tools and pivot
#else
			leftRect.xMin += largeSpace; // Spacing between tools and pivot
#endif
			leftRect.xMin += 64 * 2; // Pivot buttons
			leftRect.xMax = playButtonsPosition;

			Rect rightRect = new Rect(0, 0, screenWidth, Screen.height);
			rightRect.xMin = playButtonsPosition;
			rightRect.xMin += _commandStyle.fixedWidth * 3; // Play buttons
			rightRect.xMax = screenWidth;
			rightRect.xMax -= space; // Spacing right
			rightRect.xMax -= dropdownWidth; // Layout
			rightRect.xMax -= space; // Spacing between layout and layers
			rightRect.xMax -= dropdownWidth; // Layers
#if UNITY_2019_3_OR_NEWER
			rightRect.xMax -= space; // Spacing between layers and account
#else
			rightRect.xMax -= largeSpace; // Spacing between layers and account
#endif
			rightRect.xMax -= dropdownWidth; // Account
			rightRect.xMax -= space; // Spacing between account and cloud
			rightRect.xMax -= buttonWidth; // Cloud
			rightRect.xMax -= space; // Spacing between cloud and collab
			rightRect.xMax -= 78; // Colab

			// Add spacing around existing controls
			leftRect.xMin += space;
			leftRect.xMax -= space;
			rightRect.xMin += space;
			rightRect.xMax -= space;

			// Add top and bottom margins
#if UNITY_2019_3_OR_NEWER
			leftRect.y = 4;
			leftRect.height = 22;
			rightRect.y = 4;
			rightRect.height = 22;
#else
			leftRect.y = 5;
			leftRect.height = 24;
			rightRect.y = 5;
			rightRect.height = 24;
#endif
			

			DrawButtons(leftRect, ToolbarExtenderConfig.Instance.LeftButtons, ToolbarExtenderConfig.Instance.LeftPosition);
			DrawButtons(rightRect, ToolbarExtenderConfig.Instance.RightButtons, ToolbarExtenderConfig.Instance.RightPosition);
		}


		private static void DrawButtons(Rect rect, ICollection<BaseToolbarButton> buttons, ToolbarExtenderConfig.ButtonPositions position)
		{
			if (!ToolbarExtenderConfig.Instance.IsActive)
				return;
			
			if (rect.width > 0 && !buttons.IsNullOrEmpty())
			{
				var width = buttons.Sum(x => x.GetWidth());

				if (position == ToolbarExtenderConfig.ButtonPositions.Right)
					rect = rect.AlignRight(width);
				else if (position == ToolbarExtenderConfig.ButtonPositions.Center)
					rect = rect.AlignCenter(width);

				foreach (var handler in buttons)
				{
					var buttonRect = rect.AlignLeft(handler.GetWidth());
					rect.x += buttonRect.width;

					handler.Draw(buttonRect);
				}

				// EditorGUI.DrawRect(leftRect, Color.cyan);
			}
		}
    }
}