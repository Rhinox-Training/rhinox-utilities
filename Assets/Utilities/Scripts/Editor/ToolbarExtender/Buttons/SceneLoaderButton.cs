using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Utilities.Editor;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rhinox.Magnus.Editor
{
    public class SceneLoaderButton : BaseToolbarDropdown<SceneReference>
    {
        public enum SceneLoaderState
        {
            OnlyBuildScenes,
            AllScenesInFolders,
            AllScenes,
            Custom
        }

        [HideLabel, EnumToggleButtons] public SceneLoaderState State;

        [ShowIf(nameof(State), SceneLoaderState.Custom)]
        public SceneReference[] Scenes;

        [ShowIf(nameof(State), SceneLoaderState.AllScenesInFolders), FolderPath]
        public string[] SceneFolders = new[] {"Assets"};

        protected override SceneReference[] _options => _sceneOptions;

        private SceneReference[] _sceneOptions;
        protected override string Label => null;
        protected override Texture Icon => UnityIcon.InternalIcon("d_SceneAsset Icon");
        
        protected SceneReference[] FetchSceneData()
        {
            switch (State)
            {
                case SceneLoaderState.OnlyBuildScenes:
                    return EditorBuildSettings.scenes
                        .Select(x => new SceneReference(x.path))
                        .ToArray();
                
                case SceneLoaderState.AllScenesInFolders:
                    return AssetDatabase.FindAssets("t:Scene", SceneFolders)
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<SceneAsset>)
                        .Select(x => new SceneReference(x))
                        .ToArray();
                
                case SceneLoaderState.AllScenes:
                    return AssetDatabase.FindAssets("t:Scene")
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<SceneAsset>)
                        .Select(x => new SceneReference(x))
                        .ToArray();

                case SceneLoaderState.Custom:
                    return Scenes;
                
                default:
                    return Array.Empty<SceneReference>();
            }
        }
        
        protected override void Execute()
        {
            _sceneOptions = FetchSceneData();
            
            base.Execute();
        }

        protected override string GetName(SceneReference data)
        {
            // TODO check for identical scene names with different folders
            var uniqueName = Path.GetFileNameWithoutExtension(data.ScenePath);

            if (State != SceneLoaderState.OnlyBuildScenes)
                return uniqueName;
            
            // for some reason buildIndex returns the number of built scenes when it is not in that list...
            if (data.BuildIndex >= 0 && data.BuildIndex < SceneManager.sceneCountInBuildSettings)
                return $"{data.BuildIndex}: {uniqueName}";
            
            return uniqueName;
        }

        protected override void SelectionMade(SceneReference scene)
        {
            if (scene == null) return;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(scene.ScenePath, OpenSceneMode.Single);
        }
    }
}