using System;
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
			EditorGUI.PropertyField(actionTypePos, property.FindPropertyRelative(nameof(Rule.action)));
			DrawActionParameter(actionParamPos, property);
			EditorGUIUtility.labelWidth = 120;
			EditorGUI.PropertyField(actionLine2, property.FindPropertyRelative(nameof(Rule.applyToChildren)));
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
			switch(actionType)
			{
				case Rule.ActionType.SetLayer:
					var l = prop.FindPropertyRelative(nameof(Rule.actionValueParam));
					l.intValue = EditorGUI.LayerField(position, l.intValue);
					break;
				case Rule.ActionType.SetTag:
					EditorGUI.PropertyField(position, prop.FindPropertyRelative(nameof(Rule.actionStringParam)), GUIContent.none);
					break;
				case Rule.ActionType.SetName:
				case Rule.ActionType.PrependName:
				case Rule.ActionType.AppendName:
					EditorGUI.PropertyField(position, prop.FindPropertyRelative(nameof(Rule.actionStringParam)), GUIContent.none);
					break;
				case Rule.ActionType.SetCastShadowsMode:
					var m = prop.FindPropertyRelative(nameof(Rule.actionValueParam));
					m.intValue = (int)(object)EditorGUI.EnumPopup(position, (ShadowCastingMode)m.intValue);
					break;
				case Rule.ActionType.SetReceiveShadowsMode:
					var b = prop.FindPropertyRelative(nameof(Rule.actionValueParam));
					b.boolValue = EditorGUI.Toggle(position, b.boolValue);
					break;
				case Rule.ActionType.SetLightmapScale:
					var s = prop.FindPropertyRelative(nameof(Rule.actionValueParam));
					s.floatValue = EditorGUI.FloatField(position, s.floatValue);
					break;
			}
		}

		private void Split(Rect input, float width, out Rect l, out Rect r)
		{
			l = input;
			l.width = width;
			r = input;
			r.xMin += width + 2;
		}

		private void SplitRight(Rect input, float width, out Rect l, out Rect r)
		{
			l = input;
			l.xMax -= width + 2;
			r = input;
			r.xMin = input.xMax - width;
		}
	}
}
