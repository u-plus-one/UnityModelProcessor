using UnityEngine;

namespace ModelProcessor.Editor.RuleSystem
{
	[System.Serializable]
	public class Rule
	{
		public Operator conditionOperator = Operator.And;
		public Condition[] conditions;
		public Action[] actions;
		public bool applyToChildren = false;

		public void ApplyToModel(GameObject modelRoot)
		{
			ApplyRecursively(new PartInfo(modelRoot));
		}

		private void ApplyRecursively(PartInfo obj)
		{
			//Apply the rule to the object
			bool result = ApplyOnObject(obj);
			//Check if the object itself wasn't destroyed
			if(obj.gameObject == null) return;
			//Rule was already applied to its children, no need to reiterate
			if (result && applyToChildren) return;
			foreach(var child in GetChildren(obj.gameObject.transform))
			{
				ApplyRecursively(new PartInfo(child.gameObject));
			}
		}

		private Transform[] GetChildren(Transform t)
		{
			var children = new Transform[t.childCount];
			for(int i = 0; i < t.childCount; i++)
			{
				children[i] = t.GetChild(i);
			}
			return children;
		}

		public bool ApplyOnObject(PartInfo p)
		{
			if(CheckConditions(p))
			{
				if(applyToChildren)
				{
					foreach(var child in p.gameObject.GetComponentsInChildren<Transform>(true))
					{
						ApplyActions(new PartInfo(child.gameObject));
					}
				}
				else
				{
					ApplyActions(new PartInfo(p.gameObject));
				}
				return true;
			}
			return false;
		}

		private bool CheckConditions(PartInfo p)
		{
			if(conditions == null || conditions.Length == 0) return true;
			if(conditionOperator == Operator.And)
			{
				foreach(var c in conditions)
				{
					if(!c.Evaluate(p)) return false;
				}
				return true;
			}
			else if(conditionOperator == Operator.Or)
			{
				foreach(var c in conditions)
				{
					if(c.Evaluate(p)) return true;
				}
				return false;
			}
			else
			{
				throw new System.NotImplementedException();
			}
		}

		private void ApplyActions(PartInfo p)
		{
			foreach(var a in actions)
			{
				a.Apply(p);
			}
		}
	}
}