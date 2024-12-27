using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ModelProcessor.Editor.RuleSystem
{
	internal static class GUIUtils
	{
		private static GUIStyle toggleButtonStyle;

		public static GUIStyle ToggleButtonStyle
		{
			get
			{
				if(toggleButtonStyle == null)
				{
					toggleButtonStyle = new GUIStyle(EditorStyles.helpBox)
					{
						alignment = TextAnchor.MiddleCenter,
						normal = { textColor = TextColor(0.5f) },
						hover = { textColor = TextColor(0.75f) },
						onNormal = { textColor = TextColor(1) },
						onHover = { textColor = TextColor(1) },
					};
				}
				return toggleButtonStyle;
			}
		}

		public static bool ToggleButton(Rect pos, string content, bool state)
		{
			GUI.color = new Color(1, 1, 1, state ? 1f : 0.33f);
			var result = GUI.Toggle(pos, state, content, ToggleButtonStyle);
			GUI.color = Color.white;
			return result;
		}

		public static Color32 TextColor(float a)
		{
			byte gray = (byte)(EditorGUIUtility.isProSkin ? 210 : 9);
			byte alpha = (byte)(a * 255f);
			return new Color32(gray, gray, gray, alpha);
		}

		public static ReorderableList CreateReorderableList(SerializedProperty listProperty, bool allowFoldout)
		{
			var list = new ReorderableList(listProperty.serializedObject, listProperty, true, true, true, true);

			list.elementHeight = EditorGUIUtility.singleLineHeight;

			list.drawHeaderCallback = rect =>
			{
				if(allowFoldout)
				{
					rect.xMin += 10;
					EditorGUI.BeginChangeCheck();
					listProperty.isExpanded = EditorGUI.Foldout(rect, listProperty.isExpanded, GUIContent.none, true);
					if(EditorGUI.EndChangeCheck())
					{
						//Fixes broken GUI after changing expanded state
#if UNITY_2021_2_OR_NEWER
						typeof(ReorderableList).GetMethod("InvalidateCache", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(list, Array.Empty<object>());
#else
						typeof(ReorderableList).GetMethod("ClearCache", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(list, Array.Empty<object>());
#endif
					}
				}
				GUI.Label(rect, listProperty.displayName, EditorStyles.boldLabel);
			};

			list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				if(listProperty.isExpanded)
				{
					rect.xMin -= 8;
					EditorGUI.PropertyField(rect, listProperty.GetArrayElementAtIndex(index), GUIContent.none);
				}
			};

			list.elementHeightCallback = (int indexer) =>
			{
				if(!listProperty.isExpanded)
				{
					return 0;
				}
				else
				{
					if(list.elementHeight > 0)
					{
						return list.elementHeight;
					}
					else
					{
						return EditorGUI.GetPropertyHeight(list.serializedProperty.GetArrayElementAtIndex(indexer));
					}
				}
			};

			return list;
		}

		public static void Split(Rect input, float width, out Rect l, out Rect r)
		{
			l = input;
			l.width = width;
			r = input;
			r.xMin += width + 2;
		}

		public static void SplitRight(Rect input, float width, out Rect l, out Rect r)
		{
			l = input;
			l.xMax -= width + 2;
			r = input;
			r.xMin = input.xMax - width;
		}

		public static int AsInt(string s, int fallback = 0)
		{
			return int.TryParse(s, out var result) ? result : fallback;
		}

		public static float AsFloat(string s, float fallback = 0)
		{
			return float.TryParse(s, out var result) ? result : fallback;
		}

		public static bool AsBool(string s, bool fallback = false)
		{
			return AsInt(s, fallback ? 1 : 0) > 0;
		}

		public static Rect NextProperty(Rect pos, float height)
		{
			pos.y += pos.height + EditorGUIUtility.standardVerticalSpacing;
			pos.height = height;
			return pos;
		}
	}
}