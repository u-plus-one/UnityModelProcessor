using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor
{
	[System.Serializable]
	public class Rule
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

		public enum ActionType : int
		{
			None = 0,
			//game object operations
			[InspectorName("GameObject/Set Inactive")]
			SetGameObjectInactive = 001,
			[InspectorName("GameObject/Destroy")]
			DestroyGameObject = 002,
			[InspectorName("GameObject/Destroy Children")]
			DestroyChildObjects = 003,
			[InspectorName("GameObject/Mark Static")]
			MarkStatic = 004,
			[InspectorName("GameObject/Set Static Flags")]
			SetStaticFlags = 005,
			[InspectorName("GameObject/Set Layer")]
			SetLayer = 010,
			[InspectorName("GameObject/Set Tag")]
			SetTag = 011,
			[InspectorName("Object Name/Set")]
			SetName = 012,
			[InspectorName("Object Name/Prepend")]
			PrependName = 013,
			[InspectorName("Object Name/Append")]
			AppendName = 014,
			//component operations
			[InspectorName("Renderer/Remove")]
			RemoveRenderer = 101,
			[InspectorName("Collider/Remove")]
			RemoveCollider = 102,
			//rendering operations
			[InspectorName("Renderer/Set Cast Shadows Mode")]
			SetCastShadowsMode = 201,
			[InspectorName("Renderer/Set Receive Shadows Mode")]
			SetReceiveShadowsMode = 202,
			[InspectorName("Renderer/Set Lightmap Scale")]
			SetLightmapScale = 203,
			//Debug stuff
			[InspectorName("Debug/Add Helper Component")]
			AddHelperComponent = 999
		}

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

		public class Action
		{
			public ActionType type = ActionType.None;
			public string parameter = "";

			public void Apply(PartInfo part)
			{
				switch(type)
				{
					case ActionType.None:
						break;
					case ActionType.SetGameObjectInactive:
						part.gameObject.SetActive(false);
						break;
					case ActionType.DestroyGameObject:
						Object.DestroyImmediate(part.gameObject);
						break;
					case ActionType.MarkStatic:
						//Set all static flags
						GameObjectUtility.SetStaticEditorFlags(part.gameObject, (StaticEditorFlags)~0);
						break;
					case ActionType.SetStaticFlags:
						GameObjectUtility.SetStaticEditorFlags(part.gameObject, (StaticEditorFlags)int.Parse(parameter));
						break;
					case ActionType.SetLayer:
						part.gameObject.layer = LayerMask.NameToLayer(parameter);
						break;
					case ActionType.SetTag:
						part.gameObject.tag = !string.IsNullOrWhiteSpace(parameter) ? parameter : "Untagged";
						break;
					case ActionType.DestroyChildObjects:
						foreach(Transform child in part.gameObject.transform)
						{
							Object.DestroyImmediate(child.gameObject);
						}
						break;
					case ActionType.SetName:
						if(string.IsNullOrEmpty(parameter))
						{
							Debug.LogError("Attempted to set game object to an empty name.");
						}
						part.gameObject.name = parameter;
						break;
					case ActionType.PrependName:
						part.gameObject.name = parameter + part.gameObject.name;
						break;
					case ActionType.AppendName:
						part.gameObject.name += parameter;
						break;
					case ActionType.RemoveRenderer:
						if(part.gameObject.TryGetComponent<MeshFilter>(out var filter))
							Object.DestroyImmediate(filter);
						if(part.gameObject.TryGetComponent(out Renderer renderer))
							Object.DestroyImmediate(renderer);
						break;
					case ActionType.RemoveCollider:
						if(part.gameObject.TryGetComponent<Collider>(out var collider))
							Object.DestroyImmediate(collider);
						break;
					case ActionType.AddHelperComponent:
						var type = System.Type.GetType("HelperComponent,Assembly-CSharp", false, true);
						if(type != null)
						{
							part.gameObject.AddComponent(type);
						}
						else
						{
							Debug.LogError("AddHelperComponent requires a script named 'HelperComponent' in the project.");
						}
						break;
					case ActionType.SetCastShadowsMode:
						if(part.gameObject.TryGetComponent(out renderer))
						{
							var mode = (UnityEngine.Rendering.ShadowCastingMode)System.Enum.Parse(typeof(UnityEngine.Rendering.ShadowCastingMode), parameter);
							renderer.shadowCastingMode = mode;
						}
						break;
					case ActionType.SetReceiveShadowsMode:
						if(part.gameObject.TryGetComponent(out renderer))
						{
							renderer.receiveShadows = bool.Parse(parameter);
						}
						break;
					case ActionType.SetLightmapScale:
						if(part.gameObject.TryGetComponent(out renderer))
						{
							SerializedObject so = new SerializedObject(renderer);
							so.FindProperty("m_ScaleInLightmap").floatValue = float.Parse(parameter);
							so.ApplyModifiedProperties();
						}
						break;
					default:
						Debug.LogError($"Model processor action of type '{this.type}' is not implemented.");
						break;
				}
			}
		}

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

		public Operator conditionOperator = Operator.And;
		public Condition[] conditions;
		public bool applyToChildren = false;
		public Action[] actions;

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