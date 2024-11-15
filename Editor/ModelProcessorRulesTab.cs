using UnityEditor;

namespace ModelProcessor.Editor
{
	public class ModelProcessorRulesTab
	{
		private SerializedObject extraDataSerializedObject;

		public ModelProcessorRulesTab(SerializedObject extraDataSerializedObject)
		{
			this.extraDataSerializedObject = extraDataSerializedObject;
			this.extraDataSerializedObject.FindProperty("Rules").managedReferenceValue
		}

		public void OnEnable()
		{

		}

		public void OnDisable()
		{

		}

		public void OnInspectorGUI()
		{
			//TODO
		}
	}
}