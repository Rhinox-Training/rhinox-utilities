using UnityEngine;
using UnityEditor;

namespace Rhinox.Utilities.Odin.Editor
{
    public static class UtilityWindows
    {
        private const string __prefix = "Tools/Rhinox";

        [MenuItem(__prefix + "/Texture Packer")]
        private static void OpenTexturePacker()
        {
            TexturePackerWindow.Open();
        }

        [MenuItem(__prefix + "/Find Dependencies", false, 2500)]
        private static void OpenFindDependenciesWindow()
        {
            DependenciesWindow.ShowWindow();
        }

        [MenuItem(__prefix + "/Advanced Scene Search", false, 2500)]
        public static void OpenWindow()
        {
            AdvancedSceneSearchWindow.OpenWindow();
        }

        [MenuItem(__prefix + "/Clean Up Missing Components", false, 3500)]
        private static void CleanUpComponents()
        {
            MissingScriptsWindow.ShowWindow();
        }
    }
}