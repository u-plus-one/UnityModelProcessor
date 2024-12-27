using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ModelProcessor.Editor.RuleSystem
{
	[CustomPropertyDrawer(typeof(Rule))]
	public class RuleGUI : PropertyDrawer
	{
		private static GUIStyle headerStyle = null;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(headerStyle == null) headerStyle = new GUIStyle(EditorStyles.toolbar) { fixedHeight = 0 };

			position.xMin -= 8;

			var boxPosition = position;
			boxPosition.xMin -= 2;
			boxPosition.xMax += 2;
			boxPosition.yMax -= EditorGUIUtility.standardVerticalSpacing;
			GUI.Box(boxPosition, GUIContent.none, EditorStyles.helpBox);

			var conditionOperator = property.FindPropertyRelative(nameof(Rule.conditionOperator));
			var conditions = property.FindPropertyRelative(nameof(Rule.conditions));
			var actions = property.FindPropertyRelative(nameof(Rule.actions));
			var applyToChildren = property.FindPropertyRelative(nameof(Rule.applyToChildren));

			position.height = EditorGUIUtility.singleLineHeight;
			var headerPos = position;
			var conditionsPos = GUIUtils.NextProperty(headerPos, EditorGUI.GetPropertyHeight(conditions));
			var actionsPos = GUIUtils.NextProperty(conditionsPos, EditorGUI.GetPropertyHeight(actions));

			EditorGUIUtility.labelWidth = 60;

			//Header
			EditorGUI.LabelField(headerPos, property.displayName, EditorStyles.boldLabel);
			//Conditions
			EditorGUI.PropertyField(conditionsPos, conditions);
			//Actions
			EditorGUI.PropertyField(actionsPos, actions);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float h = EditorGUIUtility.singleLineHeight;
			h += EditorGUIUtility.standardVerticalSpacing;
			h += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(Rule.conditions)));
			h += EditorGUIUtility.standardVerticalSpacing;
			h += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(Rule.actions)));
			h += EditorGUIUtility.standardVerticalSpacing;
			return h;
		}
	}

	public abstract class ElementDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.xMin -= 10;
			position.height = EditorGUIUtility.singleLineHeight;
			DrawGUI(position, property);
		}

		protected abstract void DrawGUI(Rect position, SerializedProperty property);

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(Condition))]
	public class ConditionDrawer : ElementDrawer
	{
		protected override void DrawGUI(Rect position, SerializedProperty property)
		{
			var invert = property.FindPropertyRelative(nameof(Condition.invert));
			var type = property.FindPropertyRelative(nameof(Condition.type));
			var param = property.FindPropertyRelative(nameof(Condition.parameter));
			GUIUtils.Split(position, 32, out var invertPos, out position);
			GUIUtils.Split(position, 180, out var typePos, out var paramPos);

			//Invert toggle
			invert.boolValue = GUIUtils.ToggleButton(invertPos, "NOT", invert.boolValue);

			//Type selector
			EditorGUI.BeginChangeCheck();
			var lastConditionType = (ConditionType)type.intValue;
			EditorGUI.PropertyField(typePos, type, GUIContent.none);
			if(EditorGUI.EndChangeCheck())
			{
				OnConditionTypeChanged(lastConditionType, (ConditionType)type.intValue, property);
			}
			//Parameter
			DrawConditionParameter(paramPos, (ConditionType)type.intValue, param);
		}

		private void DrawConditionParameter(Rect position, ConditionType conditionType, SerializedProperty param)
		{
			string value = param.stringValue;
			switch(conditionType)
			{
				case ConditionType.NameStartsWith:
				case ConditionType.NameEndsWith:
				case ConditionType.NameContains:
				case ConditionType.NameMatchesRegex:
				case ConditionType.PathStartsWith:
				case ConditionType.PathEndsWith:
				case ConditionType.PathContains:
				case ConditionType.PathMatchesRegex:
					value = EditorGUI.TextField(position, value);
					break;
				case ConditionType.ChildDepthEquals:
				case ConditionType.ChildDepthGreaterThan:
				case ConditionType.ChildDepthGreaterOrEqual:
				case ConditionType.ChildDepthLessThan:
				case ConditionType.ChildDepthLessOrEqual:
					value = Mathf.Clamp(EditorGUI.IntField(position, GUIUtils.AsInt(value)), 0, 99).ToString();
					break;
			}
			param.stringValue = value;
		}

		private void OnConditionTypeChanged(ConditionType lastType, ConditionType newType, SerializedProperty property)
		{
			bool wasNameAction = (int)lastType >= 10 && (int)lastType < 20;
			bool isNameAction = (int)newType >= 10 && (int)newType < 20;
			if(wasNameAction && isNameAction)
			{
				//Retain the current value
				return;
			}
			string defaultValue;
			switch(lastType)
			{
				default:
					defaultValue = "";
					break;
			}
			property.FindPropertyRelative(nameof(Condition.parameter)).stringValue = defaultValue;
		}
	}

	[CustomPropertyDrawer(typeof(Action))]
	public class ActionDrawer : ElementDrawer
	{
		protected override void DrawGUI(Rect position, SerializedProperty property)
		{
			var type = property.FindPropertyRelative(nameof(Action.type));
			var param = property.FindPropertyRelative(nameof(Action.parameter));
			GUIUtils.Split(position, 212, out var typePos, out var paramPos);

			//Type selector
			EditorGUI.BeginChangeCheck();
			var lastActionType = (ActionType)type.intValue;
			EditorGUI.PropertyField(typePos, type, GUIContent.none);
			if(EditorGUI.EndChangeCheck())
			{
				OnActionTypeChanged(lastActionType, (ActionType)type.intValue, property);
			}
			//Parameter
			DrawActionParameter(paramPos, (ActionType)type.intValue, param);
		}

		private void DrawActionParameter(Rect position, ActionType actionType, SerializedProperty param)
		{
			string value = param.stringValue;
			switch(actionType)
			{
				case ActionType.SetLayer:
					value = EditorGUI.LayerField(position, GUIUtils.AsInt(value)).ToString();
					break;
				case ActionType.SetTag:
					value = EditorGUI.TagField(position, value);
					break;
				case ActionType.SetStaticFlags:
					StaticEditorFlags flags = (StaticEditorFlags)GUIUtils.AsInt(value);
					value = ((int)(object)EditorGUI.EnumFlagsField(position, flags)).ToString();
					break;
				case ActionType.SetName:
				case ActionType.PrependName:
				case ActionType.AppendName:
					value = EditorGUI.TextField(position, value);
					break;
				case ActionType.SetCastShadowsMode:
					value = ((int)(object)EditorGUI.EnumPopup(position, (ShadowCastingMode)GUIUtils.AsInt(value))).ToString();
					break;
				case ActionType.SetReceiveShadowsMode:
					value = EditorGUI.Toggle(position, GUIUtils.AsBool(value, true)) ? "1" : "0";
					break;
				case ActionType.SetLightmapScale:
					value = EditorGUI.FloatField(position, GUIUtils.AsFloat(value, 1)).ToString(CultureInfo.InvariantCulture);
					break;
			}
			param.stringValue = value;
		}

		private void OnActionTypeChanged(ActionType lastType, ActionType newType, SerializedProperty property)
		{
			bool wasNameAction = lastType == ActionType.SetName || lastType == ActionType.PrependName || lastType == ActionType.AppendName;
			bool isNameAction = newType == ActionType.SetName || newType == ActionType.PrependName || newType == ActionType.AppendName;
			if(wasNameAction && isNameAction)
			{
				//Retain the current value
				return;
			}
			string defaultValue;
			switch(newType)
			{
				case ActionType.SetTag:
					defaultValue = "Untagged";
					break;
				case ActionType.SetCastShadowsMode:
					defaultValue = ((int)ShadowCastingMode.On).ToString();
					break;
				default:
					defaultValue = "";
					break;
			}
			property.FindPropertyRelative(nameof(Action.parameter)).stringValue = defaultValue;
		}
	}

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
