using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor
{
	public class RulesTabGUI
	{
		public SerializedObject extraDataSerializedObject;

		public void OnEnable()
		{

		}

		public void OnDisable()
		{

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
			EditorGUILayout.PropertyField(set);
			/*
			GUILayout.Label("Rules", EditorStyles.boldLabel);
			GUILayout.BeginVertical(EditorStyles.helpBox);
			var rulesEnabled = set.FindPropertyRelative(nameof(RuleSet.enabled));
			EditorGUILayout.PropertyField(rulesEnabled);
			GUI.enabled = rulesEnabled.boolValue;
			var rules = set.FindPropertyRelative(nameof(RuleSet.rules));
			for(int i = 0; i < rules.arraySize; i++)
			{
				var rule = rules.GetArrayElementAtIndex(i);
				DrawRule(rule, i);
			}
			GUI.enabled = true;
			GUILayout.EndVertical();
			*/
		}
	}
}