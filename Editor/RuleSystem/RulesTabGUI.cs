using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ModelProcessor.Editor.RuleSystem
{
	public class RulesTabGUI
	{
		public SerializedObject extraDataSerializedObject;

		private ReorderableList localRulesList;
		private ReorderableList externalRulesList;

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
			var enabled = extraDataSerializedObject.FindProperty(nameof(ModelProcessorSettings.applyRules));
			var applyProjectRules = extraDataSerializedObject.FindProperty(nameof(ModelProcessorSettings.applyProjectRules));
			var rules = extraDataSerializedObject.FindProperty(nameof(ModelProcessorSettings.rules));
			var externalRuleAssets = extraDataSerializedObject.FindProperty(nameof(ModelProcessorSettings.externalRules));

			GUILayout.Space(20);
			GUILayout.Label("Processor Rules", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(enabled);
			GUILayout.Space(10);
			using(new EditorGUI.DisabledGroupScope(!enabled.boolValue))
			{
				EditorGUILayout.PropertyField(applyProjectRules);
				GUILayout.Space(10);
				if(localRulesList == null)
				{
					localRulesList = GUIUtils.CreateReorderableList(rules, true);
					localRulesList.elementHeight = 0;
					localRulesList.showDefaultBackground = true;
				}
				localRulesList.DoLayoutList();
				if(externalRulesList == null)
				{
					externalRulesList = GUIUtils.CreateReorderableList(externalRuleAssets, true);
					externalRulesList.elementHeight = 0;
					externalRulesList.showDefaultBackground = true;
				}
				externalRulesList.DoLayoutList();
			}
		}
	}
}