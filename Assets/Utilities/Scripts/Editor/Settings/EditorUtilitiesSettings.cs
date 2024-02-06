using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Utilities.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public class EditorUtilitiesSettings : CustomProjectSettings<EditorUtilitiesSettings>
    {
        public override string Name => "Editor Utility Settings";
        
        [Title("Scene/Hierarchy Helper")]
        [ToggleLeft] public bool ShowSelectionInfoInSceneOverlay = false;
        [ToggleLeft] public bool ShowSelectionInfoInHierarchy = false;
        [ToggleLeft] public bool OverrideFocusBehaviour = false;

        [Indent, ShowIf(nameof(OverrideFocusBehaviour))]
        public float DefaultBoundsSize = 1;

        [Title("Custom Inspectors")]
        [ToggleLeft] public bool OverrideTransformInspector = true;
        [ToggleLeft] public bool OverrideMeshFilterInspector = true;
        
        [Title("Hierarchy Icons")]
        [LabelWidth(250)]
        public bool EnableCustomIconsInHierarchy;
        
        [ListDrawerSettings, ShowIf(nameof(EnableCustomIconsInHierarchy))]
        public List<HierarchyIconSettingsEntry> Entries;
        
        [Button("Check for new icons")]
        protected void UpdateIcons()
        {
            if (Entries == null)
                Entries = new List<HierarchyIconSettingsEntry>();
            
            foreach (var kvp in CustomHierarchyDrawing.DefaultTypeIcons)
            {
                if (!kvp.Key.InheritsFrom(typeof(Component)) || kvp.Value == null)
                    continue;

                var defaultTypeEntry = Entries.FirstOrDefault(x => x.Type == kvp.Key);
                if (defaultTypeEntry != null)
                {
                    if (defaultTypeEntry.Icon != kvp.Value)
                        defaultTypeEntry.Icon = kvp.Value;
                    else
                        continue;
                }
                else
                {
                    var entry = new HierarchyIconSettingsEntry(kvp.Key, kvp.Value);
                    Entries.Add(entry);
                }
            }

            var types = TypeCache.GetTypesWithAttribute<CustomHierarchyIconAttribute>();
            foreach (Type type in types)
            {
                if (!type.InheritsFrom(typeof(Component)))
                    continue;
                
                var curTypeEntry = Entries.FirstOrDefault(x => x.Type == type);
                if (curTypeEntry != null)
                {
                    var attr = type.GetCustomAttribute<CustomHierarchyIconAttribute>();
                    var texture = attr.FindIcon();

                    if (texture == null)
                        continue;
                    
                    if (curTypeEntry.Icon != texture)
                    {
                        curTypeEntry.Icon = texture;
                    }
                    else
                        continue;
                }
                else
                {
                    var attr = type.GetCustomAttribute<CustomHierarchyIconAttribute>();
                    var texture = attr.FindIcon();
                    if (texture == null)
                        continue;
                    var entry = new HierarchyIconSettingsEntry(type, texture);
                    Entries.Add(entry);
                }
            }

        }

        [Button("Reset custom icons")]
        protected void LoadCustomIconDefaultsButton()
        {
            if (EditorUtility.DisplayDialog("Confirmation", "Do you wish to reset the custom icon configuration?",
                    "Confirm"))
            {
                LoadCustomIconDefaults();
            }
        }
        
        protected void LoadCustomIconDefaults()
        {
            Entries = new List<HierarchyIconSettingsEntry>();
            foreach (var kvp in CustomHierarchyDrawing.DefaultTypeIcons)
            {
                if (!kvp.Key.InheritsFrom(typeof(Component)) || kvp.Value == null)
                    continue;
                var entry = new HierarchyIconSettingsEntry(kvp.Key, kvp.Value);
                Entries.Add(entry);
            }

            var types = TypeCache.GetTypesWithAttribute<CustomHierarchyIconAttribute>();
            foreach (Type type in types)
            {
                if (!type.InheritsFrom(typeof(Component)))
                    continue;
                var attr = type.GetCustomAttribute<CustomHierarchyIconAttribute>();
                var texture = attr.FindIcon();
                if (texture == null)
                    continue;
                var entry = new HierarchyIconSettingsEntry(type, texture);
                Entries.Add(entry);
            }

        }

        protected override void LoadDefaults()
        {
            base.LoadDefaults();
            LoadCustomIconDefaults();
        }
    }
}