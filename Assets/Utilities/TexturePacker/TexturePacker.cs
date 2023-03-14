using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rhinox.Utilities
{
    [Flags]
    public enum TextureChannel
    {
        Red   = 1 << 0,
        Green = 1 << 1,
        Blue  = 1 << 2,
        Alpha = 1 << 3
    }
    
    [HideReferenceObjectPicker, Serializable]
    public class TextureChannelInput
    {
        [HideLabel, HorizontalGroup("Row")]
        public bool Enabled;
        
        [LabelWidth(50), HorizontalGroup("Row")]
        public TextureChannel Output;
        
        public TextureChannelInput() {}

        public TextureChannelInput(TextureChannel output)
        {
            Output = output;
        }
        
        public TextureChannelInput(TextureChannel output, bool enabled = false)
        {
            this.Output = output;
            this.Enabled = enabled;
        }
    }
    
    [HideReferenceObjectPicker, HideLabel]
    public class TexturePacker
    {
        private readonly string _shaderName = "Hidden/TexturePacker";
        private Material _material;

        [ListDrawerSettings(Expanded = true, CustomAddFunction = "AddInput")]
        public List<TextureInput> Input = new List<TextureInput>();

        private static TextureChannel[] _channelValues = Enum.GetValues(typeof(TextureChannel)).OfType<TextureChannel>().ToArray();

        public void Initialize()
        {
            TryCreateOutputMaterial();
        }

        private void TryCreateOutputMaterial()
        {
            if (_material == null)
                _material = new Material(Shader.Find(_shaderName)) {hideFlags = HideFlags.HideAndDontSave};
        }

        private void AddInput()
        {
            Input.Add(new TextureInput());
        }


        private string GetPropertyName(int i, string param)
        {
            return $"_Input{i:D2}{param}";
        }

        public void ClearProperties()
        {
            for (int i = 0; i < 6; ++i)
            {
                _material.SetTexture(GetPropertyName(i, "Tex"), Texture2D.blackTexture);
                _material.SetVector(GetPropertyName(i, "In"), Vector4.zero);
            }
        }

        private Vector4 GetInputs(TextureInput texInput)
        {
            Vector4 states = Vector4.zero;

            for (int i = 0; i < _channelValues.Length; ++i)
            {
                var state = texInput.GetChannelInput(_channelValues[i]).Enabled;
                states[i] = state ? 1f : 0f;
            }

            return states;
        }

        private Matrix4x4 GetOutputs(TextureInput texInput)
        {
            Matrix4x4 m = Matrix4x4.zero;

            for (int i = 0; i <_channelValues.Length; ++i)
            {
                Vector4 inChannel = Vector4.zero;
                var output = texInput.GetChannelInput(_channelValues[i]).Output;
                
                for (int j = 0; j <_channelValues.Length; ++j)
                    inChannel[j] = output.HasFlag(_channelValues[j]) ? 1f : 0f;
                m.SetRow(i, inChannel);
            }
            
            return m;
        }

        public Texture2D Create(int resolution)
        {
            if (_material == null)
                TryCreateOutputMaterial();
            
            int idx = 0;
            bool hasAlpha = false;
            foreach (var input in Input)
            {
                var tex = input.texture;
                _material.SetTexture(GetPropertyName(idx, "Tex"), tex);

                var inChannels = GetInputs(input);
                _material.SetVector(GetPropertyName(idx, "In"), inChannels);

                var outMatrix = GetOutputs(input);
                CheckForAlpha(ref hasAlpha, outMatrix, inChannels);
                _material.SetMatrix(GetPropertyName(idx, "Out"), outMatrix);
                ++idx;
            }

            var texture = GenerateTexture(resolution, resolution, _material, hasAlpha);

            return texture;
        }

        private void CheckForAlpha(ref bool hasAlpha, Matrix4x4 outMatrix, Vector4 inChannels)
        {
            if (hasAlpha) return;
            
            var alphaColumn = outMatrix.GetColumn(3);
            alphaColumn = Vector3.Scale(alphaColumn, inChannels);
            hasAlpha = alphaColumn.sqrMagnitude > 0;
        }

        public static Texture2D GenerateTexture(int width, int height, Material mat, bool hasAlpha = true)
        {
            var previousActive = RenderTexture.active;
            RenderTexture tempRT = RenderTexture.GetTemporary(width, height);
            RenderTexture.active = tempRT;
            
            Graphics.Blit(Texture2D.blackTexture, tempRT, mat);

            Texture2D output = new Texture2D(tempRT.width, tempRT.height, hasAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);

            output.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
            output.Apply();
            output.filterMode = FilterMode.Bilinear;

            RenderTexture.ReleaseTemporary(tempRT);
            RenderTexture.active = previousActive;

            return output;
        }
    }
}