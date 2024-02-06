using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;

namespace Rhinox.Utilities.Editor
{
    public class SelectionHistoryWindow : CustomEditorWindow
    {
        private Rect _rect;
        private Vector2 _scrollPosition;
        
        private Texture _pinIcon;
        private GUIStyle _prefixStyle;
        private GUIStyle _prefixStyleSelected;
        private GUIStyle _defaultStyle;
        private GUIStyle _defaultStyleSelected;

        private SelectionHistory.ResolvedSelectionData[] _pinned;
        private SelectionHistory.ResolvedSelectionData[] _data;

        
        [MenuItem(WindowHelper.ToolsPrefix + "Selection History", false, 201)]
        internal static void ShowWindow()
        {
            var editorWindow = GetWindow(typeof(SelectionHistoryWindow));
            editorWindow.autoRepaintOnSceneChange = true;
            editorWindow.titleContent.text = "Selection History";
            editorWindow.Show();
        }

        private void Init()
        {
            if (_pinIcon == null)
                _pinIcon = UnityIcon.AssetIcon("Fa_Thumbtack").Pad(15); // texture is kinda large so 15 padding is needed

            if (_prefixStyle == null)
                _prefixStyle = CustomGUIStyles.LabelRight;
            
            if (_prefixStyleSelected == null)
                _prefixStyleSelected = CustomGUIStyles.BoldLabelRight;
            
            if (_defaultStyle == null)
                _defaultStyle = CustomGUIStyles.Label;
            
            if (_defaultStyleSelected == null)
                _defaultStyleSelected = CustomGUIStyles.BoldLabel;
        }

        protected override void OnGUI()
        {
#if !ENABLE_INPUT_SYSTEM
            EditorGUILayout.HelpBox("Enable the new InputSystem to be use the back/forward mouse buttons to go to previous and next selection.", MessageType.Warning);
#endif
            
                    
            Init();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, CustomGUIStyles.Box);
            _pinned = SelectionHistory.Pinned;
            _data = SelectionHistory.Data;
            
            var height = EditorGUIUtility.singleLineHeight;
            var rect = _rect.AlignTop(height);
            rect.y += 2;
            
            // Draw all pinned ones first
            for (var i = 0; i < _pinned.Length; i++)
            {
                var selectionI = SelectionHistory.IsPinInSelection(i);
                DrawData(rect, selectionI, _pinned[i], i);
                
                // Check if we clicked the item, if so, go to this selection
                if (eUtility.IsClicked(rect))
                    SelectionHistory.GoToPin(i);
                
                rect = rect.MoveDownLine(padding: 0);
            }
            
            // Draw regular ones
            for (var i = _data.Length - 1; i >= 0; i--)
            {
                var pinI = SelectionHistory.GetPinIndex(i);

                if (pinI >= 0) // We've already handled this.
                    continue;

                var selection = _data[i];
                DrawData(rect, i, selection, pinI);
                
                // Check if we clicked the item, if so, go to this selection
                if (eUtility.IsClicked(rect))
                    SelectionHistory.GoTo(i);
                
                rect = rect.MoveDownLine(padding: 0);
            }
            
            GUILayout.EndScrollView();
            
            var totalRect = GUILayoutUtility.GetLastRect();
            if (totalRect.IsValid())
                _rect = totalRect;
            
            if (GUILayout.Button("RESET"))
                SelectionHistory.Reset();
        }

        private void DrawData(Rect rect, int i, SelectionHistory.ResolvedSelectionData selection, int pinI)
        {
            bool pinned = pinI >= 0;
            bool selected = i == SelectionHistory.CurrentIndex;

            // if it's our current selection, draw a box around it
            if (selected)
                EditorGUI.DrawRect(rect, CustomGUIStyles.DarkEditorBackground);

            string prefix = i >= 0 ? $"{i} : " : "-";
            
            GUILayout.BeginHorizontal();
            string action = pinned ? "Unpin" : "Pin";
            using (new eUtility.GuiColor(pinned ? Color.white : Color.grey))
            {
                if (CustomEditorGUI.IconButton(_pinIcon, tooltip: $"{action} this selection", width: 18))
                {
                    if (pinned)
                        SelectionHistory.Unpin(pinI);
                    else
                        SelectionHistory.Pin(i);
                }
            }
            
            GUILayout.Label(prefix, selected ? _prefixStyleSelected : _prefixStyle, GUILayout.MaxWidth(25));
            GUILayout.Label(GetDescription(selection.Objects), selected ? _defaultStyleSelected : _defaultStyle);
            GUILayout.EndHorizontal();
        }

        private string GetDescription(Object[] objects)
        {
            if (objects.IsNullOrEmpty())
                return $"No Selection";
            var text = GetDescription(objects[0]);
            if (objects.Length > 1)
                text += $"+ {objects.Length - 1} others";
            return text;
        }

        private string GetDescription(Object o)
        {
            const string AssetLabel = "Asset";
            const string SceneLabel = "Scene";
            if (o is GameObject go)
                return $"[{(go.activeInHierarchy ? SceneLabel : AssetLabel)}] {o.name}";
            
            // TODO: Probably an asset? not sure
            return $"[Asset] {o.name}";
        }
    }
}