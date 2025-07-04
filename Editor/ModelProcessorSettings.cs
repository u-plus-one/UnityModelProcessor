using ModelProcessor.Editor.RuleSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModelProcessor.Editor
{
	public class ModelProcessorSettings : ScriptableObject
	{
		[System.Serializable]
		public class RuleSet
		{
			public bool enabled = true;
			public Rule[] rules = Array.Empty<Rule>();

			public void ApplyRulesToModel(GameObject model)
			{
				if(!enabled) return;
				foreach(var rule in rules)
				{
					rule.ApplyToModel(model.gameObject);
				}
			}
		}

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

		public bool applyRules = true;
		public bool applyProjectRules = true;
		public List<Rule> rules = new List<Rule>();
		public List<RuleAsset> externalRules = new List<RuleAsset>();

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
