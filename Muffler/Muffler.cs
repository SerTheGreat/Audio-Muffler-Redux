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
	        
	        //Looking for a part containing the Ear:
	        Part earPart = null;
	        for (int i = 0; i < meshToPartList.Count && earPart == null; i++) {
	        	if (isPointInMesh(earPosition, meshToPartList[i].meshFilter)) {
	        		earPart = meshToPartList[i].part;
	         	}
	        }

			float atmosphericCutoff = Mathf.Lerp(minimalCutoff, 30000, (float)FlightGlobals.ActiveVessel.atmDensity);
			if (earPart != null) {
				audioMixer.setOutsideCutoff(Mathf.Min(wallCutoff, atmosphericCutoff));
			} else {
				audioMixer.setOutsideCutoff(atmosphericCutoff);
			}

	        AudioSource[] audioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
	        
	        for (int i = 0; i < audioSources.Length; i++) {
	        	AudioSource audioSource = audioSources[i];

				writeDebug("Sound " + i + ":" + audioSource.transform.name + " " + audioSource.transform.position + " " + audioSource.bypassEffects + " " + audioSource.bypassListenerEffects + " " + 
	        	             (audioSource.clip == null ? "null" : audioSource.clip.name) + " " + StockAudio.isAmbient(audioSource));
	        	
				if (audioSource.bypassEffects || StockAudio.isPreserved(audioSource)) {
	        		continue;
	        	}

	        	bool isFilterSet = false;
	        	for (int j = 0; earPart != null && j < meshToPartList.Count && !isFilterSet; j++) {
	        		MeshToPart meshToPart = meshToPartList[j];
	        		if (!StockAudio.isAmbient(audioSource) && isPointInMesh(audioSource.transform.position, meshToPart.meshFilter)) {
	        			if (meshToPart.part.Equals(earPart)) {
							writeDebug("Sound " + i + ":" + audioSource.name + " SAME AS EAR");
							audioSource.outputAudioMixerGroup = null; //if audioSource is in the same part with the Ear then skipping filtering
	        			} else {
							writeDebug("Sound " + i + ":" +audioSource.name + " ANOTHER PART");
							audioSource.outputAudioMixerGroup = audioMixer.inVesselGroup; //if audioSource is in another part of the vessel then applying constant muffling
	        			}
	        			isFilterSet = true;
	        		}
	        	}
	        	if (isFilterSet) {
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
	    
	    private bool isPointInMesh(Vector3 point, MeshFilter meshFilter) {
			Vector3 localPoint = meshFilter.transform.InverseTransformPoint(point);
			return meshFilter.mesh.bounds.Contains(localPoint);
	    }

		private void writeDebug(string message) {
			if (debug) {
				KSPLog.print ("Audio Muffler:" + message);
			}
		}
	    
	}
}
