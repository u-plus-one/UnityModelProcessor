using ModelProcessor.Editor.RuleSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GadgetFactory.Editor
{
	[CustomPropertyDrawer(typeof(RuleAsset))]
	public class RuleAssetDrawer : PropertyDrawer
	{
		private GUIContent dropdownContent = new GUIContent();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var labelPos = position;
			var dropdownPos = position;
			if(!string.IsNullOrWhiteSpace(label.text))
			{
				labelPos.width = EditorGUIUtility.labelWidth;
				dropdownPos.width -= EditorGUIUtility.labelWidth;
				dropdownPos.x += EditorGUIUtility.labelWidth;
			}
			dropdownPos.width -= 20; // Reserve space for the button on the right
			var buttonPos = dropdownPos;
			buttonPos.width = 20; // Width for the button
			buttonPos.x += dropdownPos.width; // Align button to the right
			EditorGUI.LabelField(labelPos, label);
			var value = property.objectReferenceValue as RuleAsset;
			dropdownContent.text = value ? value.name : "(None)";
			if(EditorGUI.DropdownButton(dropdownPos, dropdownContent, FocusType.Keyboard))
			{
				var so = property.serializedObject;
				var path = property.propertyPath;
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("(None)"), value == null, () =>
				{
					so.FindProperty(path).objectReferenceValue = null;
					so.ApplyModifiedProperties();
				});
				foreach(var asset in GetAllRuleAssets())
				{
					menu.AddItem(new GUIContent(asset.name), value == asset, () =>
					{
						so.FindProperty(path).objectReferenceValue = asset;
						so.ApplyModifiedProperties();
					});
				}
				menu.DropDown(dropdownPos);
			}
			GUI.enabled = value != null;
			if(GUI.Button(buttonPos, "...", EditorStyles.miniButtonRight))
			{
				// Select object in inspector and ping project window to it
				Selection.activeObject = value;
				EditorGUIUtility.PingObject(value);
			}
			EditorGUI.EndProperty();
		}

		private IEnumerable<RuleAsset> GetAllRuleAssets()
		{
			// Return all item definitions found in the Assets
			return AssetDatabase.FindAssets("t:" + nameof(RuleAsset))
				.Select(guid => AssetDatabase.LoadAssetAtPath<RuleAsset>(AssetDatabase.GUIDToAssetPath(guid)))
				.OrderBy(def => def.name);
		}
	}
}
