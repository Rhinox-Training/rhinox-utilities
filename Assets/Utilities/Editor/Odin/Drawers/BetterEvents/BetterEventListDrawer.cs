using System.Collections.Generic;
using Rhinox.GUIUtils.Odin;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Rhinox.Utilities.Odin.Editor
{
    public class BetterEventListDrawer : OdinValueDrawer<BetterEvent>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var e = (BetterEvent) this.ValueEntry.WeakSmartValue;

            if (e.Events == null)
            {
                e.Events = new List<BetterEventEntry>();
                this.ValueEntry.WeakSmartValue = e;
            }

            this.Property.Children["Events"].Draw(label);
        }
    }
}