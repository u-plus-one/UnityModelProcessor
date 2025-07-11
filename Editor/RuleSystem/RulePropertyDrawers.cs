using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace ModelProcessor.Editor.RuleSystem
{
	[CustomPropertyDrawer(typeof(Rule))]
	public class RuleGUI : PropertyDrawer
	{
		const float SPACING = 10;

		private static GUIStyle headerStyle = null;

		private Dictionary<string, ReorderableList> lists = new Dictionary<string, ReorderableList>();

		private static string[] conditionOperatorNames = System.Enum.GetNames(typeof(Operator)).Select(s => s.ToUpper()).ToArray();

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			return base.CreatePropertyGUI(property);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			property.isExpanded = true;
			var conditions = property.FindPropertyRelative(nameof(Rule.conditions));
			conditions.isExpanded = true;
			var actions = property.FindPropertyRelative(nameof(Rule.actions));
			actions.isExpanded = true;

			if(headerStyle == null) headerStyle = new GUIStyle(EditorStyles.toolbar) { fixedHeight = 0 };
			if(!lists.TryGetValue(conditions.propertyPath, out var conditionsList))
			{
				conditionsList = GUIUtils.CreateReorderableList(conditions, false);
				conditionsList.showDefaultBackground = false;
				conditionsList.drawHeaderCallback = pos => DrawConditionsHeader(conditionsList, pos, property);
				conditionsList.displayAdd = false;
				conditionsList.displayRemove = false;
				//conditionsList.drawFooterCallback = pos => DrawListFooter(conditionsList, pos);
				conditionsList.footerHeight = 0;
				conditionsList.drawFooterCallback = _ => { };
				conditionsList.drawNoneElementCallback = pos => GUI.Label(pos, "None (All Objects)");
				conditionsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
				{
					DrawListElement(conditionsList, index, rect);
				};
				lists[conditions.propertyPath] = conditionsList;
			}
			if(!lists.TryGetValue(actions.propertyPath, out var actionsList))
			{
				actionsList = GUIUtils.CreateReorderableList(actions, false);
				actionsList.showDefaultBackground = false;
				actionsList.drawHeaderCallback = pos => DrawActionsHeader(actionsList, pos, property);
				actionsList.displayAdd = false;
				actionsList.displayRemove = false;
				actionsList.footerHeight = 0;
				//actionsList.drawFooterCallback = pos => DrawListFooter(conditionsList, pos);
				actionsList.drawFooterCallback = _ => { };
				actionsList.drawNoneElementCallback = pos => GUI.Label(pos, "None (All Objects)");
				actionsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
				{
					DrawListElement(actionsList, index, rect);
				};
				lists[actions.propertyPath] = actionsList;
			}

			position.xMin -= 8;

			var boxPosition = position;
			boxPosition.xMin -= 2;
			boxPosition.xMax += 2;
			boxPosition.yMax -= SPACING;
			GUI.Box(boxPosition, GUIContent.none, EditorStyles.helpBox);

			position.height = EditorGUIUtility.singleLineHeight;
			var headerPos = position;
			var conditionsPos = GUIUtils.NextProperty(headerPos, EditorGUI.GetPropertyHeight(conditions));
			var actionsPos = GUIUtils.NextProperty(conditionsPos, EditorGUI.GetPropertyHeight(actions));

			EditorGUIUtility.labelWidth = 60;

			//Header
			EditorGUI.LabelField(headerPos, property.displayName, EditorStyles.centeredGreyMiniLabel);
			//Conditions
			//GUI.Box(conditionsPos, GUIContent.none, EditorStyles.helpBox);
			conditionsList.DoList(conditionsPos);
			//Actions
			//GUI.Box(actionsPos, GUIContent.none, EditorStyles.helpBox);
			actionsList.DoList(actionsPos);
		}

		private static void DrawListElement(ReorderableList conditionsList, int index, Rect rect)
		{
			var element = conditionsList.serializedProperty.GetArrayElementAtIndex(index);
			GUIUtils.SplitRight(rect, 20, out rect, out var removePos);
			EditorGUI.PropertyField(rect, element, GUIContent.none);
			if(GUI.Button(removePos, ReorderableList.defaultBehaviours.iconToolbarMinus, ReorderableList.defaultBehaviours.preButton))
			{
				conditionsList.Select(index);
				ReorderableList.defaultBehaviours.DoRemoveButton(conditionsList);
			}
		}

		private void DrawListHeader(ReorderableList list, Rect pos)
		{
			var boxPos = pos;
			boxPos.xMin -= 8;
			boxPos.xMax += 8;
			boxPos.yMin -= 2;
			boxPos.yMax += 2;
			GUI.Box(boxPos, GUIContent.none, EditorStyles.helpBox);
			GUI.Label(pos, list.serializedProperty.displayName, EditorStyles.boldLabel);
		}

		private void DrawListFooter(ReorderableList list, Rect pos)
		{
			pos.xMax -= 8;
			GUIUtils.SplitRight(pos, pos.height, out pos, out var removePos);
			GUIUtils.SplitRight(pos, pos.height, out pos, out var addPos);
			if(GUI.Button(addPos, ReorderableList.defaultBehaviours.iconToolbarPlus, ReorderableList.defaultBehaviours.preButton))
			{
				ReorderableList.defaultBehaviours.DoAddButton(list);
			}
			using(new EditorGUI.DisabledGroupScope(list.count <= 0))
			{
				if(GUI.Button(removePos, ReorderableList.defaultBehaviours.iconToolbarMinus, ReorderableList.defaultBehaviours.preButton))
				{
					ReorderableList.defaultBehaviours.DoRemoveButton(list);
				}
			}
		}

		private void DrawConditionsHeader(ReorderableList list, Rect pos, SerializedProperty property)
		{
			DrawListHeader(list, pos);
			GUIUtils.SplitRight(pos, 120, out pos, out var toolbarPos);
			GUIUtils.SplitRight(toolbarPos, 20, out toolbarPos, out var addPos);
			var op = property.FindPropertyRelative(nameof(Rule.conditionOperator));
			op.intValue = GUI.Toolbar(toolbarPos, op.intValue, conditionOperatorNames);
			if(GUI.Button(addPos, ReorderableList.defaultBehaviours.iconToolbarPlus, ReorderableList.defaultBehaviours.preButton))
			{
				ReorderableList.defaultBehaviours.DoAddButton(list);
			}
		}

		private void DrawActionsHeader(ReorderableList list, Rect pos, SerializedProperty property)
		{
			DrawListHeader(list, pos);
			GUIUtils.SplitRight(pos, 120, out pos, out var toolbarPos);
			GUIUtils.SplitRight(toolbarPos, 20, out toolbarPos, out var addPos);
			var applyToChildren = property.FindPropertyRelative(nameof(Rule.applyToChildren));
			applyToChildren.boolValue = EditorGUI.ToggleLeft(toolbarPos, "Apply to Children", applyToChildren.boolValue);
			if(GUI.Button(addPos, ReorderableList.defaultBehaviours.iconToolbarPlus, ReorderableList.defaultBehaviours.preButton))
			{
				ReorderableList.defaultBehaviours.DoAddButton(list);
			}
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
