using System.Reflection;
using UnityEditor;
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

		public void Initialize(AssetUserData userData)
		{
			var serializedObj = new SerializedObject(this);
			var prop = serializedObj.GetIterator();
			prop.Next(true);
			while(prop.Next(false))
			{
				InitField(userData, prop);
			}
			serializedObj.ApplyModifiedPropertiesWithoutUndo();
		}

		private bool InitField(AssetUserData userData, SerializedProperty property)
		{
			string propName = property.name;
			if(userData.ContainsKey(propName))
			{
				switch(property.propertyType)
				{
					case SerializedPropertyType.Boolean: 
						property.boolValue = userData.GetBool(propName);
						break;
					case SerializedPropertyType.Integer:
						property.intValue = userData.GetInt(propName);
						break;
					case SerializedPropertyType.Float:
						property.floatValue = userData.GetFloat(propName);
						break;
					case SerializedPropertyType.String:
						property.stringValue = userData.GetString(propName);
						break;
					default:
						throw new System.NotImplementedException();
				}
				return true;
			}
			return false;
		}
	} 
}
