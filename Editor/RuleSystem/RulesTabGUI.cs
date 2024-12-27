using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ModelProcessor.Editor.RuleSystem
{
	public class RulesTabGUI
	{
		public SerializedObject extraDataSerializedObject;

		private ReorderableList list;

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
			var enabled = set.FindPropertyRelative(nameof(RuleSet.enabled));

			GUILayout.Space(20);
			GUILayout.Label("Rule Set", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(enabled);
			using(new EditorGUI.DisabledGroupScope(!enabled.boolValue))
			{
				if(list == null)
				{
					list = GUIUtils.CreateReorderableList(extraDataSerializedObject
						.FindProperty(nameof(ModelProcessorSettings.ruleSet))
						.FindPropertyRelative(nameof(RuleSet.rules)), true);
					list.elementHeight = 0;
					list.showDefaultBackground = true;
				}

				list.DoLayoutList();
			}
		}
	}
}