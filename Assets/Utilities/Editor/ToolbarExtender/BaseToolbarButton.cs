using System;
using System.Collections.Generic;
using System.Linq;
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
using UnityEditor.UIElements;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    [HideReferenceObjectPicker, Serializable, DrawAsReference]
    public abstract class BaseToolbarButton
    {
        protected const int ToolbarHeight = 24;
        
        [ShowInInspector, DisplayAsString, HideLabel, PropertyOrder(-100)]
        private string TypeName => GetType().Name.SplitCamelCase();

        protected virtual GUIStyle Style => CustomGUIStyles.Button;
        
        protected abstract string Label { get; }
        protected virtual string Tooltip => String.Empty;
        
        public virtual void Draw(Rect rect)
        {
            var content = GUIContentHelper.TempContent(Label, Tooltip);
            if (GUI.Button(rect, content, Style))
                Execute(rect);
        }

        public virtual float GetWidth() => Style.CalcMaxWidth(Label);

        protected abstract void Execute(Rect rect);
    }

    [Serializable]
    public abstract class BaseToolbarIconButton : BaseToolbarButton
    {
        protected override string Label => string.Empty;
        
        protected abstract Texture Icon { get; }
        
        public override void Draw(Rect rect)
        {
            if (Icon == null) return;

            if (CustomEditorGUI.IconButton(rect, Icon, Tooltip))
                Execute(rect);
        }

        public override float GetWidth() => ToolbarHeight;
    }
    
    public abstract class BaseToolbarDropdown<T> : BaseToolbarButton
    {
        protected abstract T[] _options { get; }

#if ODIN_INSPECTOR
        protected bool _supportMultiselect;
#endif

        protected override void Execute(Rect rect)
        {
#if ODIN_INSPECTOR
            OdinSelector<T> selector = new GenericSelector<T>(string.Empty, _options, _supportMultiselect, GetName);
            selector.SelectionConfirmed += SelectionMade;
            ConfigureSelector(selector);
            selector.ShowInPopup();
#else
            SimplePicker<T> picker = new SimplePicker<T>(_options, GetName);
            picker.OptionSelected += SelectionMade;
            picker.Show(rect); // ShowAsContext
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

        protected virtual void SelectionMade(IEnumerable<T> selection)
        {
            SelectionMade(selection.FirstOrDefault());
        }
        
        protected abstract void SelectionMade(T selection);

    }
    
    public abstract class BaseToolbarIconDropdown<T> : BaseToolbarDropdown<T>
    {
        protected override string Label => string.Empty;

        private HoverTexture _icon;
        
        protected abstract Texture2D Icon { get; }
        
        public override void Draw(Rect rect)
        {
            if (_icon == null)
            {
                var icon = Icon;
                if (icon == null) return;
                _icon = new HoverTexture(icon);
            }
            
            if (CustomEditorGUI.IconButton(rect, _icon, Tooltip))
                Execute(rect);
        }

        public override float GetWidth() => ToolbarHeight;
    }
}