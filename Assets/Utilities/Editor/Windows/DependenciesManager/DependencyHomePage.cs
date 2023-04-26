using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public class DependencyHomePage
    {
        [ListDrawerSettings(Expanded = true, NumberOfItemsPerPage = 12, DraggableItems = false)]
        [AssetsOnly, DrawAsUnityObject]
        public List<Object> Objects = new List<Object>();

        private DependenciesWindow _mainWindow;

        public void Initialize(DependenciesWindow window)
        {
            _mainWindow = window;
        }

        [Button(ButtonSizes.Large), ButtonGroup("Find", 100)]
        public void FindDependencies()
        {
            _mainWindow.ClearSelections();
            _mainWindow.DependenciesManager.FindDependencies(Objects, _mainWindow.Settings.IgnoredFileRegexs,
                _mainWindow.Settings.IgnoredDirectoryRegexs);
            _mainWindow.ForceMenuTreeRebuild();
        }

        // [Button(ButtonSizes.Medium), ButtonGroup("Find")]
        // public void FindDependant()
        // {
        // 	// NOT WORKING
        // 	DependenciesManager.FindDependant(Objects);
        // 	ForceMenuTreeRebuild();
        // }

        [Button(ButtonSizes.Medium), ButtonGroup("Add"), GUIColor(.4f, .8f, .6f)]
        public void AddAllScenes()
        {
            var paths = _mainWindow.AssetManager.GetAssetsPaths("t:Scene")
                .Where(p => p.StartsWith("Assets/"));
            foreach (var path in paths)
            {
                Objects.AddUnique(AssetDatabase.LoadAssetAtPath<Object>(path));
            }
        }

        [Button(ButtonSizes.Medium), ButtonGroup("Add"), GUIColor(.4f, .8f, .6f)]
        public void AddAllResources()
        {
            var paths = _mainWindow.AssetManager.AllAssets
                .Where(path => path.Contains("/Resources/"));
            foreach (var path in paths)
            {
                Objects.AddUnique(AssetDatabase.LoadAssetAtPath<Object>(path));
            }
        }

        [Button(ButtonSizes.Medium), ButtonGroup("Add"), GUIColor(.4f, .8f, .6f)]
        public void AddAllPrefabs()
        {
            var paths = _mainWindow.AssetManager.GetAssetsPaths("t:Prefab");
            foreach (var path in paths)
                Objects.AddUnique(AssetDatabase.LoadAssetAtPath<Object>(path));
        }

        [Button(ButtonSizes.Small), ButtonGroup("Add2"), GUIColor(.4f, .8f, .6f)]
        public void AddSelection()
        {
            foreach (var obj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                Objects.AddUnique(AssetDatabase.LoadAssetAtPath<Object>(path));
            }
        }

        [Button(ButtonSizes.Small), ButtonGroup("Add2"), GUIColor(.4f, .8f, .6f)]
        public void AddLiterallyEverything()
        {
            var paths = _mainWindow.AssetManager.GetAssetsPaths("");
            foreach (var path in paths)
                Objects.AddUnique(AssetDatabase.LoadAssetAtPath<Object>(path));
        }
        
        [Button(ButtonSizes.Small), GUIColor(.8f, .4f, .6f)]
        public void Clear()
        {
            Objects.Clear();
            _mainWindow.Clear();
        }
    }
}