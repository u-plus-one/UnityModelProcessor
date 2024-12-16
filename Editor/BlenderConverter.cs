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

		private class MeshInfo
		{
			public readonly Mesh mesh;
			public readonly List<Transform> users;

			public int UserCount => users.Count;

			public Transform FirstUser => users[0];

			public bool IsSkinned => FirstUser.TryGetComponent<SkinnedMeshRenderer>(out _);

			public MeshInfo(Mesh mesh, Transform user)
			{
				this.mesh = mesh;
				users = new List<Transform> { user };
			}

			public void AddUser(Transform user)
			{
				users.Add(user);
			}
		}

		const float SQRT2_HALF = 0.70710678f;
		private static readonly Vector3 Z_FLIP_SCALE = new Vector3(-1, 1, -1);
		//(-90°, 0°, 0°)
		private static readonly Quaternion ROTATION_FIX = new Quaternion(-SQRT2_HALF, 0, 0, SQRT2_HALF);
		//(-90°, 180°, 0°)
		private static readonly Quaternion ROTATION_FIX_Z_FLIP = new Quaternion(0, SQRT2_HALF, SQRT2_HALF, 0);
		//(90°, 0°, 0°)
		private static readonly Quaternion ANIM_ROTATION_FIX = new Quaternion(SQRT2_HALF, 0, 0, SQRT2_HALF);

		//(-90°, 0°, 0°)
		private static readonly Matrix4x4 ROTATION_FIX_MATRIX = Matrix4x4.Rotate(ROTATION_FIX);
		//(-90°, 180°, 0°)
		private static readonly Matrix4x4 ROTATION_FIX_MATRIX_Z_FLIP = Matrix4x4.Rotate(ROTATION_FIX_Z_FLIP);

		public static void FixTransforms(GameObject root, bool matchAxes, ModelImporter modelImporter)
		{
			VerboseLog("Applying fix on " + root.name);
			var meshes = GatherMeshes(root.transform);

			var transformationDeltas = new Dictionary<Transform, Matrix4x4>();
			var transforms = root.GetComponentsInChildren<Transform>(true);

			bool rootHasMesh = root.TryGetComponent<MeshFilter>(out _) || root.TryGetComponent<SkinnedMeshRenderer>(out _);
			if(rootHasMesh)
			{
				//Fix root transform
				var a = root.transform;
				var transformationMatrix = ApplyTransformFix(a, matchAxes, true);
				transformationDeltas.Add(a, transformationMatrix);
			}

			for(int i = 0; i < transforms.Length; i++)
			{
				var transform = transforms[i];
				if(transform == root.transform) continue;

				var transformationMatrix = ApplyTransformFix(transform, matchAxes, rootHasMesh);
				transformationDeltas.Add(transform, transformationMatrix);
			}

			Quaternion rotation = matchAxes ? ROTATION_FIX_Z_FLIP : ROTATION_FIX;
			Matrix4x4 matrix = Matrix4x4.Rotate(rotation);
			foreach(var mesh in meshes)
			{
				//if(GetDepth(mesh.FirstUser) == 0) continue;
				ApplyMeshFix(mesh, matrix, modelImporter.importTangents != ModelImporterTangents.None);
			}

			List<Mesh> fixedSkinnedMeshes = new List<Mesh>();
			foreach(var skinnedMeshRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				ApplyBindPoseFix(skinnedMeshRenderer, transformationDeltas, fixedSkinnedMeshes);
			}
		}

		public static void FixAnimationClipOrientation(AnimationClip clip, bool flipZ)
		{
			var bindings = AnimationUtility.GetCurveBindings(clip);

			VerboseLog("Fixing animation clip: " + clip.name);
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

		private static List<MeshInfo> GatherMeshes(Transform root)
		{
			var list = new List<MeshInfo>();
			foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
			{
				var mesh = filter.sharedMesh;
				var transform = filter.transform;
				if(!mesh) continue;
				if(list.Any(mi => mi.mesh == mesh))
				{
					list.First(mi => mi.mesh == mesh).AddUser(transform);
				}
				else
				{
					list.Add(new MeshInfo(mesh, transform));
				}
			}
			foreach(var skinnedRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				var mesh = skinnedRenderer.sharedMesh;
				var transform = skinnedRenderer.transform;
				if(!mesh) continue;
				if(list.Any(mi => mi.mesh == mesh))
				{
					list.First(mi => mi.mesh == mesh).AddUser(transform);
				}
				else
				{
					list.Add(new MeshInfo(mesh, transform));
				}
			}
			return list;
		}

		private static void ApplyMeshFix(MeshInfo mi, Matrix4x4 transformation, bool calculateTangents)
		{
			VerboseLog("Fixing mesh: " + mi.mesh.name);
			var mesh = mi.mesh;
			var verts = mesh.vertices;

			//Transform vertices
			for(int i = 0; i < verts.Length; i++)
			{
				verts[i] = transformation.MultiplyPoint(verts[i]);
			}
			mesh.vertices = verts;

			//Transform normals
			if(mesh.normals != null)
			{
				var normals = mesh.normals;
				for(int i = 0; i < verts.Length; i++)
				{
					normals[i] = transformation.MultiplyPoint(normals[i]);
				}
				mesh.normals = normals;
			}

			//Transform tangents
			//TODO: find out if / how to do it
			/*
			for(int i = 0; i < tangents.Length; i++)
			{
				tangents[i] = matrix.MultiplyPoint3x4(tangents[i]);
			}
			*/
			//m.SetTangents(tangents);

			if(calculateTangents)
			{
				mesh.RecalculateTangents();
			}
			mesh.RecalculateBounds();
		}

		private static Matrix4x4 ApplyTransformFix(Transform t, bool matchAxes, bool rootHasMesh)
		{
			Matrix4x4 before = t.localToWorldMatrix;

			int depth = GetDepth(t);
			VerboseLog($"Fixing transform: {t.name} (depth {depth})");

			//Fix position
			if(depth > 1 || rootHasMesh)
			{
				t.localPosition = ROTATION_FIX_MATRIX.MultiplyPoint(t.localPosition);
			}
			//TODO: find out how to undo rotations without remembering the original rotation
			Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();
			foreach(Transform c in t)
			{
				originalRotations.Add(c, c.rotation);
			}

			//Fix rotation by applying the inverse of the rotation that was applied to the mesh
			t.localRotation *= Quaternion.Inverse(ROTATION_FIX);

			foreach(Transform c in t)
			{
				//Undo rotations on children
				c.rotation = originalRotations[c];
			}

			if(matchAxes)
			{
				//Mirror local positions and rotations
				t.localPosition = Vector3.Scale(t.localPosition, Z_FLIP_SCALE);
				var q1 = t.localRotation;
				q1.x *= -1;
				q1.z *= -1;
				t.localRotation = q1;
			}

			//Flip z and y in scale
			t.localScale = new Vector3(t.localScale.x, t.localScale.z, t.localScale.y);

			Matrix4x4 after = t.localToWorldMatrix;

			if (t.TryGetComponent<Camera>(out _) || t.TryGetComponent<Light>(out _))
			{
				t.Rotate(new Vector3(-90f, 0f, 0f), Space.Self);

				if (matchAxes)
				{
					t.Rotate(new Vector3(0f, 0f, 180f), Space.Self);
				}
			}

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
				VerboseLog("Bindposes fixed for " + m.name);
			}
		}

		private static void VerboseLog(string message)
		{
			if(ModelPostProcessor.VerboseLogging)
			{
				Debug.Log("[Blender Converter] " + message);
			}
		}

		private static int GetDepth(Transform t)
		{
			int d = 0;
			while(t.parent != null)
			{
				t = t.parent;
				d++;
			}
			return d;
		}
	}
}