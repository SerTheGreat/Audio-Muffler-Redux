using System;
using UnityEngine;
using System.Diagnostics;

namespace AudioMuffler
{
	public class VesselCacheManager
	{

		public VesselGeometryCache vesselGeometry = new VesselGeometryCache();
		public VesselSoundsCache vesselSounds  = new VesselSoundsCache();

		private bool cachesInitialized = false;

		private int previousPartCount = int.MaxValue;
		private Stopwatch stopWatch = new Stopwatch();
		private int scheduledInterval = 0;
		private bool scheduled = false;

		public void setSchedule(int mininterval) {
			if (scheduled) {
				return;
			}
			scheduled = true;
			scheduledInterval = mininterval;
		}

		public void rebuildAllCaches(AudioSource[] audioSources) {
			previousPartCount = FlightGlobals.ActiveVessel.Parts.Count;
			stopWatch.Stop();
			stopWatch.Reset();

			vesselGeometry.rebuildCache();
			vesselSounds.rebuildCache(audioSources);

			stopWatch.Start();
			scheduled = false;
		}

		//This method is intended to be called every Update to trigger cache rebuild if it was scheduled
		public void maintainCaches(AudioSource[] audioSources) {
			if (!cachesInitialized) {
				rebuildAllCaches(audioSources);
				cachesInitialized = true;
			} else {
				//If active vessel wasn't changed and part count reduced then we should control frequency of cache rebuilds because 
				//on high part count vessel crash a wave of events occur causing very frequent rebuilds which may lead to serious lags.
				//Part count increases and vessel changes aren't expected to happen so frequent, so there's no need to control interval.
				if (!scheduled || (stopWatch.ElapsedMilliseconds < scheduledInterval) && (FlightGlobals.ActiveVessel.Parts.Count < previousPartCount)) {
					return;
				} else {
					rebuildAllCaches(audioSources);
				}
			}
		}

	}
}

