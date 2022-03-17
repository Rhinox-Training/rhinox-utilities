using UnityEditor;

namespace Rhinox.Utilities.Editor
{
    public static class UtilityWindows
    {
        private const string __prefix = "Tools/Rhinox";
        
        [MenuItem(__prefix + "/Take a Screenshot", false, 100)]
        private static void ScreenshotWindow() => Screenshot.ShowWindow();

        [MenuItem(__prefix + "/GameObject Replacer")]
        private static void Init()
        {
            GameObjectReplacer.ShowWindow();
        }
        
        [MenuItem(__prefix + "/Find Asset By GUID", false, 2500)]
        public static void DoFindByGuidMenu()
        {
            FindAssetByGUIDHelper.DoFindByGuidMenu();
        }
    }
}
