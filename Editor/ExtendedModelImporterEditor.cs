using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

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

		private static readonly string[] tabNames = new string[] {
			"Model",
			"Rig",
			"Animation",
			"Materials",
			"Processor"
		};

		private object[] tabs;
		private int activeTabIndex;
		private MultiObjectState blenderModelState;

		protected override Type extraDataType => typeof(ModelProcessorSettings);

		protected override void InitializeExtraDataInstance(UnityEngine.Object extraData, int targetIndex)
		{
			var assetPath = AssetDatabase.GetAssetPath(targets[targetIndex]);
			var userData = AssetImporter.GetAtPath(assetPath).userData;

			var settings = (ModelProcessorSettings)extraData;
			settings.LoadJson(userData);
		}

		public override void OnEnable()
		{
			//Create the tabs
			var param = new object[] { this };
			tabs = new object[]
			{
				Activator.CreateInstance(Type.GetType("UnityEditor.ModelImporterModelEditor, UnityEditor"), param),
				Activator.CreateInstance(Type.GetType("UnityEditor.ModelImporterRigEditor, UnityEditor"), param),
				Activator.CreateInstance(Type.GetType("UnityEditor.ModelImporterClipEditor, UnityEditor"), param),
				Activator.CreateInstance(Type.GetType("UnityEditor.ModelImporterMaterialEditor, UnityEditor"), param),
				new ModelProcessorRulesTab()
			};

			foreach(var tab in tabs)
			{
				tab.GetType().GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(tab, Array.Empty<object>());
			}
			activeTabIndex = EditorPrefs.GetInt(GetType().Name + "ActiveEditorIndex");

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

			base.OnEnable();
		}

		public override void OnDisable()
		{
			base.OnDisable();
			foreach(var tab in tabs)
			{
				tab.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(tab, new object[0]);
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			extraDataSerializedObject?.Update();

			//Draw the tab header
			DrawTabHeader();

			if(activeTabIndex == 0)
			{
				//Draw custom settings for the model tab
				extraDataSerializedObject.Update();
				DrawCustomSettings();
				extraDataSerializedObject.ApplyModifiedProperties();
			}

			//Draw the built-in GUI for the active tab
			DrawActiveBuiltinTab();

			serializedObject.ApplyModifiedProperties();
			extraDataSerializedObject.ApplyModifiedProperties();

			//Apply and revert buttons
			ApplyRevertGUI();

			/*
			if(GUILayout.Button("Force apply"))
			{
				ApplyAndImport();
			}
			*/
		}

		private void DrawCustomSettings()
		{
			if(blenderModelState != MultiObjectState.None)
			{
				DrawBlenderSettings();
			}
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
						activeTabIndex = GUILayout.Toolbar(activeTabIndex, tabNames, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
						if(check.changed)
						{
							EditorPrefs.SetInt(GetType().Name + "ActiveEditorIndex", activeTabIndex);
							tabs[activeTabIndex].GetType().GetMethod("OnInspectorGUI").Invoke(tabs[activeTabIndex], Array.Empty<object>());
						}
					}
					GUILayout.FlexibleSpace();
				}
			}
		}

		private void DrawActiveBuiltinTab()
		{
			var activeTab = tabs[activeTabIndex];
			activeTab.GetType().GetMethod("OnInspectorGUI").Invoke(activeTab, Array.Empty<object>());
		}

		protected override void Apply()
		{
			// tabs can do work before or after the application of changes in the serialization object
			foreach(var tab in tabs)
			{
				var m = tab.GetType().GetMethod("PreApply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if(m != null)
				{
					m.Invoke(tab, Array.Empty<object>());
				}
			}

			for(int i = 0; i < targets.Length; i++)
			{
				var extraData = (ModelProcessorSettings)extraDataTargets[i];
				var userData = extraData.ToJson();
				var path = AssetDatabase.GetAssetPath(targets[i]);
				AssetImporter.GetAtPath(path).userData = userData;
			}
			base.Apply();

			foreach(var tab in tabs)
			{
				var m = tab.GetType().GetMethod("PostApply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if(m != null)
				{
					m.Invoke(tab, Array.Empty<object>());
				}
			}
		}
	}
}
