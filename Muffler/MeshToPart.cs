using UnityEngine;

namespace AudioMuffler {

	/// <summary>
	/// Description of MeshToPart.
	/// </summary>
	public class MeshToPart
	{
		
		public MeshFilter meshFilter { get; set;}
		public Part part { get; set;}
		
		public MeshToPart(MeshFilter meshFilter, Part part)
		{
			this.meshFilter = meshFilter;
			this.part = part;
		}
	}

}