/*
 * Created by SharpDevelop.
 * Date: 26.10.2016
 * Time: 11:17
 */
using System;

namespace AudioMuffler
{
	/// <summary>
	/// Description of Config.
	/// </summary>
	public class AudioMufflerConfig
	{
		public bool debug {get; set;}
		public bool engageMuffler {get; set;}
		public int minCacheUpdateInterval {get; set;}
		public float wallCutoff {get; set;}
		public float minimalCutoff {get; set;}
		public bool helmetOutsideIVA {get; set;}
		public bool helmetOutsideEVA {get; set;}
		public bool helmetInMapView {get; set;}
		public bool helmetForUnmanned { get; set;}
		public bool vesselInMapView {get; set;}
		public bool outsideInMapView {get; set;}
		
		public static AudioMufflerConfig loadConfig() {
			AudioMufflerConfig config = new AudioMufflerConfig();
			
			string path = KSP.IO.IOUtils.GetFilePathFor(typeof(Muffler), "muffler.cfg").Replace("/", System.IO.Path.DirectorySeparatorChar.ToString());
	        ConfigNode node = ConfigNode.Load(path);
	        
	        config.debug = bool.Parse(node.GetValue("debug"));
	        config.engageMuffler = bool.Parse(node.GetValue("enabled"));
			config.minCacheUpdateInterval = int.Parse(node.GetValue("minCacheUpdateInterval"));
			config.wallCutoff = float.Parse(node.GetValue("wallCutoff"));
			config.minimalCutoff = float.Parse(node.GetValue("minimalCutoff"));
			config.helmetOutsideIVA = bool.Parse(node.GetValue("helmetOutsideIVA"));
			config.helmetOutsideEVA = bool.Parse(node.GetValue("helmetOutsideEVA"));
			config.helmetForUnmanned = bool.Parse(node.GetValue("helmetOutsideEVA"));
			config.helmetInMapView = bool.Parse(node.GetValue("helmetInMapView"));
			config.vesselInMapView = bool.Parse(node.GetValue("vesselInMapView"));
			config.outsideInMapView = bool.Parse(node.GetValue("outsideInMapView"));
			
			return config;
		}
		
	}
}
