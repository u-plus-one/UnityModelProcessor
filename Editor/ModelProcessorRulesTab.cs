using UnityEditor;

namespace ModelProcessor.Editor
{
	public class ModelProcessorRulesTab
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
			var rules = extraDataSerializedObject.FindProperty("rules");
			EditorGUILayout.PropertyField(rules);
		}
	}
}