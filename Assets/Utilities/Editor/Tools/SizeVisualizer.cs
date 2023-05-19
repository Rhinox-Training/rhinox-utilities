using System;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rhinox.Utilities.Editor
{
    public class SizeVisualizer : CustomSceneOverlayWindow<SizeVisualizer>
    {
        protected override string Name => "Size Visualizer";
        private const string _menuItemPath = WindowHelper.ToolsPrefix + "Show Unit Size #s";

        private PersistentValue<float> MeterCutoff;
        private PersistentValue<bool> ShowAxisAligned;
        
        [MenuItem(_menuItemPath, false, -198)]
        public static void SetupWindow() => Window.Setup();

        [MenuItem(_menuItemPath, true)]
        public static bool SetupValidateWindow() => Window.HandleValidateWindow();

        protected override void Initialize()
        {
            base.Initialize();
            
            MeterCutoff = PersistentValue<float>.Create(typeof(PolyCounter), nameof(MeterCutoff), 1f);
            ShowAxisAligned = PersistentValue<bool>.Create(typeof(PolyCounter), nameof(ShowAxisAligned), false);
        }

        protected override void OnSceneGUI(SceneView sceneView)
        {
            base.OnSceneGUI(sceneView);
            
            foreach (var go in Selection.gameObjects)
            {
                if (!go.activeInHierarchy) continue;
                var renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                var bounds = ShowAxisAligned ? renderers.GetCombinedBounds() : renderers.GetCombinedLocalBounds(go.transform);
                if (bounds == default) continue;
                
                HandlesExt.PushZTest(CompareFunction.Less);
                HandlesExt.PushColor(Color.grey);
                
                if (!ShowAxisAligned)
                    HandlesExt.PushMatrix(go.transform.localToWorldMatrix);
                
                Handles.DrawWireCube(bounds.center, bounds.size);
                var lines = GetRelevantLines(bounds);

                foreach (var line in lines)
                    line.Draw();
                
                HandlesExt.PopZTest();
                HandlesExt.PopColor();
                
                HandlesExt.PushColor(Color.white);
                
                foreach (var line in lines)
                {
                    var length = line.GetDrawnLength();
                    var lengthLabel = length < MeterCutoff ? $"{length * 100:#.00} cm" : $"{length:#.00} m";
                    Handles.Label(line.Center, lengthLabel, CustomGUIStyles.BoldLabelCentered);
                }
                
                HandlesExt.PopColor();
                
                if (!ShowAxisAligned)
                    HandlesExt.PopMatrix();
            }
        }

        protected override void OnGUI()
        {
            GUIContentHelper.PushLabelWidth(100);
            
            GUILayout.BeginHorizontal();

            MeterCutoff.ShowField("Meter Cutoff");
            if (MeterCutoff < 0) MeterCutoff.Set(0);
            ShowAxisAligned.ShowInternalIcon("Transform Icon", ShowAxisAligned ? "The global axis is used" : "The object's local axis is used");
            
            GUILayout.EndHorizontal();
            
            GUIContentHelper.PopLabelWidth();
        }

        private static HandlesLine[] GetRelevantLines(Bounds bounds)
        {
            // TODO maybe use viewpos to highlight other lines?
            
            var origin = bounds.center + bounds.extents;
            
            return new []
            {
                new HandlesLine(origin, origin - bounds.size.With(x: 0, y: 0), Color.blue),
                new HandlesLine(origin, origin - bounds.size.With(x: 0, z: 0), Color.green),
                new HandlesLine(origin, origin - bounds.size.With(y: 0, z: 0), Color.red),
            };
        }
        
        protected override string GetMenuPath() => _menuItemPath;
    }
}