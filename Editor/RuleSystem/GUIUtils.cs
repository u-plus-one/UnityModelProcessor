using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ModelProcessor.Editor.RuleSystem
{
	internal static class GUIUtils
	{
		private static Dictionary<string, ReorderableList> lists = new Dictionary<string, ReorderableList>();

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

		public static void ClearLists()
		{
			lists.Clear();
		}

		public static ReorderableList GetList(SerializedProperty property, SerializedProperty listProperty, bool allowFoldout, System.Action<Rect, SerializedProperty> drawHeaderAction)
		{
			if(lists.TryGetValue(listProperty.propertyPath, out var list) && listProperty.serializedObject != null)
			{
				return list;
			}
			else
			{
				list = new ReorderableList(listProperty.serializedObject, listProperty, true, true, true, true);

				list.elementHeight = EditorGUIUtility.singleLineHeight;

				list.drawHeaderCallback = rect =>
				{
					GUI.Label(rect, listProperty.displayName, EditorStyles.boldLabel);
					drawHeaderAction?.Invoke(rect, property);
					if(allowFoldout)
					{
						var newRect = new Rect(rect.x + 10, rect.y, rect.width - 10, rect.height);
						listProperty.isExpanded = EditorGUI.Foldout(newRect, listProperty.isExpanded, listProperty.displayName, true);
					}
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

				lists[listProperty.propertyPath] = list;
				return lists[listProperty.propertyPath];
			}
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