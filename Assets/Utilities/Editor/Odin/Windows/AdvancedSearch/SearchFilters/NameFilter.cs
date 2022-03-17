using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class NameFilter : BaseAdvancedSearchSearchFilter
{
    [ShowInInspector, CustomValueDrawer(nameof(DrawName)), HorizontalGroup]
    [OnValueChanged(nameof(TriggerChanged))]
    private string _name;
    
    private SettingData CaseSensitive; 
    private SettingData MatchWholeWord;
    private SettingData UseRegex;

    private RegexOptions _regexOpts = RegexOptions.None;

    private bool _reselectInput; // Do we need to reselect the search box?

    public NameFilter() : base("Name")
    {
        CaseSensitive = new SettingData("Match Case", "case_sens", false, icon: "FontCase");
        MatchWholeWord = new SettingData("Match Whole Word", "match_whole", false, icon: "Fa_Crosshairs");
        UseRegex = new SettingData("Use Regex", "use_regex", false, icon: "Regex");

        CaseSensitive.Changed += TriggerChanged;
        MatchWholeWord.Changed += TriggerChanged;
        UseRegex.Changed += TriggerChanged;
    }

    [OnInspectorGUI, HorizontalGroup(width: 60)]
    private void DrawSettings()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            CaseSensitive.Draw();
            MatchWholeWord.Draw();
            UseRegex.Draw();
        }
    }
    
    private string DrawName(string value, GUIContent label)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.SetNextControlName("nameSearchy");
        
            _name = EditorGUILayout.TextField(label, _name, SirenixGUIStyles.ToolbarSearchTextField);

            if (_reselectInput)
            {
                GUI.FocusControl("nameSearchy");
                _reselectInput = false;
            }

            if (GUILayout.Button("X", SirenixGUIStyles.ToolbarSearchCancelButton, GUILayout.Width(20f)))
            {
                GUI.FocusControl("nameSearchy");
                _name = "";
                _reselectInput = true;
                GUIUtility.keyboardControl = 0;
            }
        }
        return _name;
    }

    public override void HandleDragged(Object draggedObject)
    {
        if (!(draggedObject is GameObject)) return;
        
        if (string.IsNullOrEmpty(_name))
            _name = draggedObject.name;

        base.HandleDragged(draggedObject);
    }

    public override void Reset()
    {
        _name = "";
        base.Reset();
    }

    public override ICollection<GameObject> ApplyFilter(ICollection<GameObject> selectedObjs)
    {
        if (string.IsNullOrEmpty(_name)) return selectedObjs;
        
        Regex search;

        _regexOpts = CaseSensitive.State ? RegexOptions.None : RegexOptions.IgnoreCase;

        if (UseRegex.State)
        {
            var pattern = _name;
            if (MatchWholeWord.State)
            {
                if (!pattern.StartsWith("^")) pattern = "^" + pattern;
                if (!pattern.EndsWith("$")) pattern += "$";
            } 
                
            search = new Regex(pattern, _regexOpts);
            
        }
        else
        {
            if (MatchWholeWord.State)
                search = new Regex(WildcardToRegex(_name), _regexOpts);
            else
                search = new Regex(WildcardToRegex($"*{_name}*"), _regexOpts);
        }

        foreach (var obj in selectedObjs.ToArray())
        {
            if (!search.IsMatch(obj.name))
                selectedObjs.Remove(obj);
        }

        return selectedObjs;
    }

    public override string GetFilterInfo()
    {
        if (string.IsNullOrEmpty(_name)) return string.Empty;
        
        string filterText = string.Empty;

        if (UseRegex.State)
            filterText += $"A name that matches the following regex: \"{_name}\"";
        else if (MatchWholeWord.State)
            filterText += $"A name exactly equal to \"{_name}\"";
        else
            filterText += $"A name containing \"{_name}\"";

        if (CaseSensitive.State)
            filterText += " (Case Sensitive)";

        return filterText;
    }

    public static string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
    }
}