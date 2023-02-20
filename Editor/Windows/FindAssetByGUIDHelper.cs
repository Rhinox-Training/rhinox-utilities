using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public static class FindAssetByGUIDHelper
    {
        [MenuItem(WindowHelper.WindowPrefix + "Find Asset By GUID", false, 2500)]
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
    }
}