using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModelProcessor.Editor
{
	[System.Serializable]
	public class ModelProcessorRules
	{
		public struct PartInfo
		{
			public readonly GameObject gameObject;
			public readonly string hierarchyPath;
			public readonly int childDepth;

			public PartInfo(GameObject g)
			{
				var t = g.transform;
				gameObject = g;
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

		[System.Serializable]
		public class Rule
		{
			public enum ConditionType : int
			{
				Always = 0,
				//name conditions
				NameStartsWith = 1,
				NameEndsWith = 2,
				NameContains = 3,
				NameMatchesRegex = 4,
				PathStartsWith = 5,
				PathEndsWith = 6,
				PathContains = 7,
				PathMatchesRegex = 8,
				//component conditions
				[InspectorName("Child Depth ==")]
				ChildDepthEquals = 11,
				[InspectorName("Child Depth >")]
				ChildDepthGreaterThan = 12,
				[InspectorName("Child Depth >=")]
				ChildDepthGreaterOrEqual = 13,
				[InspectorName("Child Depth <")]
				ChildDepthLessThan = 14,
				[InspectorName("Child Depth <=")]
				ChildDepthLessOrEqual = 15,
				HasMesh = 21,
				HasSkinnedMesh = 22,
				HasCollider = 25,
				HasLight = 26,
				HasCamera = 27
			}

			public enum ActionType : int
			{
				None = 0,
				//game object operations
				SetGameObjectInactive = 001,
				DestroyGameObject = 002,
				MarkStatic = 003,
				SetLayer = 004,
				SetTag = 005,
				DestroyChildObjects = 006,
				SetName = 007,
				PrependName = 008,
				AppendName = 009,
				//component operations
				RemoveRenderer = 101,
				RemoveCollider = 102,
				AddHelperComponent = 199,
				//rendering operations
				SetCastShadowsMode = 201,
				SetReceiveShadowsMode = 202,
				SetLightmapScale = 203,
			}

			public ConditionType condition = ConditionType.Always;
			public string conditionString = "";
			public bool invertCondition;

			public ActionType action = ActionType.None;
			public string actionString = "";
			public bool applyToChildren = false;

			public void ApplyRecursively(PartInfo obj)
			{
				bool applied = ApplyOnObject(obj);
				//Check if the object itself wasn't destroyed
				if(obj.gameObject != null)
				{
					if(!(applied && applyToChildren))
					{
						foreach(Transform child in obj.gameObject.transform)
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
							PerformAction(child.gameObject);
						}
					}
					else
					{
						PerformAction(obj.gameObject);
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
					case ConditionType.NameStartsWith:
						return obj.gameObject.name.StartsWith(conditionString);
					case ConditionType.NameEndsWith:
						return obj.gameObject.name.EndsWith(conditionString);
					case ConditionType.NameContains:
						return obj.gameObject.name.Contains(conditionString);
					case ConditionType.NameMatchesRegex:
						return System.Text.RegularExpressions.Regex.IsMatch(obj.gameObject.name, conditionString);
					case ConditionType.PathStartsWith:
						return obj.hierarchyPath.StartsWith(conditionString);
					case ConditionType.PathEndsWith:
						return obj.hierarchyPath.EndsWith(conditionString);
					case ConditionType.PathContains:
						return obj.hierarchyPath.Contains(conditionString);
					case ConditionType.PathMatchesRegex:
						return System.Text.RegularExpressions.Regex.IsMatch(obj.hierarchyPath, conditionString);
					case ConditionType.ChildDepthEquals:
						return obj.childDepth == int.Parse(conditionString);
					case ConditionType.ChildDepthGreaterThan:
						return obj.childDepth > int.Parse(conditionString);
					case ConditionType.ChildDepthGreaterOrEqual:
						return obj.childDepth >= int.Parse(conditionString);
					case ConditionType.ChildDepthLessThan:
						return obj.childDepth < int.Parse(conditionString);
					case ConditionType.ChildDepthLessOrEqual:
						return obj.childDepth <= int.Parse(conditionString);
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
					default:
						Debug.LogError($"Model processor condition of type '{condition}' is not implemented.");
						return false;
				}
			}

			private void PerformAction(GameObject obj)
			{
				switch(action)
				{
					case ActionType.None:
						break;
					case ActionType.SetGameObjectInactive:
						obj.gameObject.SetActive(false);
						break;
					case ActionType.DestroyGameObject:
						Object.DestroyImmediate(obj);
						break;
					case ActionType.MarkStatic:
						//Set all static flags
						GameObjectUtility.SetStaticEditorFlags(obj.gameObject, (StaticEditorFlags)~0);
						break;
					case ActionType.SetLayer:
						obj.gameObject.layer = LayerMask.NameToLayer(actionString);
						break;
					case ActionType.SetTag:
						obj.gameObject.tag = actionString;
						break;
					case ActionType.DestroyChildObjects:
						foreach(Transform child in obj.transform)
						{
							Object.DestroyImmediate(child.gameObject);
						}
						break;
					case ActionType.SetName:
						obj.name = actionString;
						break;
					case ActionType.PrependName:
						obj.name = actionString + obj.name;
						break;
					case ActionType.AppendName:
						obj.name += actionString;
						break;
					case ActionType.RemoveRenderer:
						if(obj.TryGetComponent<MeshFilter>(out var filter))
							Object.DestroyImmediate(filter);
						if(obj.TryGetComponent(out Renderer renderer))
							Object.DestroyImmediate(renderer);
						break;
					case ActionType.RemoveCollider:
						if(obj.TryGetComponent<Collider>(out var collider))
							Object.DestroyImmediate(collider);
						break;
					case ActionType.AddHelperComponent:
						var type = System.Type.GetType("HelperComponent");
						if(type != null)
						{
							obj.gameObject.AddComponent(type);
						}
						break;
					case ActionType.SetCastShadowsMode:
						if(obj.TryGetComponent(out renderer))
						{
							var mode = (UnityEngine.Rendering.ShadowCastingMode)Enum.Parse(typeof(UnityEngine.Rendering.ShadowCastingMode), actionString);
							renderer.shadowCastingMode = mode;
						}
						break;
					case ActionType.SetReceiveShadowsMode:
						if(obj.TryGetComponent(out renderer))
						{
							renderer.receiveShadows = bool.Parse(actionString);
						}
						break;
					case ActionType.SetLightmapScale:
						if(obj.TryGetComponent(out renderer))
						{
							SerializedObject so = new SerializedObject(renderer);
							so.FindProperty("m_ScaleInLightmap").floatValue = float.Parse(actionString);
							so.ApplyModifiedProperties();
						}
						break;
					default:
						Debug.LogError($"Model processor action of type '{action}' is not implemented.");
						break;
				}
			}
		}

		public bool enabled = true;
		public Rule[] rules = Array.Empty<Rule>();

		public void ApplyRulesToModel(GameObject model)
		{
			if(!enabled) return;
			foreach(var rule in rules)
			{
				rule.ApplyRecursively(new PartInfo(model.gameObject));
			}
		}
	}
}