using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor
{
	[System.Serializable]
	public class Rule
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

		public enum ConditionType : int
		{
			[InspectorName("Always")]
			Always = 0,
			[InspectorName("GameObject/Is Root")]
			RootObject = 1,
			[InspectorName("GameObject/Inactive (Self)")]
			GameObjectInactiveSelf = 2,
			[InspectorName("GameObject/Inactive (In Hierarchy)")]
			GameObjectInactiveInHierarchy = 3,
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
			[InspectorName("GameObject/Is Empty")]
			IsEmpty = 38,
			[InspectorName("GameObject/Is Empty (No Children)")]
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
			//renderer operations
			[InspectorName("Renderer/Remove")]
			RemoveRenderer = 101,
			[InspectorName("Renderer/Set Cast Shadows Mode")]
			SetCastShadowsMode = 102,
			[InspectorName("Renderer/Set Receive Shadows Mode")]
			SetReceiveShadowsMode = 103,
			[InspectorName("Renderer/Set Lightmap Scale")]
			SetLightmapScale = 104,
			//collider operations
			[InspectorName("Collider/Remove")]
			RemoveCollider = 201,
			[InspectorName("Collider/Set Convex")]
			SetColliderConvex = 202,
			[InspectorName("Collider/Box Collider")]
			ToBoxCollider = 203,
			[InspectorName("Collider/Sphere Collider")]
			ToSphereCollider = 204,
			//Debug stuff
			[InspectorName("Debug/Add Helper Component")]
			AddHelperComponent = 999
		}

		public ConditionType condition = ConditionType.Always;
		public bool invertCondition;
		public string conditionParam = "";

		public ActionType action = ActionType.None;
		public string actionParam = "";
		public bool applyToChildren = false;

		public void ApplyToModel(GameObject modelRoot)
		{
			ApplyRecursively(new PartInfo(modelRoot));
		}

		private void ApplyRecursively(PartInfo obj)
		{
			bool applied = ApplyOnObject(obj);
			//Check if the object itself wasn't destroyed
			if(obj.gameObject != null)
			{
				if(!(applied && applyToChildren))
				{
					var children = new List<Transform>();
					for(int i = 0; i < obj.gameObject.transform.childCount; i++)
					{
						children.Add(obj.gameObject.transform.GetChild(i));
					}
					foreach(var child in children)
					{
						ApplyRecursively(new PartInfo(child.gameObject));
					}
				}
			}
		}

		public bool ApplyOnObject(PartInfo obj)
		{
			bool condition = CheckCondition(obj);
			if(invertCondition) condition = !condition;
			if(condition)
			{
				if(applyToChildren)
				{
					foreach(var child in obj.gameObject.GetComponentsInChildren<Transform>(true))
					{
						PerformAction(new PartInfo(child.gameObject));
					}
				}
				else
				{
					PerformAction(new PartInfo(obj.gameObject));
				}
				return true;
			}
			return false;
		}

		private bool CheckCondition(PartInfo obj)
		{
			switch(condition)
			{
				case ConditionType.Always:
					return true;
				case ConditionType.RootObject:
					return obj.childDepth == 0;
				case ConditionType.GameObjectInactiveSelf:
					return !obj.gameObject.activeSelf;
				case ConditionType.GameObjectInactiveInHierarchy:
					return !obj.gameObject.activeInHierarchy;
				case ConditionType.NameStartsWith:
					return obj.gameObject.name.StartsWith(conditionParam);
				case ConditionType.NameEndsWith:
					return obj.gameObject.name.EndsWith(conditionParam);
				case ConditionType.NameContains:
					return obj.gameObject.name.Contains(conditionParam);
				case ConditionType.NameMatchesRegex:
					return System.Text.RegularExpressions.Regex.IsMatch(obj.gameObject.name, conditionParam);
				case ConditionType.PathStartsWith:
					return obj.hierarchyPath.StartsWith(conditionParam);
				case ConditionType.PathEndsWith:
					return obj.hierarchyPath.EndsWith(conditionParam);
				case ConditionType.PathContains:
					return obj.hierarchyPath.Contains(conditionParam);
				case ConditionType.PathMatchesRegex:
					return System.Text.RegularExpressions.Regex.IsMatch(obj.hierarchyPath, conditionParam);
				case ConditionType.ChildDepthEquals:
					return obj.childDepth == int.Parse(conditionParam);
				case ConditionType.ChildDepthGreaterThan:
					return obj.childDepth > int.Parse(conditionParam);
				case ConditionType.ChildDepthGreaterOrEqual:
					return obj.childDepth >= int.Parse(conditionParam);
				case ConditionType.ChildDepthLessThan:
					return obj.childDepth < int.Parse(conditionParam);
				case ConditionType.ChildDepthLessOrEqual:
					return obj.childDepth <= int.Parse(conditionParam);
				case ConditionType.HasChildren:
					return obj.gameObject.transform.childCount > 0;
				case ConditionType.HasMesh:
					return obj.gameObject.TryGetComponent<Renderer>(out _);
				case ConditionType.HasSkinnedMesh:
					return obj.gameObject.TryGetComponent<SkinnedMeshRenderer>(out _);
				case ConditionType.HasCollider:
					return obj.gameObject.TryGetComponent<Collider>(out _);
				case ConditionType.HasLight:
					return obj.gameObject.TryGetComponent<Light>(out _);
				case ConditionType.HasCamera:
					return obj.gameObject.TryGetComponent<Camera>(out _);
				case ConditionType.IsEmpty:
					return obj.childDepth > 0 && obj.gameObject.GetComponents<Component>().Length == 1;
				case ConditionType.IsEmptyWithoutChildren:
					return obj.childDepth > 0 && obj.gameObject.GetComponents<Component>().Length == 1 && obj.gameObject.transform.childCount == 0;
				default:
					Debug.LogError($"Model processor condition of type '{condition}' is not implemented.");
					return false;
			}
		}

		private void PerformAction(PartInfo part)
		{
			switch(action)
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
					GameObjectUtility.SetStaticEditorFlags(part.gameObject, (StaticEditorFlags)int.Parse(actionParam));
					break;
				case ActionType.SetLayer:
					part.gameObject.layer = LayerMask.NameToLayer(actionParam);
					break;
				case ActionType.SetTag:
					part.gameObject.tag = !string.IsNullOrWhiteSpace(actionParam) ? actionParam : "Untagged";
					break;
				case ActionType.DestroyChildObjects:
					foreach(Transform child in part.gameObject.transform)
					{
						Object.DestroyImmediate(child.gameObject);
					}
					break;
				case ActionType.SetName:
					if(string.IsNullOrEmpty(actionParam))
					{
						Debug.LogError("Attempted to set game object to an empty name.");
					}
					part.gameObject.name = actionParam;
					break;
				case ActionType.PrependName:
					part.gameObject.name = actionParam + part.gameObject.name;
					break;
				case ActionType.AppendName:
					part.gameObject.name += actionParam;
					break;
				case ActionType.RemoveRenderer:
					if(part.gameObject.TryGetComponent<MeshFilter>(out var filter))
						Object.DestroyImmediate(filter);
					if(part.gameObject.TryGetComponent(out Renderer renderer))
						Object.DestroyImmediate(renderer);
					break;
				case ActionType.SetCastShadowsMode:
					if(part.gameObject.TryGetComponent(out renderer))
					{
						var mode = (UnityEngine.Rendering.ShadowCastingMode)System.Enum.Parse(typeof(UnityEngine.Rendering.ShadowCastingMode), actionParam);
						renderer.shadowCastingMode = mode;
					}
					break;
				case ActionType.SetReceiveShadowsMode:
					if(part.gameObject.TryGetComponent(out renderer))
					{
						renderer.receiveShadows = bool.Parse(actionParam);
					}
					break;
				case ActionType.SetLightmapScale:
					if(part.gameObject.TryGetComponent(out renderer))
					{
						SerializedObject so = new SerializedObject(renderer);
						so.FindProperty("m_ScaleInLightmap").floatValue = float.Parse(actionParam);
						so.ApplyModifiedProperties();
					}
					break;
				case ActionType.RemoveCollider:
					if(part.gameObject.TryGetComponent<Collider>(out var collider))
						Object.DestroyImmediate(collider);
					break;
				case ActionType.SetColliderConvex:
					if(part.gameObject.TryGetComponent<MeshCollider>(out var mc))
						mc.convex = bool.Parse(actionParam);
					break;
				case ActionType.ToBoxCollider:
					if(part.gameObject.TryGetComponent(out Collider c))
						Object.DestroyImmediate(c);
					part.gameObject.AddComponent<BoxCollider>();
					break;
				case ActionType.ToSphereCollider:
					if(part.gameObject.TryGetComponent(out Collider c2))
						Object.DestroyImmediate(c2);
					part.gameObject.AddComponent<SphereCollider>();
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
				default:
					Debug.LogError($"Model processor action of type '{action}' is not implemented.");
					break;
			}
		}
	}
}