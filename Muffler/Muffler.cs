using System.Collections.Generic;
using System;
using UnityEngine;

namespace AudioMuffler {
	
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class Muffler : MonoBehaviour
	{
	    private bool debug = true;
		private bool engageMuffler = true;
		private float wallCutoff = 500f;
		private float minimalCutoff = 0f;
		private bool helmetOutsideIVA = true;
		private bool helmetOutsideEVA = true;
		private bool helmetInMapView = false;
		private bool vesselInMapView = true;
		private bool outsideInMapView = true;
	    private List<MeshToPart> meshToPartList = new List<MeshToPart>();
	    private AudioMixerFacade audioMixer;

	    void Awake()
	    {
			string path = KSP.IO.IOUtils.GetFilePathFor(typeof(Muffler), "muffler.cfg").Replace("/", System.IO.Path.DirectorySeparatorChar.ToString());
			if (debug) {
				writeDebug("Loading cfg from path " + path);
			}
	    
	        ConfigNode node = ConfigNode.Load(path);

	        debug = bool.Parse(node.GetValue("debug"));
	        engageMuffler = bool.Parse(node.GetValue("enabled"));
			wallCutoff = float.Parse(node.GetValue("wallCutoff"));
			minimalCutoff = float.Parse(node.GetValue("minimalCutoff"));
			helmetOutsideIVA = bool.Parse(node.GetValue("helmetOutsideIVA"));
			helmetOutsideEVA = bool.Parse(node.GetValue("helmetOutsideEVA"));
			helmetInMapView = bool.Parse(node.GetValue("helmetInMapView"));
			vesselInMapView = bool.Parse(node.GetValue("vesselInMapView"));
			outsideInMapView = bool.Parse(node.GetValue("outsideInMapView"));
	    }

	    void Start()
	    {
	        if (!engageMuffler)
	            return;

	        GameEvents.onVesselChange.Add(VesselChange);
	        GameEvents.onVesselWasModified.Add(VesselWasModified);

			AudioSource[] audioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
			audioMixer = AudioMixerFacade.initializeMixer(KSP.IO.IOUtils.GetFilePathFor(typeof(Muffler), "mixer.bundle").Replace("/", System.IO.Path.DirectorySeparatorChar.ToString()));
	        StockAudio.prepareAudioSources(audioMixer, audioSources);
			audioMixer.setInVesselCutoff(wallCutoff);
			rebuildVesselMeshList();
	    }

	    void VesselChange(Vessel v)
	    {
	        rebuildVesselMeshList();
			writeDebug("Vessel change " + v.name);
	    }
	    
	    void VesselWasModified(Vessel vessel) {
	    	if (vessel.isActiveVessel) {
	    		rebuildVesselMeshList();
	    	}
	    }

	    void Update()
	    {
	        if (!engageMuffler)
	            return;

	        Vector3 earPosition = CameraManager.GetCurrentCamera().transform.position;
			writeDebug("Ear position = " + earPosition);
	        
	        //Looking for a part containing the Ear:
	        Part earPart = null;
	        for (int i = 0; i < meshToPartList.Count && earPart == null; i++) {
	        	if (isPointInMesh(earPosition, meshToPartList[i].meshFilter)) {
	        		earPart = meshToPartList[i].part;
	         	}
	        }

			writeDebug("Ear part = " + (earPart != null ? earPart.name : "null"));

			//Setting up helmet channel:
			bool muteHelmet = (earPart == null) 
				&& (!helmetOutsideEVA && FlightGlobals.ActiveVessel.isEVA
					|| !helmetOutsideIVA && !(CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA));
						
			//Handling Map view settings:
			bool isMapView = CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Map;
			muteHelmet = muteHelmet || !helmetInMapView && isMapView;
			audioMixer.muteHelmet(muteHelmet);
			audioMixer.muteInVessel(!vesselInMapView && isMapView);
			audioMixer.muteOutside(!outsideInMapView && isMapView);

			//Setting up outside channel:
			float atmosphericCutoff = Mathf.Lerp(minimalCutoff, 30000, (float)FlightGlobals.ActiveVessel.atmDensity);
			if (earPart != null) {
				audioMixer.setOutsideCutoff(Mathf.Min(wallCutoff, atmosphericCutoff));
			} else {
				audioMixer.setOutsideCutoff(atmosphericCutoff);
			}

			//Routing all current audio sources:
	        AudioSource[] audioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
	        for (int i = 0; i < audioSources.Length; i++) {
	        	AudioSource audioSource = audioSources[i];

				writeDebug("Sound " + i + ":" + audioSource.transform.name + " " + audioSource.transform.position + " " + audioSource.bypassEffects + " " + audioSource.bypassListenerEffects + " " + 
	        	             (audioSource.clip == null ? "null" : audioSource.clip.name) + " " + StockAudio.isAmbient(audioSource));
	        	
				if (/*audioSource.bypassEffects ||*/ StockAudio.isPreserved(audioSource)) {
	        		continue;
	        	}

				if (isSoundInHelmet(audioSource)) {
					writeDebug("Sound " + i + ":" + audioSource.name + " IN HELMET");
					audioSource.outputAudioMixerGroup = audioMixer.helmetGroup;
					continue;
				}

				bool isRouted = false;
				for (int j = 0; earPart != null && j < meshToPartList.Count && !isRouted; j++) {
	        		MeshToPart meshToPart = meshToPartList[j];
					if (!StockAudio.isAmbient(audioSource) && isPointInMesh(audioSource.transform.position, meshToPart.meshFilter)) {
	        			if (meshToPart.part.Equals(earPart)) {
							writeDebug("Sound " + i + ":" + audioSource.name + " SAME AS EAR");
							audioSource.outputAudioMixerGroup = null; //if audioSource is in the same part with the Ear then skipping filtering
	        			} else {
							writeDebug("Sound " + i + ":" +audioSource.name + " ANOTHER PART");
							audioSource.outputAudioMixerGroup = audioMixer.inVesselGroup; //if audioSource is in another part of the vessel then applying constant muffling
	        			}
	        			isRouted = true;
	        		}
	        	}
	        	if (isRouted) {
	        		continue;
	        	}
				audioSource.outputAudioMixerGroup = audioMixer.outsideGroup;
				writeDebug("Sound " + i + ":" + audioSource.name + " OUTSIDE");
	        }
	        
	    }
	    
	    private void rebuildVesselMeshList() {
	    	meshToPartList.Clear();          
	        for (int i = 0; i < FlightGlobals.ActiveVessel.Parts.Count; i++) {
	        	Part part = FlightGlobals.ActiveVessel.Parts[i];
				List<MeshFilter> filters = part.FindModelComponents<MeshFilter>();
	        	for (int j = 0; j < filters.Count; j++) {
	        		meshToPartList.Add(new MeshToPart(filters[j], part));
	        	}
	        }
	    }

		private bool isSoundInHelmet(AudioSource audioSource) {
			return !StockAudio.isAmbient(audioSource) && audioSource.transform.position == Vector3.zero;
		}
	    
	    private bool isPointInMesh(Vector3 point, MeshFilter meshFilter) {
			Vector3 localPoint;
			Bounds bounds;
			if (FlightGlobals.ActiveVessel.isEVA) { //Some strange coordinates are used when in EVA mode
				localPoint = FlightGlobals.ActiveVessel.evaController.referenceTransform.InverseTransformPoint(point); //this is the kerbal transform
				//localPoint = meshFilter.transform.InverseTransformPoint(point); //this is the JetPack transform
				bounds = new Bounds(new Vector3(0.0f, 0.0f, -0.3f), new Vector3(1.2f, 1.2f, 1.6f)); //Artificial EVA bounds (they are always the same)
			} else {
				localPoint = meshFilter.transform.InverseTransformPoint(point); 
				bounds = meshFilter.mesh.bounds;
			}
			return bounds.Contains(localPoint);
	    }

		private void writeDebug(string message) {
			if (debug) {
				KSPLog.print("Audio Muffler:" + message);
			}
		}

		private void visualizeTransform(Transform transform) {
			if (transform == null) {
				return;
			}
			writeDebug("TRANSFORM: " + transform.position);
			GameObject gameObject = transform.gameObject;
			Vector3 origin = transform.position;
			LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
			if (lineRenderer == null) {
				lineRenderer = gameObject.AddComponent<LineRenderer>();
				lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
				lineRenderer.SetVertexCount(6);
			}
			lineRenderer.SetWidth(0.05f, 0.01f);
			lineRenderer.SetColors(Color.white, Color.green);
			lineRenderer.SetPosition(0, origin);
			lineRenderer.SetPosition(1, transform.TransformPoint(Vector3.right));
			lineRenderer.SetPosition(2, origin);
			lineRenderer.SetPosition(3, transform.TransformPoint(Vector3.up));
			lineRenderer.SetPosition(4, origin);
			lineRenderer.SetPosition(5, transform.TransformPoint(Vector3.forward));
		}
	    
	}
}
