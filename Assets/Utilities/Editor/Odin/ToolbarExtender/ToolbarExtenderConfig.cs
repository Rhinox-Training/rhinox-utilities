using System.Collections.Generic;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Odin.Editor
{
    public class ToolbarExtenderConfig : CustomProjectSettings<ToolbarExtenderConfig>
    {
        public enum ButtonPositions
        {
            Left,
            Center,
            Right
        }

        [SerializeField]
        private bool _isActive;

        public bool IsActive => _isActive && !(LeftButtons.IsNullOrEmpty() && RightButtons.IsNullOrEmpty());
        
        [EnumToggleButtons]
        public ButtonPositions LeftPosition = ButtonPositions.Right;
        
        [ListDrawerSettings(DraggableItems = true)]
        public List<BaseToolbarButton> LeftButtons = new List<BaseToolbarButton>();
        
     
        [EnumToggleButtons, Space(10)]
        public ButtonPositions RightPosition = ButtonPositions.Left;
        
        [ListDrawerSettings(DraggableItems = true)]
        public List<BaseToolbarButton> RightButtons = new List<BaseToolbarButton>();


        [SettingsProvider]
        public static SettingsProvider CreatePostBuildSettingsProvider()
        {
            return Instance.CreateSettingsProvider();
        }

        public override void OnChanged()
        {
        }
    }
}