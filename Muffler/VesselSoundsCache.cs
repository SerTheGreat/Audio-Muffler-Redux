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
		private int previousPartCount = int.MaxValue;
		private Stopwatch stopWatch = new Stopwatch();

		public void rebuildCacheWith(AudioSource[] audioSources) {
			rebuildCacheWith(audioSources, 0);
		}
		
		public void rebuildCacheWith(AudioSource[] audioSources, int minInterval) {
			Stopwatch sw = Stopwatch.StartNew();

			if ((stopWatch.ElapsedMilliseconds < minInterval) && (FlightGlobals.ActiveVessel.Parts.Count < previousPartCount)) {
				return;
			}
			previousPartCount = FlightGlobals.ActiveVessel.Parts.Count;

			stopWatch.Stop();
			stopWatch.Reset();

			soundIDToPart.Clear();
			for (int i = 0; i < audioSources.Length; i++) {
				AudioSource audioSource = audioSources[i];
				for (int p = 0; p < FlightGlobals.ActiveVessel.Parts.Count; p++) {
					Part part = FlightGlobals.ActiveVessel.Parts[p];
					if (part.transform.Equals(audioSource.transform)) {
						soundIDToPart.Add(audioSource.GetInstanceID(), part);
					}
				}
			}
			stopWatch.Start();

			sw.Stop();
			KSPLog.print("AudioMuffler: VesselSoundsCache rebuild time = " + sw.ElapsedMilliseconds);
		}
		
		public Part getPartFor(AudioSource audioSource) {
			Part part;
			return soundIDToPart.TryGetValue(audioSource.GetInstanceID(), out part) ? part : null;
		}
		
	}
}
