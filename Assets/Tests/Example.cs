using System;
using System.Collections.Generic;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Collections;
using Rhinox.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class GenericList : ToggleableList<GameObject>
{
    
}

[SmartFallbackDrawn(false)]
public class Example : MonoBehaviour
{
    public UnityEvent EventUnity;
    public BetterEvent StartEvent;
    public SerializableType Type;
    public SerializableType Type2;
    public SerializableGuid Guid;
    public SceneReferenceData SceneTest;
    public readonly string[] Options = new[] { "One", "Two", "FortyTwo" };

    [Serializable]
    public class StringWithOptions
    {
        [HideLabel, ValueDropdown("$parent.parent.Options")]
        public string IGotOptions;
        [ValueDropdown("$parent.parent.Options")]
        public string WithLabel;
    }

    public List<StringWithOptions> ListOfOptions;

    // Rotate a button 10 degrees clockwise when pressed.

    float rotAngle = 0;
    Vector2 pivotPoint;

    [TitleGroup("Foobar")] public float B;
    
    [SerializeReference]
    public ToggleableList<GameObject> Objects;
    
    [SerializeReference]
    public GenericList Objects2;

    [SerializeReference]
    public SimplePair<bool, bool> MyPair;

    private void Start()
    {
        StartEvent.AddListener(() => Debug.Log($"{name} Triggered From script"));
        StartEvent += () => Debug.Log($"{name} Triggered From script 2");
        StartEvent.Invoke();
    }

    void OnGUI()
    {
        GUILayout.Label("Test Before");
        var mat = GUI.matrix;
        GUIUtility.RotateAroundPivot(rotAngle, pivotPoint);
        if (GUILayout.Button("Rotate"))
        {
            rotAngle += 10;
        }
        GUI.matrix = mat;
        pivotPoint = GUILayoutUtility.GetLastRect().center;

        
        GUILayout.Label("Test After");
    }

    [Button]
    void PrintStoof()
    {
        Debug.Log("Stoof");
    }

    [Button]
    void DrawListOfOptions()
    {
        foreach (var item in ListOfOptions)
        {
            Debug.Log($"{item.IGotOptions} - {item.WithLabel}");
        }
    }
}