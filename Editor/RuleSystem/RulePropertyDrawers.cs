using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ModelProcessor.Editor.RuleSystem
{
	[CustomPropertyDrawer(typeof(Rule))]
	public class RuleGUI : PropertyDrawer
	{
		const float SPACING = 10;

		private static GUIStyle headerStyle = null;

		private static string[] conditionOperatorNames = System.Enum.GetNames(typeof(Operator)).Select(s => s.ToUpper()).ToArray();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(headerStyle == null) headerStyle = new GUIStyle(EditorStyles.toolbar) { fixedHeight = 0 };

			position.xMin -= 8;

			var boxPosition = position;
			boxPosition.xMin -= 2;
			boxPosition.xMax += 2;
			boxPosition.yMax -= SPACING;
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
			EditorGUI.LabelField(headerPos, property.displayName, EditorStyles.centeredGreyMiniLabel);
			//Conditions
			GUIUtils.GetList(property, conditions, false, DrawConditionsHeader).DoList(conditionsPos);
			//Actions
			GUIUtils.GetList(property, actions, false, DrawActionsHeader).DoList(actionsPos);
		}

		private void DrawConditionsHeader(Rect pos, SerializedProperty property)
		{
			pos.xMin = pos.xMax - 120;
			var op = property.FindPropertyRelative(nameof(Rule.conditionOperator));
			op.intValue = GUI.Toolbar(pos, op.intValue, conditionOperatorNames);
		}

		private void DrawActionsHeader(Rect pos, SerializedProperty property)
		{
			pos.xMin = pos.xMax - 120;
			var applyToChildren = property.FindPropertyRelative(nameof(Rule.applyToChildren));
			applyToChildren.boolValue = EditorGUI.ToggleLeft(pos, "Apply to Children", applyToChildren.boolValue);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float h = EditorGUIUtility.singleLineHeight;
			h += EditorGUIUtility.standardVerticalSpacing;
			h += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(Rule.conditions)));
			h += EditorGUIUtility.standardVerticalSpacing;
			h += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(Rule.actions)));
			h += SPACING;
			return h;
		}
	}

	public abstract class ElementDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.xMin += 6;
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
}
