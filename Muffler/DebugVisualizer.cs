using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioMuffler
{
	public class DebugVisualizer
	{

		static Material material = null;

		public DebugVisualizer()
		{
		}

		private static void initializeMaterial() {
			if (material == null) {
				//material = new Material(Shader.Find("Specular"));
				/*material = new Material( "Shader \"Lines/Colored Blended\" {" +
					"SubShader { Pass { " +
					"    Blend SrcAlpha OneMinusSrcAlpha " +
					"    ZWrite Off Cull Off Fog { Mode Off } " +
					"    BindChannels {" +
					"      Bind \"vertex\", vertex Bind \"color\", color }" +
					"} } }" );*/
			}
			var shader = Shader.Find("Hidden/Internal-Colored");
			material = new Material(shader);
			material.hideFlags = HideFlags.HideAndDontSave;
			// Turn on alpha blending
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Turn off depth writes
			material.SetInt("_ZWrite", 0);

			//To not hide lines behind objects
			material.SetInt("_ZTest", 0);
		}

		private static Vector3 CT(Vector3 vector, bool asInternal = false) {
			if (1 == 1)
				return vector;
			if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Internal) {
				if (asInternal) {
					return vector;
				}
				return InternalSpace.WorldToInternal(vector);
			}
			return vector;
		}

		public static void drawPartMeshes(Color color) {
			initializeMaterial();
			material.SetPass(0);
			GL.PushMatrix();
			GL.Begin(GL.LINES);
			GL.Color(color);
			for (int i = 0; i < FlightGlobals.ActiveVessel.Parts.Count; i++) {
				Part part = FlightGlobals.ActiveVessel.Parts[i];
				List<MeshFilter> filters = part.FindModelComponents<MeshFilter>();
				foreach (MeshFilter filter in filters) {
					//GL.MultMatrix(internalModelCoords ? currentCamera.transform.parent.localToWorldMatrix : filter.transform.localToWorldMatrix);
					//GL.LoadOrtho();

					Vector3 center = filter.mesh.bounds.center;
					Vector3 extents = filter.mesh.bounds.extents;

					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, -extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, -extents.y, -extents.z))));

					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, -extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, +extents.y, -extents.z))));

					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, -extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, +extents.z))));


					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, +extents.y, +extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, +extents.y, +extents.z))));

					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, +extents.y, +extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, -extents.y, +extents.z))));

					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, +extents.y, +extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, +extents.y, -extents.z))));


					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, +extents.y, +extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, +extents.y, -extents.z))));

					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, +extents.y, +extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, +extents.z))));


					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, -extents.y, -extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, +extents.y, -extents.z))));

					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, -extents.y, -extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, -extents.y, +extents.z))));


					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, +extents.y, -extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, +extents.y, -extents.z))));


					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, +extents.z))));
					GL.Vertex(CT(filter.transform.TransformPoint(center + new Vector3(+extents.x, -extents.y, +extents.z))));

				}
			}

			GL.End();
			GL.PopMatrix();

		}

		public static void visualizeAudioSources(AudioSource[] audiosources, Color color) {
			initializeMaterial();
			material.SetPass(0);
			GL.PushMatrix();
			GL.Begin(GL.LINES);
			GL.Color(color);
			foreach (AudioSource source in audiosources) {
				bool isInternal = InternalSpace.Instance.transform == source.transform.root;
				GL.Vertex(CT(source.transform.position + 0.01f * Vector3.down, isInternal));
				GL.Vertex(CT(source.transform.position + 0.01f * Vector3.up, isInternal));
				GL.Vertex(CT(source.transform.position + 0.01f * Vector3.left, isInternal));
				GL.Vertex(CT(source.transform.position + 0.01f * Vector3.right, isInternal));
			}
			GL.End();
			GL.PopMatrix();
		}

		public static void visualizeTransform(Transform transform, Color color) {
			initializeMaterial();
			material.SetPass(0);
			Color colorOffset = new Color(0.3f, 0.3f, 0.3f, 0);
			GL.PushMatrix();
			GL.Begin(GL.LINES);
			GL.Color(color);
			GL.Vertex((transform.position));
			GL.Vertex((transform.TransformPoint(Vector3.right)));
			GL.Color(color - colorOffset);
			GL.Vertex((transform.position));
			GL.Vertex((transform.TransformPoint(Vector3.up)));
			GL.Color((color - 2 * colorOffset));
			GL.Vertex((transform.position));
			GL.Vertex((transform.TransformPoint(Vector3.forward)));
			GL.End();
			GL.PopMatrix();
		}

	}
}