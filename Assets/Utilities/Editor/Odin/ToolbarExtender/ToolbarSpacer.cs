using UnityEngine;

namespace Rhinox.Utilities.Odin.Editor
{
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