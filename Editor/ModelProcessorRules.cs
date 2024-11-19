using UnityEngine;

namespace ModelProcessor.Editor
{
    [System.Serializable]
    public class ModelProcessorRules
    {
        [System.Serializable]
        public class Rule
        {
            public enum ConditionType
            {
                Always = 0,
                NameStartsWith = 1,
                NameEndsWith = 2,
                NameContains = 3,
            }

            public enum ActionType
            {
                None = 0,
                SetGameObjectInactive = 1,
                DestroyGameObject = 2,
                RemoveRenderer = 3,
                RemoveCollider = 4,
                AddHelperComponent = 99
            }

            public ConditionType condition = ConditionType.Always;
            public string conditionString = "";
            public ActionType action = ActionType.None;
        }

        public bool enabled = true;
		public Rule[] rules = new Rule[0];

		public void ProcessModel(GameObject model)
		{
			if(!enabled) return;

			foreach(var rule in rules)
			{
				if(rule.condition == Rule.ConditionType.Always)
				{
					ProcessRule(model, rule);
				}
				else
				{
					switch(rule.condition)
					{
						case Rule.ConditionType.NameStartsWith:
							if(model.name.StartsWith(rule.conditionString))
								ProcessRule(model, rule);
							break;
						case Rule.ConditionType.NameEndsWith:
							if(model.name.EndsWith(rule.conditionString))
								ProcessRule(model, rule);
							break;
						case Rule.ConditionType.NameContains:
							if(model.name.Contains(rule.conditionString))
								ProcessRule(model, rule);
							break;
					}
				}
			}
		}

		private void ProcessRule(GameObject model, Rule rule)
		{
			switch(rule.action)
			{
				case Rule.ActionType.SetGameObjectInactive:
					model.SetActive(false);
					break;
				case Rule.ActionType.DestroyGameObject:
					Object.DestroyImmediate(model);
					break;
				case Rule.ActionType.RemoveRenderer:
					var renderer = model.GetComponent<Renderer>();
					if(renderer != null)
						Object.DestroyImmediate(renderer);
					break;
				case Rule.ActionType.RemoveCollider:
					var collider = model.GetComponent<Collider>();
					if(collider != null)
						Object.DestroyImmediate(collider);
					break;
				case Rule.ActionType.AddHelperComponent:
					var type = System.Type.GetType("HelperComponent");
					if(type != null)
					{
						model.AddComponent(type);
					}
					break;
			}
		}
	}
}