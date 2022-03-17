using Rhinox.GUIUtils.Editor;
using UnityEditor;
using UnityEngine;
 
public class FindAssetByGUIDHelper : EditorWindow
{
    public static void DoFindByGuidMenu()
    {
        EditorInputDialog.Create("GUID", "Find an asset from it's GUID.")
            .TextField("Guid:", out var guid)
            .OnAccept(() =>
            {
                FindAssetByGuid(guid);
            });
    }
        
    static void FindAssetByGuid(string searchGuid)
    {
        string path = AssetDatabase.GUIDToAssetPath(searchGuid);
        if (string.IsNullOrEmpty(path)) return;
        var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (obj == null) return;
            
        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(obj);
    }
}