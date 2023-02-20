using System;
using System.IO;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Utilities.Editor;
using Sirenix.OdinInspector;
using ObjectFieldAlignment = Sirenix.OdinInspector.ObjectFieldAlignment;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Odin.Editor
{
    public class TexturePackerRoot
    {
        /// ================================================================================================================
        /// PARAMETERS
        [ShowInInspector, HorizontalGroup("Root", order: -1)]
        public TexturePacker _texturePacker = new TexturePacker();

        [ShowInInspector, PreviewField(ObjectFieldAlignment.Left, Height = 128), HideLabel,
         HorizontalGroup("Root/Preview", width: 138),
         TitleGroup("Root/Preview/Preview", Alignment = TitleAlignments.Centered)]
        public Texture2D Preview { get { return _texturePacker.Create(128); } }

        [VerticalGroup("Root/Preview/Preview/Properties", order: 100), LabelWidth(70)]
        [ValueDropdown("_textureSizes")]
        public int Resolution = 2048;
        private static int[] _textureSizes = { 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

        public void Initialize()
        {
            _texturePacker.Initialize();
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
    
    public class TexturePackerWindow : EditorWindow
    {
        [ShowInInspector]
        private TexturePackerRoot _root = new TexturePackerRoot();
        
#if !ODIN_INSPECTOR
        private DrawablePropertyView _propertyView;
#endif

        /// ================================================================================================================
        /// METHODS
        [MenuItem(WindowHelper.WindowPrefix + "Texture Packer")]
        public static void Open()
        {
            var window = GetWindow<TexturePackerWindow>();
            window.titleContent.text = "Texture Packer";
            window.position = CustomEditorGUI.GetEditorWindowRect().AlignCenter(800, 500);
        }

        private void Awake()
        {
            _root.Initialize();

        }
#if !ODIN_INSPECTOR
        private void OnGUI()
        {
            if (_propertyView == null)
                _propertyView = new DrawablePropertyView(_root);
            _propertyView.DrawLayout();
            //_texturePacker = EditorGUILayout.ObjectField(GUIContent.none, _texturePacker, typeof(TexturePacker), false);
        }
#endif

        // TODO: probably old code
        // public void DrawPreview(TexturePacker texPacker, int resolution = 128)
        // {
        //     GUILayout.Label("Preview", TexturePackerStyles.Heading);
        //
        //     GUILayout.BeginVertical(TexturePackerStyles.Section);
        //
        //     GUILayout.BeginHorizontal();
        //     GUILayout.FlexibleSpace();
        //
        //     Vector2 previewSize = new Vector2(256, 256);
        //     GUILayout.Label("", TexturePackerStyles.MidBox, GUILayout.Width(previewSize.x), GUILayout.Height(previewSize.y));
        //     Rect previewRect = GUILayoutUtility.GetLastRect();
        //     Rect alphaRect = new Rect(previewRect.x + 5, previewRect.y + 5, previewRect.width - 10, previewRect.height - 10);
        //
        //     texPacker.ClearProperties();
        //
        //     Texture2D preview = texPacker.Create(resolution);
        //     EditorGUI.DrawPreviewTexture(alphaRect, preview);
        //
        //     GUILayout.FlexibleSpace();
        //     GUILayout.EndHorizontal();
        //
        //     GUILayout.EndVertical();
        // }

       
    }
}
