﻿using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor
{
	public class ModelProcessorSettings : ScriptableObject
	{
		public bool applyAxisConversion = false;
		public bool flipZAxis = true;

		public bool fixLights = true;
		public float lightIntensityFactor = 0.01f;
		public float lightRangeFactor = 0.1f;

		public ModelProcessorRules rules = new ModelProcessorRules();

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
