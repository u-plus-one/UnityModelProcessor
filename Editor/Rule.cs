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
			Always = 0,
			RootObject = 1,
			//name conditions
			NameStartsWith = 11,
			NameEndsWith = 12,
			NameContains = 13,
			NameMatchesRegex = 14,
			PathStartsWith = 15,
			PathEndsWith = 16,
			PathContains = 17,
			PathMatchesRegex = 18,
			//component conditions
			[InspectorName("Child Depth ==")]
			ChildDepthEquals = 21,
			[InspectorName("Child Depth >")]
			ChildDepthGreaterThan = 22,
			[InspectorName("Child Depth >=")]
			ChildDepthGreaterOrEqual = 23,
			[InspectorName("Child Depth <")]
			ChildDepthLessThan = 24,
			[InspectorName("Child Depth <=")]
			ChildDepthLessOrEqual = 25,
			HasMesh = 31,
			HasSkinnedMesh = 32,
			HasCollider = 35,
			HasLight = 36,
			HasCamera = 37,
			IsEmpty = 38,
			[InspectorName("Is Empty (No Children)")]
			IsEmptyWithoutChildren = 39,
		}

		public enum ActionType : int
		{
			None = 0,
			//game object operations
			SetGameObjectInactive = 001,
			DestroyGameObject = 002,
			DestroyChildObjects = 003,
			MarkStatic = 004,
			SetStaticFlags = 005,
			SetLayer = 010,
			SetTag = 011,
			SetName = 012,
			PrependName = 013,
			AppendName = 014,
			//component operations
			RemoveRenderer = 101,
			RemoveCollider = 102,
			//rendering operations
			SetCastShadowsMode = 201,
			SetReceiveShadowsMode = 202,
			SetLightmapScale = 203,
			//Debug stuff
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
				case ActionType.RemoveCollider:
					if(part.gameObject.TryGetComponent<Collider>(out var collider))
						Object.DestroyImmediate(collider);
					break;
				case ActionType.AddHelperComponent:
					var type = System.Type.GetType("HelperComponent");
					if(type != null)
					{
						part.gameObject.AddComponent(type);
					}
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
				default:
					Debug.LogError($"Model processor action of type '{action}' is not implemented.");
					break;
			}
		}
	}
}