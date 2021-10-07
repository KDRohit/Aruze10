using UnityEngine;
using System.Collections;

/**
Class for added functionality for the buttons that are used to select a major symbol in Zynga02 free spins
*/
public class Zynga02FreeSpinButton : PickGameButton 
{
	[SerializeField] private MeshRenderer[] majorSymbolRenderers = null;	// List of major symbol reveal objects, need these so we can grey them out when they aren't picked

	/// Grey out the major symbol objects when this button is revealed as unpicked
	public void greyOutMajorSymbols()
	{
		foreach (MeshRenderer meshRenderer in majorSymbolRenderers)
		{
			meshRenderer.material.shader = ShaderCache.find("Unlit/GUI Texture Monochrome");
		}
	}
}
