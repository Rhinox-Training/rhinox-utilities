﻿using System;
using System.IO;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Utilities.Editor;
using Sirenix.OdinInspector;
using ObjectFieldAlignment = Sirenix.OdinInspector.ObjectFieldAlignment;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    [Serializable]
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
        private TexturePackerRoot _root = new TexturePackerRoot();
        
        private SmartPropertyView _propertyView;

        /// ================================================================================================================
        /// METHODS
        [MenuItem(WindowHelper.ToolsPrefix + "Texture Packer", false, 202)]
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
        private void OnGUI()
        {
            if (_propertyView == null)
                _propertyView = new SmartPropertyView(_root);
            _propertyView.DrawLayout();
        }
    }
}
