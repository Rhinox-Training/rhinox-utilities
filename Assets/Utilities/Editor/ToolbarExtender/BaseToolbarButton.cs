using System;
using System.Collections.Generic;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Attributes;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
#endif
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Odin.Editor
{
    [HideReferenceObjectPicker, Serializable, DrawAsReference]
    public abstract class BaseToolbarButton
    {
        protected const int ToolbarHeight = 24;
        
        [ShowInInspector, DisplayAsString, HideLabel, PropertyOrder(-100)]
        private string TypeName => GetType().Name.SplitCamelCase();

        protected virtual GUIStyle Style => CustomGUIStyles.Button;
        
        protected virtual GUILayoutOption[] LayoutOptions => new [] { GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true) };
		
        protected abstract string Label { get; }
        protected virtual string Tooltip => String.Empty;
        
        public virtual void Draw()
        {
            var content = GUIContentHelper.TempContent(Label, Tooltip);
            if (GUILayout.Button(content, Style, LayoutOptions))
                Execute();
        }

        protected abstract void Execute();
    }

    [Serializable]
    public abstract class BaseToolbarIconButton : BaseToolbarButton
    {
        protected override string Label => string.Empty;
        
        protected abstract Texture Icon { get; }
        
        public override void Draw()
        {
            if (Icon == null) return;

            if (CustomEditorGUI.IconButton(Icon, Style, ToolbarHeight, ToolbarHeight, Tooltip))
                Execute();
        }
    }
    
    public abstract class BaseToolbarDropdown<T> : BaseToolbarButton
    {
        protected int _selected;

        protected abstract T[] _options { get; }

#if ODIN_INSPECTOR
        protected bool _supportMultiselect;
#endif

        protected override void Execute()
        {
#if ODIN_INSPECTOR
            OdinSelector<T> selector = new GenericSelector<T>(string.Empty, _options, _supportMultiselect, GetName);
            selector.SelectionConfirmed += SelectionMade;
            ConfigureSelector(selector);
            selector.ShowInPopup();
#else
            var menu = new GenericMenu();
            foreach (var option in _options)
            {
                menu.AddItem(new GUIContent(option.ToString()), false, () =>
                {
                    SelectionMade(new[] {option});
                });
            }
            menu.ShowAsContext();
#endif
        }
        
#if ODIN_INSPECTOR
        protected virtual void ConfigureSelector(OdinSelector<T> selector)
        {
            if (!_supportMultiselect)
                selector.EnableSingleClickToSelect();
        }
#endif

        protected virtual string GetName(T data)
        {
            return data?.ToString() ?? "<null>";
        }

        protected abstract void SelectionMade(IEnumerable<T> selection);
    }
    
    public abstract class BaseToolbarIconDropdown<T> : BaseToolbarDropdown<T>
    {
        protected override string Label => string.Empty;
        
        protected abstract Texture Icon { get; }
        
        public override void Draw()
        {
            if (Icon == null) return;
            
            if (CustomEditorGUI.IconButton(Icon, Style, ToolbarHeight, ToolbarHeight, Tooltip))
                Execute();
        }
    }
}