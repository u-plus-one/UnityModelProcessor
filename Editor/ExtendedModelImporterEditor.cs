using System;
using System.Reflection;
using ModelProcessor.Editor.RuleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace ModelProcessor.Editor
{
	[CustomEditor(typeof(ModelImporter)), CanEditMultipleObjects]
	public class ExtendedModelImporterEditor : AssetImporterEditor
	{
		public enum MultiObjectState
		{
			None,
			Partial,
			All
		}

		public enum Tab : int
		{
			Model = 0,
			Rig = 1,
			Animation = 2,
			Materials = 3,
			Processor = 4
		}

		private static readonly string[] tabNames = new string[] {
			"Model",
			"Rig",
			"Animation",
			"Materials",
			"Processor"
		};

		private object[] tabs;
		private Tab activeTabType;
		private RulesTabGUI rulesTabGui;

		private MultiObjectState blenderModelState;

		private object ActiveTab => tabs[(int)activeTabType];

		private bool IsPreset
		{
			get
			{
#if UNITY_2022_1_OR_NEWER
				return UnityEditor.Presets.Preset.IsEditorTargetAPreset(serializedObject.targetObject);
#else
				return false;
#endif
			}
		}

		protected override bool useAssetDrawPreview => activeTabType != Tab.Animation;

		protected override Type extraDataType => typeof(ModelProcessorSettings);

		protected override void InitializeExtraDataInstance(UnityEngine.Object extraData, int targetIndex)
		{
			var assetPath = AssetDatabase.GetAssetPath(targets[targetIndex]);
			var userData = ((ModelImporter)targets[targetIndex]).userData;

			var settings = (ModelProcessorSettings)extraData;
			settings.LoadJson(userData);
		}

		public override void OnEnable()
		{
			//Create the tabs
			var param = new object[] { this };
			rulesTabGui = new RulesTabGUI();
			tabs = new object[]
			{
				Activator.CreateInstance(Type.GetType("UnityEditor.ModelImporterModelEditor, UnityEditor"), param),
				Activator.CreateInstance(Type.GetType("UnityEditor.ModelImporterRigEditor, UnityEditor"), param),
				Activator.CreateInstance(Type.GetType("UnityEditor.ModelImporterClipEditor, UnityEditor"), param),
				Activator.CreateInstance(Type.GetType("UnityEditor.ModelImporterMaterialEditor, UnityEditor"), param),
				rulesTabGui
			};

			foreach(var tab in tabs)
			{
				InvokeMethod(tab, "OnEnable");
			}
			activeTabType = (Tab)EditorPrefs.GetInt(GetType().Name + "ActiveEditorIndex");

			//Check how many of the selected models are blender models
			int blenderModels = 0;
			for(int i = 0; i < targets.Length; i++)
			{
				if(ModelPostProcessor.IsBlendFileOrBlenderExport(AssetDatabase.GetAssetPath(targets[i])))
				{
					blenderModels++;
				}
			}
			blenderModelState = blenderModels == targets.Length ? MultiObjectState.All : blenderModels > 0 ? MultiObjectState.Partial : MultiObjectState.None;

			try
			{
				base.OnEnable();
			}
			catch(Exception e)
			{
				if(ModelPostProcessor.VerboseLogging)
				{
					Debug.LogException(e);
				}
			}
		}

		public override void OnDisable()
		{
			foreach(var tab in tabs)
			{
				InvokeMethod(tab, "OnDisable");
			}
			try
			{
				base.OnDisable();
			}
			catch(Exception e)
			{
				if(ModelPostProcessor.VerboseLogging)
				{
					Debug.LogException(e);
				}
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			extraDataSerializedObject?.Update();

			rulesTabGui.extraDataSerializedObject = extraDataSerializedObject;

			//Draw the tab header
			DrawTabHeader();

			bool extraDataChanged = false;
			if(activeTabType == 0)
			{
				//Draw custom settings for the model tab
				extraDataSerializedObject.Update();
				extraDataChanged |= DrawCustomSettings();
				extraDataSerializedObject.ApplyModifiedProperties();
			}

			//Draw the built-in GUI for the active tab
			DrawActiveBuiltinTab();

			bool mainObjectChanged = serializedObject.ApplyModifiedProperties();
			extraDataSerializedObject.ApplyModifiedProperties();
			if(IsPreset && (mainObjectChanged || extraDataChanged))
			{
				SaveCustomSettings();
			}

			//Debugging section (only visible when the package is embedded)
			if(ModelPostProcessor.IsEmbeddedPackage)
			{
				PackageDebugGUI();
			}

			//Apply and revert buttons
			if(!IsPreset)
			{
				ApplyRevertGUI();
			}
		}

		public override bool HasPreviewGUI()
		{
			if(activeTabType != Tab.Processor)
			{
				return (bool)InvokeMethod(ActiveTab, "HasPreviewGUI");
			}
			else
			{
				return true;
			}
		}

		public override void OnPreviewSettings()
		{
			if(activeTabType != Tab.Processor)
			{
				InvokeMethod(ActiveTab, "OnPreviewSettings");
			}
		}

		private static object[] previewParams = new object[2];

		public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
		{
			previewParams[0] = r;
			previewParams[1] = background;
			if(activeTabType != Tab.Processor)
			{
				InvokeMethod(ActiveTab, "OnInteractivePreviewGUI", true, previewParams);
			}
		}

		private void PackageDebugGUI()
		{
			GUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.Label("Package Debug", EditorStyles.boldLabel);
			//Verbose logging toggle
			EditorGUI.BeginChangeCheck();
			var verbose = EditorGUILayout.Toggle("Verbose Logging", ModelPostProcessor.VerboseLogging);
			if(EditorGUI.EndChangeCheck())
			{
				ModelPostProcessor.VerboseLogging = verbose;
			}
			//Force apply button to reimport the model at any time
			if(GUILayout.Button("Force apply"))
			{
#if UNITY_2022_2_OR_NEWER
				SaveChanges();
#else
				ApplyAndImport();
#endif
				GUIUtility.ExitGUI();
			}
			GUILayout.EndVertical();
		}

		private bool DrawCustomSettings()
		{
			if(blenderModelState != MultiObjectState.None || IsPreset)
			{
				EditorGUI.BeginChangeCheck();
				DrawBlenderSettings();
				return EditorGUI.EndChangeCheck();
			}
			return false;
		}

		private void DrawBlenderSettings()
		{
			GUILayout.Label("Blender Import Fixes", EditorStyles.boldLabel);
			if(blenderModelState == MultiObjectState.Partial)
			{
				EditorGUILayout.HelpBox("Only some of the selected models are .blend files or FBX files made by blender).", MessageType.Info);
				return;
			}

			var applyAxisConversion = extraDataSerializedObject.FindProperty(nameof(ModelProcessorSettings.applyAxisConversion));
			EditorGUILayout.PropertyField(applyAxisConversion);
			if(applyAxisConversion.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(extraDataSerializedObject.FindProperty(nameof(ModelProcessorSettings.matchAxes)));
				EditorGUI.indentLevel--;
			}

			var fixLightsProp = extraDataSerializedObject.FindProperty(nameof(ModelProcessorSettings.fixLights));
			EditorGUILayout.PropertyField(fixLightsProp);
			if(fixLightsProp.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(extraDataSerializedObject.FindProperty(nameof(ModelProcessorSettings.lightIntensityFactor)));
				EditorGUILayout.PropertyField(extraDataSerializedObject.FindProperty(nameof(ModelProcessorSettings.lightRangeFactor)));
				EditorGUI.indentLevel--;
			}

			//Add more blender related settings here
		}

		private void DrawTabHeader()
		{
			// Always allow user to switch between tabs even when the editor is disabled, so they can look at all parts
			// of read-only assets
			using(new EditorGUI.DisabledScope(false)) // this doesn't enable the UI, but it seems correct to push the stack
			{
				GUI.enabled = true;
				using(new GUILayout.HorizontalScope())
				{
					GUILayout.FlexibleSpace();
					using(var check = new EditorGUI.ChangeCheckScope())
					{
						activeTabType = (Tab)GUILayout.Toolbar((int)activeTabType, tabNames, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
						if(check.changed)
						{
							EditorPrefs.SetInt(GetType().Name + "ActiveEditorIndex", (int)activeTabType);
							InvokeMethod(ActiveTab, "OnInspectorGUI");
						}
					}
					GUILayout.FlexibleSpace();
				}
			}
		}

		private void DrawActiveBuiltinTab()
		{
			EditorGUI.BeginChangeCheck();
			InvokeMethod(ActiveTab, "OnInspectorGUI");
			if(EditorGUI.EndChangeCheck() && IsPreset)
			{
				SaveCustomSettings();
			}
		}

		protected override void Apply()
		{
			if(serializedObject == null) return;

			// tabs can do work before or after the application of changes in the serialization object
			foreach(var tab in tabs)
			{
				InvokeMethod(tab, "PreApply");
			}

			SaveCustomSettings();
			base.Apply();

			foreach(var tab in tabs)
			{
				InvokeMethod(tab, "PostApply", false);
			}
		}

		private void SaveCustomSettings()
		{
			for(int i = 0; i < targets.Length; i++)
			{
				//Serialize custom settings to user data
				var extraData = (ModelProcessorSettings)extraDataTargets[i];
				var userData = extraData.ToJson();
				var mi = (ModelImporter)targets[i];
				mi.userData = userData;
			}
		}

		private static object InvokeMethod(object obj, string methodName, bool logError = true, params object[] parameters)
		{
			try
			{
				var method = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if(method != null)
				{
					return method.Invoke(obj, parameters);
				}
				else
				{
					if(logError) Debug.LogError($"Could not find method to invoke: {methodName} on object {obj.GetType().Name}");
					return null;
				}
			}
			catch(Exception e)
			{
				Debug.LogException(e);
				return null;
			}
		}
	}
}
