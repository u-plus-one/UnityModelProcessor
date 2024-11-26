using System.IO;
using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor
{
	public class ModelPostProcessor : AssetPostprocessor
	{
		private const string BLENDER_CREATOR_ID = "Blender (stable FBX IO)";

		private static readonly byte[] fileHeaderData = new byte[512];

		//Entry point for unity to process models
		private void OnPostprocessModel(GameObject root)
		{
			GetSettings(out var modelImporter, out var customSettings);

			bool modified = false;

			//Apply transform orientation fix if enabled
			if(customSettings.applyAxisConversion)
			{
				bool flipZ = customSettings.matchAxes;
				BlenderConverter.FixTransforms(root, flipZ, modelImporter);
				modified = true;
			}

			//Apply light fix if enabled
			if(customSettings.fixLights)
			{
				var intensityFactor = customSettings.lightIntensityFactor;
				var rangeFactor = customSettings.lightRangeFactor;
				modified |= BlenderConverter.FixLights(root, intensityFactor, rangeFactor);
			}

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
			if(ext == "blend")
			{
				return true;
			}
			else if(ext == "fbx")
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
	}
}
