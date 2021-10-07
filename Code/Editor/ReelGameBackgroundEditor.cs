using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/* 
This is a custom inspector to allow slot games using ReelGameBackground script to toggle 
on the special win overlay in the editor, to verify the game will visually change correctly
when one of those types of overlays is enabled.

Original Author: Scott Lepthien
Creation Date: 2/23/2017
*/
[CustomEditor(typeof(ReelGameBackground))]
public class ReelGameBackgroundEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		ReelGameBackground reelGameBackgroundScript = (ReelGameBackground)target;

		if (Application.isPlaying)
		{
			bool isAlreadyShowingOverlay = SpinPanel.instance != null && SpinPanel.instance.isShowingSpecialWinOverlay;

			// only allow games setup as base games, that use ortho cameras, and that don't have static reel areas to force toggle on the special win overlay
			// as those will be the only games that will be affected
			if (!isAlreadyShowingOverlay && reelGameBackgroundScript.isUsingOrthoCameras
				&& reelGameBackgroundScript.gameSize == ReelGameBackground.GameSizeOverrideEnum.Basegame)
			{
				bool isTogglingForcingSpecialWinOverlayOn = reelGameBackgroundScript.isForceShowingSpecialWinOverlay;
				isTogglingForcingSpecialWinOverlayOn = GUILayout.Toggle(isTogglingForcingSpecialWinOverlayOn, "Force Show Special Win Overlay");

				if (isTogglingForcingSpecialWinOverlayOn != reelGameBackgroundScript.isForceShowingSpecialWinOverlay)
				{
					reelGameBackgroundScript.toggleForceShowSpecialWinOverlay(isTogglingForcingSpecialWinOverlayOn);
				}
			}
		}

		if (!Application.isPlaying && reelGameBackgroundScript.isUsingOrthoCameras)
		{
			bool isTogglingForceUsingUIBoundsScaling = reelGameBackgroundScript.isForceUsingUIBoundsScaling;
			isTogglingForceUsingUIBoundsScaling = GUILayout.Toggle(isTogglingForceUsingUIBoundsScaling, "Test UI Bounds Scaling");
			
			if (isTogglingForceUsingUIBoundsScaling != reelGameBackgroundScript.isForceUsingUIBoundsScaling)
			{
				reelGameBackgroundScript.toggleForceUsingUIBoundsScaling(isTogglingForceUsingUIBoundsScaling);
			}

			if (isTogglingForceUsingUIBoundsScaling)
			{
				bool isCreatingDebugCollidersForForceUsingUIBoundsScaling = GUILayout.Toggle(reelGameBackgroundScript.isCreatingDebugCollidersForForceUsingUIBoundsScaling, "Create UI Bounds Debug Colliders");
				
				if (isCreatingDebugCollidersForForceUsingUIBoundsScaling != reelGameBackgroundScript.isCreatingDebugCollidersForForceUsingUIBoundsScaling)
				{
					reelGameBackgroundScript.isCreatingDebugCollidersForForceUsingUIBoundsScaling = isCreatingDebugCollidersForForceUsingUIBoundsScaling;
				
					if (isCreatingDebugCollidersForForceUsingUIBoundsScaling)
					{
						reelGameBackgroundScript.forceRunUpdateWhenGameIsNotRunning();
					}
					else
					{
						reelGameBackgroundScript.destroyDebugColliders();
					}
				}
			}
		}

		if (Application.isPlaying)
		{
			if (GUILayout.Button("Force Update"))
			{
				reelGameBackgroundScript.forceUpdate();
			}
		}

		// Show default inspector property editor
		DrawDefaultInspector();
	}
}
