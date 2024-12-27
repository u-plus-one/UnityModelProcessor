using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor.RuleSystem
{
	public class RulesTabGUI
	{
		public SerializedObject extraDataSerializedObject;

		public void OnEnable()
		{

		}

		public void OnDisable()
		{
			GUIUtils.ClearLists();
		}

		public void OnInspectorGUI()
		{
			extraDataSerializedObject.Update();
			DrawRuleSet();
			extraDataSerializedObject.ApplyModifiedProperties();
		}

		public void PreApply()
		{

		}

		public void PostApply()
		{

		}

		private void DrawRuleSet()
		{
			var set = extraDataSerializedObject.FindProperty(nameof(ModelProcessorSettings.ruleSet));
			var enabled = set.FindPropertyRelative(nameof(RuleSet.enabled));
			var rules = set.FindPropertyRelative(nameof(RuleSet.rules));

			GUILayout.Space(20);
			GUILayout.Label("Rule Set", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(enabled);
			using(new EditorGUI.DisabledGroupScope(!enabled.boolValue))
			{
				var list = GUIUtils.GetList(set, rules, false, null);
				list.elementHeight = 0;
				list.DoLayoutList();
			}
		}
	}
}