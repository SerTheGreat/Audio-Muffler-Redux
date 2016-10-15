using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace AudioMuffler {

	/// <summary>
	/// Description of StockAudio.
	/// </summary>
	public class StockAudio
	{
		
		private static List<string> AMBIENT = new List<string> {"FX Sound", "airspeedNoise"};
		private static List<string> PRESERVED = new List<string> {"MusicLogic"};
		
		public static bool isAmbient(AudioSource audioSource) {
			return AMBIENT.Contains(audioSource.name) || audioSource.name.StartsWith("Explosion");
		}
		
		public static bool isPreserved(AudioSource audioSource) {
			return PRESERVED.Contains(audioSource.name);
		}

		public static void prepareAudioSources(AudioMixerFacade audioMixer, AudioSource[] audioSources) {
			for (int i = 0; i < audioSources.Length; i++) {
				if (isAmbient (audioSources [i])) {
					audioSources [i].outputAudioMixerGroup = audioMixer.outsideGroup;
				}
			}
		}
		
		/*public static void prepareAudioSources(AudioSource[] audioSources) {
			for (int i = 0; i < audioSources.Length; i++) {
				if (isAmbient(audioSources[i])) {
					audioSources[i].bypassEffects = false;
					audioSources[i].bypassListenerEffects = false;					
				} else if (isPreserved(audioSources[i])) {
					audioSources[i].bypassEffects = true;					
				}
			}
		}*/
		
		
	}
}