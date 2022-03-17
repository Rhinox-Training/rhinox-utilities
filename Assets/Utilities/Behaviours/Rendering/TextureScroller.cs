using Rhinox.Lightspeed;
using UnityEngine;

namespace Rhinox.Utilities
{
    [RefactoringOldNamespace("")]
    [RequireComponent(typeof(Renderer))]
    public class TextureScroller : MonoBehaviour
    {
        public Vector2 ScrollSpeed = new Vector2(1, 0);

        //Texture _texture;
        Material _material;

        void Start()
        {
            _material = GetComponent<Renderer>().sharedMaterial;
            if (_material == null)
                enabled = false;
            //_texture = _material.GetTexture(0);
        }

        void Update()
        {
            var offset = ScrollSpeed * Time.time;
            _material.SetTextureOffset("_MainTex", offset);
        }

        private void OnDestroy()
        {
            if (_material)
                _material.SetTextureOffset("_MainTex", Vector2.zero);
        }
    }
}
