using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ModelProcessor.Editor
{
	public static class BlenderConverter
	{
		private class TransformCurves
		{
			public EditorCurveBinding positionX;
			public EditorCurveBinding positionY;
			public EditorCurveBinding positionZ;

			public EditorCurveBinding rotationX;
			public EditorCurveBinding rotationY;
			public EditorCurveBinding rotationZ;
			public EditorCurveBinding rotationW;

			public EditorCurveBinding scaleX;
			public EditorCurveBinding scaleY;
			public EditorCurveBinding scaleZ;
		}

		private struct TransformSnapshot
		{
			public Vector3 position;
			public Quaternion rotation;

			public TransformSnapshot(Transform t)
			{
				position = t.position;
				rotation = t.rotation;
			}
		}

		const float SQRT2_HALF = 0.70710678f;
		private static readonly Vector3 Z_FLIP_SCALE = new Vector3(-1, 1, -1);
		private static readonly Quaternion ROTATION_FIX = new Quaternion(-SQRT2_HALF, 0, 0, SQRT2_HALF);
		private static readonly Quaternion ROTATION_FIX_Z_FLIP = new Quaternion(0, SQRT2_HALF, SQRT2_HALF, 0);
		private static readonly Quaternion ANIM_ROTATION_FIX = new Quaternion(SQRT2_HALF, 0, 0, SQRT2_HALF);

		public static void FixTransforms(GameObject root, bool flipZ, ModelImporter modelImporter)
		{
			//Debug.Log("Applying fix on "+root.name);
			var meshes = GetUniqueMeshes(root.transform);

			var transformSnapshots = new Dictionary<Transform, TransformSnapshot>();
			foreach(var t in root.GetComponentsInChildren<Transform>(true))
			{
				if(t == root.transform) continue;
				transformSnapshots.Add(t, new TransformSnapshot(t));
			}

			var deltas = new Dictionary<Transform, Matrix4x4>();
			var transforms = transformSnapshots.Keys.ToArray();
			for(int i = 0; i < transforms.Length; i++)
			{
				var transform = transforms[i];
				if(transform == null || transform == root.transform) continue;
				//Delete objects that are hidden and have no children
				//TODO: turn this into a setting (or rule)
				if(ShouldDeleteObject(transform))
				{
					Object.DestroyImmediate(transform.gameObject);
				}
				else
				{
					var snapshot = transformSnapshots[transform];
					if (transform.TryGetComponent<Light>(out _))
					{
						FixCameraTransforms(transform, flipZ);
						continue;
					}
					if (transform.TryGetComponent<Camera>(out _))
					{
						FixCameraTransforms(transform, flipZ);
						continue;
					}
					var transformationMatrix = ApplyTransformFix(transform, snapshot.position, snapshot.rotation, flipZ);
					deltas.Add(transform, transformationMatrix);
				}
			}

			Quaternion rotation = flipZ ? ROTATION_FIX_Z_FLIP : ROTATION_FIX;
			Matrix4x4 matrix = Matrix4x4.Rotate(rotation);
			foreach(var mesh in meshes)
			{
				ApplyMeshFix(mesh, matrix, modelImporter.importTangents != ModelImporterTangents.None);
			}

			List<Mesh> fixedSkinnedMeshes = new List<Mesh>();
			foreach(var skinnedMeshRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				ApplyBindPoseFix(skinnedMeshRenderer, deltas, fixedSkinnedMeshes);
			}
		}

		public static void FixCameraTransforms(Transform t, bool flipZ)
		{
            if (flipZ)
            {
                t.position = Vector3.Scale(t.position, Z_FLIP_SCALE);
				t.Rotate(new(0f, 180f, 0f), Space.World);
            }
        }

		public static void FixAnimationClipOrientation(AnimationClip clip, bool flipZ)
		{
			var bindings = AnimationUtility.GetCurveBindings(clip);

			Dictionary<string, TransformCurves> transformCurves = new Dictionary<string, TransformCurves>();
			foreach(var binding in bindings)
			{
				if(binding.type == typeof(Transform))
				{
					if(!transformCurves.ContainsKey(binding.path)) transformCurves.Add(binding.path, new TransformCurves());
					var curves = transformCurves[binding.path];
					switch(binding.propertyName)
					{
						case "m_LocalPosition.x": curves.positionX = binding; break;
						case "m_LocalPosition.y": curves.positionY = binding; break;
						case "m_LocalPosition.z": curves.positionZ = binding; break;
						case "m_LocalRotation.x": curves.rotationX = binding; break;
						case "m_LocalRotation.y": curves.rotationY = binding; break;
						case "m_LocalRotation.z": curves.rotationZ = binding; break;
						case "m_LocalRotation.w": curves.rotationW = binding; break;
						case "m_LocalScale.x": curves.scaleX = binding; break;
						case "m_LocalScale.y": curves.scaleY = binding; break;
						case "m_LocalScale.z": curves.scaleZ = binding; break;
						default: Debug.LogError($"Unknown binding in transform animation: {binding.propertyName}"); break;
					}
				}
			}

			foreach(var kv in transformCurves)
			{
				var curves = kv.Value;
				if(curves.positionX.path != null)
				{
					//Position is animated
					var posXCurve = AnimationUtility.GetEditorCurve(clip, curves.positionX);
					var posYCurve = AnimationUtility.GetEditorCurve(clip, curves.positionY);
					var posZCurve = AnimationUtility.GetEditorCurve(clip, curves.positionZ);
					for(int i = 0; i < posXCurve.keys.Length; i++)
					{
						var time = posXCurve.keys[i].time;
						var keyX = posXCurve[i];
						var keyY = posYCurve[i];
						var keyZ = posZCurve[i];
						Vector3 pos = new Vector3(keyX.value, keyY.value, keyZ.value);
						Vector3 inTangents = new Vector3(keyX.inTangent, keyY.inTangent, keyZ.inTangent);
						Vector3 outTangents = new Vector3(keyX.outTangent, keyY.outTangent, keyZ.outTangent);
						if(flipZ)
						{
							pos = Vector3.Scale(pos, Z_FLIP_SCALE);
							inTangents = Vector3.Scale(inTangents, Z_FLIP_SCALE);
							outTangents = Vector3.Scale(outTangents, Z_FLIP_SCALE);
						}
						posXCurve.MoveKey(i, new Keyframe(time, pos.x, inTangents.x, outTangents.x));
						posYCurve.MoveKey(i, new Keyframe(time, pos.y, inTangents.y, outTangents.y));
						posZCurve.MoveKey(i, new Keyframe(time, pos.z, inTangents.z, outTangents.z));
					}
					AnimationUtility.SetEditorCurve(clip, curves.positionX, posXCurve);
					AnimationUtility.SetEditorCurve(clip, curves.positionY, posYCurve);
					AnimationUtility.SetEditorCurve(clip, curves.positionZ, posZCurve);
				}
				if(curves.rotationX.path != null)
				{
					//Rotation is animated
					var rotXCurve = AnimationUtility.GetEditorCurve(clip, curves.rotationX);
					var rotYCurve = AnimationUtility.GetEditorCurve(clip, curves.rotationY);
					var rotZCurve = AnimationUtility.GetEditorCurve(clip, curves.rotationZ);
					var rotWCurve = AnimationUtility.GetEditorCurve(clip, curves.rotationW);
					for(int i = 0; i < rotXCurve.keys.Length; i++)
					{
						var time = rotXCurve.keys[i].time;
						var keyX = rotXCurve[i];
						var keyY = rotYCurve[i];
						var keyZ = rotZCurve[i];
						var keyW = rotWCurve[i];
						Quaternion rot = new Quaternion(keyX.value, keyY.value, keyZ.value, keyW.value);
						Quaternion inTangents = new Quaternion(keyX.inTangent, keyY.inTangent, keyZ.inTangent, keyW.inTangent);
						Quaternion outTangents = new Quaternion(keyX.outTangent, keyY.outTangent, keyZ.outTangent, keyW.outTangent);
						if(flipZ)
						{
							var mirror = new Quaternion(0, 1, 0, 0);
							rot = mirror * (rot * ANIM_ROTATION_FIX) * mirror;
							inTangents = mirror * (inTangents * ANIM_ROTATION_FIX) * mirror;
							outTangents = mirror * (outTangents * ANIM_ROTATION_FIX) * mirror;
						}
						else
						{
							rot *= ANIM_ROTATION_FIX;
							inTangents *= ANIM_ROTATION_FIX;
							outTangents *= ANIM_ROTATION_FIX;
						}
						rotXCurve.MoveKey(i, new Keyframe(time, rot.x, inTangents.x, outTangents.x));
						rotYCurve.MoveKey(i, new Keyframe(time, rot.y, inTangents.y, outTangents.y));
						rotZCurve.MoveKey(i, new Keyframe(time, rot.z, inTangents.z, outTangents.z));
						rotWCurve.MoveKey(i, new Keyframe(time, rot.w, inTangents.w, outTangents.w));
					}
					AnimationUtility.SetEditorCurve(clip, curves.rotationX, rotXCurve);
					AnimationUtility.SetEditorCurve(clip, curves.rotationY, rotYCurve);
					AnimationUtility.SetEditorCurve(clip, curves.rotationZ, rotZCurve);
					AnimationUtility.SetEditorCurve(clip, curves.rotationW, rotWCurve);
				}
				if(curves.scaleX.path != null)
				{
					//Scale is animated
					var scaleXCurve = AnimationUtility.GetEditorCurve(clip, curves.scaleX);
					var scaleYCurve = AnimationUtility.GetEditorCurve(clip, curves.scaleY);
					var scaleZCurve = AnimationUtility.GetEditorCurve(clip, curves.scaleZ);
					//Just swap Y and Z curves
					AnimationUtility.SetEditorCurve(clip, curves.scaleX, scaleXCurve);
					AnimationUtility.SetEditorCurve(clip, curves.scaleY, scaleZCurve);
					AnimationUtility.SetEditorCurve(clip, curves.scaleZ, scaleYCurve);
				}
			}
		}

		public static bool FixLights(GameObject root, float intensityFactor, float rangeFactor)
		{
			bool modified = false;
			foreach(var light in root.GetComponentsInChildren<Light>(true))
			{
				if(light.type != LightType.Directional)
				{
					var power = light.intensity;
					light.intensity = power * intensityFactor;
					light.range = power * rangeFactor;
					modified = true;
				}
			}
			return modified;
		}


		private static bool ShouldDeleteObject(Transform obj)
		{
			bool delete = false;
			if(obj.TryGetComponent<MeshRenderer>(out var renderer))
			{
				delete = !renderer.enabled;
				delete &= obj.childCount == 0;
			}
			return delete;
		}

		private static List<Mesh> GetUniqueMeshes(Transform transform)
		{
			var list = new List<Mesh>();
			foreach(var filter in transform.GetComponentsInChildren<MeshFilter>(true))
			{
				if(filter.sharedMesh && !list.Contains(filter.sharedMesh))
				{
					list.Add(filter.sharedMesh);
				}
			}
			foreach(var skinnedRenderer in transform.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				if(skinnedRenderer.sharedMesh && !list.Contains(skinnedRenderer.sharedMesh))
				{
					list.Add(skinnedRenderer.sharedMesh);
				}
			}
			return list;
		}

		private static void ApplyMeshFix(Mesh m, Matrix4x4 matrix, bool calculateTangents)
		{
			//Debug.Log("Fixing mesh: " + m.name);
			var verts = m.vertices;
			for(int i = 0; i < verts.Length; i++)
			{
				verts[i] = matrix.MultiplyPoint(verts[i]);
			}
			m.vertices = verts;

			if(m.normals != null)
			{
				var normals = m.normals;
				for(int i = 0; i < verts.Length; i++)
				{
					normals[i] = matrix.MultiplyPoint(normals[i]);
				}
				m.normals = normals;
			}

			/*
			for(int i = 0; i < tangents.Length; i++)
			{
				tangents[i] = matrix.MultiplyPoint3x4(tangents[i]);
			}
			*/
			//m.SetTangents(tangents);

			if(calculateTangents)
			{
				m.RecalculateTangents();
			}
			m.RecalculateBounds();
		}

		private static Matrix4x4 ApplyTransformFix(Transform t, Vector3 storedPos, Quaternion storedRot, bool flipZ)
		{
			Matrix4x4 before = t.localToWorldMatrix;

			//Debug.Log("Fixing transform: " + t.name);
			t.position = storedPos;
			t.rotation = storedRot;

			if(flipZ)
			{
				t.position = Vector3.Scale(t.position, Z_FLIP_SCALE);
				t.eulerAngles = Vector3.Scale(t.eulerAngles, Z_FLIP_SCALE);
			}

			float sign = flipZ ? 1 : -1;

			if((t.localEulerAngles - new Vector3(89.98f * sign, 0, 0)).magnitude < 0.001f)
			{
				//Reset local rotation
				t.localRotation = Quaternion.identity;
			}
			else
			{
				t.Rotate(new Vector3(-90 * sign, 0, 0), Space.Self);
			}

			t.localScale = new Vector3(t.localScale.x, t.localScale.z, t.localScale.y);

			Matrix4x4 after = t.localToWorldMatrix;

			return after * before.inverse;
		}

		//TODO: Find out how to modify bind poses to match the new bone positions
		private static void ApplyBindPoseFix(SkinnedMeshRenderer skinnedMeshRenderer, Dictionary<Transform, Matrix4x4> transformations, List<Mesh> fixedMeshes)
		{
			var m = skinnedMeshRenderer.sharedMesh;

			if(fixedMeshes.Contains(m)) return;

			fixedMeshes.Add(m);

			if(m.bindposes != null)
			{
				var bindposes = m.bindposes;
				if(bindposes != null)
				{
					for(int i = 0; i < bindposes.Length; i++)
					{
						var fix = transformations[skinnedMeshRenderer.bones[i]];
						bindposes[i] *= fix.inverse;
					}
				}
				m.bindposes = bindposes;
				//Debug.Log("Bindposes fixed for " + m.name);
			}
		}
	}
}