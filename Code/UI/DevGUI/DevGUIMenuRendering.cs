using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuRendering : DevGUIMenu
{
	private bool showCameraChildren;
	private static bool enableOcclusionCulling = true;

	public override void drawGuts()
	{
		
		if (ResolutionChangeHandler.instance != null)
		{
			string s = "Virtual Screen: " + (ResolutionChangeHandler.instance.virtualScreenMode ? "On" : "Off");
			s += "(" + ResolutionChangeHandler.instance.virtualOverrideMode.ToString() + ")";
			if (GUILayout.Button(s))
			{
				switch (ResolutionChangeHandler.instance.virtualOverrideMode)
				{
					case ResolutionChangeHandler.VirtualScreenOverride.AUTO:
						ResolutionChangeHandler.instance.virtualOverrideMode = ResolutionChangeHandler.VirtualScreenOverride.FORCE_ON;
						break;
					case ResolutionChangeHandler.VirtualScreenOverride.FORCE_ON:
						ResolutionChangeHandler.instance.virtualOverrideMode = ResolutionChangeHandler.VirtualScreenOverride.FORCE_OFF;
						break;
					case ResolutionChangeHandler.VirtualScreenOverride.FORCE_OFF:
						ResolutionChangeHandler.instance.virtualOverrideMode = ResolutionChangeHandler.VirtualScreenOverride.AUTO;
						break;
				}
				ResolutionChangeHandler.instance.forceResize();
			}
		}

		ScreenOrientation prevOrientation = UnityEngine.Screen.orientation;
		string[] orientNames = System.Enum.GetNames(typeof(ScreenOrientation));
		ScreenOrientation newOrientation = (ScreenOrientation)GUILayout.SelectionGrid((int)prevOrientation, orientNames, 3);
		if (newOrientation != prevOrientation)
		{
			UnityEngine.Screen.orientation = newOrientation;
		}
		
		if (GUILayout.Button("Swap Resolution Width/Height"))
		{
			UnityEngine.Screen.SetResolution(UnityEngine.Screen.height, UnityEngine.Screen.width, true);
		}
		
		showCameraChildren = GUILayout.Toggle(showCameraChildren, "Show children for each camera");
		enableOcclusionCulling = GUILayout.Toggle(enableOcclusionCulling, "Occlusion culling on all cameras");

		Camera[] cams = Object.FindObjectsOfType<Camera>();

		foreach (Camera cam in cams)
		{
			cam.enabled = GUILayout.Toggle(cam.enabled, cam.name);
			cam.useOcclusionCulling = enableOcclusionCulling;
			if (showCameraChildren && cam.enabled)
			{
				List<GameObject> children = CommonGameObject.findAllChildren(cam.gameObject, true);
				foreach (GameObject go in children)
				{
					bool isActive = GUILayout.Toggle(go.activeSelf, "     " + go.name);

					if (isActive != go.activeSelf)
					{
						go.SetActive(isActive);
					}
				}
			}
		}

	}

	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
