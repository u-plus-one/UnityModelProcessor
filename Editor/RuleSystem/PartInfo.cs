using UnityEngine;

namespace ModelProcessor.Editor.RuleSystem
{
	public struct PartInfo
	{
		public readonly GameObject gameObject;
		public readonly string name;
		public readonly string hierarchyPath;
		public readonly int childDepth;

		public PartInfo(GameObject g)
		{
			var t = g.transform;
			gameObject = g;
			name = g.name;
			childDepth = 0;
			hierarchyPath = t.name;
			while(t.parent != null)
			{
				childDepth++;
				hierarchyPath = t.parent.name + "/" + hierarchyPath;
				t = t.parent;
			}
		}
	}
}