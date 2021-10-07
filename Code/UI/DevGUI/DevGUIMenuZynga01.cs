using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuZynga01 : DevGUIMenu
{
	private string symbolLandScaleAmountX = SymbolAnimator3d.landScaleAmountX + "";
	private string symbolLandScaleAmountY = SymbolAnimator3d.landScaleAmountY + "";
	private string symbolLandScaleAmountZ = SymbolAnimator3d.landScaleAmountZ + "";
	private string symbolLandScaleTime1 = SymbolAnimator3d.landScaleTime1 + "";
	private string symbolLandScaleAmount2X = SymbolAnimator3d.landScaleAmount2X + "";
	private string symbolLandScaleAmount2Y = SymbolAnimator3d.landScaleAmount2Y + "";
	private string symbolLandScaleAmount2Z = SymbolAnimator3d.landScaleAmount2Z + "";
	private string symbolLandScaleTime2 = SymbolAnimator3d.landScaleTime2 + "";
	private string symbolLandScaleTime3 = SymbolAnimator3d.landScaleTime3 + "";

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("Squish Squash Ease Type 1: " + SymbolAnimator3d.landEaseType1.ToString());
		if (GUILayout.Button ("next"))
		{
			SymbolAnimator3d.landEaseType1++;
			int parsedInt = 5;
			int.TryParse(SymbolAnimator3d.landEaseType1.ToString(), out parsedInt);

			if (parsedInt >= 33 || SymbolAnimator3d.landEaseType1.ToString() == "punch")
			{
				SymbolAnimator3d.landEaseType1 = 0;
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Squish Squash Ease Type 2: " + SymbolAnimator3d.landEaseType2.ToString());
		if (GUILayout.Button ("next"))
		{
			SymbolAnimator3d.landEaseType2++;
			int parsedInt = 5;
			int.TryParse(SymbolAnimator3d.landEaseType2.ToString(), out parsedInt);
			
			if (parsedInt >= 33 || SymbolAnimator3d.landEaseType2.ToString() == "punch")
			{
				SymbolAnimator3d.landEaseType2 = 0;
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Squish Squash Ease Type 3: " + SymbolAnimator3d.landEaseType3.ToString());
		if (GUILayout.Button ("next"))
		{
			SymbolAnimator3d.landEaseType3++;
			int parsedInt = 5;
			int.TryParse(SymbolAnimator3d.landEaseType3.ToString(), out parsedInt);
			
			if (parsedInt >= 33 || SymbolAnimator3d.landEaseType3.ToString() == "punch")
			{
				SymbolAnimator3d.landEaseType3 = 0;
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		symbolLandScaleAmountX = floatInputField("On Land Scale Amount X", symbolLandScaleAmountX, .1f).ToString();
		if (GUILayout.Button ("Set"))
		{
			SymbolAnimator3d.landScaleAmountX = float.Parse(symbolLandScaleAmountX);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		symbolLandScaleAmountY = floatInputField("On Land Scale Amount Y", symbolLandScaleAmountY, .1f).ToString();
		if (GUILayout.Button ("Set"))
		{
			SymbolAnimator3d.landScaleAmountY = float.Parse(symbolLandScaleAmountY);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		symbolLandScaleAmountZ = floatInputField("On Land Scale Amount Z", symbolLandScaleAmountZ, .1f).ToString();
		if (GUILayout.Button ("Set"))
		{
			SymbolAnimator3d.landScaleAmountZ = float.Parse(symbolLandScaleAmountZ);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		symbolLandScaleTime1 = floatInputField("On Land Scale Time 1", symbolLandScaleTime1, .1f).ToString();
		if (GUILayout.Button ("Set"))
		{
			SymbolAnimator3d.landScaleTime1 = float.Parse(symbolLandScaleTime1);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		symbolLandScaleAmount2X = floatInputField("On Land Scale Amount X 2", symbolLandScaleAmount2X, .1f).ToString();
		if (GUILayout.Button ("Set"))
		{
			SymbolAnimator3d.landScaleAmount2X = float.Parse(symbolLandScaleAmount2X);
		}
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		symbolLandScaleAmount2Y = floatInputField("On Land Scale Amount Y 2", symbolLandScaleAmount2Y, .1f).ToString();
		if (GUILayout.Button ("Set"))
		{
			SymbolAnimator3d.landScaleAmount2Y = float.Parse(symbolLandScaleAmount2Y);
		}
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		symbolLandScaleAmount2Z = floatInputField("On Land Scale Amount Z 2", symbolLandScaleAmount2Z, .1f).ToString();
		if (GUILayout.Button ("Set"))
		{
			SymbolAnimator3d.landScaleAmount2Z = float.Parse(symbolLandScaleAmount2Z);
		}
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		symbolLandScaleTime2 = floatInputField("On Land Scale Time 2", symbolLandScaleTime2, .1f).ToString();
		if (GUILayout.Button ("Set"))
		{
			SymbolAnimator3d.landScaleTime2 = float.Parse(symbolLandScaleTime2);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		symbolLandScaleTime3 = floatInputField("On Land Scale Time 3", symbolLandScaleTime3, .1f).ToString();
		if (GUILayout.Button ("Set"))
		{
			SymbolAnimator3d.landScaleTime3 = float.Parse(symbolLandScaleTime3);
		}
		GUILayout.EndHorizontal();
	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
