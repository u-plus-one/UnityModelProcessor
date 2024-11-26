using UnityEngine;

namespace ModelProcessor.Editor
{
	public class ModelProcessorSettings : ScriptableObject
	{
		[Tooltip("Flips the Y and Z coordinates to avoid incorrect rotations of 90 degrees")]
		public bool applyAxisConversion = false;
		[Tooltip("Rotate model by 180 degrees on the up axis to match Blender's axes with the ones in Unity")]
		public bool matchAxes = false;

		[Tooltip("Apply range and intensity corrections")]
		public bool fixLights = false;
		[Tooltip("Factor to multiply the light intensity by")]
		public float lightIntensityFactor = 0.01f;
		[Tooltip("Factor to multiply the light range by")]
		public float lightRangeFactor = 0.1f;

		public static ModelProcessorSettings FromJson(string userDataJson)
		{
			var settings = CreateInstance<ModelProcessorSettings>();
			JsonUtility.FromJsonOverwrite(userDataJson, settings);
			return settings;
		}

		public void LoadJson(string userDataJson)
		{
			JsonUtility.FromJsonOverwrite(userDataJson, this);
		}

		public string ToJson()
		{
			return JsonUtility.ToJson(this);
		}
	} 
}
