using System;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioMuffler {

	/// <summary>
	/// Description of AudioMixerFacade.
	/// </summary>
	public class AudioMixerFacade
	{
		
		private static bool BundleLoaded = false;
		
		private static AudioMixer audioMixer;
		
		public AudioMixerGroup masterGroup {get; set;}
		public AudioMixerGroup inVesselGroup {get; set;}
		public AudioMixerGroup outsideGroup {get; set;}
		public AudioMixerGroup helmetGroup {get; set;}
		
		public void muteInVessel(bool mute) {
			audioMixer.SetFloat("InVesselVolume", mute ? -80 : 0);
		}

		public void muteOutside(bool mute) {
			audioMixer.SetFloat("OutsideVolume", mute ? -80 : 0);
		}

		public void setInVesselCutoff(float cutoff) {
			audioMixer.SetFloat("InVesselCutoff", cutoff);
		}
		
		public void setOutsideCutoff(float cutoff) {
			audioMixer.SetFloat("OutsideCutoff", cutoff);
		}

		public void muteHelmet(bool mute) {
			audioMixer.SetFloat("HelmetVolume", mute ? -80 : 0);
		}
		
		public static AudioMixerFacade initializeMixer(string path) {
			AudioMixerFacade instance = new AudioMixerFacade ();
			if (audioMixer == null) {
				audioMixer = LoadBundle (path);
			}
			instance.masterGroup = audioMixer.FindMatchingGroups("Master") [0];
			instance.inVesselGroup = audioMixer.FindMatchingGroups("InVessel") [0];
			instance.outsideGroup = audioMixer.FindMatchingGroups("Outside") [0];
			instance.helmetGroup = audioMixer.FindMatchingGroups("Helmet") [0];
			return instance;
		}
		
		public static AudioMixer LoadBundle(string path)
		{
			if (BundleLoaded) {
				return null;
			}
			
			using (WWW www = new WWW ("file://" + path)) {
				if (www.error != null) {
					Debug.Log ("Audio Muffler: Mixer bundle not found!");
					return null;
				}
	
				AssetBundle bundle = www.assetBundle;
			
				AudioMixer audioMixer = bundle.LoadAsset<AudioMixer> ("KSPAudioMixer");
			
				bundle.Unload (false);
				www.Dispose ();
			
				BundleLoaded = true;
				return audioMixer;
			}
		}
		
	}

}