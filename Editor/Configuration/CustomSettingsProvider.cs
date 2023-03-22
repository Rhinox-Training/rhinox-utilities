using System;
using System.Collections.Generic;
using System.Reflection;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Utilities.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace Rhinox.Utilities.Editor.Configuration
{
    internal class CustomSettingsProvider : SettingsProvider
    {
        private CustomProjectSettings _settingsInstance;
        private IProjectSettingsDrawer _drawer;

        public CustomSettingsProvider(CustomProjectSettings instance, SettingsScope scope = SettingsScope.User)
            : base($"Project/Custom/{instance.Name}", scope)
        {
            _settingsInstance = instance;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _drawer = FindDrawer(_settingsInstance);

            if (_drawer != null)
            {
                guiHandler = _drawer.OnCustomGUI;
                activateHandler = _drawer.OnActivate;
            }

            base.OnActivate(searchContext, rootElement);
        }

        public IProjectSettingsDrawer FindDrawer(CustomProjectSettings instance)
        {
            var types = AppDomain.CurrentDomain.GetDefinedTypesOfType<IProjectSettingsDrawer>();
            Type applicableType = typeof(CustomProjectSettings);
            Type targetDrawerType = null;
            Type instanceType = instance.GetType();
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<ProjectSettingsDrawerAttribute>();
                if (attr == null || attr.SettingsType == null)
                    continue;

                if (!instanceType.InheritsFrom(attr.SettingsType))
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
            
            drawerInstance.LoadTarget(instance);
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
            var provider = new CustomSettingsProvider(instance, SettingsScope.Project);

            // Automatically extract all keywords from the Styles.
            provider.keywords = instance.GetKeywords();
            return provider;
        }
    }
}