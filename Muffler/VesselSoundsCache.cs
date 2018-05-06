using System;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace AudioMuffler
{
	/// <summary>
	/// Holds mapping of audio sources to the parts of current active vessel they are bound to.
	/// This allows to determine the corresponding part (if any) efficiently instead of iterating through
	/// all the vessel parts, thus improving performance in case of a high part count.
	/// </summary>
	public class VesselSoundsCache
	{
		
		private Dictionary<int, Part> soundIDToPart = new Dictionary<int, Part>(); //id is used to not mess with weak references to audio sources
		private Dictionary<int, Part> soundIDToPartIVA = new Dictionary<int, Part>(); //using a separate storage for internal model audio sources as they have completely different reference system

		public void rebuildCache(AudioSource[] audioSources) {
			Stopwatch performanceWatch = Stopwatch.StartNew();

			soundIDToPart.Clear();
			soundIDToPartIVA.Clear();
			for (int i = 0; i < audioSources.Length; i++) {
				AudioSource audioSource = audioSources[i];
				for (int p = 0; p < FlightGlobals.ActiveVessel.Parts.Count; p++) {
					Part part = FlightGlobals.ActiveVessel.Parts[p];
					if (part.internalModel && audioSource.transform.IsChildOf(part.internalModel.transform)) {
						soundIDToPartIVA.Add(audioSource.GetInstanceID(), part);
					} else if (audioSource.transform.IsChildOf(part.transform)) {						
						soundIDToPart.Add(audioSource.GetInstanceID(), part);
					}
				}
			}
			/*foreach (KeyValuePair<int, Part> entry in soundIDToPart) {
				UnityEngine.Debug.Log("ENTRY: " + entry.Key + " " + entry.Value.name);
			}*/
			performanceWatch.Stop();
			KSPLog.print("AudioMuffler: VesselSoundsCache rebuild time = " + performanceWatch.ElapsedMilliseconds);
		}
		
		public Part getPartFor(AudioSource audioSource) {
			Part part;
			return soundIDToPart.TryGetValue(audioSource.GetInstanceID(), out part) ? part : null;
		}

		public Part getPartForIVA(AudioSource audioSource) {
			Part part;
			return soundIDToPartIVA.TryGetValue(audioSource.GetInstanceID(), out part) ? part : null;
		}
		
	}
}
