using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Attributes;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEditorInternal;
using UnityEngine;

namespace Rhinox.Utilities.Odin.Editor
{
    [Serializable]
    public class TagFilter : BaseAdvancedSearchSearchFilter
    {
        [ShowInInspector, LabelText("Tags"), TagSelector]
        [CustomValueDrawer(nameof(DrawTagMask)), OnValueChanged(nameof(TriggerChanged))]
        private int _tagMask = ~0;

        private List<string> _tags = new List<string>();

        public TagFilter() : base("Tag")
        {
        }

        public override void Reset()
        {
            _tags.Clear();

            _tagMask = ~0;
            base.Reset();
        }

        private int DrawTagMask(int value, GUIContent label)
        {
            return eUtility.TagMaskField(label, value);
        }

        public override ICollection<GameObject> ApplyFilter(ICollection<GameObject> selectedObjs)
        {
            if (_tags.Count > 0)
                selectedObjs.RemoveAll(x => !_tags.Any(x.CompareTag));

            return selectedObjs;
        }

        public override string GetFilterInfo()
        {
            int allTags = (1 << InternalEditorUtility.tags.Length) - 1;
            // if mask is not set or encompasses all tags; skip
            if (_tagMask == ~0 || _tagMask == allTags)
            {
                _tags.Clear();
                return string.Empty;
            }

            eUtility.TaskMaskToTags(_tagMask, ref _tags);

            // if mask is 0; aka no tag; return that
            if (_tagMask == 0)
                return "No tag";

            // otherwise return info about what tags it filters
            string searchInfo = string.Empty;
            if (_tags.Count == 1)
                searchInfo += "A tag of: ";
            else if (_tags.Count > 1)
                searchInfo += "One of the following tags: ";

            if (_tagMask > 0)
                searchInfo += string.Join(", ", _tags);

            return searchInfo;
        }
    }
}