using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ModelProcessor.Editor
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
			GUI.Box(boxPosition, GUIContent.none, EditorStyles.helpBox);

			var headerLine = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			var conditionLine = headerLine;
			conditionLine.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			var actionLine = conditionLine;
			actionLine.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			var actionLine2 = actionLine;
			actionLine2.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			EditorGUIUtility.labelWidth = 60;

			//Header
			EditorGUI.LabelField(headerLine, property.displayName, EditorStyles.boldLabel);

			//Condition line
			Split(conditionLine, 220, out var conditionTypePos, out var conditionParamPos);
			SplitRight(conditionParamPos, 50, out conditionParamPos, out var conditionInvertPos);

			//Condition type selector
			EditorGUI.BeginChangeCheck();
			var conditionTypeProp = property.FindPropertyRelative(nameof(Rule.condition));
			var lastConditionType = (Rule.ConditionType)conditionTypeProp.intValue;
			EditorGUI.PropertyField(conditionTypePos, conditionTypeProp);
			if(EditorGUI.EndChangeCheck())
			{
				OnConditionTypeChanged(lastConditionType, (Rule.ConditionType)conditionTypeProp.intValue, property);
			}

			//Condition parameter
			DrawConditionParameter(conditionParamPos, property);
			var invert = property.FindPropertyRelative(nameof(Rule.invertCondition));
			invert.boolValue = GUI.Toggle(conditionInvertPos, invert.boolValue, new GUIContent("Invert"), EditorStyles.miniButton);



			//Action line
			Split(actionLine, 220, out var actionTypePos, out var actionParamPos);

			//Action type selector
			EditorGUI.BeginChangeCheck();
			var actionTypeProp = property.FindPropertyRelative(nameof(Rule.action));
			var lastActionType = (Rule.ActionType)actionTypeProp.intValue;
			EditorGUI.PropertyField(actionTypePos, actionTypeProp);
			if(EditorGUI.EndChangeCheck())
			{
				OnActionTypeChanged(lastActionType, (Rule.ActionType)actionTypeProp.intValue, property);
			}

			//Action parameter
			DrawActionParameter(actionParamPos, property);
			EditorGUIUtility.labelWidth = 120;
			EditorGUI.PropertyField(actionLine2, property.FindPropertyRelative(nameof(Rule.applyToChildren)));
		}

		private void OnConditionTypeChanged(Rule.ConditionType lastType, Rule.ConditionType newType, SerializedProperty property)
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
			property.FindPropertyRelative(nameof(Rule.conditionParam)).stringValue = defaultValue;
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
			property.FindPropertyRelative(nameof(Rule.actionParam)).stringValue = defaultValue;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int lines = 4;
			return lines * EditorGUIUtility.singleLineHeight + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
		}

		private void DrawConditionParameter(Rect position, SerializedProperty prop)
		{
			var conditionType = (Rule.ConditionType)prop.FindPropertyRelative(nameof(Rule.condition)).intValue;
			var conditionParam = prop.FindPropertyRelative(nameof(Rule.conditionParam));
			string value = conditionParam.stringValue;
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
					value = EditorGUI.TextField(position, value);
					break;
				case Rule.ConditionType.ChildDepthEquals:
				case Rule.ConditionType.ChildDepthGreaterThan:
				case Rule.ConditionType.ChildDepthGreaterOrEqual:
				case Rule.ConditionType.ChildDepthLessThan:
				case Rule.ConditionType.ChildDepthLessOrEqual:
					value = Mathf.Clamp(EditorGUI.IntField(position, AsInt(value)), 0, 99).ToString();
					break;
			}
			conditionParam.stringValue = value;
		}

		private void DrawActionParameter(Rect position, SerializedProperty prop)
		{
			var actionType = (Rule.ActionType)prop.FindPropertyRelative(nameof(Rule.action)).intValue;
			var paramProp = prop.FindPropertyRelative(nameof(Rule.actionParam));
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
