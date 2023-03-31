using System.Collections;
using System.Collections.Generic;
using Rhinox.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities
{
    public class UtilitiesEditorSettings : CustomProjectSettings<UtilitiesEditorSettings>
    {
        public override string Name => "Editor Utilities";
        
        [Title("Scene/Hierarchy Helper")]
        public bool ShowSelectionInfoInSceneOverlay = false;
        public bool ShowSelectionInfoInHierarchy = false;
        public bool OverrideFocusBehaviour = false;

        [Title("Custom Inspectors")]
        public bool OverrideTransformInspector = true;
        public bool OverrideMeshFilterInspector = true;
    }
}