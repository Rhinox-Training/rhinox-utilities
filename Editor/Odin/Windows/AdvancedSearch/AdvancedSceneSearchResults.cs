using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
using RectExtensions = Rhinox.Lightspeed.RectExtensions;
using SelectionChangedType = Rhinox.GUIUtils.Editor.SelectionChangedType;

namespace Rhinox.Utilities.Odin.Editor
{
    public class AdvancedSceneSearchResults
    {
        private AdvancedSceneSearchMotor _motor;

        private float _menuTreeWidth;
        private float _overviewHeight;

        private bool _overviewToggle;

        private readonly List<ResizableColumn> _columns;
        private CustomMenuTree _menuTree;

        private static readonly TimeSpan _maxTimeBetweenClicks = TimeSpan.FromSeconds(.5f);
        private DateTime _lastClick;
        private UIMenuItem _clickedItem;

        private GameObject _selectedObject;
        private readonly List<UnityEditor.Editor> _selectedObjectEditors = new List<UnityEditor.Editor>();
        private Vector2 _selectedObjectScrollPosition;

        public AdvancedSceneSearchResults(AdvancedSceneSearchMotor motor)
        {
            _motor = motor;

            _overviewToggle = true;
            _menuTreeWidth = 200;
            _overviewHeight = 300;

            _columns = new List<ResizableColumn>
            {
                ResizableColumn.FlexibleColumn(_menuTreeWidth, 80),
                ResizableColumn.DynamicColumn(0, 200)
            };

            BuildTree();

            _motor.ResultsChanged += BuildTree;
        }

        private void BuildTree()
        {
            _menuTree = new CustomMenuTree(new OdinMenuTree
            {
                Config =
                {
                    DrawSearchToolbar = true, AutoHandleKeyboardNavigation = true, UseCachedExpandedStates = false
                }
            });

            foreach (var item in _motor.Results)
                // can't have slashes as it will add depth to the tree
                _menuTree.Add(item.name.Replace("/", "_"), item);

            _menuTree.SelectionConfirmed += () =>
            {
                var sel = _menuTree.Selection.FirstOrDefault()?.RawValue as Object;
                if (sel == null) return;

                CustomEditorGUIUtility.SelectObject(sel);
            };

            _menuTree.SelectionChanged += (x) =>
            {
                if (x != SelectionChangedType.ItemAdded)
                {
                    if (!_menuTree.Selection.Any())
                        ClearEditors();
                    return;
                }

                var value = _menuTree.SelectedValue;
                var result = value as GameObject;

                ClearEditors();

                _selectedObject = result;
                foreach (var comp in result.GetComponents<Component>())
                    _selectedObjectEditors.Add(UnityEditor.Editor.CreateEditor(comp));
            };
        }

        private void ClearEditors()
        {
            foreach (var e in _selectedObjectEditors)
                Object.DestroyImmediate(e);
            _selectedObjectEditors.Clear();
        }

        [OnInspectorGUI]
        public void Draw()
        {
            if (Event.current.type == EventType.Repaint && Input.GetMouseButtonDown(0))
                HandleMouseClick();

            _menuTreeWidth = _columns[0].ColWidth;

            // Bottom Slide Toggle Bits:
            var overviewSlideRect = new Rect();
            var toggleOverviewBtnRect = new Rect();

            // Menu editor
            Rect topRect;
            using (new eUtility.HorizontalGroup(GUILayout.ExpandHeight(true)))
            {
                topRect = GUIHelper.GetCurrentLayoutRect();
                GUITableUtilities.ResizeColumns(topRect, this._columns);

                // Bottom Slide Toggle Bits:
                // The bottom slide-rect toggle needs to be drawn above, but is placed below.
                overviewSlideRect = topRect.AddY(4).AlignBottom(4);
                overviewSlideRect.width += 4;
                toggleOverviewBtnRect = overviewSlideRect.AlignBottom(14).AlignCenter(100);
                EditorGUIUtility.AddCursorRect(toggleOverviewBtnRect, MouseCursor.Arrow);
                if (SirenixEditorGUI.IconButton(toggleOverviewBtnRect.AddY(-2),
                    this._overviewToggle ? EditorIcons.TriangleDown : EditorIcons.TriangleUp))
                {
                    this._overviewToggle = !this._overviewToggle;
                }

                if (this._overviewToggle)
                {
                    this._overviewHeight -= SirenixEditorGUI
                        .SlideRect(overviewSlideRect.SetXMax(toggleOverviewBtnRect.xMin), MouseCursor.SplitResizeUpDown)
                        .y;
                    this._overviewHeight -= SirenixEditorGUI
                        .SlideRect(overviewSlideRect.SetXMin(toggleOverviewBtnRect.xMax), MouseCursor.SplitResizeUpDown)
                        .y;
                }

                // Left menu tree
                GUILayout.BeginVertical(GUILayoutOptions.Width(this._columns[0].ColWidth).ExpandHeight());
                {
                    EditorGUI.DrawRect(GUIHelper.GetCurrentLayoutRect(), SirenixGUIStyles.EditorWindowBackgroundColor);
                    _menuTree.Draw();
                }
                GUILayout.EndVertical();

                // Draw selected
                GUILayout.BeginVertical();
                {
                    DrawTopBarButtons();
                    DrawSelectedObject();
                }
                GUILayout.EndVertical();
                GUITableUtilities.DrawColumnHeaderSeperators(topRect, this._columns, SirenixGUIStyles.BorderColor);
            }

            // Bottom Slide Toggle Bits:
            if (this._overviewToggle)
            {
                GUILayoutUtility.GetRect(0, 4); // Slide Area.
            }

            EditorGUI.DrawRect(overviewSlideRect, SirenixGUIStyles.BorderColor);
            EditorGUI.DrawRect(RectExtensions.AddY(toggleOverviewBtnRect, -overviewSlideRect.height), SirenixGUIStyles.BorderColor);
            SirenixEditorGUI.IconButton(RectExtensions.AddY(toggleOverviewBtnRect, -2),
                this._overviewToggle ? EditorIcons.TriangleDown : EditorIcons.TriangleUp);

            // Overview
            DrawOverview(overviewSlideRect);
        }

        private void DrawOverview(Rect overviewSlideRect)
        {
            if (!this._overviewToggle) return;

            GUILayout.BeginVertical(GUILayout.Height(this._overviewHeight));
            {
                // this.overview.DrawOverview();
            }
            GUILayout.EndVertical();

            if (Event.current.type != EventType.Repaint) return;

            this._overviewHeight = Mathf.Max(50f, this._overviewHeight);
            var wnd = GUIHelper.CurrentWindow;
            if (wnd)
            {
                var height = wnd.position.height - overviewSlideRect.yMax;
                this._overviewHeight = Mathf.Min(this._overviewHeight, height);
            }
        }

        private void DrawSelectedObject()
        {
            _selectedObjectScrollPosition = GUILayout.BeginScrollView(_selectedObjectScrollPosition);
            foreach (var e in _selectedObjectEditors)
            {
                SirenixEditorGUI.HorizontalLineSeparator(SirenixGUIStyles.BorderColor);
                using (var g = new eUtility.HorizontalGroup(false,
                    new GUIStyle { padding = new RectOffset(5, 5, 0, 0) }, GUILayoutOptions.Height(18)))
                {
                    SirenixEditorGUI.DrawSolidRect(g.Rect, SirenixGUIStyles.BoxBackgroundColor);
                    var type = e.target.GetType();
                    var img = EditorGUIUtility.ObjectContent(e.target, type).image;

                    GUILayout.Box(img, GUIStyle.none, GUILayoutOptions.Height(18).ExpandWidth(false));

                    GUILayout.Label(type.GetNiceName(), SirenixGUIStyles.BoldLabelCentered);
                }

                SirenixEditorGUI.HorizontalLineSeparator(SirenixGUIStyles.BorderColor, 2);

                GUILayout.Space(1);
                e.OnInspectorGUI();
                GUILayout.Space(3);
            }

            GUILayout.EndScrollView();
        }

        private void HandleMouseClick()
        {
            foreach (var item in _menuTree.MenuItems)
            {
                if (item.Rect.Contains(Event.current.mousePosition)) continue;

                if (item == _clickedItem && DateTime.Now - _lastClick < _maxTimeBetweenClicks)
                {
                    HandleDoubleClick(item);
                    _clickedItem = null;
                    _lastClick = default(DateTime);
                }
                else
                {
                    _lastClick = DateTime.Now;
                    _clickedItem = item;
                }
            }
        }

        private void HandleDoubleClick(UIMenuItem item)
        {
            Object obj = (Object) item.RawValue;
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        private void DrawTopBarButtons()
        {
            var rect = RectExtensions.AlignRight(GUIHelper.GetCurrentLayoutRect(), 150);

            rect.x -= 5;
            rect.y -= 26;
            rect.height = 18;


            if (GUI.Button(rect, "Select All"))
                Selection.objects = _motor.Results.ToArray();
        }

        public void Terminate()
        {
            ClearEditors();
        }
    }
}