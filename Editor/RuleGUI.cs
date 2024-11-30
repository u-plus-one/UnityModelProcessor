using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ModelProcessor.Editor
{
	[CustomPropertyDrawer(typeof(Rule))]
	public class RuleGUI : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var conditionLine = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			var actionLine = conditionLine;
			actionLine.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			var actionLine2 = actionLine;
			actionLine2.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			EditorGUIUtility.labelWidth = 60;
			Split(conditionLine, 220, out var conditionTypePos, out var conditionParamPos);
			SplitRight(conditionParamPos, 50, out conditionParamPos, out var conditionInvertPos);
			EditorGUI.PropertyField(conditionTypePos, property.FindPropertyRelative(nameof(Rule.condition)));
			DrawConditionParameter(conditionParamPos, property);
			var invert = property.FindPropertyRelative(nameof(Rule.invertCondition));
			invert.boolValue = GUI.Toggle(conditionInvertPos, invert.boolValue, new GUIContent("Invert"), EditorStyles.miniButton);
			Split(actionLine, 220, out var actionTypePos, out var actionParamPos);

			EditorGUI.BeginChangeCheck();
			var actionTypeProp = property.FindPropertyRelative(nameof(Rule.action));
			var lastActionType = (Rule.ActionType)actionTypeProp.intValue;
			EditorGUI.PropertyField(actionTypePos, actionTypeProp);
			if(EditorGUI.EndChangeCheck())
			{
				OnActionTypeChanged(lastActionType, (Rule.ActionType)actionTypeProp.intValue, property);
			}


			DrawActionParameter(actionParamPos, property);
			EditorGUIUtility.labelWidth = 120;
			EditorGUI.PropertyField(actionLine2, property.FindPropertyRelative(nameof(Rule.applyToChildren)));
		}

		private void OnActionTypeChanged(Rule.ActionType lastType, Rule.ActionType newType, SerializedProperty property)
		{
			bool wasNameAction = lastType == Rule.ActionType.SetName || lastType == Rule.ActionType.PrependName || lastType == Rule.ActionType.AppendName;
			bool isNameAction = newType == Rule.ActionType.SetName || newType == Rule.ActionType.PrependName || newType == Rule.ActionType.AppendName;
			if(wasNameAction && isNameAction)
			{
				//Retain the current value
				return;
			}
			string defaultValue;
			switch(newType)
			{
				case Rule.ActionType.SetTag:
					defaultValue = "Untagged";
					break;
				case Rule.ActionType.SetCastShadowsMode:
					defaultValue = ((int)ShadowCastingMode.On).ToString();
					break;
				default:
					defaultValue = "";
					break;
			}
			property.FindPropertyRelative(nameof(Rule.actionStringParam)).stringValue = defaultValue;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int lines = 3;
			return lines * EditorGUIUtility.singleLineHeight + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
		}

		private void DrawConditionParameter(Rect position, SerializedProperty prop)
		{
			var conditionType = (Rule.ConditionType)prop.FindPropertyRelative(nameof(Rule.condition)).intValue;
			switch(conditionType)
			{
				case Rule.ConditionType.NameStartsWith:
				case Rule.ConditionType.NameEndsWith:
				case Rule.ConditionType.NameContains:
				case Rule.ConditionType.NameMatchesRegex:
				case Rule.ConditionType.PathStartsWith:
				case Rule.ConditionType.PathEndsWith:
				case Rule.ConditionType.PathContains:
				case Rule.ConditionType.PathMatchesRegex:
					EditorGUI.PropertyField(position, prop.FindPropertyRelative(nameof(Rule.conditionString)), GUIContent.none);
					break;
				case Rule.ConditionType.ChildDepthEquals:
				case Rule.ConditionType.ChildDepthGreaterThan:
				case Rule.ConditionType.ChildDepthGreaterOrEqual:
				case Rule.ConditionType.ChildDepthLessThan:
				case Rule.ConditionType.ChildDepthLessOrEqual:
					EditorGUI.PropertyField(position, prop.FindPropertyRelative(nameof(Rule.conditionInt)), GUIContent.none);
					break;
			}
		}

		private void DrawActionParameter(Rect position, SerializedProperty prop)
		{
			var actionType = (Rule.ActionType)prop.FindPropertyRelative(nameof(Rule.action)).intValue;
			var paramProp = prop.FindPropertyRelative(nameof(Rule.actionStringParam));
			string value = paramProp.stringValue;
			switch(actionType)
			{
				case Rule.ActionType.SetLayer:
					value = EditorGUI.LayerField(position, AsInt(value)).ToString();
					break;
				case Rule.ActionType.SetTag:
					value = EditorGUI.TagField(position, value);
					break;
				case Rule.ActionType.SetStaticFlags:
					StaticEditorFlags flags = (StaticEditorFlags)AsInt(value);
					value = ((int)(object)EditorGUI.EnumFlagsField(position, flags)).ToString();
					break;
				case Rule.ActionType.SetName:
				case Rule.ActionType.PrependName:
				case Rule.ActionType.AppendName:
					value = EditorGUI.TextField(position, value);
					break;
				case Rule.ActionType.SetCastShadowsMode:
					value = ((int)(object)EditorGUI.EnumPopup(position, (ShadowCastingMode)AsInt(value))).ToString();
					break;
				case Rule.ActionType.SetReceiveShadowsMode:
					value = EditorGUI.Toggle(position, AsBool(value, true)) ? "1" : "0";
					break;
				case Rule.ActionType.SetLightmapScale:
					value = EditorGUI.FloatField(position, AsFloat(value, 1)).ToString(CultureInfo.InvariantCulture);
					break;
			}
			paramProp.stringValue = value;
		}

		private static void Split(Rect input, float width, out Rect l, out Rect r)
		{
			l = input;
			l.width = width;
			r = input;
			r.xMin += width + 2;
		}

		private static void SplitRight(Rect input, float width, out Rect l, out Rect r)
		{
			l = input;
			l.xMax -= width + 2;
			r = input;
			r.xMin = input.xMax - width;
		}

		private static int AsInt(string s, int fallback = 0)
		{
			return int.TryParse(s, out var result) ? result : fallback;
		}

		private static float AsFloat(string s, float fallback = 0)
		{
			return float.TryParse(s, out var result) ? result : fallback;
		}

		private static bool AsBool(string s, bool fallback = false)
		{
			return AsInt(s, fallback ? 1 : 0) > 0;
		}
	}
}
