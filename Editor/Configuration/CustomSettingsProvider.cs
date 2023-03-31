using System;
using System.Collections.Generic;
using System.Reflection;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Utilities.Attributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Rhinox.Utilities.Editor.Configuration
{
    internal class CustomSettingsProvider : SettingsProvider
    {
        private IProjectSettingsDrawer _drawer;
        private readonly Type _projectSettingsType;
        private CustomProjectSettings _activatedInstance;

        private CustomSettingsProvider(Type projectSettingsType, SettingsScope scope, string name, string collection = null) 
            : base(GetPath(collection, name), scope)
        {
            if (!projectSettingsType.InheritsFrom(typeof(CustomProjectSettings)))
                throw new ArgumentException(nameof(projectSettingsType));
            _projectSettingsType = projectSettingsType;
        }

        private static string GetPath(string collection, string name)
        {
            if (collection == null)
                collection = "Custom";
            if (collection == string.Empty)
                return $"Project/{name}";
            return $"Project/{collection}/{name}";
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            // Create new drawer instance
            if (_drawer == null)
            {
                _drawer = FindDrawer(_projectSettingsType);
                guiHandler = _drawer.OnCustomGUI;
                activateHandler = _drawer.OnActivate;
            }
            UpdateSettingsInstanceIfNeeded();
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            UpdateSettingsInstanceIfNeeded();
        }

        private void UpdateSettingsInstanceIfNeeded()
        {
            var instance = ProjectSettingsHelper.FindProjectSettings(_projectSettingsType);
            if (_drawer.LoadTarget(instance))
                keywords = instance.GetKeywords();
        }

        private static IProjectSettingsDrawer FindDrawer(Type projectSettingsType)
        {
            var types = AppDomain.CurrentDomain.GetDefinedTypesOfType<IProjectSettingsDrawer>();
            Type applicableType = typeof(CustomProjectSettings);
            Type targetDrawerType = null;
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<ProjectSettingsDrawerAttribute>();
                if (attr == null || attr.SettingsType == null)
                    continue;

                if (!projectSettingsType.InheritsFrom(attr.SettingsType))
                    continue;

                if (attr.SettingsType != applicableType && attr.SettingsType.InheritsFrom(applicableType))
                {
                    applicableType = attr.SettingsType;
                    targetDrawerType = type;
                }
            }

            IProjectSettingsDrawer drawerInstance = null;
            if (targetDrawerType != null)
                drawerInstance = Activator.CreateInstance(targetDrawerType) as IProjectSettingsDrawer;
            else
                drawerInstance = new ProjectSettingsDrawer();
            
            return drawerInstance;
        }
        
        
        [SettingsProviderGroup]
        private static SettingsProvider[] CreateSettingsProvidersForInstances()
        {
            var settingsProviders = new List<SettingsProvider>();
            foreach (var customProjectSettings in ProjectSettingsHelper.EnumerateProjectSettings())
            {
                var settingsProvider = CreateProvider(customProjectSettings);
                if (settingsProvider == null)
                    continue;
                settingsProviders.Add(settingsProvider);
            }
            
            return settingsProviders.ToArray();
        }

        private static SettingsProvider CreateProvider(CustomProjectSettings instance)
        {
            var attr = instance.GetType().GetCustomAttribute<CustomProjectSettingsAttribute>();
            
            var provider = new CustomSettingsProvider(instance.GetType(), SettingsScope.Project, instance.Name, attr?.CustomCollection);
            return provider;
        }
    }
}