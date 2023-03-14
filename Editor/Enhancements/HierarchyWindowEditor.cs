using System;
using System.Linq;
using System.Text.RegularExpressions;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Utilities;
#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif
using UnityEngine;
using UnityEditor;

namespace Rhinox.Utilities.Editor
{
    /// <summary>
    /// Hierarchy Window Group Header
    /// http://diegogiacomelli.com.br/unitytips-hierarchy-window-group-header
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyWindowEditor
    {
        static readonly LayerMask IgnoreLayer = Utility.GetMask("Default");
        static readonly float GameObjectIconWidth = 16;

        private static Texture2D _gradientTexture;
        private static GUIContent _editorOnlyContent;

#if ODIN_INSPECTOR
    private static Texture EditorCircleIcon => EditorIcons.AlertCircle.Active;
#else
        private static Texture EditorCircleIcon => UnityIcon.InternalIcon("d_console.warnicon.inactive.sml@2x");
#endif

        static HierarchyWindowEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowGroupHeader;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyEditorOnlyHeader;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowLayerInfo;
        }

        static void HierarchyWindowGroupHeader(int instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null || !MatchesPattern(gameObject, out bool gradient, out Color color, out string text))
                return;

            bool isSelected = Selection.Contains(instanceID);

            selectionRect.height -= 2;
            selectionRect.y += 1;
            if (gradient)
            {
                
                const float backgroundColorVal = 0.22f;
                Color backgroundColor = new Color(backgroundColorVal, backgroundColorVal, backgroundColorVal);
                DrawOnGradient(selectionRect, color, fillBackground: isSelected ? new Color(0.172f, 0.365f, 0.529f) : backgroundColor);
            }
            else
                EditorGUI.DrawRect(selectionRect, color);


            const float lightnessThreshold = 0.27f;
            bool flipTextColor = color.grayscale > lightnessThreshold;
            if (flipTextColor)
                GUIContentHelper.PushColor(Color.black);
            EditorGUI.LabelField(selectionRect, text, CustomGUIStyles.BoldLabelCentered);
            if (flipTextColor)
                GUIContentHelper.PopColor();
        }

        private static bool MatchesPattern(GameObject gameObject, out bool useGradient, out Color color, out string name)
        {
            bool result = gameObject.name.StartsWith("===", StringComparison.Ordinal) || 
                                (gameObject.name.Length > 4 
                                        && gameObject.name[1] == '=' 
                                        && gameObject.name.Substring(1, 3).StartsWith("===", System.StringComparison.Ordinal));
            if (!result)
            {
                useGradient = false;
                color = Color.clear;
                name = null;
                return false;
            }

            char c = gameObject.name[0];
            if (!TryGetColor(c, out Color colorVal))
            {
                useGradient = false;
                color = Color.clear;
                name = null;
                return false;
            }

            color = colorVal;
            useGradient = gameObject.name.EndsWith("=g", StringComparison.OrdinalIgnoreCase);
            name = gameObject.name;
            int offset = 0;
            if (c != '=')
                offset = 1;
            int endOffset = 0;
            if (useGradient)
                endOffset = -2;
            name = name
                .Substring(offset, name.Length - offset + endOffset)
                .Replace("=", "")
                .Trim()
                .ToUpperInvariant();
            return true;
        }

        private static bool TryGetColor(char c, out Color color)
        {
            if (c == '=' || c == '0')
            {
                const float gray = .45f;
                color = new Color(gray, gray, gray);
                return true;
            }

            // TODO: add colors to lightspeed
            switch (c)
            {
                case '1':
                    color = Color.green;
                    return true;
                case '2':
                    color = Color.blue;
                    return true;
                case '3':
                    color = Color.cyan;
                    return true;
                case '4':
                    color = Color.magenta;
                    return true;
                case '5':
                    color = Color.red;
                    return true;
                case '6':
                    color = Color.yellow;
                    return true;
                case '7':
                    color = new Color(0.8f, 0.5f, 0.0f);
                    return true;
                case '8':
                    color = new Color(0.4f, 0.2f, 0.8f);
                    return true;
                case '9':
                    color = new Color(0.3f, 0.1f, 0.05f);
                    return true;
                default:
                    color = Color.clear;
                    return false;
            }
        }

        static void HierarchyEditorOnlyHeader(int instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null || !gameObject.CompareTag("EditorOnly"))
                return;

            if (_editorOnlyContent == null)
                _editorOnlyContent = new GUIContent(EditorCircleIcon, "Marked as EDITOR ONLY");

            DrawOnGradient(selectionRect, new Color(.55f, .04f, .03f), _editorOnlyContent);
        }

        static void HierarchyWindowLayerInfo(int instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null || IgnoreLayer.HasLayer(gameObject.layer)) return;

            // this is assuming EditorStyles.Label is approximately the same style used by the hierarchy
            float maxWidth = GUIContentHelper.CalcMaxLabelWidth(gameObject.name);

            var contentStr = LayerMask.LayerToName(gameObject.layer);
            GUIStyle style = CustomGUIStyles.MiniLabelRight;
            float contentMax = style.CalcMaxWidth(contentStr);

            // not enough space to draw it
            if (maxWidth + contentMax + GameObjectIconWidth >= selectionRect.width)
                return;

            EditorGUI.LabelField(selectionRect, contentStr, style);
        }

        private static void DrawOnGradient(Rect rect, Color color, GUIContent iconContent = null, Color? fillBackground = null)
        {
            // Create gradient
            if (_gradientTexture == null)
            {
                int width = 50, height = (int)EditorGUIUtility.singleLineHeight;
                Gradient gradient =
                    Utility.MakeGradient(Color.white, Color.white, (0, 0), (.35f, .2f), (.6f, .4f), (1f, .7f));

                _gradientTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                    { hideFlags = HideFlags.HideAndDontSave };
                _gradientTexture.alphaIsTransparency = true;
                for (int x = 0; x < width; ++x)
                {
                    for (int y = 0; y < height; ++y)
                        _gradientTexture.SetPixel(x, y, gradient.Evaluate(x / (width - 1f)));
                }

                _gradientTexture.Apply();
            }

            if (iconContent != null)
            {
                var size = GUI.skin.label.CalcSize(iconContent);
                // make size fit unto the available rect
                var contentWidth = (EditorGUIUtility.singleLineHeight / size.y) * size.x;

                rect = RectExtensions.AlignRight(rect, contentWidth + 10);
                
                if (fillBackground.HasValue)
                    EditorGUI.DrawRect(rect, fillBackground.Value);
                GUIContentHelper.PushColor(color);
                GUI.DrawTexture(rect, _gradientTexture);
                GUI.Label(RectExtensions.AlignRight(rect, contentWidth), iconContent);
                GUIContentHelper.PopColor();
            }
            else
            {
                if (fillBackground.HasValue)
                    EditorGUI.DrawRect(rect, fillBackground.Value);
                GUIContentHelper.PushColor(color);
                GUI.DrawTexture(rect, _gradientTexture);
                GUIContentHelper.PopColor();
            }

        }
    }
}