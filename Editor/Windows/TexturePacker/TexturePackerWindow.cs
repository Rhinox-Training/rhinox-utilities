using System;
using System.IO;
using Rhinox.GUIUtils.Attributes;
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
        [ShowInInspector, HorizontalGroup("Root", order: -1), HideLabel]
        public TexturePacker _texturePacker = new TexturePacker();

        [ShowInInspector, PreviewField(ObjectFieldAlignment.Left, Height = 128), HideLabel,
         HorizontalGroup("Root/Preview", width: 138),
         TitleGroup("Root/Preview/Preview", Alignment = TitleAlignments.Centered)]
        public Texture2D Preview => CreatePreviewTexture(128);

        [HorizontalGroup("Root/Preview/Preview/Properties", order: 100)]
        [FittedLabel("W"), MinValue(1), Delayed]
        public int ResolutionW = 128;
        [HorizontalGroup("Root/Preview/Preview/Properties", order: 100)]
        [FittedLabel("H"), MinValue(1), Delayed]
        public int ResolutionH = 128;

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
            
            Texture2D output = _texturePacker.Create(ResolutionW, ResolutionH);

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
                    EditorUtility.DisplayDialog("Cannot compute...", $"The fileformat '{format}'", "OK");
                    break;
            }

            AssetDatabase.Refresh();
        }
        
        
        private Texture2D CreatePreviewTexture(float max)
        {
            if (ResolutionH <= 0 || ResolutionW <= 0) return Texture2D.blackTexture;

            int width = ResolutionW;
            int height = ResolutionH;
            float scale = Mathf.Max(ResolutionW / max, ResolutionH / max);
            width = Mathf.CeilToInt(width / scale);
            height = Mathf.CeilToInt(height / scale);
            
            return _texturePacker.Create(width, height);
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
            {
                _propertyView = new SmartPropertyView(_root);
                _propertyView.RepaintRequested += RequestRepaint;
            }
            
            _propertyView.DrawLayout();
        }

        public void RequestRepaint()
        {
            Repaint();
        }
    }
}
