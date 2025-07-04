using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor.RuleSystem
{
	[CreateAssetMenu(menuName = "Model Processor Rules", fileName = "ModelProcessorRules")]
	public class RuleAsset : ScriptableObject
	{
		public List<Rule> rules = new List<Rule>();

		public void ApplyToModel(GameObject modelRoot)
		{
			foreach(var rule in rules)
			{
				rule.ApplyToModel(modelRoot);
			}
		}

		public IEnumerable<Rule> EnumerateRules()
		{
			return rules;
		}
	}
}