using System;
using UnityEngine;

namespace AudioMuffler {
	
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class Muffler : MonoBehaviour
	{
		private AudioMufflerConfig config;
	    private AudioMixerFacade audioMixer;
		private VesselGeometryCache vesselGeometry;
		private VesselSoundsCache vesselSounds;

	    void Awake()
	    {
			vesselGeometry = new VesselGeometryCache();
			vesselSounds = new VesselSoundsCache();
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
			vesselGeometry.rebuildCache();
			vesselSounds.rebuildCacheWith(audioSources);
	    }

	    void VesselChange(Vessel v)
	    {
			vesselGeometry.rebuildCache();
			vesselSounds.rebuildCacheWith(FindObjectsOfType(typeof(AudioSource)) as AudioSource[]);
			writeDebug("Vessel change " + v.name);
	    }
	    
	    void VesselWasModified(Vessel vessel) {
	    	if (vessel.isActiveVessel) {
				vesselGeometry.rebuildCache(config.minCacheUpdateInterval);
				vesselSounds.rebuildCacheWith(FindObjectsOfType(typeof(AudioSource)) as AudioSource[]);
	    	}
	    }

	    void Update()
	    {
	        if (!config.engageMuffler)
	            return;
			
	        Vector3 earPosition = CameraManager.GetCurrentCamera().transform.position;
			writeDebug("Ear position = " + earPosition);
	        
	        //Looking for a part containing the Ear:
	        Part earPart = null;

			//if (vesselGeometry.isPointInVesselBounds(earPosition))
			{
				for (int i = 0; i < FlightGlobals.ActiveVessel.Parts.Count && earPart == null; i++) {
					Part part = FlightGlobals.ActiveVessel.Parts[i];
					if (vesselGeometry.isPointInPart(earPosition, part)) {
						earPart = part;
					}
				}
			}

			writeDebug("Ear part = " + (earPart != null ? earPart.name : "null"));

			//Setting up helmet channel:
			bool muteHelmet = (earPart == null) && !config.helmetOutsideEVA && FlightGlobals.ActiveVessel.isEVA
					|| !FlightGlobals.ActiveVessel.isEVA && !config.helmetOutsideIVA &&
						!(CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA);
						
			//Handling Map view settings:
			bool isMapView = CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.Map;
			muteHelmet = muteHelmet || !config.helmetInMapView && isMapView;
			audioMixer.muteHelmet(muteHelmet);
			audioMixer.muteInVessel(!config.vesselInMapView && isMapView);
			audioMixer.muteOutside(!config.outsideInMapView && isMapView);

			//Setting up outside channel:
			float atmosphericCutoff = Mathf.Lerp(config.minimalCutoff, 30000, (float)FlightGlobals.ActiveVessel.atmDensity);
			if (earPart != null) {
				audioMixer.setOutsideCutoff(Mathf.Min(config.wallCutoff, atmosphericCutoff));
			} else {
				audioMixer.setOutsideCutoff(atmosphericCutoff);
			}

			//Routing all current audio sources:
	        AudioSource[] audioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
	        for (int i = 0; i < audioSources.Length; i++) {
	        	AudioSource audioSource = audioSources[i];

				writeDebug("Sound " + i + ":" + audioSource.transform.name + " " + audioSource.transform.position + " " + audioSource.bypassEffects + " " + audioSource.bypassListenerEffects + " " + 
					(audioSource.clip == null ? "null" : audioSource.clip.name) + " " + StockAudio.isAmbient(audioSource));
	        	
				if (/*audioSource.bypassEffects ||*/ StockAudio.isPreserved(audioSource) || (audioSource.clip == null) && !audioSource.isPlaying) {
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
					
					//Below the following assumption is used: if an audioSource is bound to a part (by having the same transform) then it most likely is located inside that part,
					//so in the most cases it will be enough to test just the part's meshes instead of sequentially testing all of the vessel's meshes, thus greatly improve
					//performance in case of a high part count:

					if (earPart.transform.Equals(audioSource.transform) && vesselGeometry.isPointInPart(audioSource.transform.position, earPart)) {
						writeDebug("Sound " + i + ":" + audioSource.name + " SAME AS EAR");
						audioSource.outputAudioMixerGroup = null; //if audioSource is in the same part with the Ear then skipping filtering
						continue;
					}

					Part boundPart = vesselSounds.getPartFor(audioSource);
					if (boundPart != null && !boundPart.Equals(earPart) && vesselGeometry.isPointInPart(audioSource.transform.position, boundPart)) {
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
						if (vesselGeometry.isPointInPart(audioSource.transform.position, part)) {
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
