using System;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEngine;
using UnityEditor;

namespace Rhinox.Utilities.Editor
{
    public static class WindowHelper
    {
        // Note: Ensure Slash at the end
        public const string WindowPrefix =  "Window/";
        public const string ToolsPrefix =  "Tools/";
        public const string FindToolsPrefix =  ToolsPrefix + "Find.../";
        
        public static bool GetOrCreate<T>(out T window, string name, bool utility = false, bool centerOnScreen = false, Action<T> initialization = null) where T : EditorWindow
        {
            window = Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
            if (window != null)
            {
                window.Focus();
                return false;
            }

            window = EditorWindow.GetWindow<T>(utility, name);
            initialization?.Invoke(window);
            if (centerOnScreen)
                window.position = CustomEditorGUI.GetEditorWindowRect().AlignCenter(window.position.width, window.position.height);

            window.Show();

            return true;
        }
    }
}