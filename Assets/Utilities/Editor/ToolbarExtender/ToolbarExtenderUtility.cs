using System;
using System.Reflection;

namespace Rhinox.Utilities.Odin.Editor
{
	public static class ToolbarExtenderUtility
	{
		private static Type _toolbarType;

		private static Type ToolbarType
		{
			get
			{
				if (_toolbarType != null) return _toolbarType;
				_toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
				return _toolbarType;
			}
		}
	    
        public static int GetToolCount()
        {
#if UNITY_2019_1_OR_NEWER
            string fieldName = "k_ToolCount";
#else
			string fieldName = "s_ShownToolIcons";
#endif
            FieldInfo toolIcons = ToolbarType.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            
#if UNITY_2019_3_OR_NEWER
            return toolIcons != null ? ((int) toolIcons.GetValue(null)) : 8;
#elif UNITY_2019_1_OR_NEWER
			return toolIcons != null ? ((int) toolIcons.GetValue(null)) : 7;
#elif UNITY_2018_1_OR_NEWER
			return toolIcons != null ? ((Array) toolIcons.GetValue(null)).Length : 6;
#else
			return toolIcons != null ? ((Array) toolIcons.GetValue(null)).Length : 5;
#endif
        }
    }
}