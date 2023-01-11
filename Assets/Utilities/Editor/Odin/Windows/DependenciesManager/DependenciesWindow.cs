﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Rhinox.GUIUtils.Attributes;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities.Editor;
#if TEXT_MESH_PRO
using TMPro;
#endif
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using GUILayoutOptions = Sirenix.Utilities.GUILayoutOptions;

namespace Rhinox.Utilities.Odin.Editor
{
	public class DependenciesWindow : OdinMenuListEditorWindow
	{
		// =================================================================================================================
		// PROPERTIES
		// =================================================================================================================

		public static GUIContent TitleContent => new GUIContent("Dependencies List", EditorIcons.MagnifyingGlass.Raw);

		[HideInInspector] public AssetManager AssetManager = new AssetManager();
		[HideInInspector] public DependenciesManager DependenciesManager = new DependenciesManager();
		[HideInInspector] public DependencySettings Settings = new DependencySettings();

		public bool ShowPath { get; private set; }

		private Object _currentSelection;

		private string _currentSelectedPath
		{
			get { return AssetDatabase.GetAssetPath(_currentSelection); }
		}

		private IReadOnlyList<DependencyAsset> _currentSelections;

		private string _selectionDescription;

		// =================================================================================================================
		// METHODS
		
		// =================================================================================================================
		// ASSET MANAGEMENT
		internal void ClearSelections()
		{
			_currentSelections = null;
		}

		// =================================================================================================================
		// Selection Management
		private Dependency[] GetSelection()
		{
			return MenuTree.Selection
				.Select(x => x.Value as Dependency)
				.Where(x => x != null)
				.OrderBy(x => x.Path)
				.ToArray();
		}

		private void OnSelectionChange()
		{
			if (Selection.instanceIDs.Length > 1)
				return;

			var selectedAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

			if (_currentSelections != null && _currentSelections.Any(x => x.Path == selectedAssetPath))
				_currentSelection = Selection.activeObject;
		}

		private void SetSelection(IReadOnlyList<DependencyAsset> dependencies)
		{
			_currentSelections = AssetManager.GetIntersecting(dependencies, Settings.IgnoredFileRegexs,
				Settings.IgnoredDirectoryRegexs);
			SetSelectedObjects(_currentSelections);
		}

		private void SetSelectedObjects(IEnumerable<DependencyAsset> dependencies)
		{
			var guids = dependencies
				.Select(asset => AssetDatabase.AssetPathToGUID(asset.Path))
				.ToArray();

			Selection.instanceIDs = GetInstanceIDFromGUID(guids).ToArray();
		}

		static IEnumerable<int> GetInstanceIDFromGUID(params string[] guids)
		{
			// var method = typeof(AssetDatabase).GetMethod("GetInstanceIDFromGUID", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			// 
			// foreach (var guid in guids)
			// 	yield return (int) method.Invoke(null, new object[] { guid });

			foreach (var guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
				if (asset)
					yield return asset.GetInstanceID();
			}
		}

		private void SetInverseSelection(IReadOnlyList<string> paths)
		{
			SetSelection(AssetManager.InverseOf(paths, Settings.IgnoredFileRegexs, Settings.IgnoredDirectoryRegexs));
		}

		private void SetInverseSelection(IEnumerable<Dependency> dependencies)
		{
			DependencyAsset[] dependenciesToSelect = AssetManager.InverseOf(dependencies, Settings.IgnoredFileRegexs,
				Settings.IgnoredDirectoryRegexs);
			SetSelection(dependenciesToSelect);
		}

		private void SelectOther(int offset = 1)
		{
			if (_currentSelections == null) return;


			var i = _currentSelections.FindIndex(x => x.Path == _currentSelectedPath);

			if (i < 0) return;

			SetActiveSelection(_currentSelections.GetAtIndex(i + offset));
		}

		private void SetActiveSelection(DependencyAsset asset)
		{
			Selection.activeObject = asset != null ? asset.GetLoadedReference() : null;
		}

		private void SelectDirectory(int offset = 1)
		{
			if (_currentSelections == null) return;

			var i = _currentSelections.FindIndex(x => x.Path == _currentSelectedPath);

			string newDir = null;

			if (offset > 0)
			{
				var prevDirs = new HashSet<string>(_currentSelections.Take(i + offset).Select(x => x.Directory));
				newDir = _currentSelections.Skip(i).FirstOrDefault(x => !prevDirs.Contains(x.Directory))?.Directory;

				if (string.IsNullOrWhiteSpace(newDir))
					newDir = _currentSelections.FirstOrDefault()?.Directory;
			}
			else if (offset < 0)
			{
				var nextDirs = new HashSet<string>(_currentSelections.Skip(i).Select(x => x.Directory));
				for (i += offset; i >= 0; --i)
				{
					var item = _currentSelections[i];
					if (nextDirs.Contains(item.Directory)) continue;
					newDir = item.Directory;
					break;
				}

				if (string.IsNullOrWhiteSpace(newDir))
				{
					var selection = _currentSelections.LastOrDefault();
					newDir = selection?.Directory;
				}
			}
			else
				Debug.LogError("This offset is not yet defined!");

			if (!string.IsNullOrWhiteSpace(newDir))
				SetSelectedObjects(_currentSelections.Where(x => x.Directory == newDir));

			_currentSelection = Selection.activeObject;

		}

		private void RestoreSelection()
		{
			var currentFolder = eUtility.GetShownFolder();

			SetSelectedObjects(_currentSelections);

			if (!string.IsNullOrWhiteSpace(currentFolder) && _currentSelections.Any(x => x.Directory == currentFolder))
				EditorApplication.delayCall += () => eUtility.ShowFolderContents(currentFolder);
		}

		// overrides the way a selected item is shown when multiple are selected
		protected override object GetObject(OdinMenuItem item)
		{
			if (item == null)
				return null;

			object obj = item.Value;
			Func<object> func = obj as Func<object>;
			if (func != null)
				obj = func();

			if (MenuTree.Selection.Count > 1)
				return (obj as Dependency)?.GetLoadedReference();

			return obj;
		}
		
		public void Clear()
		{

			DependenciesManager.Dependencies.Clear();
			ClearSelections();

			_selectionDescription = null;

			ForceMenuTreeRebuild();
		}

		// =================================================================================================================
		// GUI METHODS

		#region GUI Methods

		public static void ShowWindow()
		{
			var w = GetWindow<DependenciesWindow>();
			w.titleContent = TitleContent;
			w.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);

			w.Settings.Load();
		}

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree(true);
			tree.DefaultMenuStyle.IconSize = 16.00f;
			tree.Config.DrawSearchToolbar = true;

			tree.Add("Home", this, EditorIcons.House);

			tree.Add("All Assets", AssetManager, EditorIcons.List);

			tree.Add("Settings", Settings, EditorIcons.SettingsCog);

			foreach (var d in DependenciesManager.Dependencies)
			{
				if (!AssetManager.AllAssets.Contains(d.Path)) continue;
				
				tree.Add(ShowPath ? d.PathNoAssets : d.Name, d, GetIconForType(d.Type));
			}

			return tree;
		}

		private Texture GetIconForType(Type t)
		{
			if (t == null) return null;
			
			var tex = Settings.IconMapper.ContainsKey(t) ? Settings.IconMapper[t] : null;
			if (tex) return tex;
			foreach (var type in Settings.IconMapper.Keys)
			{
				if (t.InheritsFrom(type))
					return Settings.IconMapper[type];
			}

			return null;
		}

		protected override void OnBeginDrawEditors()
		{
			var toolbarHeight = this.MenuTree.Config.SearchToolbarHeight;

			SirenixEditorGUI.BeginHorizontalToolbar(toolbarHeight);

			DrawToolbarBtns();

			GUILayout.FlexibleSpace();
			var newTerm = SirenixEditorGUI.ToolbarSearchField(AssetManager.SearchText);
			if (AssetManager.CheckChange(newTerm))
				ForceMenuTreeRebuild();

			SirenixEditorGUI.EndHorizontalToolbar();

			// Secondary toolbar
			var height = toolbarHeight / 1.5f;
			SirenixEditorGUI.BeginHorizontalToolbar(height, paddingTop: 4 + toolbarHeight);
			DrawTypeActions();
			DrawTypeSearchButtons(height);
			SirenixEditorGUI.EndHorizontalToolbar();
		}

		protected override void OnGUI()
		{
			GUILayout.BeginVertical();
			base.OnGUI();
			GUILayout.EndVertical();

			if (_currentSelections == null)
				return;

			const int spacer = 10;

			// Selection manager Toolbar
			SirenixEditorGUI.BeginHorizontalToolbar(20);

			GUILayout.Space(spacer);

			if (_currentSelection != null)
			{
				if (SirenixEditorGUI.ToolbarButton(new GUIContent("<<", tooltip: "Select all in previous folder")))
					SelectDirectory(-1);
				GUILayout.Space(spacer);
				if (SirenixEditorGUI.ToolbarButton(new GUIContent("<", tooltip: "Select previous asset")))
					SelectOther(-1);
				GUILayout.Space(spacer);
			}

			if (SirenixEditorGUI.ToolbarButton("Restore Selection")) RestoreSelection();

			if (_currentSelection != null)
			{
				GUILayout.Space(spacer);
				if (SirenixEditorGUI.ToolbarButton(new GUIContent(">", tooltip: "Select next asset"))) SelectOther(1);
				GUILayout.Space(spacer);
				if (SirenixEditorGUI.ToolbarButton(new GUIContent(">>", tooltip: "Select all in next folder")))
					SelectDirectory(1);
			}

			GUILayout.FlexibleSpace();

			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.LabelField(_selectionDescription);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();

			GUILayout.Space(spacer);
			SirenixEditorGUI.EndHorizontalToolbar();
		}

		#endregion GUI Methods

		// =================================================================================================================
		// TOOLBAR METHODS

		#region TOOLBAR METHODS

		private void DrawTypeSearchButtons(float height)
		{
			GUILayout.FlexibleSpace();

			var changed = false;

			var searchPieces = AssetManager.SearchText.Split();
			var activeType = searchPieces.FirstOrDefault(x => x.StartsWith("t:"))?.Split(':').Last();

			var toggleOpts = GUILayoutOptions.Height(height).Width(height * 1.5f);

			foreach (var pair in Settings.IconMapper)
			{
				if (pair.Key == typeof(Texture2D)
#if TEXT_MESH_PRO
				    || pair.Key == typeof(TMP_FontAsset)
#endif
				    )
					continue;

				var typeName = pair.Key.Name;
				var content = new GUIContent(pair.Value, typeName);
				if (GUILayout.Toggle(activeType == typeName, content, SirenixGUIStyles.ToolbarTab, toggleOpts))
				{
					activeType = typeName;
					searchPieces = searchPieces
						.Where(x => !x.StartsWith("t:"))
						.Append("t:" + activeType)
						.ToArray();

					var newSearch = string.Join(" ", searchPieces);

					if (AssetManager.CheckChange(newSearch))
						changed = true;
				}
			}

			if (changed)
				ForceMenuTreeRebuild();
		}

		private void DrawToolbarBtns()
		{
			// TOGGLES
			var prev = ShowPath;
			ShowPath = SirenixEditorGUI.ToolbarToggle(ShowPath, new GUIContent("Show path", EditorIcons.Folder.Raw));

			if (prev != ShowPath)
				ForceMenuTreeRebuild();

			if (!DependenciesManager.Dependencies.Any())
				return;

			// BUTTONS
			if (SirenixEditorGUI.ToolbarButton("Select ALL"))
			{
				SetSelection(DependenciesManager.Dependencies);
				_selectionDescription = $"ALL ({Selection.instanceIDs.Length})";
			}

			if (SirenixEditorGUI.ToolbarButton("Inverse ALL"))
			{
				SetInverseSelection(DependenciesManager.Dependencies);
				_selectionDescription = $"ALL INVERSE ({Selection.instanceIDs.Length})";
			}

			var selection = GetSelection();

			if (selection.Any() && SirenixEditorGUI.ToolbarButton("Select"))
			{
				SetSelection(selection);
				_selectionDescription = $"selection ({Selection.instanceIDs.Length})";
			}

			if (selection.Any() && SirenixEditorGUI.ToolbarButton("Inverse Select"))
			{
				SetInverseSelection(selection.Select(x => x.Path).ToArray());
				_selectionDescription = $"selection INVERSE ({Selection.instanceIDs.Length})";
			}
		}

		private Material _replacementMaterial;

		private void DrawTypeActions()
		{
			var selection = GetSelection();

			var type = selection.FirstOrDefault()?.Type;
			if (selection.Any(x => x.Type != type)) return;

			if (type == typeof(Material))
			{
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Replace Material with:"))
				{
					if (_replacementMaterial == null)
						Debug.LogError("You must fill in a Replacement Material first!");
					else
					{
						foreach (var dependency in selection)
						{
							var mat = dependency.GetLoadedReference() as Material;
							ReplaceMaterialInPrefabs(mat, _replacementMaterial,
								dependency.Users.OfType<GameObject>().ToArray());
						}
					}
				}

				_replacementMaterial = (Material) EditorGUILayout.ObjectField("", _replacementMaterial,
					typeof(Material), allowSceneObjects: false);
				GUILayout.EndHorizontal();
			}
		}
		
		private void ReplaceMaterialInPrefabs(Material old, Material newMaterial, params GameObject[] objects)
		{
			var prefabs = objects
				// .Select(go => PrefabUtility.GetCorrespondingObjectFromSource(go))
				.Where(x => x != null)
				.ToArray();

			ReplaceMaterial(old, newMaterial, prefabs);

			AssetDatabase.SaveAssets();
		}

		private void ReplaceMaterial(Material old, Material newMaterial, params GameObject[] objects)
		{
			foreach (var go in objects)
			{
				foreach (var r in go.GetComponentsInChildren<Renderer>())
				{
					var mats = r.sharedMaterials;
					if (!mats.Contains(old)) continue;

					for (int i = 0; i < mats.Length; ++i)
					{
						if (mats[i] == old)
							mats[i] = newMaterial;
					}

					r.materials = mats;
				}
			}
		}

		#endregion
	}
}