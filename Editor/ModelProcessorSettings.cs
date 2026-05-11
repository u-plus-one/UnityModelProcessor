using ModelProcessor.Editor.RuleSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
		[SerializeField, HideInInspector]
		private List<string> externalRuleGuids = new List<string>();
		
		public static ModelProcessorSettings FromJson(string userDataJson)
		{
			var settings = CreateInstance<ModelProcessorSettings>();
			settings.LoadJson(userDataJson);
			return settings;
		}

		public void LoadJson(string userDataJson)
		{
			JsonUtility.FromJsonOverwrite(userDataJson, this);
			externalRules.Clear();
			foreach (var guid in externalRuleGuids)
			{
				if (string.IsNullOrEmpty(guid))
				{
					externalRules.Add(null);
					continue;
				}
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path))
				{
					Debug.LogError("Could not locate external rule asset with GUID: " + guid);
					continue;
				}
				var asset = AssetDatabase.LoadAssetAtPath<RuleAsset>(path);
				externalRules.Add(asset);
				if (asset == null)
				{
					Debug.LogError("Could not load external rule asset at path: " + path);
				}
			}
		}

		public string ToJson()
		{
			externalRuleGuids.Clear();
			foreach (var rule in externalRules)
			{
				string guid;
				if (rule)
				{
					var path = AssetDatabase.GetAssetPath(rule);
					guid = AssetDatabase.AssetPathToGUID(path);
				}
				else
				{
					guid = "";
				}
				externalRuleGuids.Add(guid);
			}
			// Clear external rule asset list to avoid serializing instance IDs
			var clone = Object.Instantiate(this);
			clone.externalRules.Clear();
			return JsonUtility.ToJson(clone);
		}
	} 
}
