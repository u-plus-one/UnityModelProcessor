using System.IO;
using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor
{
	public class ModelPostProcessor : AssetPostprocessor
	{
		private void GetSettings(out ModelImporter importer, out ModelProcessorSettings customSettings)
		{
			importer = assetImporter as ModelImporter;
			customSettings =  ModelProcessorSettings.FromJson(importer.userData);
		}

		private void OnPostprocessModel(GameObject root)
		{
			GetSettings(out var modelImporter, out var customSettings);

			bool modified = false;
			//Apply transform orientation fix if enabled
			if(customSettings.applyAxisConversion)
			{
				bool flipZ = customSettings.matchAxes;
				modified |= BlenderConverter.FixTransforms(root, flipZ, modelImporter);
			}

			//Apply light fix if enabled
			if(customSettings.fixLights)
			{
				var intensityFactor = customSettings.lightIntensityFactor;
				var rangeFactor = customSettings.lightRangeFactor;
				modified |= BlenderConverter.FixLights(root, intensityFactor, rangeFactor);
			}

			if(modified)
			{
				modelImporter.SaveAndReimport();
			}
		}

		private void OnPostprocessAnimation(GameObject root, AnimationClip clip)
		{
			GetSettings(out var modelImporter, out var customSettings);

			//Fix animation clips if model is imported with axis conversion enabled
			if(customSettings.applyAxisConversion)
			{
				bool flipZ = customSettings.matchAxes;
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
