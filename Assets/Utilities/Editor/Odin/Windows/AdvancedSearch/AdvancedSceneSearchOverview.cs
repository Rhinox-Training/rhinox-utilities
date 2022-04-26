using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.GUIUtils.Odin.Editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using GUILayoutOptions = Sirenix.Utilities.GUILayoutOptions;

namespace Rhinox.Utilities.Odin.Editor
{
    public class AdvancedSceneSearchOverview : OdinPagerPage, IHasCustomMenu
    {
        #region wrapper

        public class MotorWrapper
        {
            public AdvancedSceneSearchMotor Motor;

            public Rect Rect { get; set; }

            public PropertyTree _tree;

            private readonly Func<ICollection<GameObject>> InitialObjectsFetcher;

            private static GUIStyle _dropZoneStyle;

            public static GUIStyle DropZoneStyle => _dropZoneStyle ?? (_dropZoneStyle =
                new GUIStyle(SirenixGUIStyles.BoxContainer)
                {
                    fontSize = 24,
                    alignment = TextAnchor.MiddleCenter
                });

            public bool ShowInfo = true;
            public bool Highlighted = false;

            public MotorWrapper(Func<ICollection<GameObject>> objFetcher)
            {
                Motor = new AdvancedSceneSearchMotor();
                Motor.Changed += OnMotorChanged;

                InitialObjectsFetcher = objFetcher;

                _tree = PropertyTree.Create(Motor);
            }

            private void OnMotorChanged(AdvancedSceneSearchMotor obj)
            {
                obj.Update(InitialObjectsFetcher());
            }

            public void Draw()
            {
                using (
                    new eUtility.VerticalGroup()) // this group is so the entire tree is wrapped in a rect for GetLastRect
                    _tree?.Draw(false);

                var rect = GUILayoutUtility.GetLastRect();
                var e = Event.current;
                if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
                    e.Use();

                HandleDropArea();
            }

            public void HandleDropArea()
            {
                if (DragAndDrop.objectReferences.Length == 0) return;

                GUI.Box(Rect, "Drop the object here to add it to the search", DropZoneStyle);

                Event e = Event.current;

                if (!(e.type == EventType.DragPerform || e.type == EventType.DragUpdated)
                    || !Rect.Contains(e.mousePosition))
                    return;

                var validDrop = DragAndDrop.objectReferences[0] is Component
                                || DragAndDrop.objectReferences[0] is GameObject
                                || DragAndDrop.objectReferences[0] is MonoScript;

                DragAndDrop.visualMode = validDrop ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                if (e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    Motor.HandleDragged(DragAndDrop.objectReferences);
                }
            }

            public void HandleDraw(Action draw, Action onClick, Action<Rect> postDraw = null)
            {
                if (Event.current.type == EventType.Layout)
                    ShowInfo = GUIHelper.ContextWidth >= 400;

                eUtility.Card(draw, onClick,
                    (r) =>
                    {
                        if (r != default(Rect))
                            Rect = r;
                        postDraw?.Invoke(r);
                    }
                );
            }
        }

        #endregion

        private readonly List<MotorWrapper> _motorWrappers;
        private readonly List<MotorWrapper> _removedWrappers; // cache to hold removed items

        private SettingData _includeDisabled;
        private SettingData _onlyInSelection;

        public AdvancedSceneSearchOverview(SlidePagedWindowNavigationHelper<object> pager) : base(pager)
        {
            _pager = pager;

            _motorWrappers = new List<MotorWrapper>
            {
                new MotorWrapper(GetInitialGameObjects)
            };
            _removedWrappers = new List<MotorWrapper>();

            _includeDisabled = new SettingData("Include Disabled", "inc_disabled", false, icon: "Fa_EyeSlash");
            _onlyInSelection = new SettingData("Only in selection", "in_selection", false, icon: "Fa_Expand");
        }

        protected override void OnDraw()
        {
            // draw all motors
            foreach (var wrapper in _motorWrappers)
            {
                // small spacing
                GUILayout.Space(5);

                // actual draw
                GUILayout.BeginHorizontal();
                GUILayout.Space(5);

                wrapper.HandleDraw(
                    () => DrawWrapperCard(wrapper),
                    () => PushResults(wrapper),
                    r => DrawCardButtons(wrapper, r)
                );

                GUILayout.Space(5);
                GUILayout.EndHorizontal();
            }

            // Handle removing elements
            if (Event.current.type == EventType.Layout && _removedWrappers.Any())
            {
                foreach (var removedWrapper in _removedWrappers)
                    _motorWrappers.Remove(removedWrapper);
                _removedWrappers.Clear();
            }

            // Draw + btn
            GUILayout.Space(5);
            using (new eUtility.HorizontalGroup())
            {
                GUILayout.FlexibleSpace();

                if (SirenixEditorGUI.IconButton(EditorIcons.Plus, SirenixGUIStyles.None))
                    _motorWrappers.Add(new MotorWrapper(GetInitialGameObjects));

                GUILayout.FlexibleSpace();
            }
        }

        private void PushResults(MotorWrapper wrapper)
        {
            if (!_pager.IsOnFirstPage) return;

            _pager.PushPage(new AdvancedSceneSearchResults(wrapper.Motor), "Results");
        }

        private void DrawCardButtons(MotorWrapper wrapper, Rect r)
        {
            if (_motorWrappers.Count <= 1) return; // no need to draw remove btn if only 1 filter

            var rect = r.AlignRight(18).AlignTop(18);

            if (SirenixEditorGUI.IconButton(rect, EditorIcons.X))
                _removedWrappers.Add(wrapper);
        }

        protected override int CalculateTopWidth()
        {
            // not sure why but if type check is not present, sometimes errors
            if (GUIHelper.ContextWidth <= 180)
            {
                if (Event.current.type == EventType.Repaint)
                    return 0;
                return _topWidth;
            }

            const int width = 18 + 4 + 18 + 5 + 18 + 5; // icons & padding / space
            return width;
        }

        protected override void OnDrawTopOverlay()
        {
            _onlyInSelection.Draw();
            _includeDisabled.Draw();

            GUILayout.Space(5);
            SirenixEditorGUI.VerticalLineSeparator(Color.grey);
            GUILayout.Space(5);

            if (SirenixEditorGUI.IconButton(EditorIcons.Refresh, 16, 16, tooltip: "Reset"))
                Reset();
        }

        private void DrawWrapperCard(MotorWrapper wrapper)
        {
            using (var g = new eUtility.HorizontalGroup())
            {
                GUILayout.BeginVertical();
                wrapper.Draw();
                GUILayout.EndVertical();

                if (wrapper.ShowInfo)
                {
                    GUILayout.BeginVertical(GUILayoutOptions.MaxWidth(150));
                    wrapper.Motor.DrawInfo(_includeDisabled.State, _onlyInSelection.State);
                    GUILayout.EndVertical();
                }
            }
        }

        private ICollection<GameObject> GetInitialGameObjects()
        {
            var objs = new List<GameObject>();
            if (_onlyInSelection.State && Selection.gameObjects.Any())
            {
                foreach (var obj in Selection.gameObjects)
                    objs.AddRange(obj.GetAllChildren(_includeDisabled.State));
            }
            else if (_includeDisabled.State)
            {
                for (int sceneI = 0; sceneI < SceneManager.sceneCount; sceneI++)
                {
                    var s = SceneManager.GetSceneAt(sceneI);
                    if (!s.isLoaded) continue;

                    var rootGameObjects = s.GetRootGameObjects();
                    foreach (var go in rootGameObjects)
                        objs.AddRange(go.GetAllChildren(true));
                }
            }
            else
            {
                objs = Object.FindObjectsOfType<Transform>().Select(x => x.gameObject).ToList();
            }

            return objs;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Reset"), false, Reset);
        }

        private void Reset()
        {
            foreach (var wrapper in _motorWrappers)
                wrapper.Motor.Reset();
        }
    }
}