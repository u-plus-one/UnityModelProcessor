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
				NameMatchesRegex = 4
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
			public ActionType action = ActionType.None;
			public string actionString = "";
			public bool applyToChildren = false;

			public void ApplyRecursively(Transform obj)
			{
				bool applied = ApplyOnObject(obj);
				//Check if the object itself wasn't destroyed
				if(obj != null)
				{
					if(!(applied && applyToChildren))
					{
						foreach(Transform child in obj)
						{
							ApplyRecursively(child);
						}
					}
				}
			}

			public bool ApplyOnObject(Transform obj)
			{
				if(CheckCondition(obj))
				{
					if(applyToChildren)
					{
						foreach(var t in obj.GetComponentsInChildren<Transform>(true))
						{
							PerformAction(t);
						}
					}
					else
					{
						PerformAction(obj);
					}
					return true;
				}
				return false;
			}

			public bool CheckCondition(Transform obj)
			{
				switch(condition)
				{
					case ConditionType.Always:
						return true;
					case ConditionType.NameStartsWith:
						return obj.name.StartsWith(conditionString);
					case ConditionType.NameEndsWith:
						return obj.name.EndsWith(conditionString);
					case ConditionType.NameContains:
						return obj.name.Contains(conditionString);
					case ConditionType.NameMatchesRegex:
						return System.Text.RegularExpressions.Regex.IsMatch(obj.name, conditionString);
					default:
						Debug.LogError($"Model processor condition of type '{condition}' is not implemented.");
						return false;
				}
			}

			public void PerformAction(Transform obj)
			{
				switch(action)
				{
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
				rule.ApplyRecursively(model.transform);
			}
		}
	}
}