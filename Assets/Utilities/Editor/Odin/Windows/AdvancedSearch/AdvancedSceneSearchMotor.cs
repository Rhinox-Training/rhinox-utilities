using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Utilities;
using Rhinox.Utilities.Editor;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Odin.Editor
{
    [HideReferenceObjectPicker, HideLabel]
    public class AdvancedSceneSearchMotor
    {
        public BaseAdvancedSearchSearchFilter[] Filters => new BaseAdvancedSearchSearchFilter[]
        {
            NameFilter, TagFilter, LayerFilter, ShaderFilter, ComponentFilter
        };

        [LabelWidth(60), ShowIf("@IsActive(NameFilter)"), HideLabel]
        public NameFilter NameFilter;

        [LabelWidth(60), ShowIf("@IsActive(TagFilter)"), HideLabel, HorizontalGroup("Masks")]
        public TagFilter TagFilter;

        [LabelWidth(60), ShowIf("@IsActive(LayerFilter)"), HideLabel, HorizontalGroup("Masks")]
        public LayerFilter LayerFilter;

        [LabelWidth(60), ShowIf("@IsActive(ShaderFilter)"), HideLabel]
        public ShaderFilter ShaderFilter;

        [ShowIf("@IsActive(ComponentFilter)"), HideLabel]
        public ComponentFilter ComponentFilter;

        public ICollection<GameObject> Results;
        private string _matchesInfo;
        private string _searchInfo;
        private string _filterInfo;

        private static GUIStyle _wrappedLabelStyle;

        public static GUIStyle WrappedLabelStyle => _wrappedLabelStyle ?? (_wrappedLabelStyle =
            new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                wordWrap = true
            });

        public event Action<AdvancedSceneSearchMotor> Changed;
        public event Action ResultsChanged;

        public AdvancedSceneSearchMotor()
        {
            Init();
            foreach (var filter in Filters)
                filter.Changed += OnFilterChange;
        }

        private void OnFilterChange(BaseAdvancedSearchSearchFilter filter)
        {
            TriggerChanged();
        }

        private void TriggerChanged()
        {
            Changed?.Invoke(this);
        }

        private bool IsActive(BaseAdvancedSearchSearchFilter filter)
        {
            return filter != null && filter.Enabled;
        }

        public void Init()
        {
            ComponentFilter = new ComponentFilter();
            NameFilter = new NameFilter();
            TagFilter = new TagFilter();
            LayerFilter = new LayerFilter();
            ShaderFilter = new ShaderFilter();
        }

        public void HandleDragged(Object[] objectReferences)
        {
            foreach (var obj in objectReferences)
            {
                foreach (var filter in Filters)
                    filter.HandleDragged(obj);
            }

            TriggerChanged();
        }

        public void Reset()
        {
            Init();
            TriggerChanged();
        }

        public void DrawInfo(bool includeDisabled, bool onlyInSelection)
        {
            // only during layout can we change strings (without giving errors)
            if (Event.current.type == EventType.Layout)
            {
                // match info
                if (Results != null)
                    _matchesInfo = $"Matches found: {Results.Count}";

                // search info
                _searchInfo = $"You are searching for";
                if (includeDisabled) _searchInfo += " enabled";
                _searchInfo += " GameObjects";
                if (onlyInSelection && Selection.gameObjects.Any()) _searchInfo += " within the currect selection";
                _searchInfo += " with:";

                // filter info
                _filterInfo = string.Empty;
                foreach (var filter in Filters)
                {
                    var info = filter.GetFilterInfo();
                    if (string.IsNullOrWhiteSpace(info)) continue;

                    if (!string.IsNullOrWhiteSpace(_filterInfo)) _filterInfo += Environment.NewLine;
                    _filterInfo += info;
                }
            }

            GUILayout.Label(_matchesInfo, SirenixGUIStyles.BoldLabelCentered);
            GUILayout.Label(_searchInfo, WrappedLabelStyle);
            GUILayout.Label(_filterInfo, WrappedLabelStyle);
        }

        public void Update(ICollection<GameObject> objs)
        {
            objs = objs.ToList();

            foreach (var filter in Filters)
            {
                if (filter.Enabled)
                    objs = filter.ApplyFilter(objs);
            }

            Results = objs;
            ResultsChanged?.Invoke();
        }
    }
}