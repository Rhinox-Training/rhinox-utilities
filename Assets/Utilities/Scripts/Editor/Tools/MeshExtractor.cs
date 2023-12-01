using System;
using System.IO;
using Rhinox.Perceptor;
using Rhinox.Utilities;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Editor
{
    public static class MeshExtractor
    {
        private static string _progressTitle = "Extracting Meshes";
        private static string _targetExtension = ".asset";
        
        [MenuItem("Assets/Extract Meshes", validate = true)]
        private static bool ExtractMeshesMenuItemValidate()
        {
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                var assetPath = AssetDatabase.GetAssetPath(Selection.objects[i]);
                if (!assetPath.EndsWith(".obj") && !assetPath.EndsWith(".fbx"))
                    return false;
            }

            return true;
        }

        [MenuItem("Assets/Extract Meshes")]
        private static void ExtractMeshesMenuItem()
        {
            EditorUtility.DisplayProgressBar(_progressTitle, "", 0);
            try
            {
                for (int i = 0; i < Selection.objects.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(_progressTitle, Selection.objects[i].name,
                        (float) i / (Selection.objects.Length - 1));
                    ExtractMeshes(Selection.objects[i]);
                }
            }
            catch (Exception e)
            {
                PLog.Error<UtilityLogger>(e.ToString());
            }

            EditorUtility.ClearProgressBar();
        }

        private static void ExtractMeshes(Object selectedObject)
        {
            //Create Folder Hierarchy
            string selectedObjectPath = AssetDatabase.GetAssetPath(selectedObject);
            string origExtension = Path.GetExtension(selectedObjectPath);
            string parentfolderPath =
                selectedObjectPath.Substring(0, selectedObjectPath.Length - (selectedObject.name.Length + 5));
            string objectFolderName = selectedObject.name;
            string objectFolderPath = parentfolderPath + "/" + objectFolderName;
            string meshFolderName = "Meshes";
            string meshFolderPath = objectFolderPath + "/" + meshFolderName;

            if (!AssetDatabase.IsValidFolder(objectFolderPath))
            {
                AssetDatabase.CreateFolder(parentfolderPath, objectFolderName);

                if (!AssetDatabase.IsValidFolder(meshFolderPath))
                {
                    AssetDatabase.CreateFolder(objectFolderPath, meshFolderName);
                }
            }

            //Create Meshes
            Object[] objects = AssetDatabase.LoadAllAssetsAtPath(selectedObjectPath);

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] is Mesh)
                {
                    EditorUtility.DisplayProgressBar(_progressTitle, selectedObject.name + " : " + objects[i].name,
                        (float) i / (objects.Length - 1));

                    Mesh mesh = Object.Instantiate(objects[i]) as Mesh;

                    AssetDatabase.CreateAsset(mesh,
                        meshFolderPath + "/" + SanitizeFileName(objects[i].name) + _targetExtension);
                }
            }

            //Cleanup

            string name = SanitizeFileName(selectedObject.name);
            AssetDatabase.MoveAsset(selectedObjectPath, objectFolderPath + "/" + name + origExtension);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string SanitizeFileName(string fileName)
        {
            string name = fileName;
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}