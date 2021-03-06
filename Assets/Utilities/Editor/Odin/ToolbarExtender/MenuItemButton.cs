using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Odin.Editor
{
    public class MenuItemButton : BaseToolbarIconButton
    {
        [SerializeField, ValueDropdown(nameof(GetIconOptions)), OnValueChanged(nameof(ResolveIcon))]
        private string _icon;
        
        static Type EditorIconType => typeof(EditorIcons);

        private static PropertyInfo[] _cachedProperties;

        private EditorIcon _resolvedIcon;
        protected override EditorIcon Icon => _resolvedIcon;

        public string MenuItem;

        protected override void Execute()
        {
            EditorApplication.ExecuteMenuItem(MenuItem);
        }

        private IEnumerable<ValueDropdownItem<string>> GetIconOptions()
        {
            foreach (PropertyInfo iconProp in FetchIconProperties())
                yield return new ValueDropdownItem<string>(iconProp.Name, iconProp.Name);
        }

        private void ResolveIcon()
        {
            var icon = FetchIconProperties().FirstOrDefault(x => x.Name == _icon);
            
            if (icon == null)
                _resolvedIcon = EditorIcons.Info;
            else
                _resolvedIcon = (EditorIcon) icon.GetGetMethod().Invoke(null, null);
        }

        private static PropertyInfo[] FetchIconProperties()
        {
            if (_cachedProperties != null)
                return _cachedProperties;

            return _cachedProperties = EditorIconType.GetProperties(Flags.StaticPublic)
                .Where(x => typeof(EditorIcon).IsAssignableFrom(x.GetReturnType()) )
                .OrderBy(x => x.Name)
                .ToArray();
        }
    }
}