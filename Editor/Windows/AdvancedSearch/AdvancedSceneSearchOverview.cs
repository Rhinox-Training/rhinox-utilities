using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.GUIUtils.Editor.Helpers;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Odin.Editor
{
    public class AdvancedSceneSearchOverview : PagerPage, IHasCustomMenu
    {
        #region wrapper

        public class MotorWrapper : IRepaintRequestHandler
        {
            public AdvancedSceneSearchMotor Motor;
            private DrawablePropertyView _view;

            public Rect Rect { get; set; }

            
            private readonly Func<ICollection<GameObject>> InitialObjectsFetcher;

            private static GUIStyle _dropZoneStyle;

            public static GUIStyle DropZoneStyle => _dropZoneStyle ?? (_dropZoneStyle =
                new GUIStyle(CustomGUIStyles.Box)
                {
                    fontSize = 24,
                    alignment = TextAnchor.MiddleCenter
                });

            public bool ShowInfo = true;
            public bool Highlighted = false;
            
            public IRepaintRequest Repainter;

            public MotorWrapper(Func<ICollection<GameObject>> objFetcher, IRepaintRequest repainter)
            {
                Motor = new AdvancedSceneSearchMotor();
                Motor.Changed += OnMotorChanged;

                Repainter = repainter;
                InitialObjectsFetcher = objFetcher;

                _view = new DrawablePropertyView(Motor);
                _view.RepaintRequested += RequestRepaint;
            }

            private void OnMotorChanged(AdvancedSceneSearchMotor obj)
            {
                obj.Update(InitialObjectsFetcher());
            }

            public void Draw()
            {
                using (new eUtility.VerticalGroup()) // this group is so the entire tree is wrapped in a rect for GetLastRect
                    _view?.Draw();

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
                // if (Event.current.type == EventType.Layout)
                //     ShowInfo = GUIHelper.ContextWidth >= 400;

                eUtility.Card(draw, onClick,
                    (r) =>
                    {
                        if (r != default(Rect))
                            Rect = r;
                        postDraw?.Invoke(r);
                    }
                );
            }

            public void RequestRepaint()
            {
                Repainter?.RequestRepaint();
            }

            public void UpdateRequestTarget(IRepaintRequest target)
            {
                Repainter = target;
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
                new MotorWrapper(GetInitialGameObjects, this)
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

                if (CustomEditorGUI.IconButton(UnityIcon.AssetIcon("Fa_Plus"), CustomGUIStyles.Clean))
                    _motorWrappers.Add(new MotorWrapper(GetInitialGameObjects, this));

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

            var rect = r.AlignRight(18).AlignTop(16);

            if (CustomEditorGUI.IconButton(rect, UnityIcon.AssetIcon("Fa_Times")))
                _removedWrappers.Add(wrapper);
        }

        protected override int CalculateTopWidth()
        {
            const int width = 20 + 8 + 20 + 5 + 20 + 5; // icons & padding / space
            return width;
        }

        protected override void OnDrawTopOverlay()
        {
            _onlyInSelection.Draw();
            _includeDisabled.Draw();

            GUILayout.Space(5);
            CustomEditorGUI.VerticalLine(Color.grey);
            GUILayout.Space(5);

            if (CustomEditorGUI.IconButton(UnityIcon.AssetIcon("Fa_Redo"), CustomGUIStyles.Label, 20, 16, tooltip: "Reset")) 
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
                    GUILayout.BeginVertical(GUILayout.MaxWidth(150));
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