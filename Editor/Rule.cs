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
			Always = 0x00,
			[InspectorName("Previous Condition")]
			PreviousCondition = 0x01,
			//game object conditions
			[InspectorName("GameObject/Is Root")]
			RootObject = 0x10,
			[InspectorName("GameObject/Inactive (Self)")]
			GameObjectInactiveSelf = 0x11,
			[InspectorName("GameObject/Inactive (In Hierarchy)")]
			GameObjectInactiveInHierarchy = 0x12,
			//name conditions
			[InspectorName("Name/Starts With")]
			NameStartsWith = 0x20,
			[InspectorName("Name/Ends With")]
			NameEndsWith = 0x21,
			[InspectorName("Name/Contains")]
			NameContains = 0x22,
			[InspectorName("Name/Equals")]
			NameEquals = 0x23,
			[InspectorName("Name/Regex Match")]
			NameMatchesRegex = 0x24,
			[InspectorName("Path/Starts With")]
			PathStartsWith = 0x25,
			[InspectorName("Path/Ends With")]
			PathEndsWith = 0x26,
			[InspectorName("Path/Contains")]
			PathContains = 0x27,
			[InspectorName("Path/Equals")]
			PathEquals = 0x28,
			[InspectorName("Path/Regex Match")]
			PathMatchesRegex = 0x29,
			//parent-child conditions
			[InspectorName("Child Depth/==")]
			ChildDepthEquals = 0x30,
			[InspectorName("Child Depth/>")]
			ChildDepthGreaterThan = 0x31,
			[InspectorName("Child Depth/>=")]
			ChildDepthGreaterOrEqual = 0x32,
			[InspectorName("Child Depth/<")]
			ChildDepthLessThan = 0x33,
			[InspectorName("Child Depth/<=")]
			ChildDepthLessOrEqual = 0x34,
			[InspectorName("Has Children")]
			HasChildren = 0x35,
			//component conditions
			[InspectorName("Has Component/Mesh")]
			HasMesh = 0x40,
			[InspectorName("Has Component/Skinned Mesh")]
			HasSkinnedMesh = 0x41,
			[InspectorName("Has Component/Collider")]
			HasCollider = 0x42,
			[InspectorName("Has Component/Light")]
			HasLight = 0x43,
			[InspectorName("Has Component/Camera")]
			HasCamera = 0x44,
			//empty conditions
			[InspectorName("GameObject/Is Empty")]
			IsEmpty = 0x50,
			[InspectorName("GameObject/Is Empty (No Children)")]
			IsEmptyWithoutChildren = 0x51,
			//custom
			[InspectorName("Custom Function")]
			CustomFunction = 0xFF
		}

		public enum ActionType : int
		{
			None = 0x00,
			//game object operations
			[InspectorName("GameObject/Set Inactive")]
			SetGameObjectInactive = 0x01,
			[InspectorName("GameObject/Destroy")]
			DestroyGameObject = 0x02,
			[InspectorName("GameObject/Destroy Children")]
			DestroyChildObjects = 0x03,
			[InspectorName("GameObject/Mark Static")]
			MarkStatic = 0x04,
			[InspectorName("GameObject/Set Static Flags")]
			SetStaticFlags = 0x05,
			[InspectorName("GameObject/Set Layer")]
			SetLayer = 0x06,
			[InspectorName("GameObject/Set Tag")]
			SetTag = 0x07,
			//object name operations
			[InspectorName("Object Name/Set")]
			SetName = 0x10,
			[InspectorName("Object Name/Prepend")]
			PrependName = 011,
			[InspectorName("Object Name/Append")]
			AppendName = 012,
			//renderer operations
			[InspectorName("Renderer/Remove")]
			RemoveRenderer = 0x20,
			[InspectorName("Renderer/Set Cast Shadows Mode")]
			SetCastShadowsMode = 0x21,
			[InspectorName("Renderer/Set Receive Shadows Mode")]
			SetReceiveShadowsMode = 0x22,
			[InspectorName("Renderer/Set Lightmap Scale")]
			SetLightmapScale = 0x23,
			//collider operations
			[InspectorName("Collider/Remove")]
			RemoveCollider = 0x30,
			[InspectorName("Collider/Set Convex")]
			SetColliderConvex = 0x31,
			[InspectorName("Collider/Box Collider")]
			ToBoxCollider = 0x32,
			[InspectorName("Collider/Sphere Collider")]
			ToSphereCollider = 0x33,
			//Debug stuff
			[InspectorName("Debug/Add Helper Component")]
			AddHelperComponent = 0xE0,
			[InspectorName("Debug/Log To Console")]
			LogToConsole = 0xE1,
			[InspectorName("Debug/Log To Console (Formatted)")]
			LogToConsoleFormat = 0xE2,
			//Custom function
			[InspectorName("Custom Function")]
			CustomFunction = 0xFF
		}

		public ConditionType condition = ConditionType.Always;
		public bool invertCondition;
		public string conditionParam = "";

		public ActionType action = ActionType.None;
		public string actionParam = "";
		public bool applyToChildren = false;

		public void ApplyToModel(GameObject modelRoot)
		{
			ApplyRecursively(new PartInfo(modelRoot), true);
		}

		private void ApplyRecursively(PartInfo obj, bool previousConditionResult)
		{
			bool result = ApplyOnObject(obj, previousConditionResult);
			//Check if the object itself wasn't destroyed
			if(obj.gameObject != null)
			{
				if(!(result && applyToChildren))
				{
					var children = new List<Transform>();
					for(int i = 0; i < obj.gameObject.transform.childCount; i++)
					{
						children.Add(obj.gameObject.transform.GetChild(i));
					}
					foreach(var child in children)
					{
						ApplyRecursively(new PartInfo(child.gameObject), result);
					}
				}
			}
		}

		public bool ApplyOnObject(PartInfo obj, bool previousConditionResult)
		{
			bool condition = CheckCondition(obj, previousConditionResult);
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

		private bool CheckCondition(PartInfo obj, bool previousConditionResult)
		{
			switch(condition)
			{
				case ConditionType.Always:
					return true;
				case ConditionType.PreviousCondition:
					return previousConditionResult;
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
				case ConditionType.NameEquals:
					return obj.gameObject.name == conditionParam;
				case ConditionType.NameMatchesRegex:
					return System.Text.RegularExpressions.Regex.IsMatch(obj.gameObject.name, conditionParam);
				case ConditionType.PathStartsWith:
					return obj.hierarchyPath.StartsWith(conditionParam);
				case ConditionType.PathEndsWith:
					return obj.hierarchyPath.EndsWith(conditionParam);
				case ConditionType.PathContains:
					return obj.hierarchyPath.Contains(conditionParam);
				case ConditionType.PathEquals:
					return obj.hierarchyPath == conditionParam;
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
				case ConditionType.CustomFunction:
					//TODO: Implement custom function
					return false;
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
					part.gameObject.layer = int.Parse(actionParam);
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
				case ActionType.LogToConsole:
					Debug.Log(actionParam+": "+part.hierarchyPath);
					break;
				case ActionType.LogToConsoleFormat:
					Debug.Log(string.Format(actionParam, part.hierarchyPath));
					break;
				case ActionType.CustomFunction:
					//TODO: Implement custom function
					break;
				default:
					Debug.LogError($"Model processor action of type '{action}' is not implemented.");
					break;
			}
		}
	}
}