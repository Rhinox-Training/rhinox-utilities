using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.GUIUtils.Editor.Helpers;
using Rhinox.Lightspeed;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
using RectExtensions = Rhinox.Lightspeed.RectExtensions;
using SelectionChangedType = Rhinox.GUIUtils.Editor.SelectionChangedType;

namespace Rhinox.Utilities.Odin.Editor
{
    public class AdvancedSceneSearchResults : PagerPage
    {
        private AdvancedSceneSearchMotor _motor;

        private float _overviewHeight;

        private bool _overviewToggle;

        private CustomMenuTree _menuTree;

        private static readonly TimeSpan _maxTimeBetweenClicks = TimeSpan.FromSeconds(.5f);
        private DateTime _lastClick;
        private IMenuItem _clickedItem;

        private GameObject _selectedObject;
        private readonly List<UnityEditor.Editor> _selectedObjectEditors = new List<UnityEditor.Editor>();
        private Vector2 _selectedObjectScrollPosition;

        private const float OverViewHeight = 300;

        public AdvancedSceneSearchResults(SlidePageNavigationHelper<object> pager, AdvancedSceneSearchMotor motor)
            : base(pager)
        {
            _motor = motor;

            _overviewToggle = true;
            _overviewHeight = OverViewHeight;

            BuildTree();

            _motor.ResultsChanged += BuildTree;
        }

        private void BuildTree()
        {
            _menuTree = new CustomMenuTree();
            _menuTree.DrawSearchToolbar = true;

            foreach (var item in _motor.Results)
                // can't have slashes as it will add depth to the tree
                _menuTree.Add(item.name.Replace("/", "_"), item);

            _menuTree.SelectionConfirmed += () =>
            {
                var sel = _menuTree.Selection.FirstOrDefault()?.RawValue as Object;
                if (sel == null) return;

                CustomEditorGUI.SelectObject(sel);
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

        protected override void OnDraw()
        {
            if (Event.current.type == EventType.Repaint && Input.GetMouseButtonDown(0))
                HandleMouseClick();
            
            // Bottom Slide Toggle Bits:
            var overviewSlideRect = new Rect();
            var toggleOverviewBtnRect = new Rect();

            // Menu editor
            using (new eUtility.HorizontalGroup(GUILayout.ExpandHeight(true)))
            {
                // Bottom Slide Toggle Bits:
                // The bottom slide-rect toggle needs to be drawn above, but is placed below.
                overviewSlideRect = new Rect(); // topRect.AddY(4).AlignBottom(4);
                overviewSlideRect.width += 4;
                toggleOverviewBtnRect = overviewSlideRect.AlignBottom(14).AlignCenter(100);
                EditorGUIUtility.AddCursorRect(toggleOverviewBtnRect, MouseCursor.Arrow);
                if (CustomEditorGUI.IconButton(toggleOverviewBtnRect.AddY(-2),
                    this._overviewToggle ? UnityIcon.InternalIcon("d_scrolldown@2x") : UnityIcon.InternalIcon("d_scrollup@2x")))
                {
                    this._overviewToggle = !this._overviewToggle;
                }

                _overviewHeight = _overviewToggle ? OverViewHeight : 20;

                // Left menu tree
                GUILayout.BeginVertical(CustomGUIStyles.Clean, GUILayout.Width(250));
                Rect currentLayoutRect = CustomEditorGUI.GetTopLevelLayoutRect();
                
                _menuTree.HandleRefocus(currentLayoutRect);
                
                EditorGUI.DrawRect(currentLayoutRect, new Color(1f, 1f, 1f, 0.035f));
                
                _menuTree.Draw();
                
                GUILayout.EndVertical();
                
                _menuTree.Update();

                // Draw selected
                GUILayout.BeginVertical();
                {
                    DrawSelectedObject();
                }
                GUILayout.EndVertical();
            }

            // Bottom Slide Toggle Bits:
            if (this._overviewToggle)
            {
                GUILayoutUtility.GetRect(0, 4); // Slide Area.
            }

            EditorGUI.DrawRect(overviewSlideRect, CustomGUIStyles.BorderColor);
            EditorGUI.DrawRect(toggleOverviewBtnRect.AddY(-overviewSlideRect.height), CustomGUIStyles.BorderColor);
            CustomEditorGUI.IconButton(toggleOverviewBtnRect.AddY(-2),
                this._overviewToggle ? UnityIcon.InternalIcon("d_scrolldown@2x") : UnityIcon.InternalIcon("d_scrollup@2x"));

            // Overview
            DrawOverview(overviewSlideRect);
        }

        private void DrawOverview(Rect overviewSlideRect)
        {
            if (!this._overviewToggle) return;

            // GUILayout.BeginVertical(GUILayout.Height(this._overviewHeight));
            // {
            //     // this.overview.DrawOverview();
            // }
            // GUILayout.EndVertical();

            if (Event.current.type != EventType.Repaint) return;

            this._overviewHeight = Mathf.Max(50f, this._overviewHeight);
            // var wnd = GUIHelper.CurrentWindow;
            // if (wnd)
            // {
            //     var height = wnd.position.height - overviewSlideRect.yMax;
            //     this._overviewHeight = Mathf.Min(this._overviewHeight, height);
            // }
        }

        private void DrawSelectedObject()
        {
            _selectedObjectScrollPosition = GUILayout.BeginScrollView(_selectedObjectScrollPosition);
            foreach (var e in _selectedObjectEditors)
            {
                CustomEditorGUI.HorizontalLine(CustomGUIStyles.BorderColor);
                using (var g = new eUtility.HorizontalGroup(false,
                    new GUIStyle { padding = new RectOffset(5, 5, 0, 0) }, GUILayout.Height(18)))
                {
                    EditorGUI.DrawRect(g.Rect, CustomGUIStyles.BoxBackgroundColor);
                    var type = e.target.GetType();
                    var img = EditorGUIUtility.ObjectContent(e.target, type).image;

                    GUILayout.Box(img, GUIStyle.none, GUILayout.Height(18), GUILayout.ExpandWidth(false));

                    GUILayout.Label(type.Name.SplitCamelCase(), CustomGUIStyles.BoldLabelCentered);
                }

                CustomEditorGUI.HorizontalLine(CustomGUIStyles.BorderColor, 2);

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

        private void HandleDoubleClick(IMenuItem item)
        {
            Object obj = (Object) item.RawValue;
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        protected override void OnDrawTopOverlay()
        {
            base.OnDrawTopOverlay();
            
            if (GUILayout.Button("Select All", GUILayout.Width(150), GUILayout.Height(18)))
                Selection.objects = _motor.Results.ToArray();
        }

        public void Terminate()
        {
            ClearEditors();
        }
    }
}