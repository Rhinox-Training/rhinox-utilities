using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Odin.Editor
{
    [HideReferenceObjectPicker]
    public abstract class BaseToolbarButton
    {
        protected const int ToolbarHeight = 24;
        
        [ShowInInspector, DisplayAsString, HideLabel, PropertyOrder(-100)]
        private string TypeName => GetType().Name.SplitPascalCase();

        protected virtual GUIStyle Style => SirenixGUIStyles.Button;
        
        protected virtual GUILayoutOptions.GUILayoutOptionsInstance LayoutOptions => GUILayoutOptions.ExpandWidth(false).ExpandHeight();
		
        protected abstract string Label { get; }
        protected virtual string Tooltip => String.Empty;
        
        public virtual void Draw()
        {
            var content = GUIHelper.TempContent(Label, Tooltip);
            if (GUILayout.Button(content, Style, LayoutOptions))
                Execute();
        }

        protected abstract void Execute();
    }

    public abstract class BaseToolbarIconButton : BaseToolbarButton
    {
        protected override string Label => string.Empty;
        
        protected abstract EditorIcon Icon { get; }
        
        public override void Draw()
        {
            if (Icon == null) return;

            var rect = GUILayoutUtility.GetRect(Icon.HighlightedGUIContent, Style, LayoutOptions.Width(ToolbarHeight).Height(ToolbarHeight));
            if (SirenixEditorGUI.IconButton(rect, Icon, Style, Tooltip))
                Execute();
        }
    }
    
    public abstract class BaseToolbarDropdown<T> : BaseToolbarButton
    {
        protected int _selected;

        protected abstract T[] _options { get; }

        protected bool _supportMultiselect;

        protected override void Execute()
        {
            OdinSelector<T> selector = new GenericSelector<T>(string.Empty, _options, _supportMultiselect, GetName);
            selector.SelectionConfirmed += SelectionMade;
            ConfigureSelector(selector);
            selector.ShowInPopup();
        }

        protected virtual void ConfigureSelector(OdinSelector<T> selector)
        {
            if (!_supportMultiselect)
                selector.EnableSingleClickToSelect();
        }

        protected virtual string GetName(T data)
        {
            return data?.ToString() ?? "<null>";
        }

        protected abstract void SelectionMade(IEnumerable<T> selection);
    }
    
    public abstract class BaseToolbarIconDropdown<T> : BaseToolbarDropdown<T>
    {
        protected override string Label => string.Empty;
        
        protected abstract EditorIcon Icon { get; }
        
        public override void Draw()
        {
            if (Icon == null) return;
            
            var rect = GUILayoutUtility.GetRect(Icon.HighlightedGUIContent, Style, LayoutOptions.Width(ToolbarHeight).Height(ToolbarHeight));
            if (SirenixEditorGUI.IconButton(rect, Icon, Style, Tooltip))
                Execute();
        }
    }
}