using UnityEditor;

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
			var rules = extraDataSerializedObject.FindProperty("rules");
			EditorGUILayout.PropertyField(rules);
		}
	}
}