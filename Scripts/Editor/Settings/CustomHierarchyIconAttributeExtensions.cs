using System;
using Rhinox.GUIUtils.Editor;
using Rhinox.Utilities.Attributes;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public static class CustomHierarchyIconAttributeExtensions
    {
        public static Texture2D FindIcon(this CustomHierarchyIconAttribute attribute)
        {
            switch (attribute.Set)
            {
                case IconSet.Internal:
                    return UnityIcon.InternalIcon(attribute.Name);
                case IconSet.Asset:
                    return UnityIcon.AssetIcon(attribute.Name);
                case IconSet.Odin:
#if ODIN_INSPECTOR
                    return UnityIcon.OdinIcon(attribute.Name);
#else
                    return null;
#endif
                default:
                    return null;
            }
        }
    }
}