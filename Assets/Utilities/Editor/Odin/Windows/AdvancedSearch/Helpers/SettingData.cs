using System;
using Rhinox.GUIUtils.Editor;
using Rhinox.Utilities.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class SettingData
{
    public string PrefsKey;
    public string SettingName;
    public Texture SettingIcon;

    private const string SETTINGS_KEY = "AdvancedSceneSearch";
    private bool _state;

    public event Action Changed; 

    public SettingData(string settingName, string prefsKey, bool defaultValue, string icon = null)
    {
        SettingName = settingName;
        SettingIcon = string.IsNullOrWhiteSpace(icon) ? null : UnityIcon.AssetIcon(icon);
        PrefsKey = prefsKey;
        _state = EditorPrefs.GetBool(SETTINGS_KEY + PrefsKey, defaultValue);
    }

    public bool State
    {
        get { return _state; }
        set
        {
            if (_state == value) return;
            
            EditorPrefs.SetBool(SETTINGS_KEY + PrefsKey, value);
            _state = value;
            Changed?.Invoke();
        }
    }

    public void Draw()
    {
        using (new eUtility.GuiColor(State ? Color.white : Color.gray))
        {
            if (SettingIcon)
                State = GUILayout.Toggle(State, new GUIContent(SettingIcon, tooltip: SettingName),  SirenixGUIStyles.Label, GUILayoutOptions.Height(16).Width(20));
            else
                State = GUILayout.Toggle(State, SettingName,  SirenixGUIStyles.Label);
               
        }
    }
}