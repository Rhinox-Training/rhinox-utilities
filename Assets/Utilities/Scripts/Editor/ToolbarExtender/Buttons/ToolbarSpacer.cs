using System;
using Rhinox.GUIUtils.Attributes;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    [Serializable, DrawAsReference]
    public class ToolbarSpacer : BaseToolbarButton
    {
        public int Amount;
		
        protected override string Label => string.Empty;

        public override float GetWidth() => Amount;

        public override void Draw(Rect rect)
        { }

        protected override void Execute(Rect rect) { }
    }
}