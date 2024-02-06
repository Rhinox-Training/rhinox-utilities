using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Editor
{
    public class SelectionHistory : ScriptableObject
    {
        public class ResolvedSelectionData
        {
            public Object[] Objects;
        }
        
        [Serializable]
        private struct SelectionData
        {
            // TODO we serialize this in a way so we are able to resolve it after the scene is closed/reopened (i.e. paths)
            public Object[] Objects;

            public Object[] ResolveObjects()
            {
                return Objects;
            }
            
            public override bool Equals(object obj) 
            {
                if (obj == null || !(obj is SelectionData other))
                    return false;
                
                return other.Objects.ContainEqual(this.Objects);
            }

            public static implicit operator ResolvedSelectionData(SelectionData data) => new ResolvedSelectionData {
                Objects = data.ResolveObjects()
            };
        }

        public int MaxHistory = 50;

        [SerializeField]
        private List<SelectionData> _pinnedSelections = new List<SelectionData>();
        
        [SerializeField, HideInInspector]
        private List<SelectionData> _selections = new List<SelectionData>(50);
        
        private int _currentIndex = 0;
        private bool _ignoreNext = false;

        private static SelectionHistory _instance;

        public static ResolvedSelectionData[] Pinned => _instance._pinnedSelections.Select(x => (ResolvedSelectionData) x).ToArray();
        public static ResolvedSelectionData[] Data => _instance._selections.Select(x => (ResolvedSelectionData) x).ToArray();
        public static int CurrentIndex => _instance._currentIndex;
        
        [InitializeOnLoadMethod]
        private static void SetupTracker()
        {
            var assets = eUtility.FindAssets<SelectionHistory>();
            _instance = assets.FirstOrDefault();
            
            if (_instance == null)
            {
                _instance = ScriptableObject.CreateInstance<SelectionHistory>();
                AssetDatabase.CreateAsset(_instance, "Assets/SelectionData.asset");
            }
        }

        // SetupTracker will load this object, causing this to fire
        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += Update;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= Update;
        }

        private void OnSelectionChanged()
        {
            // If we made the selection change ourselves, we don't want this to register the change
            if (_ignoreNext)
            {
                _ignoreNext = false;
                return;
            }
            
            // Wrap the objects
            var data = new SelectionData()
            {
                Objects = Selection.objects
            };

            // Remove any equal selection
            _selections.RemoveAll(x => x.Equals(data));
            
            // Reduce our history to our max allowed
            while (_selections.Count > MaxHistory)
                _selections.RemoveAt(0);
            
            // Go to the latest index & add it
            _currentIndex = _selections.Count;
            _selections.Add(data);
        }

        private void Update()
        {
            // Go to prev selection
            if (Utility.GetKeyUp(KeyCode.Mouse3) && _currentIndex > 0)
                SelectByIndex(_currentIndex - 1);
            
            // Go to next selection
            if (Utility.GetKeyUp(KeyCode.Mouse4) && _currentIndex < _selections.Count - 1)
                SelectByIndex(_currentIndex + 1);
        }
        
        public static void Reset()
        {
            _instance._selections.Clear();
        }
        
        public static void Unpin(int i)
        {
            var data = _instance._pinnedSelections[i];
            if (!_instance._pinnedSelections.Contains(data))
                return;
            _instance._pinnedSelections.Remove(data);
        }

        public static void Pin(int i)
        {
            var data = _instance._selections[i];
            if (_instance._pinnedSelections.Contains(data))
                return;
            _instance._pinnedSelections.Add(data);
        }

        public static int IsPinInSelection(int pinIndex)
        {
            var data = _instance._pinnedSelections[pinIndex];
            return _instance._selections.FindIndex(x => x.Equals(data));
        }

        public static int GetPinIndex(int i)
        {
            if (i < 0) // If our index is invalid = already pinned
                return -1;
            var data = _instance._selections[i];
            return _instance._pinnedSelections.FindIndex(x => x.Equals(data));
        }

        public static void GoTo(int i)
        {
            if (i < 0)
                i = _instance._selections.Count - i;
            _instance.SelectByIndex(i);
        }
        
        public static void GoToPin(int i)
        {
            _instance.SelectByPinIndex(i);
        }

        private void SelectByPinIndex(int i)
        {
            var selectionI = IsPinInSelection(i);
            if (selectionI >= 0)
                SelectByIndex(selectionI);
            else // if not selected, it's a new selection, so do not ignore
                Selection.objects = _pinnedSelections[i].Objects;
        }
        
        private void SelectByIndex(int i)
        {
            _ignoreNext = true;
            _currentIndex = i;
            Selection.objects = _selections[_currentIndex].Objects;
        }
    }
}