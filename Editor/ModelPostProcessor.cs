using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ModelProcessor.Editor
{
	public class ModelPostProcessor : AssetPostprocessor
	{
		public const string PACKAGE_ID = "com.github.u-plus-one.unitymodelprocessor";

		private const string BLENDER_CREATOR_ID = "Blender (stable FBX IO)";

		private static readonly byte[] fileHeaderData = new byte[512];

		public static bool IsEmbeddedPackage { get; private set; } = false;

		public static bool VerboseLogging
		{
			get => EditorPrefs.GetBool("ModelProcessorVerboseLogging", false);
			set => EditorPrefs.SetBool("ModelProcessorVerboseLogging", value);
		}

		[InitializeOnLoadMethod]
		private static void Init()
		{
			//Check if the package itself is embedded
#if UNITY_2022_3_OR_NEWER
			var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PACKAGE_ID);
#else
			var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + PACKAGE_ID + "/package.json");
#endif
			IsEmbeddedPackage = packageInfo.source == PackageSource.Embedded;
			if(!IsEmbeddedPackage)
			{
				//Turn off verbose logging if the package is not embedded
				VerboseLogging = false;
			}
		}

		//Entry point for unity to process models
		private void OnPostprocessModel(GameObject root)
		{
			GetSettings(out var modelImporter, out var customSettings);

			bool modified = false;

			//Apply transform orientation fix if enabled
			if(customSettings.applyAxisConversion)
			{
				if(CanFixModel(root))
				{
					BlenderConverter.FixModelOrientation(root, customSettings.matchAxes, modelImporter);
					if(modelImporter.animationType == ModelImporterAnimationType.Human)
					{
						Debug.LogWarning($"Fixing humanoid models is currently a work in progress and may result in a broken model. ({root.name})");
						if(root.TryGetComponent(out Animator anim))
						{
							var avatar = anim.avatar;
							var humanDesc = avatar.humanDescription;
							humanDesc.human = humanDesc.human.ToArray();
							humanDesc.skeleton = humanDesc.skeleton.ToArray();
							BlenderConverter.FixHumanDescription(root, ref humanDesc, customSettings.matchAxes);
							var newAvatar = AvatarBuilder.BuildHumanAvatar(root, humanDesc);
							newAvatar.name = avatar.name;
							anim.avatar = newAvatar;
							Object.DestroyImmediate(avatar);
							context.AddObjectToAsset("avatar", newAvatar);
						}
					}
					modified = true;
				}
				else
				{
					Debug.LogWarning($"Skipping transform fix for {root.name} because the model is unsupported.");
				}
			}

			//Apply light fix if enabled
			if(customSettings.fixLights)
			{
				var intensityFactor = customSettings.lightIntensityFactor;
				var rangeFactor = customSettings.lightRangeFactor;
				modified |= BlenderConverter.FixLights(root, intensityFactor, rangeFactor);
			}

			if(customSettings.ruleSet.enabled)
			{
				customSettings.ruleSet.ApplyRulesToModel(root);
			}

			if(customSettings.rootsToPrefabs)
			{
				var collectionObject = ScriptableObject.CreateInstance<PrefabCollection>();
				collectionObject.name = root.name;
				context.AddObjectToAsset("collection", collectionObject);
				//context.SetMainObject(collectionObject);
				foreach(Transform child in root.transform)
				{
					//var childInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					var childInstance = Object.Instantiate(child.gameObject);
					childInstance.name = child.name;
					context.AddObjectToAsset(childInstance.name, childInstance, AssetPreview.GetAssetPreview(childInstance));
				}
				for(int i = root.transform.childCount - 1; i >= 0; i--)
				{
					Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
				}
			}

			root.name = "_root";
			//Object.DestroyImmediate(root);
			AssetDatabase.RemoveObjectFromAsset(root);

			//Save and reimport model if any changes were made
			if(modified)
			{
				modelImporter.SaveAndReimport();
			}
		}

		//Entry point for unity to process animation clips
		private void OnPostprocessAnimation(GameObject root, AnimationClip clip)
		{
			GetSettings(out var modelImporter, out var customSettings);

			//Fix animation clips if model is imported with axis conversion enabled
			if(customSettings.applyAxisConversion)
			{
				if(!CanFixModel(root))
				{
					Debug.LogWarning($"Skipping animation fix for clip {clip.name} because the model is unsupported.");
					return;
				}
				bool flipZ = customSettings.matchAxes;
				BlenderConverter.FixAnimationClipOrientation(clip, flipZ);
				modelImporter.SaveAndReimport();
			}
		}

		private void GetSettings(out ModelImporter importer, out ModelProcessorSettings customSettings)
		{
			importer = assetImporter as ModelImporter;
			customSettings = ModelProcessorSettings.FromJson(importer.userData);
		}

		public static bool IsBlendFileOrBlenderExport(string assetPath)
		{
			//Test if the file is a .blend file or a file that was exported from Blender
			string ext = Path.GetExtension(assetPath).ToLower();
			if(ext == ".blend")
			{
				return true;
			}
			else if(ext == ".fbx")
			{
				//Only get the first 512 bytes to get the creator info
				using(var stream = File.Open(assetPath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					stream.Read(fileHeaderData, 0, 512);
				}
				//Convert header data to string
				string headerString = System.Text.Encoding.ASCII.GetString(fileHeaderData);
				return headerString.Contains(BLENDER_CREATOR_ID);
			}
			else
			{
				//Not a blend file or FBX
				return false;
			}
		}

		private static bool CanFixModel(GameObject root)
		{
			return true;
		}
	}
}
