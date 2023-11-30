﻿using System;
using System.Collections.Generic;
using Rhinox.GUIUtils.Attributes;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Collections;
using Rhinox.Utilities;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class GenericList : ToggleableList<GameObject> {}

[Serializable]
public class GenericPairList : PairList<string, GameObject> {}

[Serializable]
public class GenericDictionary : SerializableDictionary<string, string>{}

public interface ITest {}

public class Test1 : ITest
{
    public string ThisIsTest1;
}

public class Test2 : ITest
{
    public string Also;
    public bool ThisIsTest2;
}

public class Test3 : ITest
{
    public float ThisIsTest3;
}

[SmartFallbackDrawn(false)]
public class Example : MonoBehaviour
{
    [SerializeReference] public GenericDictionary TestDictionary = new GenericDictionary();
    
    public UnityEvent EventUnity;
    public BetterEvent StartEvent;
    public SerializableType Type;
    public SerializableType Type2;
    public SerializableGuid Guid;
    public SceneReferenceData SceneTest;
    public readonly string[] Options = new[] { "One", "Two", "FortyTwo" };

    public GameObject Object;
    
    [Serializable]
    public class StringWithOptions
    {
        [HideLabel, ValueDropdown("$parent.parent.Options")]
        public string IGotOptions;
        [ValueDropdown("$parent.parent.Options")]
        public string WithLabel;
    }

    [SerializeReference]
    public List<ITest> GenericList;

    public GenericPairList TestPairList = new GenericPairList();

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
        GUILayout.Label("Test Before" /*+ EditorApplication.applicationPath*/);
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