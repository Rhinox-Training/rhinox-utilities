using System;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Editor
{
    internal static class SizeVisualizer
    {
        private static bool _active;

        private const string _menuItemPath = WindowHelper.ToolsPrefix + "Show Unit Size #s";

        private static float MeterCutoff;
        private static bool ShowAxisAligned;
        
        private const string MeterCutoff_Key = nameof(SizeVisualizer) + "_" + nameof(MeterCutoff);
        private const string ShowAxisAligned_Key = nameof(SizeVisualizer) + "_" + nameof(ShowAxisAligned);

        [MenuItem(_menuItemPath, false, -198)]
        public static void ActivateSizeVisualizer()
        {
            MeterCutoff = EditorPrefs.GetFloat(MeterCutoff_Key, 1f);
            ShowAxisAligned = EditorPrefs.GetBool(ShowAxisAligned_Key, false);
            
            if (!_active)
            {
                Utility.SubscribeToSceneGui(ShowSizeVisualizer);
                _active = true;
            }
            else
            {
                Utility.UnsubscribeFromSceneGui(ShowSizeVisualizer);
                _active = false;
            }
        }

        [MenuItem(_menuItemPath, true)]
        public static bool IsActive()
        {
            Menu.SetChecked(_menuItemPath, _active);
            return true; // returns whether it is clickable
        }
        
        private static void ShowSizeVisualizer(SceneView sceneview)
        {
            if (sceneview.camera == null) return;
            
            SceneOverlay.AddWindow("Size Visualizer Options", SizeVisualizerOptionsFunc);
            
            for (var i = 0; i < Selection.gameObjects.Length; i++)
            {
                var o = Selection.gameObjects[i];
                if (!o.activeInHierarchy) continue;
                var renderers = o.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                var bounds = ShowAxisAligned ? renderers.GetCombinedLocalBounds(o.transform) : renderers.GetCombinedBounds();
                if (bounds == default) continue;
                
                HandlesExt.PushZTest(CompareFunction.Less);
                HandlesExt.PushColor(Color.grey);
                
                if (ShowAxisAligned)
                    HandlesExt.PushMatrix(o.transform.localToWorldMatrix);
                
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
                
                if (ShowAxisAligned)
                    HandlesExt.PopMatrix();
            }
        }

        private static void SizeVisualizerOptionsFunc(Object target, SceneView sceneview)
        {
            var cutoff = EditorGUILayout.FloatField("Meter Cutoff", MeterCutoff);
            if (cutoff < 0) cutoff = 0;
            
            var alignment = EditorGUILayout.Toggle("Axis Aligned", ShowAxisAligned);

            if (Math.Abs(cutoff - MeterCutoff) > float.Epsilon)
            {
                MeterCutoff = cutoff;
                EditorPrefs.SetFloat(MeterCutoff_Key, MeterCutoff);
            }
            if (alignment != ShowAxisAligned)
            {
                ShowAxisAligned = alignment;
                EditorPrefs.SetBool(ShowAxisAligned_Key, ShowAxisAligned);
            }
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
    }
}