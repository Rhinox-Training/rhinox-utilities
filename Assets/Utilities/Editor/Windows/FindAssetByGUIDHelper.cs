using System.Linq;
using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public static class FindAssetByGUIDHelper
    {
        [MenuItem(WindowHelper.FindToolsPrefix + "Find Asset By GUID", false, -100)]
        public static void DoFindByGuidMenu()
        {
            EditorInputDialog.Create("GUID", "Find an asset from it's GUID.")
                .TextField("Guid:", out var guid)
                .OnAccept(() => { FindAssetByGuid(guid); })
                .Show();
        }

        static void FindAssetByGuid(string searchGuid)
        {
            string path = AssetDatabase.GUIDToAssetPath(searchGuid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Asset with GUID {searchGuid} not found...");
                return;
            }
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj == null)
            {
                Debug.LogError($"Asset with GUID {searchGuid} was not UnityEngine.Object, exiting...");
                return;
            }

            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
        
        // =============================================================================================================
        // Asset helpers
        private const int COPY_PRIORITY = (int)19.5; // NOTE: 19 is too small, but 20 is way too big, blame Unity
        
        [MenuItem("Assets/Copy Asset GUID", false, COPY_PRIORITY)]
        private static void CopyAssetGUID()
        {
            var element = Selection.objects.FirstOrDefault();
            var assetPath = AssetDatabase.GetAssetPath(element);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            GUIUtility.systemCopyBuffer = guid;
        }
        
        [MenuItem("Assets/Copy Asset GUID", true, COPY_PRIORITY)]
        private static bool CopyAssetGUIDValidate()
        {
            return Selection.objects != null && Selection.objects.Length == 1;
        }
    }
}