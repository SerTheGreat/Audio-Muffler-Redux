using System;
using UnityEngine;

namespace AudioMuffler {

	//TODO optimize string comparisons
	
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class Muffler : MonoBehaviour
	{
		private AudioMufflerConfig config;
	    private AudioMixerFacade audioMixer;
		private VesselCacheManager cacheManager;

		void Awake()
	    {
			cacheManager = new VesselCacheManager();
			//Camera.onPostRender += DebugPostRender;
	    }

	    void Start()
	    {
	    	config = AudioMufflerConfig.loadConfig();
	    	
	        if (!config.engageMuffler)
	            return;

	        GameEvents.onVesselChange.Add(VesselChange);
	        GameEvents.onVesselWasModified.Add(VesselWasModified);

			AudioSource[] audioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
			audioMixer = AudioMixerFacade.initializeMixer(KSP.IO.IOUtils.GetFilePathFor(typeof(Muffler), "mixer.bundle").Replace("/", System.IO.Path.DirectorySeparatorChar.ToString()));
	        StockAudio.prepareAudioSources(audioMixer, audioSources);
			audioMixer.setInVesselCutoff(config.wallCutoff);
	    }

	    void VesselChange(Vessel v)
	    {
			cacheManager.rebuildAllCaches(FindObjectsOfType(typeof(AudioSource)) as AudioSource[]);
			writeDebug("Vessel change " + v.name);
	    }
	    
	    void VesselWasModified(Vessel vessel) {
	    	if (vessel.isActiveVessel) {
				cacheManager.setSchedule(config.minCacheUpdateInterval);
	    	}
	    }

	    void LateUpdate()
	    {
	        if (!config.engageMuffler)
	            return;

			AudioSource[] audioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];

			cacheManager.maintainCaches(audioSources);
			
	        //Looking for a part containing the Ear:
	        Part earPart = null;

			//if (vesselGeometry.isPointInVesselBounds(earPosition))
			{
				if (CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA) {
					earPart = CameraManager.Instance.IVACameraActiveKerbal.InPart;
					writeDebug("Ear position = IVA");
				} else {
					Vector3 earPosition = CameraManager.GetCurrentCamera().transform.position;
					writeDebug("Ear position = " + earPosition);
					for (int i = 0; i < FlightGlobals.ActiveVessel.Parts.Count && earPart == null; i++) {
						Part part = FlightGlobals.ActiveVessel.Parts[i];
						if (cacheManager.vesselGeometry.isPointInPart(earPosition, part)) {
							earPart = part;
						}
					}
				}
			}

			writeDebug("Ear part = " + (earPart != null ? earPart.name : "null"));

			//Setting up helmet channel:

			bool unmanned = FlightGlobals.ActiveVessel.crewableParts == 0;

			bool muteHelmet = (earPart == null) && !config.helmetOutsideEVA && FlightGlobals.ActiveVessel.isEVA
				|| !FlightGlobals.ActiveVessel.isEVA && !unmanned && !config.helmetOutsideIVA && !(CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
				|| !config.helmetForUnmanned && unmanned;
			
			//Setting up outside channel:
			float atmosphericCutoff = Mathf.Lerp(config.minimalCutoff, 30000, (float)FlightGlobals.ActiveVessel.atmDensity);
			if (earPart != null) {
				audioMixer.setOutsideCutoff(Mathf.Min(config.wallCutoff, atmosphericCutoff));
				audioMixer.setInVesselVolume(-2f);
				audioMixer.setOutsideVolume(-12f);
			} else {
				audioMixer.setOutsideCutoff(atmosphericCutoff);
				audioMixer.setInVesselVolume(0f);
				audioMixer.setOutsideVolume(0f);
			}

			//Handling Map view settings:
			bool isMapView = CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Map;
			muteHelmet = muteHelmet	|| !config.helmetInMapView && isMapView;

			audioMixer.muteHelmet(muteHelmet);
			audioMixer.muteInVessel(!config.vesselInMapView && isMapView);
			audioMixer.muteOutside(!config.outsideInMapView && isMapView);

			//Routing all current audio sources:
	        for (int i = 0; i < audioSources.Length; i++) {
				AudioSource audioSource = audioSources[i];

				/*
					Hereafter audio sources that are bound to a part's InternalModel are handled differently from the rest because InternalModel has its own reference system,
					so these two systems shouldn't be mixed until the way to convert coordinates between them is found
				*/

				if (config.debug) {
					writeDebug("Sound " + i + ": " + audioSource.transform.name + " " + audioSource.transform.position + " " + audioSource.bypassEffects + " " + audioSource.bypassListenerEffects + " " +
					(audioSource.clip == null ? "null" : audioSource.clip.name) + " " + StockAudio.isAmbient(audioSource) + " " + StockAudio.isInVessel(audioSource));
				}

				//This "if" is here because of strange behaviour of StageManager's audio source which always has clip = null and !playing when checked
				if (StockAudio.isInVessel(audioSource)) { 
					writeDebug("Sound " + i + ":" + audioSource.name + " IN VESSEL");
					audioSource.outputAudioMixerGroup = earPart != null ? audioMixer.inVesselGroup : audioMixer.outsideGroup;
					continue;
				}
	        	
				if (/*audioSource.bypassEffects ||*/ StockAudio.isPreserved(audioSource) || (audioSource.clip == null) || (!audioSource.isPlaying)) {
	        		continue;
	        	}

				if (StockAudio.isAmbient(audioSource)) {
					writeDebug("Sound " + i + ":" + audioSource.name + " OUTSIDE");
					audioSource.outputAudioMixerGroup = audioMixer.outsideGroup;
					continue;
				}

				if (isSoundInHelmet(audioSource)) {
					writeDebug("Sound " + i + ":" + audioSource.name + " IN HELMET");
					audioSource.outputAudioMixerGroup = audioMixer.helmetGroup;
					continue;
				}

				bool isRouted = false;
				if (earPart != null /*&& vesselGeometry.isPointInVesselBounds(audioSource.transform.position)*/) {

					Part boundToPartIVA = cacheManager.vesselSounds.getPartForIVA(audioSource);
					if (boundToPartIVA != null) {
						if (earPart.Equals(boundToPartIVA)) {
							writeDebug("Sound " + i + ":" + audioSource.name + " SAME AS EAR, INTERNAL");
							audioSource.outputAudioMixerGroup = null; //if audioSource is in the same part with the Ear then skipping filtering
							continue;
						} else {
							writeDebug("Sound " + i + ":" + audioSource.name + " ANOTHER PART, INTERNAL");
							audioSource.outputAudioMixerGroup = audioMixer.inVesselGroup; //if audioSource is in another part of the vessel then applying constant muffling
							continue;
						}
					}
					
					//Below the following assumption is used: if an audioSource is bound to a part (by having the same transform) then it most likely is located inside that part,
					//so in the most cases it will be enough to test just the part's meshes instead of sequentially testing all of the vessel's meshes, thus greatly improve
					//performance in case of a high part count:

					if (CameraManager.Instance.currentCameraMode != CameraManager.CameraMode.IVA) { //TODO remove this check when a proper way to transform coordinates between InternalModel and part's transform is found
						if (earPart.transform.Equals(audioSource.transform) && cacheManager.vesselGeometry.isPointInPart(audioSource.transform.position, earPart)) {
							writeDebug("Sound " + i + ":" + audioSource.name + " SAME AS EAR");
							audioSource.outputAudioMixerGroup = null; //if audioSource is in the same part with the Ear then skipping filtering
							continue;
						}
					}

					Part boundPart = cacheManager.vesselSounds.getPartFor(audioSource);
					if (boundPart != null && !boundPart.Equals(earPart) && cacheManager.vesselGeometry.isPointInPart(audioSource.transform.position, boundPart)) {
						writeDebug("Sound " + i + ":" + audioSource.name + " ANOTHER PART");
						audioSource.outputAudioMixerGroup = audioMixer.inVesselGroup; //if audioSource is in another part of the vessel then applying constant muffling
						continue;
					}

					//To this point the audioSource should be already routed as in the game the vast majority of sounds are either helmet sounds, or bound to parts, or are ambient.
					//So only a few sounds are expected to pass to the following mesh-by-mesh test:

					for (int p = 0; p < FlightGlobals.ActiveVessel.Parts.Count && !isRouted; p++) {
						Part part = FlightGlobals.ActiveVessel.Parts[p];
						if (part.Equals(boundPart)) { //if the audioSource is bound to some part, then this part is already checked earlier
							continue;
						}
						if (cacheManager.vesselGeometry.isPointInPart(audioSource.transform.position, part)) {
							if (part.Equals(earPart)) {
								writeDebug("Sound " + i + ":" + audioSource.name + " SAME AS EAR");
								audioSource.outputAudioMixerGroup = null; //if audioSource is in the same part with the Ear then skipping filtering
							} else {
								writeDebug("Sound " + i + ":" + audioSource.name + " ANOTHER PART");
								audioSource.outputAudioMixerGroup = audioMixer.inVesselGroup; //if audioSource is in another part of the vessel then applying constant muffling
							}
							isRouted = true;
						}
					}
				}

	        	if (isRouted) {
	        		continue;
	        	}

				writeDebug("Sound " + i + ":" + audioSource.name + " OUTSIDE");
				audioSource.outputAudioMixerGroup = audioMixer.outsideGroup;
	        }
	    }
	    
		private bool isSoundInHelmet(AudioSource audioSource) {
			return !StockAudio.isAmbient(audioSource) && audioSource.transform.position == Vector3.zero;
		}

		private void writeDebug(string message) {
			if (config.debug) {
				KSPLog.print("[Audio Muffler] " + message);
			}
		}

		private void visualizeTransform(Transform transform, Color color) {
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
			lineRenderer.SetColors(Color.white, color);
			lineRenderer.SetPosition(0, origin);
			lineRenderer.SetPosition(1, transform.TransformPoint(Vector3.right));
			lineRenderer.SetPosition(2, origin);
			lineRenderer.SetPosition(3, transform.TransformPoint(Vector3.up));
			lineRenderer.SetPosition(4, origin);
			lineRenderer.SetPosition(5, transform.TransformPoint(Vector3.forward));
		}

		void DebugPostRender(Camera currentCamera) {
			//KSPLog.print("CAMERA: " + currentCamera.name);
			if ((currentCamera.name == "InternalCamera") || (currentCamera.tag == "MainCamera")) {
				DebugVisualizer.drawPartMeshes(Color.red - new Color(0.9f, 0.9f, 0.9f,0), false);
				DebugVisualizer.visualizeTransform(FlightGlobals.ActiveVessel.parts[0].transform, Color.green);
				DebugVisualizer.visualizeTransform(FlightGlobals.ActiveVessel.parts[0].internalModel.transform, Color.blue);
				DebugVisualizer.visualizeAudioSources(FindObjectsOfType(typeof(AudioSource)) as AudioSource[], Color.yellow);
			}
		}
			    
	}
}
