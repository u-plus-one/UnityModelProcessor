using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor.RuleSystem
{
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

	[System.Serializable]
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
}