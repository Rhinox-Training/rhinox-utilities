using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Editor
{
    internal class AlignHelper : CustomSceneOverlayWindow<AlignHelper>
    {
        protected override string Name => "Align Helper";
        private const string _menuItemPath = WindowHelper.ToolsPrefix + "Align Helper &a";
        
        
        [Title("Align", titleAlignment: TitleAlignments.Centered)]
        public static BoundsAligner BoundsAligner = new BoundsAligner();

        [Title("Gap Align", titleAlignment: TitleAlignments.Centered)]
        public static GapAligner GapAligner = new GapAligner();
        
        private static Texture AlignTopTexture;
        private static Texture AlignBottomTexture;
        private static Texture AlignMiddleTexture;

        private const int IconSize = 19;
        
        [MenuItem(_menuItemPath, false, -200)]
        public static void SetupWindow() => Window.Setup();

        [MenuItem(_menuItemPath, true)]
        public static bool SetupValidateWindow() => Window.HandleValidateWindow();

        protected override void Initialize()
        {
            base.Initialize();
            
            AlignTopTexture = UnityIcon.AssetIcon("AlignTop");
            AlignBottomTexture = UnityIcon.AssetIcon("AlignBottom");
            AlignMiddleTexture = UnityIcon.AssetIcon("AlignMiddle");
        }

        protected override void OnGUI()
        {
            using (new eUtility.HorizontalGroup())
            {
                using (new eUtility.VerticalGroup())
                {
                    DrawAxisAlignOptions(Axis.X);
                    DrawAxisAlignOptions(Axis.Y);
                    DrawAxisAlignOptions(Axis.Z);
                }

                GUILayout.Space(5);
                CustomEditorGUI.VerticalLine(Color.grey);
                GUILayout.Space(5);

                using (new eUtility.VerticalGroup())
                {
                    GUILayout.Label("Gap", CustomGUIStyles.CenteredLabel);
                    GapAligner.Gap = EditorGUILayout.FloatField("", GapAligner.Gap, GUILayout.Width(38));
                    using (new eUtility.HorizontalGroup())
                    {
                        DrawGapButton(Axis.X);
                        DrawGapButton(Axis.Y);
                        DrawGapButton(Axis.Z);
                    }
                }
            }
        }

        private static void DrawGapButton(Axis axis)
        {
            if (GUILayout.Button(axis.ToString(), CustomGUIStyles.CenteredLabel))
                GapAligner.AddGap(axis);
        }

        private static void DrawAxisAlignOptions(Axis axis)
        {
            using (var g = new eUtility.HorizontalGroup())
            {
                // Keep a rect to keep track where an icon will appear, this will be used to calculate the pivot for GUI rotations
                var iconRect = g.Rect.AlignLeft(IconSize);
                iconRect.height = IconSize; // limit height

                // Draw axis label
                GUILayout.Label(axis.ToString(), CustomGUIStyles.CenteredLabel, GUILayout.Width(15));
                iconRect.x += GUILayoutUtility.GetLastRect().width;

                CustomEditorGUI.VerticalLine(Color.grey);
                GUILayout.Space(5);
                iconRect.x += 6;

                
                // angle of rotation of the icons
                float angle = 0;
                if (axis == Axis.X) angle = 90;
                else if (axis == Axis.Y) angle = 180;
                // else if (axis == Axis.Z) angle = 180;

                // Draw MAX option
                using (new eUtility.Rotation(angle, iconRect.center))
                {
                    if (CustomEditorGUI.IconButton(AlignBottomTexture, IconSize, IconSize, tooltip: $"Align by MAX {axis}"))
                        BoundsAligner.AlignBy(axis, false);
                }

                iconRect.x += IconSize;

                // Draw MIN option
                using (new eUtility.Rotation(angle, iconRect.center))
                {
                    if (CustomEditorGUI.IconButton(AlignTopTexture, IconSize, IconSize, tooltip: $"Align by MIN {axis}"))
                        BoundsAligner.AlignBy(axis, true);
                }

                iconRect.x += IconSize;

                if (axis == Axis.Z) angle = 90;

                // Draw MIDDLE option
                using (new eUtility.Rotation(angle, iconRect.center))
                {
                    if (CustomEditorGUI.IconButton(AlignMiddleTexture, IconSize, IconSize, tooltip: $"Align by MIDDLE {axis}"))
                        BoundsAligner.AlignCenter(axis);
                }

                iconRect.x += IconSize;

                // using (new eUtility.Rotation(90, iconRect.center))
                // {
                //     if (SirenixEditorGUI.IconButton(AlignMiddleTexture, tooltip: "Align by middle - vertical"))
                //         BoundsAligner.AlignCenterY();
                // }
                // iconRect.x += IconSize;
            }
        }

        protected override string GetMenuPath() => _menuItemPath;
    }

    [HideReferenceObjectPicker, HideLabel]
    internal class BoundsAligner
    {
        private enum BoundsTarget
        {
            Min,
            Middle,
            Max
        }

        public void AlignBy(Axis axis, bool negative)
        {
            BoundsAlign(axis, negative ? BoundsTarget.Min : BoundsTarget.Max);
        }

        public void AlignCenter(Axis axis)
        {
            BoundsAlign(axis, BoundsTarget.Middle);
        }

        private void BoundsAlign(Axis axis, BoundsTarget target)
        {
            var selection = Selection.gameObjects;

            if (selection.Length <= 1) return;

            var boundsByObj = selection.ToDictionary(x => x, x => x.GetObjectBounds());

            // Remove objects without Bounds from the list
            foreach (var obj in selection)
            {
                if (boundsByObj[obj] == default(Bounds))
                    boundsByObj.Remove(obj);
            }

            var targetValues = boundsByObj.Values.Select(x => GetBoundsTargetValue(x, target, axis));
            float targetValue = GetTargetValue(targetValues, target);

            foreach (var (obj, bounds) in boundsByObj)
            {
                // calculate 'targetValue' for this object
                var currentValue = GetBoundsTargetValue(bounds, target, axis);

                // get the offset to that point from the pivot
                var pivotOffset = GetVectorAxis(obj.transform.position, axis) - currentValue;

                // using that offset, apply the targetValue to the obj
                Undo.RegisterCompleteObjectUndo(obj.transform, "Align By Bounds");

                var t = obj.transform;
                switch (axis)
                {
                    case Axis.X:
                        t.position = t.position.With(x: targetValue + pivotOffset);
                        break;
                    case Axis.Y:
                        t.position = t.position.With(y: targetValue + pivotOffset);
                        break;
                    case Axis.Z:
                        t.position = t.position.With(z: targetValue + pivotOffset);
                        break;
                }
            }
        }

        private float GetTargetValue(IEnumerable<float> targetValues, BoundsTarget target)
        {
            switch (target)
            {
                case BoundsTarget.Min:
                    return targetValues.Min();
                case BoundsTarget.Middle:
                    return targetValues.Average();
                case BoundsTarget.Max:
                    return targetValues.Max();
                default:
                    throw new Exception($"BoundsTarget {target} not supported");
            }
        }

        private float GetBoundsTargetValue(Bounds bounds, BoundsTarget target, Axis axis)
        {
            var targetPoint = GetBoundsTarget(bounds, target);
            return GetVectorAxis(targetPoint, axis);
        }

        private Vector3 GetBoundsTarget(Bounds bounds, BoundsTarget target)
        {
            switch (target)
            {
                case BoundsTarget.Min:
                    return bounds.min;
                case BoundsTarget.Middle:
                    return bounds.center;
                case BoundsTarget.Max:
                    return bounds.max;
                default:
                    throw new Exception($"BoundsTarget {target} not supported");
            }
        }

        private float GetVectorAxis(Vector3 vec, Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return vec.x;
                case Axis.Y:
                    return vec.y;
                case Axis.Z:
                    return vec.z;
                default:
                    throw new Exception($"Axis {axis} not supported");
            }
        }
    }

    [HideReferenceObjectPicker, HideLabel]
    internal class GapAligner
    {
        public float Gap = .01f;

        public void AddGap(Axis axis)
        {
            if (axis.HasFlag(Axis.X))
                AlignX();
            if (axis.HasFlag(Axis.Y))
                AlignY();
            if (axis.HasFlag(Axis.Z))
                AlignZ();
        }

        // [ButtonGroup, Button("X", ButtonSizes.Medium)]
        private void AlignX()
        {
            var selection = Selection.gameObjects;
            if (!selection.Any()) return;

            var boundsDict = selection.ToDictionary(x => x, x => x.GetObjectBounds());

            var current = boundsDict.Values.Min(x => x.min.x);
            foreach (var go in selection.OrderBy(x => boundsDict[x].min.x))
            {
                var bounds = boundsDict[go];
                var pivotOffset = go.transform.position.x - bounds.min.x;

                Undo.RegisterCompleteObjectUndo(go.transform, "Align With Gap (X)");

                var t = go.transform;
                t.position = t.position.With(x: current + pivotOffset);

                current += bounds.extents.x * 2 + Gap;
            }
        }

        // [ButtonGroup, Button("Y", ButtonSizes.Medium)]
        private void AlignY()
        {
            var selection = Selection.gameObjects;
            if (!selection.Any()) return;

            var boundsDict = selection.ToDictionary(x => x, x => x.GetObjectBounds());

            var current = boundsDict.Values.Min(x => x.min.y);
            foreach (var go in selection.OrderBy(x => boundsDict[x].min.y))
            {
                var bounds = boundsDict[go];
                var pivotOffset = go.transform.position.y - bounds.min.y;

                Undo.RegisterCompleteObjectUndo(go.transform, "Align With Gap (Y)");

                var t = go.transform;
                t.position = t.position.With(y: current + pivotOffset);

                current += bounds.extents.y * 2 + Gap;
            }
        }

        // [ButtonGroup, Button("Z", ButtonSizes.Medium)]
        private void AlignZ()
        {
            var selection = Selection.gameObjects;
            if (!selection.Any()) return;

            var boundsDict = selection.ToDictionary(x => x, x => x.GetObjectBounds());

            var current = boundsDict.Values.Min(x => x.min.z);
            foreach (var go in selection.OrderBy(x => boundsDict[x].min.z))
            {
                var bounds = boundsDict[go];
                var pivotOffset = go.transform.position.z - bounds.min.z;

                Undo.RegisterCompleteObjectUndo(go.transform, "Align With Gap (Z)");

                var t = go.transform;
                t.position = t.position.With(z: current + pivotOffset);

                current += bounds.extents.z * 2 + Gap;
            }
        }
    }
}