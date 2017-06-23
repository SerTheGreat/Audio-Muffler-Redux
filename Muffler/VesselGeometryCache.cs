using System;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace AudioMuffler
{
	public class VesselGeometryCache
	{
		private Bounds EVA_BOUNDS = new Bounds(new Vector3(0.0f, 0.3f, 0.0f), new Vector3(1.2f, 1.6f, 1.2f)); //Artificial EVA bounds (they are always the same)

		//private Bounds vesselBounds = new Bounds();	//active vessel's bounds relative to vesselTransform. This should be checked prior to iterating through part meshes to improve efficiency
		//private Transform vesselTransform = null;

		public Dictionary<Part, List<MeshFilter>> partMeshes { get; set;}

		public VesselGeometryCache() {
			partMeshes = new Dictionary<Part, List<MeshFilter>>();
		}

		public void rebuildCache() {
			Stopwatch performanceWatch = Stopwatch.StartNew();

			partMeshes.Clear();
			Vector3 min = Vector3.zero;
			Vector3 max = Vector3.zero;
			//vesselTransform = FlightGlobals.ActiveVessel.vesselTransform;
			/*if (FlightGlobals.ActiveVessel.isEVA) {
				vesselBounds = EVA_BOUNDS;
			}*/

			for (int i = 0; i < FlightGlobals.ActiveVessel.Parts.Count; i++) {
				Part part = FlightGlobals.ActiveVessel.Parts[i];
				List<MeshFilter> filters = part.FindModelComponents<MeshFilter>();
				partMeshes.Add(part, filters);

				if (FlightGlobals.ActiveVessel.isEVA) {
					continue;
				}

				//this optimzation causes huge lags when part count / vessel changes
				//Determining vessel bounds:
				/*for (int j = 0; j < filters.Count; j++) {
					MeshFilter filter = filters[j];
					for (int v = 0; v < filter.mesh.vertices.Length; v++) {
						Vector3 vertice = vesselTransform.InverseTransformPoint(filter.transform.TransformPoint(filter.mesh.vertices[v]));
						min.x = Mathf.Min(min.x, vertice.x);
						min.y = Mathf.Min(min.y, vertice.y);
						min.z = Mathf.Min(min.z, vertice.z);
						max.x = Mathf.Max(max.x, vertice.x);
						max.y = Mathf.Max(max.y, vertice.y);
						max.z = Mathf.Max(max.z, vertice.z);
					}

				}*/
			}

			/*if (!FlightGlobals.ActiveVessel.isEVA) {
				vesselBounds.SetMinMax(min, max);
			}*/

			performanceWatch.Stop();
			KSPLog.print("AudioMuffler: VesselGeometryCache rebuild time = " + performanceWatch.ElapsedMilliseconds);
		}

		/*public bool isPointInVesselBounds(Vector3 point) {
				if (vesselTransform == null) {
				return false;
			}
			return vesselBounds.Contains(vesselTransform.InverseTransformPoint(point));
		}*/

		public bool isPointInPart(Vector3 point, Part part) {
			if (FlightGlobals.ActiveVessel.isEVA) {
				return isPointInEVA(point);
			}
			
			List<MeshFilter> filters;
			if (!partMeshes.TryGetValue(part, out filters)) {
				return false;
			}
			for (int i = 0; i < filters.Count; i++) {
				MeshFilter filter = filters[i];
				if (isPointInMesh(point, filter)) {
					return true;
				}
			}
			return false;
		}

		private bool isPointInEVA(Vector3 point) {
			Vector3 localPoint = FlightGlobals.ActiveVessel.transform.InverseTransformPoint(point);
			//localPoint = meshFilter.transform.InverseTransformPoint(point); //this is the JetPack transform
			return EVA_BOUNDS.Contains(localPoint);
		}

		private bool isPointInMesh(Vector3 point, MeshFilter meshFilter) {
			if (meshFilter == null) {
				return false;
			}
			Vector3 localPoint = meshFilter.transform.InverseTransformPoint(point);
			return meshFilter.mesh.bounds.Contains(localPoint);
		}

	}
}

