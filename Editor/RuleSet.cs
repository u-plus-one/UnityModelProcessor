using System;
using UnityEngine;

namespace ModelProcessor.Editor
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
}