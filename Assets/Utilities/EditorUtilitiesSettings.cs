using System.Collections;
using System.Collections.Generic;
using Rhinox.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class EditorUtilitiesSettings : CustomProjectSettings<EditorUtilitiesSettings>
    {
        public override string Name => "Editor Utilities";
        
        [Title("Scene/Hierarchy Helper")]
        [ToggleLeft] public bool ShowSelectionInfoInSceneOverlay = false;
        [ToggleLeft] public bool ShowSelectionInfoInHierarchy = false;
        [ToggleLeft] public bool OverrideFocusBehaviour = false;

        [Indent, ShowIf(nameof(OverrideFocusBehaviour))]
        public float DefaultBoundsSize = 1;

        [Title("Custom Inspectors")]
        [ToggleLeft] public bool OverrideTransformInspector = true;
        [ToggleLeft] public bool OverrideMeshFilterInspector = true;
    }
}