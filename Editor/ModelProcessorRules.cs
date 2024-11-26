using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModelProcessor.Editor
{
    [System.Serializable]
    public class ModelProcessorRules
    {
        [System.Serializable]
        public class Rule
        {
            public enum ConditionType : int
            {
                Always = 0,
                NameStartsWith = 1,
                NameEndsWith = 2,
                NameContains = 3,
            }

            public enum ActionType : int
            {
                None = 0,
				//game object operations
                SetGameObjectInactive = 001,
                DestroyGameObject = 002,
				MarkStatic = 003,
				//component operations
                RemoveRenderer = 101,
                RemoveCollider = 102,
                AddHelperComponent = 199
            }

            public ConditionType condition = ConditionType.Always;
            public string conditionString = "";
            public ActionType action = ActionType.None;
        }

        public bool enabled = true;
		public Rule[] rules = Array.Empty<Rule>();

		public void ApplyRulesToModel(GameObject model)
		{
			if(!enabled) return;
			ApplyRulesRecursively(model.transform);
		}

		private void ApplyRulesRecursively(Transform obj)
		{
			ApplyRules(obj);
			if(obj == null)
			{
				//Object was destroyed
				Debug.Log("object was destroyed: "+obj.name);
				return;
			}
			foreach(Transform child in obj)
			{
				ApplyRulesRecursively(child);
			}
		}

		private void ApplyRules(Transform obj)
		{
			foreach(var rule in rules)
			{
				if(CheckCondition(obj, rule))
				{
					ApplyActions(obj, rule);
				}
			}
		}

		private bool CheckCondition(Transform obj, Rule rule)
		{
			switch(rule.condition)
			{
				case Rule.ConditionType.Always:
					return true;
				case Rule.ConditionType.NameStartsWith:
					return obj.name.StartsWith(rule.conditionString);
				case Rule.ConditionType.NameEndsWith:
					return obj.name.EndsWith(rule.conditionString);
				case Rule.ConditionType.NameContains:
					return obj.name.Contains(rule.conditionString);
				default:
					Debug.LogError($"Model processor condition of type '{rule.condition}' is not implemented.");
					return false;
			}
		}

		private void ApplyActions(Transform obj, Rule rule)
		{
			switch(rule.action)
			{
				case Rule.ActionType.SetGameObjectInactive:
					obj.gameObject.SetActive(false);
					break;
				case Rule.ActionType.DestroyGameObject:
					Object.DestroyImmediate(obj);
					break;
				case Rule.ActionType.MarkStatic:
					//Set all static flags
					GameObjectUtility.SetStaticEditorFlags(obj.gameObject, (StaticEditorFlags)~0);
					break;
				case Rule.ActionType.RemoveRenderer:
					var renderer = obj.GetComponent<Renderer>();
					if(renderer != null)
						Object.DestroyImmediate(renderer);
					break;
				case Rule.ActionType.RemoveCollider:
					var collider = obj.GetComponent<Collider>();
					if(collider != null)
						Object.DestroyImmediate(collider);
					break;
				case Rule.ActionType.AddHelperComponent:
					var type = System.Type.GetType("HelperComponent");
					if(type != null)
					{
						obj.gameObject.AddComponent(type);
					}
					break;
				default:
					Debug.LogError($"Model processor action of type '{rule.action}' is not implemented.");
					break;
			}
		}
    }
}