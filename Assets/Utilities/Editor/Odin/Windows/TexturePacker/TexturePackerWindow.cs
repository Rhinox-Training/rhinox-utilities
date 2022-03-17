using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using ObjectFieldAlignment = Sirenix.OdinInspector.ObjectFieldAlignment;

namespace Rhinox.Utilities.Odin.Editor
{
    public class TexturePackerWindow : OdinEditorWindow
    {
        /// ================================================================================================================
        /// PARAMETERS
        [ShowInInspector, HorizontalGroup("Root")]
        private TexturePacker _texturePacker = new TexturePacker();

        [ShowInInspector, PreviewField(ObjectFieldAlignment.Left, Height = 128), HideLabel,
         HorizontalGroup("Root/Preview", width: 138),
         TitleGroup("Root/Preview/Preview", Alignment = TitleAlignments.Centered)]
        private Texture2D Preview { get { return _texturePacker.Create(128); } }

        [VerticalGroup("Root/Preview/Preview/Properties", order: 100), LabelWidth(70)]
        [ValueDropdown("_textureSizes")]
        public int Resolution = 2048;
        private static int[] _textureSizes = { 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        
        /// ================================================================================================================
        /// METHODS
        public static void Open()
        {
            var window = GetWindow<TexturePackerWindow>();
            window.titleContent.text = "Texture Packer";
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 500);
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            _texturePacker.Initialize();
        }
        
        public void DrawPreview(TexturePacker texPacker, int resolution = 128)
        {
            GUILayout.Label("Preview", TexturePackerStyles.Heading);

            GUILayout.BeginVertical(TexturePackerStyles.Section);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Vector2 previewSize = new Vector2(256, 256);
            GUILayout.Label("", TexturePackerStyles.MidBox, GUILayout.Width(previewSize.x), GUILayout.Height(previewSize.y));
            Rect previewRect = GUILayoutUtility.GetLastRect();
            Rect alphaRect = new Rect(previewRect.x + 5, previewRect.y + 5, previewRect.width - 10, previewRect.height - 10);

            texPacker.ClearProperties();

            Texture2D preview = texPacker.Create(resolution);
            EditorGUI.DrawPreviewTexture(alphaRect, preview);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        [Button("Save Texture", ButtonSizes.Medium)]
        private void Generate()
        {
            string savePath = EditorUtility.SaveFilePanel("Save", Application.dataPath, "texture.png", "*.png,*.jpg,*.jpeg,*.exr");
            if (string.IsNullOrWhiteSpace(savePath)) return;

            var format = Path.GetExtension(savePath).ToLower();
            
            Texture2D output = _texturePacker.Create(Resolution);

            switch (format)
            {
                case ".png":
                    File.WriteAllBytes(savePath, output.EncodeToPNG());
                    break;
                    
                case ".jpeg":
                case ".jpg":
                    File.WriteAllBytes(savePath, output.EncodeToJPG());
                    break;
                    
                case ".exr":
                    File.WriteAllBytes(savePath, output.EncodeToEXR());
                    break;
                
                default:
                    EditorUtility.DisplayDialog("Cannot compute...", string.Format("The fileformat '{0}'", format), "OK");
                    break;
            }

            AssetDatabase.Refresh();
        }
    }
}
