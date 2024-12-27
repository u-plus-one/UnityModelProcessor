using UnityEngine;

namespace ModelProcessor.Editor.RuleSystem
{
	public enum Operator : byte
	{
		And = 0,
		Or = 1
	}

	public enum ConditionType : int
	{
		[InspectorName("Always")]
		Always = 0,
		[InspectorName("Root Object")]
		RootObject = 1,
		//name conditions
		[InspectorName("Name/Starts With")]
		NameStartsWith = 11,
		[InspectorName("Name/Ends With")]
		NameEndsWith = 12,
		[InspectorName("Name/Contains")]
		NameContains = 13,
		[InspectorName("Name/Matches Regex")]
		NameMatchesRegex = 14,
		[InspectorName("Path/Starts With")]
		PathStartsWith = 15,
		[InspectorName("Path/Ends With")]
		PathEndsWith = 16,
		[InspectorName("Path/Contains")]
		PathContains = 17,
		[InspectorName("Path/Matches Regex")]
		PathMatchesRegex = 18,
		//parent-child conditions
		[InspectorName("Child Depth/==")]
		ChildDepthEquals = 21,
		[InspectorName("Child Depth/>")]
		ChildDepthGreaterThan = 22,
		[InspectorName("Child Depth/>=")]
		ChildDepthGreaterOrEqual = 23,
		[InspectorName("Child Depth/<")]
		ChildDepthLessThan = 24,
		[InspectorName("Child Depth/<=")]
		ChildDepthLessOrEqual = 25,
		[InspectorName("Has Children")]
		HasChildren = 26,
		//component conditions
		[InspectorName("Has Component/Mesh")]
		HasMesh = 31,
		[InspectorName("Has Component/Skinned Mesh")]
		HasSkinnedMesh = 32,
		[InspectorName("Has Component/Collider")]
		HasCollider = 35,
		[InspectorName("Has Component/Light")]
		HasLight = 36,
		[InspectorName("Has Component/Camera")]
		HasCamera = 37,
		[InspectorName("Is Empty")]
		IsEmpty = 38,
		[InspectorName("Is Empty (No Children)")]
		IsEmptyWithoutChildren = 39,
	}

	[System.Serializable]
	public class Condition
	{
		public bool invert;
		public ConditionType type = ConditionType.Always;
		public string parameter = "";

		public bool Evaluate(PartInfo p)
		{
			bool result = PerformCheck(p);
			if(invert) result = !result;
			return result;
		}

		private bool PerformCheck(PartInfo p)
		{
			switch(type)
			{
				case ConditionType.Always:
					return true;
				case ConditionType.RootObject:
					return p.childDepth == 0;
				case ConditionType.NameStartsWith:
					return p.gameObject.name.StartsWith(parameter);
				case ConditionType.NameEndsWith:
					return p.gameObject.name.EndsWith(parameter);
				case ConditionType.NameContains:
					return p.gameObject.name.Contains(parameter);
				case ConditionType.NameMatchesRegex:
					return System.Text.RegularExpressions.Regex.IsMatch(p.gameObject.name, parameter);
				case ConditionType.PathStartsWith:
					return p.hierarchyPath.StartsWith(parameter);
				case ConditionType.PathEndsWith:
					return p.hierarchyPath.EndsWith(parameter);
				case ConditionType.PathContains:
					return p.hierarchyPath.Contains(parameter);
				case ConditionType.PathMatchesRegex:
					return System.Text.RegularExpressions.Regex.IsMatch(p.hierarchyPath, parameter);
				case ConditionType.ChildDepthEquals:
					return p.childDepth == int.Parse(parameter);
				case ConditionType.ChildDepthGreaterThan:
					return p.childDepth > int.Parse(parameter);
				case ConditionType.ChildDepthGreaterOrEqual:
					return p.childDepth >= int.Parse(parameter);
				case ConditionType.ChildDepthLessThan:
					return p.childDepth < int.Parse(parameter);
				case ConditionType.ChildDepthLessOrEqual:
					return p.childDepth <= int.Parse(parameter);
				case ConditionType.HasChildren:
					return p.gameObject.transform.childCount > 0;
				case ConditionType.HasMesh:
					return p.gameObject.TryGetComponent<Renderer>(out _);
				case ConditionType.HasSkinnedMesh:
					return p.gameObject.TryGetComponent<SkinnedMeshRenderer>(out _);
				case ConditionType.HasCollider:
					return p.gameObject.TryGetComponent<Collider>(out _);
				case ConditionType.HasLight:
					return p.gameObject.TryGetComponent<Light>(out _);
				case ConditionType.HasCamera:
					return p.gameObject.TryGetComponent<Camera>(out _);
				case ConditionType.IsEmpty:
					return p.childDepth > 0 && p.gameObject.GetComponents<Component>().Length == 1;
				case ConditionType.IsEmptyWithoutChildren:
					return p.childDepth > 0 && p.gameObject.GetComponents<Component>().Length == 1 && p.gameObject.transform.childCount == 0;
				default:
					Debug.LogError($"Model processor condition of type '{type}' is not implemented.");
					return false;
			}
		}
	}
}