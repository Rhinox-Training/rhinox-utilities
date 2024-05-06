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
using UnityEditor.PackageManager.UI;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    [HideReferenceObjectPicker, Serializable, DrawAsReference]
    public abstract class BaseToolbarButton
    {
        protected const int ToolbarHeight = 24;
        
        [ShowInInspector, DisplayAsString, HideLabel, PropertyOrder(-100)]
        private string TypeName => GetType().Name.SplitCamelCase();

        protected virtual GUIStyle Style => Label.IsNullOrEmpty() ? CustomGUIStyles.CommandButton : CustomGUIStyles.Button;

        protected virtual GUIContent Content => GUIContentHelper.TempContent(Label, Icon, tooltip: Tooltip);

        protected abstract Texture Icon { get; }
        protected abstract string Label { get; }
        protected virtual string Tooltip => String.Empty;
        
        public virtual void Draw(Rect rect)
        {
            if (GUI.Button(rect, Content, Style))
                Execute();
        }

        public virtual float GetWidth() => Style.CalcWidth(Content, ToolbarHeight);

        protected abstract void Execute();
    }
    
    public abstract class BaseToolbarDropdown<T> : BaseToolbarButton
    {
        protected abstract T[] _options { get; }

#if ODIN_INSPECTOR
        protected bool _supportMultiselect;
#endif

        protected override void Execute()
        {
            SimplePicker<T> picker = new SimplePicker<T>(_options, GetName);
            picker.OptionSelected += SelectionMade;
            picker.Show(new Rect(0, 25, 0, 0)); // ShowAsContext
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
}