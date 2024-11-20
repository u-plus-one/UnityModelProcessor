using System.IO;
using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor
{
	public class ModelPostProcessor : AssetPostprocessor
	{

		private void OnPostprocessModel(GameObject root)
		{
			var modelImporter = assetImporter as ModelImporter;
			var userData = AssetUserData.TryDeserialize(modelImporter.userData);
			bool modified = false;

			//Apply transform orientation fix if enabled
			if(userData.GetBool(nameof(ModelProcessorSettings.applyAxisConversion)))
			{
				bool flipZ = userData.GetBool(nameof(ModelProcessorSettings.matchAxes));
				modified |= BlenderConverter.FixTransforms(root, flipZ, modelImporter);
			}

			//Apply light fix if enabled
			if(userData.GetBool(nameof(ModelProcessorSettings.fixLights)))
			{
				var intensityFactor = userData.GetFloat(nameof(ModelProcessorSettings.lightIntensityFactor), 0.01f);
				var rangeFactor = userData.GetFloat(nameof(ModelProcessorSettings.lightRangeFactor), 0.1f);
				modified |= BlenderConverter.FixLights(root, intensityFactor, rangeFactor);
			}

			if(modified)
			{
				modelImporter.SaveAndReimport();
			}
		}

		private void OnPostprocessAnimation(GameObject root, AnimationClip clip)
		{
			var modelImporter = assetImporter as ModelImporter;
			var userData = AssetUserData.TryDeserialize(modelImporter.userData);
			//Fix animation clips if model is imported with axis conversion enabled
			if(userData.GetBool(nameof(ModelProcessorSettings.applyAxisConversion)))
			{
				bool flipZ = userData.GetBool(nameof(ModelProcessorSettings.matchAxes));
				BlenderConverter.FixAnimationClipOrientation(clip, flipZ);
				modelImporter.SaveAndReimport();
			}
		}

		public static bool IsBlendFileOrBlenderFBX(string assetPath)
		{
			if(assetPath.EndsWith(".blend", System.StringComparison.OrdinalIgnoreCase)) return true;
			if(File.ReadAllText(assetPath).Contains("Blender (stable FBX IO)")) return true;
			return false;
		}
	}
}
