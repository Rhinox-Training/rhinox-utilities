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

        public override void Draw()
        {
            GUILayout.Space(Amount);
        }

        protected override void Execute() { }
    }
}