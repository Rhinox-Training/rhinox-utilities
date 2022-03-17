﻿using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AutohookAttribute))]
public class AutohookPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // First, lets attempt to find a valid component we could hook into this property
        var component = FindAutohookTarget(property);
        if (component != null)
        {
            // if we found something, AND the autohook is empty, lets slot it.
            // the reason were straight up looking for a target component is so we
            // can skip drawing the field if theres a valid autohook. 
            // this just looks a bit cleaner but isnt particularly safe. YMMV
            if (property.objectReferenceValue == null)
                property.objectReferenceValue = component;
            return;
        }
        
        // haven't found one? lets just draw the default property field, let the user manually
        // hook something in.
        EditorGUI.PropertyField(position, property, label);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // if theres a valid autohook target we skip drawing, so height is zeroed
        var component = FindAutohookTarget(property);
        if (component != null)
            return 0;
        
        // otherwise, return its default height (which should be the standard 16px unity usually uses)
        return base.GetPropertyHeight(property, label);
    }

    /// <summary>
    /// Takes a SerializedProperty and finds a local component that can be slotted into it.
    /// Local in this context means its a component attached to the same GameObject.
    /// This could easily be changed to use GetComponentInParent/GetComponentInChildren
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    protected virtual Component FindAutohookTarget(SerializedProperty property)
    {
        var root = property.serializedObject;

        if (root.targetObject is Component)
        {
            // first, lets find the type of component were trying to autohook...
            var type = GetTypeFromProperty(property);
            
            // ...then use GetComponent(type) to see if there is one on our object.
            var component = (Component)root.targetObject;
            return FindAutohookTarget(component, type);
        }
        
        Debug.LogError("Autohook Failed; target is not a component?");

        return null;
    }
    
    protected virtual Component FindAutohookTarget(Component target, Type type)
    {
        return target.GetComponent(type);
    }

    /// <summary>
    /// Uses reflection to get the type from a serialized property
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    protected static System.Type GetTypeFromProperty(SerializedProperty property)
    {
        // first, lets get the Type of component this serialized property is part of...
        var parentComponentType = property.serializedObject.targetObject.GetType();
        // ... then, using reflection well get the raw field info of the property this
        // SerializedProperty represents...
        var fieldInfo = parentComponentType.GetField(property.propertyPath, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        // ... using that we can return the raw .net type!
        return fieldInfo.FieldType;
    }
}

[CustomPropertyDrawer(typeof(AutohookFromChildrenAttribute))]
public class AutohookFromChildrenPropertyDrawer : AutohookPropertyDrawer
{
    protected override Component FindAutohookTarget(Component target, Type type)
    {
        return target.GetComponentInChildren(type);
    }
}

[CustomPropertyDrawer(typeof(AutohookFromParentAttribute))]
public class AutohookFromParentPropertyDrawer : AutohookPropertyDrawer
{
    protected override Component FindAutohookTarget(Component target, Type type)
    {
        return target.GetComponentInParent(type);
    }
}