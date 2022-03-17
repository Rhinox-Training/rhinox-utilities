using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rhinox.Lightspeed;
using Rhinox.Utilities;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class ShaderFilter : BaseAdvancedSearchSearchFilter
{
    private static PropertyInfo _chachedObjRefValueMethod;
    private static PropertyInfo objRefValueMethod => _chachedObjRefValueMethod ?? (_chachedObjRefValueMethod = typeof(SerializedProperty).GetProperty("objectReferenceStringValue",BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

    
    [ShowInInspector, CustomValueDrawer(nameof(DrawShaderOptions)), InlineButton(nameof(Reset), "X")]
    [DisableContextMenu, OnValueChanged(nameof(TriggerChanged))]
    private string _shader = string.Empty;

    private const string MissingShader = "Missing";

    private MenuCommand _mc;
    private Rect _buttonPos;

    public ShaderFilter() : base("Shader") { }

    private string DrawShaderOptions(string value, GUIContent label)
    {
        var options = ShaderUtil.GetAllShaderInfo()
            .Where(x => !x.name.ToLower().StartsWith("hidden"))
            .Select(x => x.name)
            .ToList();
        
        options.Insert(0, MissingShader);
        
        return SirenixEditorFields.Dropdown(label, _shader, options);
    }

    public override void Reset()
    {
        _shader = string.Empty;

        base.Reset();
    }

    public override ICollection<GameObject> ApplyFilter(ICollection<GameObject> selectedObjs)
    {
        if (string.IsNullOrWhiteSpace(_shader))
            return selectedObjs;

        selectedObjs.RemoveAll(obj => !HasMaterialUsingShader(obj));
        return selectedObjs;
    }

    private bool HasMaterialUsingShader(GameObject obj)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (_shader == MissingShader)
        {
            //(!(x is null) && x == null) || (x != null && x.shader == null)
            //renderer.sharedMaterials.Any(x => IsMissing(x))
            return renderer != null && IsMissingMaterial(renderer);
        }

        return renderer != null && renderer.sharedMaterials.Any(x => x != null && x.shader.name == _shader);

    }

    private bool IsMissingMaterial(Renderer r)
    {
        SerializedObject so = new SerializedObject(r);
        
        var spArr = so.FindProperty("m_Materials");

        if (spArr == null)
            return false;

        for (int i = 0; i < spArr.arraySize; ++i)
        {
            var sp = spArr.GetArrayElementAtIndex(i);
            
            if (IsPropertyMissing(sp))
                return true;

            var propSo = new SerializedObject(sp.objectReferenceValue);
            sp =  propSo.FindProperty("m_Shader");

            if (IsPropertyMissing(sp))
            {
                propSo.Dispose();
                return true;
            }
            propSo.Dispose();
        }
       
        so.Dispose();

        return false;
    }

    private static bool IsPropertyMissing(SerializedProperty sp)
    {
        string objectReferenceStringValue = string.Empty;

        if (objRefValueMethod != null)
        {
            objectReferenceStringValue = (string) objRefValueMethod.GetGetMethod(true).Invoke(sp, new object[] { });
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