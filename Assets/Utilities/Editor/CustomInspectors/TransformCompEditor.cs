using System;
using System.Reflection;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Editor
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Transform), true)]
	public class TransformCompEditor : UnityEditor.Editor
	{
		private static class Properties
		{
			public const string PosName = "m_LocalPosition";
			public const string RotName = "m_LocalRotation";
			public const string ScaleName = "m_LocalScale";
			public const string ScaleConstraintName = "m_ConstrainProportionsScale";
			
			public static GUIContent WorldPositionLabel = new GUIContent("WP", "World Position");
			public static GUIContent PositionLabel = new GUIContent("P", "Position; Click to rest to 0,0,0");
			public static GUIContent RotationLabel = new GUIContent("R", "Rotation; Click to rest to 0,0,0");
			public static GUIContent ScaleLabel = new GUIContent("S", "Scale; Click to rest to 1,1,1");
			
			
			public static GUIContent CenterBtnLabel = new GUIContent("Center", "Move object to center of children without affecting children");
			public static GUIContent ResetBtnLabel = new GUIContent("Reset", "Reset scale without affecting children");
			public static GUIContent PingBtnLabel = new GUIContent("Ping", "Ping the GameObject in the hierarchy");
			public const string OpenLockedBtnTooltip = "Open a locked inspector with this object selected";

			public static GUIContent CopyBtnLabel = new GUIContent("C", "Copy");
			public static GUIContent PasteBtnLabel = new GUIContent("P", "Paste");
			
#if UNITY_2021_1_OR_NEWER // Default icon but it does not exist prior to this version
			private static Texture2D _linkedIcon = UnityIcon.InternalIcon("d_Linked");
#else // We offer a substitute behaviour, so we need an icon for it
			private static Texture2D _linkedIcon = UnityIcon.AssetIcon("Fa_Link").Pad(20);
#endif
			public static GUIContent LinkedContent = new GUIContent(_linkedIcon, "Disable Constrained Scale");
			
#if UNITY_2021_1_OR_NEWER // Default icon but it does not exist prior to this version
			private static Texture2D _unlinkedIcon = UnityIcon.InternalIcon("d_Unlinked");
#else // We offer a substitute behaviour, so we need an icon for it
			private static Texture2D _unlinkedIcon = UnityIcon.AssetIcon("Fa_Unlink").Pad(20);
#endif
			public static GUIContent UnlinkedContent = new GUIContent(_unlinkedIcon, "Enable Constrained scale");
			
			public const int ButtonHeight = 18;
		}
		
		private Vector3 _worldPos;
		private SerializedProperty _pos;
		private SerializedProperty _rot;
		private SerializedProperty _scale;
		private SerializedProperty _scaleConstraint;

		private bool _initialized;
		private bool _isConstrained; // backup for when it was unsupported
		private Transform _target;
		
		private static Vector3? _positionClipboard = null;
		private static Quaternion? _rotationClipboard = null;
		private static Vector3? _scaleClipboard = null;

		protected void OnEnable()
		{ 
			Init();
		}

		private bool Init()
		{
			if (serializedObject.targetObject == null) return false;

			_target = (Transform) serializedObject.targetObject;

			_pos = serializedObject.FindProperty(Properties.PosName);
			_rot = serializedObject.FindProperty(Properties.RotName);
			_scale = serializedObject.FindProperty(Properties.ScaleName);
			_scaleConstraint = serializedObject.FindProperty(Properties.ScaleConstraintName);
			if (_scaleConstraint != null)
				_isConstrained = _scaleConstraint.boolValue;

			_worldPos = _target.position;

			return _initialized = true;
		}

		private bool Button(GUIContent content, GUIStyle style = null)
		{
			if (style == null)
				style = CustomGUIStyles.Button;
			return GUILayout.Button(content, style, GUILayout.Height(Properties.ButtonHeight));
		}

		public override void OnInspectorGUI()
		{
			// if cannot initialize, draw the base gui
			if ((!_initialized && !Init()) || _drawDefault)
			{
				base.OnInspectorGUI();
				return;
			}
			
			var labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 10;

			serializedObject.Update();

			using (new eUtility.HorizontalGroup())
			{
				using (new eUtility.VerticalGroup())
				{
					DrawPosition();
					DrawRotation();
					DrawScale();
					
					DrawWorldPosition();
				}
				
				// SirenixEditorGUI.VerticalLineSeparator();
				
				using (new eUtility.VerticalGroup(false, GUILayout.Width(25)))
				{
					if (Button(Properties.CenterBtnLabel))
						HandlePivotShift();
					
					DrawCopyPaste();

					DrawScaleHelpers();

					DrawHierarchyHelpers();
				}
			}
			
			EditorGUIUtility.labelWidth = labelWidth;

			serializedObject.ApplyModifiedProperties();
		}

		private void HandleScaleShift()
		{
			foreach (Transform t in targets)
				t.ShiftScaleTo(Vector3.one, true);
		}

		private void HandlePivotShift()
		{
			if (Event.current.button == 0)
			{
				ShiftPivot(t => t.gameObject.GetObjectBounds().center);
				return;
			}
				
			var menu = new GenericMenu();
				
			var content = new GUIContent("Center on Bounds");
			menu.AddItem(content, false, () => ShiftPivot(t => t.gameObject.GetObjectBounds().center));
			menu.AddSeparator(string.Empty);
				
			content = new GUIContent("Center on Origin");
			menu.AddItem(content, false,  () => ShiftPivot(t => t.parent == null ? Vector3.zero : t.parent.position));

			menu.ShowAsContext();
		}

		private void ShiftPivot(Func<Transform, Vector3> targetGetter)
		{
			foreach (Transform t in targets)
			{
				var newCenter = targetGetter(t);
				t.ShiftPivotTo(newCenter, true);
			}
		}

		private static void ShowPropertyContextMenu(SerializedProperty property, GenericMenu menu = null)
		{
			var t = typeof(EditorGUI);
			var m = t.GetMethod("DoPropertyContextMenu", BindingFlags.NonPublic | BindingFlags.Static);
			m.Invoke(null, new object[] { property, null, menu });
		}
		
		private static void DrawResetButton(SerializedProperty prop, GUIContent label,
			GenericMenu.MenuFunction copy, bool canCopy,
			GenericMenu.MenuFunction paste, bool canPaste, 
			GenericMenu.MenuFunction reset,
			float width = 20f)
		{
			if (!GUILayout.Button(label, GUILayout.Width(width))) return;
			
			if (Event.current.button == 0)
			{
				reset.Invoke();
				return;
			}
				
			var menu = new GenericMenu();
				
			var content = new GUIContent("Reset");
			menu.AddItem(content, false, reset);
			menu.AddSeparator(string.Empty);
				
			content = new GUIContent("Copy");
			if (canCopy) menu.AddItem(content, false, copy);
			else menu.AddDisabledItem(content);
				
			content = new GUIContent("Paste");
			if (canPaste) menu.AddItem(content, false, paste);
			else menu.AddDisabledItem(content);
				
			menu.AddSeparator(string.Empty);
				
			ShowPropertyContextMenu(prop, menu);
		}

		private void DrawPosition()
		{
			GUILayout.BeginHorizontal();
			DrawResetButton(_pos, Properties.PositionLabel, 
				() => SaveToClipboard(scale: false, rot: false), targets.Length == 1, 
				() => ApplyClipboard(scale: false, rot: false), _positionClipboard.HasValue,
				() => Set(_pos, Vector3.zero));
			
			EditorGUILayout.PropertyField(_pos.FindPropertyRelative("x"));
			EditorGUILayout.PropertyField(_pos.FindPropertyRelative("y"));
			EditorGUILayout.PropertyField(_pos.FindPropertyRelative("z"));
			GUILayout.EndHorizontal();
		}

		private void Set(SerializedProperty prop, Vector3 val)
		{
			prop.serializedObject.Update();
			prop.vector3Value = val;
			prop.serializedObject.ApplyModifiedProperties();
			_target.hasChanged = true;
		}
		
		private void Set(SerializedProperty prop, Quaternion val)
		{
			prop.serializedObject.Update();
			prop.quaternionValue = val;
			prop.serializedObject.ApplyModifiedProperties();
			_target.hasChanged = true;
		}

		void DrawRotation()
		{
			GUILayout.BeginHorizontal();
			DrawResetButton(_rot, Properties.RotationLabel,
				() => SaveToClipboard(scale: false, pos: false), targets.Length == 1,
				() => ApplyClipboard(scale: false, pos: false), _rotationClipboard.HasValue,
				() => Set(_rot, Quaternion.identity));

			Vector3 visible = _target.localEulerAngles;

			visible.x = WrapAngle(visible.x);
			visible.y = WrapAngle(visible.y);
			visible.z = WrapAngle(visible.z);

			Axis changed = CheckDifference(_rot);
			Axis altered = Axis.None;

			GUILayoutOption opt = GUILayout.MinWidth(30f);

			if (FloatField("X", ref visible.x, (changed & Axis.X) != 0, false, opt)) altered |= Axis.X;
			if (FloatField("Y", ref visible.y, (changed & Axis.Y) != 0, false, opt)) altered |= Axis.Y;
			if (FloatField("Z", ref visible.z, (changed & Axis.Z) != 0, false, opt)) altered |= Axis.Z;
			
			if (altered != Axis.None)
			{
				RegisterUndo("Change Rotation", serializedObject.targetObjects);

				foreach (Transform t in serializedObject.targetObjects)
				{
					Vector3 v = t.localEulerAngles;

					if ((altered & Axis.X) != 0) v.x = visible.x;
					if ((altered & Axis.Y) != 0) v.y = visible.y;
					if ((altered & Axis.Z) != 0) v.z = visible.z;

					t.localEulerAngles = v;
				}
			}

			GUILayout.EndHorizontal();
		}
		
		private void DrawScale()
		{
			GUILayout.BeginHorizontal();
			{ 
				DrawResetButton(_scale, Properties.ScaleLabel, 
					() => SaveToClipboard(pos: false, rot: false), targets.Length == 1, 
					() => ApplyClipboard(pos: false, rot: false), _scaleClipboard.HasValue,
					() => Set(_scale, Vector3.one));

				var scale = _scale.vector3Value;
				if (_isConstrained && scale.LossyEquals(Vector3.zero)) scale = Vector3.one;

				using (new eUtility.DisabledGroup(_isConstrained && scale.x.LossyEquals(0)))
					EditorGUILayout.PropertyField(_scale.FindPropertyRelative("x"));
				using (new eUtility.DisabledGroup(_isConstrained && scale.y.LossyEquals(0)))
					EditorGUILayout.PropertyField(_scale.FindPropertyRelative("y"));
				using (new eUtility.DisabledGroup(_isConstrained && scale.z.LossyEquals(0)))
					EditorGUILayout.PropertyField(_scale.FindPropertyRelative("z"));

				if (_isConstrained)
					ApplyConstrainedScale(scale);
			}
			GUILayout.EndHorizontal();
		}

		private void ApplyConstrainedScale(Vector3 scale)
		{
			var newScale = _scale.vector3Value;
			if (!newScale.x.LossyEquals(scale.x))
			{
				var ratio = newScale.x / scale.x;
				_scale.vector3Value = new Vector3(newScale.x, ratio * scale.y, ratio * scale.z);
			}
			else if (!newScale.y.LossyEquals(scale.y))
			{
				var ratio = newScale.y / scale.y;
				_scale.vector3Value = new Vector3(ratio * scale.x, newScale.y, ratio * scale.z);
			}
			else if (!newScale.z.LossyEquals(scale.z))
			{
				var ratio = newScale.z / scale.z;
				_scale.vector3Value = new Vector3(ratio * scale.x, ratio * scale.y, newScale.z);
			}
		}

		private void DrawWorldPosition()
		{
			using (new eUtility.HorizontalGroup())
			{
				using (new eUtility.DisabledGroup(true))
				{
					EditorGUILayout.LabelField(Properties.WorldPositionLabel, GUILayout.Width(20));
					EditorGUILayout.FloatField("X", _worldPos.x);
					EditorGUILayout.FloatField("Y", _worldPos.y);
					EditorGUILayout.FloatField("Z", _worldPos.z);

				}
			}
		}

		private void DrawCopyPaste()
		{
			using (new eUtility.HorizontalGroup())
			{
				if (Button(Properties.CopyBtnLabel, CustomGUIStyles.ButtonLeft))
					SaveToClipboard();

				using (new eUtility.DisabledGroup(!_positionClipboard.HasValue))
				{
					if (Button(Properties.PasteBtnLabel, CustomGUIStyles.ButtonRight))
					{
						ApplyClipboard();
						GUI.FocusControl(null);
					}
				}
			}
		}
		
		private void DrawScaleHelpers()
		{
			using (new eUtility.HorizontalGroup())
			{
				if (CustomEditorGUI.IconButton(_isConstrained ? Properties.LinkedContent : Properties.UnlinkedContent, CustomGUIStyles.IconButtonLeft))
				{
					_isConstrained = !_isConstrained;
					if (_scaleConstraint != null)
						_scaleConstraint.boolValue = _isConstrained;
				}

				if (Button(Properties.ResetBtnLabel, CustomGUIStyles.ButtonRight))
					HandleScaleShift();
			}
			
		}

		private void DrawHierarchyHelpers()
		{
			using (new eUtility.HorizontalGroup())
			{
				var go = ((Transform) target).gameObject;
				using (new eUtility.DisabledGroup(!go.activeInHierarchy))
				{
					if (Button(Properties.PingBtnLabel, CustomGUIStyles.ButtonLeft))
						EditorGUIUtility.PingObject(go);
				}
			
#if ODIN_INSPECTOR
				if (SirenixEditorGUI.IconButton(EditorIcons.Pen, CustomGUIStyles.ButtonRight, Properties.ButtonHeight, Properties.ButtonHeight, Properties.OpenLockedBtnTooltip))
					GUIHelper.OpenInspectorWindow(go);
#endif
			}
		}

		private void SaveToClipboard(bool pos = true, bool rot = true, bool scale = true)
		{
			if (pos) _positionClipboard = _target.localPosition;
			else _positionClipboard = null;
			
			if (rot) _rotationClipboard = _target.localRotation;
			else _rotationClipboard = null;
			
			if (scale) _scaleClipboard = _target.localScale;
			else _scaleClipboard = null;
		}

		private void ApplyClipboard(bool pos = true, bool rot = true, bool scale = true)
		{
			// If applying nothing, just return
			if (!(pos   && _positionClipboard.HasValue) &&
			    !(rot   && _rotationClipboard.HasValue) &&
			    !(scale && _scaleClipboard.HasValue)) return;
			
			Undo.RecordObjects(targets, "Paste Clipboard Values");
			for (int i = 0; i < targets.Length; i++)
			{
				var t = ((Transform) targets[i]);
				if (pos   && _positionClipboard.HasValue) t.localPosition = _positionClipboard.Value;
				if (rot   && _rotationClipboard.HasValue) t.localRotation = _rotationClipboard.Value;
				if (scale && _scaleClipboard.HasValue) t.localScale = _scaleClipboard.Value;
			}
		}

		#region Rotation

		Axis CheckDifference(Transform t, Vector3 original)
		{
			Vector3 next = t.localEulerAngles;

			Axis axes = Axis.None;

			if (Differs(next.x, original.x)) axes |= Axis.X;
			if (Differs(next.y, original.y)) axes |= Axis.Y;
			if (Differs(next.z, original.z)) axes |= Axis.Z;

			return axes;
		}

		Axis CheckDifference(SerializedProperty property)
		{
			Axis axes = Axis.None;

			if (property.hasMultipleDifferentValues)
			{
				Vector3 original = property.quaternionValue.eulerAngles;

				foreach (Transform t in serializedObject.targetObjects)
				{
					axes |= CheckDifference(t, original);
					if (axes == Axis.XYZ) break;
				}
			}

			return axes;
		}

		/// <summary>
		/// Draw an editable float field.
		/// </summary>
		/// <param name="hidden">Whether to replace the value with a dash</param>
		/// <param name="greyedOut">Whether the value should be greyed out or not</param>

		static bool FloatField(string name, ref float value, bool hidden, bool greyedOut, GUILayoutOption opt)
		{
			float newValue = value;
			GUI.changed = false;

			if (!hidden)
			{
				if (greyedOut)
				{
					GUI.color = new Color(0.7f, 0.7f, 0.7f);
					newValue = EditorGUILayout.FloatField(name, newValue, opt);
					GUI.color = Color.white;
				}
				else
				{
					newValue = EditorGUILayout.FloatField(name, newValue, opt);
				}
			}
			else if (greyedOut)
			{
				GUI.color = new Color(0.7f, 0.7f, 0.7f);
				float.TryParse(EditorGUILayout.TextField(name, "--", opt), out newValue);
				GUI.color = Color.white;
			}
			else
			{
				float.TryParse(EditorGUILayout.TextField(name, "--", opt), out newValue);
			}

			if (GUI.changed && Differs(newValue, value))
			{
				value = newValue;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Because Mathf.Approximately is too sensitive.
		/// </summary>

		static bool Differs(float a, float b)
		{
			return Mathf.Abs(a - b) > 0.0001f;
		}

		private static void RegisterUndo(string name, params Object[] objects)
		{
			if (objects == null || objects.Length <= 0) return;
			
			Undo.RecordObjects(objects, name);

			foreach (Object obj in objects)
			{
				if (obj == null) continue;
				EditorUtility.SetDirty(obj);
			}
		}

		public static float WrapAngle(float angle)
		{
			while (angle > 180f) angle -= 360f;
			while (angle < -180f) angle += 360f;
			return angle;
		}

		#endregion
		
		/// <summary>
		/// This is used through reflection by Unity to 'Focus' on an object
		/// Default focus in unity is scuft; so override it using the bounds
		/// This method defines whether this component defines valid bounds and can override the focus bounds
		/// </summary>
		public bool HasFrameBounds()
		{
			var t = ((Transform) target);
			
			// If there are MeshRenderers or colliders in the children
			// Let unity handle it => return false
			if (t.gameObject.GetComponentInChildren<MeshRenderer>()) return false;
			if (t.gameObject.GetComponentInChildren<Collider>()) return false;
			
			// Override ParticleSystem behaviour
			if (t.gameObject.GetComponent<ParticleSystemRenderer>()) return true;
			
			// If there are MeshRenderers or colliders in the parent
			// We override it (see below) => return true
			if (t.gameObject.GetComponentInParent<MeshRenderer>()) return true;
			if (t.gameObject.GetComponentInParent<Collider>()) return true;

			return true;
		}

		/// <summary>
		/// Same as above, but the actual implementation of the bounds
		/// Hardly ever triggered so no point in caching
		/// </summary>
		public Bounds OnGetFrameBounds()
		{
			// assuming it will not get here if there is a child mesh, hence not calculating that

			var t = (Transform) target;

			var system = t.gameObject.GetComponent<ParticleSystemRenderer>();
			if (system)
				return system.bounds;
			
			var parent = t.parent;
			if (parent != null)
			{
				// Find it from parent down (aka in parent or siblings)
				var b = parent.gameObject.GetObjectBounds();
				if (!b.Equals(default))
					return b;
				
				// If not found there, try to find it above the parent
				var mesh = t.gameObject.GetComponentInParent<MeshRenderer>();

				if (mesh != null)
					return mesh.bounds;
			
				var collider = t.gameObject.GetComponentInParent<Collider>();

				if (collider != null)
					return collider.bounds;
			}
			
			// default small bounds
			return new Bounds(t.position, Vector3.one);
		}
		
		
		private static bool _drawDefault;

		[MenuItem("CONTEXT/Transform/Toggle Custom Editor", false)]
		private static void ToggleCustomEditor(MenuCommand menuCommand)
		{
			_drawDefault = !_drawDefault;
		}

	}
}