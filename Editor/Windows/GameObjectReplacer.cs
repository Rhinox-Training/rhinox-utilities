#if UNITY_2018_3_OR_NEWER
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

#pragma warning disable 618

namespace Rhinox.Utilities.Editor
{
	public class GameObjectReplacer : EditorWindow 
	{
		private static GameObjectReplacer Window;
		
		private static GameObject[] GameObjects;
		private static GameObject GameObjectReplacement;
		private static bool CopyRotation, CopyScale;
		
		private static Rect m_PrefabIconRect;
		private static Texture m_PrefabIcon;
		private static bool m_ShowInSceneView;
		
		[MenuItem(WindowHelper.ToolsPrefix + "GameObject Replacer", false, 200)]
		internal static void ShowWindow()
		{
			WindowHelper.GetOrCreate(out Window, "GameObject Replacer", centerOnScreen: true, initialization: (w) =>
			{
				w.minSize = new Vector2(250, 125);
				GameObjectReplacement = null;
			});
		}
		
		private void OnEnable()
		{
			ShowWindow();
		}

		private void OnScene(SceneView sceneView)
		{
			GameObjects = Selection.gameObjects;

			GUI.skin.font = ((GUIStyle)"ShurikenLabel").font;
			
			Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(10, Screen.height-148, 216,100), "GameObject Replacer", GUI.skin.window);
			GetLayoutFields(true);
			Repaint();
			GUILayout.EndArea();
			Handles.EndGUI();
        }
    
        public void OnGUI () 
		{
			GameObjects = Selection.gameObjects;
			GUI.skin.font = ((GUIStyle)"ShurikenLabel").font;
			
			GUILayout.Space(5);
			EditorGUILayout.BeginVertical((GUIStyle)"HelpBox");
			{
				if(m_PrefabIcon == null)
					m_PrefabIcon = UnityIcon.InternalIcon("d_Prefab Icon").Pad(20);
				
				EditorGUILayout.LabelField(new GUIContent("GameObject Replacer", m_PrefabIcon), CustomGUIStyles.TitleBackground);

				GUILayout.Space(15);
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					GUILayout.BeginVertical();

					GetLayoutFields();

					GUILayout.Space(5);
					GUILayout.EndVertical();
					GUILayout.Space(10);
				}
				GUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();
			SceneView.RepaintAll();
		}
		
		private void Replace()
		{
			PrefabType prefabType = PrefabUtility.GetPrefabType(GameObjectReplacement);
			
			List<GameObject> newSelection = new List<GameObject>();
			foreach(GameObject gameObject in GameObjects)
			{
				if (gameObject == GameObjectReplacement)
				{
					newSelection.Add(gameObject);
					continue;
				}
					
				GameObject newGameObject = null;
				Object newPrefab = PrefabUtility.GetPrefabParent(GameObjectReplacement);
				
				switch (prefabType)
				{
					case PrefabType.PrefabInstance:
						newGameObject =	PrefabUtility.InstantiatePrefab(newPrefab) as GameObject;
						PrefabUtility.SetPropertyModifications(newGameObject, PrefabUtility.GetPropertyModifications(GameObjectReplacement));
						break;
					case PrefabType.ModelPrefab:
					case PrefabType.Prefab:
						newGameObject = PrefabUtility.InstantiatePrefab(GameObjectReplacement) as GameObject;
						break;
					case PrefabType.None:
						newGameObject = Instantiate(GameObjectReplacement);
						newGameObject.name = GameObjectReplacement.name;
                	    break;
				}
			
				Undo.RegisterCreatedObjectUndo(newGameObject, "created object");
				
				newGameObject.transform.position = gameObject.transform.position;
				
				if(CopyRotation)
					newGameObject.transform.rotation = gameObject.transform.rotation;

				if(CopyScale)
					newGameObject.transform.localScale = gameObject.transform.localScale;

				newGameObject.transform.parent = gameObject.transform.parent;
				
				Undo.DestroyObjectImmediate(gameObject);
				newSelection.Add(newGameObject);
			}
			Selection.objects = newSelection.ToArray();
			
			string goString = (newSelection.Count > 1) ? " GameObjects have " : " GameObject has ";
			Debug.Log(newSelection.Count.ToString() + goString + "been replaced with: " + GameObjectReplacement.name + "\nPrefab Type: " + prefabType);
		}
		
		private void GetLayoutFields(bool isSceneView = false)
		{
			EditorGUIUtility.labelWidth = 100;
			
			GameObjectReplacement = EditorGUILayout.ObjectField("Replacement", GameObjectReplacement, typeof(GameObject),true) as GameObject;
			CopyRotation = EditorGUILayout.Toggle("Copy Rotation", CopyRotation);
			CopyScale = EditorGUILayout.Toggle("Copy Scale", CopyScale);
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Replace Selection", GUILayout.ExpandWidth(false)))
				HandleReplace();
			
            if(!m_ShowInSceneView)
			{
				if(GUILayout.Button("Scene View", GUILayout.ExpandWidth(false)))
				{
					Utility.SubscribeToSceneGui(OnScene);
					m_ShowInSceneView = true;
					
					BindingFlags bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
					MethodInfo isDocked = typeof(EditorWindow).GetProperty("docked", bindings).GetGetMethod(true);
					
					if((bool)isDocked.Invoke(this, null) == false) 
						Window.Close();
				}
			}
            
            if(isSceneView)
            {
				if(GUILayout.Button("Close", GUILayout.ExpandWidth(false)))
				{
					m_ShowInSceneView = false;
					Utility.UnsubscribeFromSceneGui(OnScene);
				}
            }
            
            EditorGUILayout.EndHorizontal();
        }

		private void HandleReplace()
		{
			if (!GameObjects.All(CanReplaceObject))
				return;
				
			if (GameObjects.Length != 0 && GameObjectReplacement != null)
				Replace();
			else 
				EditorUtility.DisplayDialog("Missing GameObjects!", "Make sure you have both a Replacement GameObject and have selected 1 or more GameObjects in the scene.", "OK");

		}

		private bool CanReplaceObject(GameObject obj)
		{
			var currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

			if (currentPrefabStage?.prefabContentsRoot == obj)
			{
				EditorUtility.DisplayDialog("Replace failed!", "You cannot replace the root of a prefab.", "OK!");
				return false;
			}
			
			var prefabOfItem = PrefabUtility.GetNearestPrefabInstanceRoot(obj);

			// not a prefab -> can edit it
			if (prefabOfItem == null)
				return true;
			
			var parentPrefabOfItem = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
			
			// if this item has a parent prefab, it is not editable
			if (parentPrefabOfItem != prefabOfItem)
			{
				EditorUtility.DisplayDialog("Replace failed!", "To replace the selected Object, please move to its prefab edit stage.", "OK!");
				return false;
			}
			
			// if the obj is the most outer prefab -> you can edit it
			if (parentPrefabOfItem == obj)
				return true;

			EditorUtility.DisplayDialog("Replace failed!", "Editing a prefab which contains the object -> Move a layer deeper to replace the selected Object.", "OK!");
			// editing a prefab which contains the prefab -> move a layer deeper
			return false;

		}
    }
}
#endif