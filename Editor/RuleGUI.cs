using UnityEditor;
using UnityEngine;

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
			EditorGUI.PropertyField(conditionParamPos, property.FindPropertyRelative(nameof(Rule.conditionString)), GUIContent.none);
			var invert = property.FindPropertyRelative(nameof(Rule.invertCondition));
			invert.boolValue = GUI.Toggle(conditionInvertPos, invert.boolValue, new GUIContent("Invert"), EditorStyles.miniButton);
			Split(actionLine, 220, out var actionTypePos, out var actionParamPos);
			EditorGUI.PropertyField(actionTypePos, property.FindPropertyRelative(nameof(Rule.action)));
			EditorGUI.PropertyField(actionParamPos, property.FindPropertyRelative(nameof(Rule.actionString)), GUIContent.none);
			EditorGUIUtility.labelWidth = 120;
			EditorGUI.PropertyField(actionLine2, property.FindPropertyRelative(nameof(Rule.applyToChildren)));
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int lines = 3;
			return lines * EditorGUIUtility.singleLineHeight + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
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
