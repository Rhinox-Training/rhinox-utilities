using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Odin.Editor
{
    [Serializable]
    public class ShaderFilter : BaseAdvancedSearchSearchFilter
    {
        private static PropertyInfo _chachedObjRefValueMethod;

        private static PropertyInfo objRefValueMethod => _chachedObjRefValueMethod ?? (_chachedObjRefValueMethod =
            typeof(SerializedProperty).GetProperty("objectReferenceStringValue",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));


        [ShowInInspector, ValueDropdown(nameof(GetShaderOptions)), InlineButton(nameof(Reset), "X")]
        [DisableContextMenu, OnValueChanged(nameof(TriggerChanged))]
        private string _shader = string.Empty;

        private const string MissingShader = "Missing";

        private MenuCommand _mc;
        private Rect _buttonPos;

        public ShaderFilter() : base("Shader")
        {
        }

        private IEnumerable<ValueDropdownItem<string>> GetShaderOptions()
        {
            yield return new ValueDropdownItem<string>("Any", string.Empty);
            yield return new ValueDropdownItem<string>("Null", null);
            yield return new ValueDropdownItem<string>(MissingShader, MissingShader);

            foreach (var info in ShaderUtil.GetAllShaderInfo())
            {
                if (info.name.ToLower().StartsWith("hidden"))
                    continue;
                yield return new ValueDropdownItem<string>(info.name, info.name);;
            }
        }

        public override void Reset()
        {
            _shader = string.Empty;

            base.Reset();
        }

        public override ICollection<GameObject> ApplyFilter(ICollection<GameObject> selectedObjs)
        {
            if (_shader == string.Empty)
                return selectedObjs;

            selectedObjs.RemoveAll(obj => !HasMaterialUsingShader(obj));
            return selectedObjs;
        }

        private bool HasMaterialUsingShader(GameObject obj)
        {
            var renderer = obj.GetComponent<Renderer>();

            if (renderer == null)
                return false;
            
            if (_shader == MissingShader)
                return IsMissingMaterial(renderer);
            
            if (_shader == null)
                return renderer.sharedMaterials.Any(x => x == null);

            return renderer.sharedMaterials.Any(x => x != null && x.shader.name == _shader);

        }

        private bool IsMissingMaterial(Renderer r)
        {
            SerializedObject so = new SerializedObject(r);

            var spArr = so.FindProperty("m_Materials");

            if (spArr == null)
                return false;

            bool missing = false;

            for (int i = 0; i < spArr.arraySize; ++i)
            {
                var sp = spArr.GetArrayElementAtIndex(i);

                if (IsPropertyMissing(sp))
                {
                    missing = true;
                    break;
                }
                
                if (sp.objectReferenceValue == null)
                    continue;

                var propSo = new SerializedObject(sp.objectReferenceValue);
                sp = propSo.FindProperty("m_Shader");

                missing = IsPropertyMissing(sp);
                propSo.Dispose();

                if (missing)
                    break;
            }

            so.Dispose();

            return missing;
        }

        private static bool IsPropertyMissing(SerializedProperty sp)
        {
            string objectReferenceStringValue = string.Empty;

            if (objRefValueMethod != null)
            {
                objectReferenceStringValue = (string)objRefValueMethod.GetGetMethod(true).Invoke(sp, new object[] { });
            }

            if (sp.objectReferenceValue == null
                && (sp.objectReferenceInstanceIDValue != 0 || objectReferenceStringValue.StartsWith("Missing")))
            {
                return true;
            }

            return false;
        }


        public override string GetFilterInfo()
        {
            if (string.IsNullOrWhiteSpace(_shader)) return string.Empty;

            return $"A material of the type '{_shader}'";
        }
    }
}