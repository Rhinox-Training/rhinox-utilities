using System;
using System.Collections.Generic;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    [Serializable]
    public class HierarchyIconSettingsEntry
    {
        public enum IconSetting
        {
            Default,
            None,
            Override
        }

        [PreviewField(ObjectFieldAlignment.Left, Height = 64), ShowInInspector, HorizontalGroup("Main"),
         HorizontalGroup("Main/Left", width: 74)]
        public Texture PreviewIcon
        {
            get
            {
                switch (Setting)
                {
                    case IconSetting.Default:
                        return Icon;
                    case IconSetting.None:
                        return null;
                    case IconSetting.Override:
                        return OverrideIcon;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private bool _isOverridden => Setting == IconSetting.Override;

        [AssignableTypeFilter(typeof(Component)),  HorizontalGroup("Main"), VerticalGroup("Main/Right", order: 1)]
        public SerializableType Type;
        
        [HideInInspector]
        public Texture Icon;

        [HideLabel, HorizontalGroup("Main/Right/Override")]
        public IconSetting Setting;
        
        [IconPicker, EnableIf(nameof(_isOverridden)), HideLabel, HorizontalGroup("Main/Right/Override")]
        public Texture OverrideIcon;
        
        public bool IsValid
        {
            get { return Type != null && Type.Type.InheritsFrom(typeof(Component)) && Icon != null; }
        }

        public HierarchyIconSettingsEntry(Type componentType, Texture icon)
        {
            Type = new SerializableType(componentType);
            Icon = icon;
            Setting = IconSetting.Default;
            OverrideIcon = icon;
        }
    }
}