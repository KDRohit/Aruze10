using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Helper tool for creating offset and tiling values for a material that should only use part of a texture.
Enter the textures original size (before import), and the pixel offset (from lower left)
and pixel size within the texture of the part you want shown on the material.
*/

public class UVCalculatorEditor : ScriptableWizard
{
	public Material materialToModify;
	public Vector2int originalTextureSize;
	public int pixelsFromLeft;
	public int pixelsFromBottom;
	public int pixelsWide;
	public int pixelsTall;
	
	[MenuItem("Zynga/Wizards/UV Calculator")]
	public static void openDialogEditor()
	{
		ScriptableWizard.DisplayWizard<UVCalculatorEditor>("UV Calculator", "Close", "Calculate");
	}
		
	public void OnWizardOtherButton()
	{
		if (materialToModify == null || originalTextureSize.x == 0 || originalTextureSize.y == 0)
		{
			return;
		}
		
		Vector2 offset = new Vector2((float)pixelsFromLeft / originalTextureSize.x, (float)pixelsFromBottom / originalTextureSize.y);
		Vector2 scale = new Vector2((float)pixelsWide / originalTextureSize.x, (float)pixelsTall / originalTextureSize.y);
		
		materialToModify.SetTextureOffset("_MainTex", offset);
		materialToModify.SetTextureScale("_MainTex", scale);
	}
}