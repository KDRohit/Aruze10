using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Main class for displaying the new dev panel.
*/

public class DevGUI : MonoBehaviour, IResetGame
{
	private const int WINDOW_ID = 666;

	public static Rect windowRect;
	private static GameObject instanceObject = null;

	private bool branchNameSet = false;
	private string branchName = "";
	private string windowTitle = "Developer's Panel";

	// Toggle the DevGUI
	public static bool isActive
	{
		get
		{
			return instanceObject != null;
		}
		set
		{
#if UNITY_WEBGL || !ZYNGA_PRODUCTION
			if ((value != (instanceObject != null)) && (Glb.dataUrl.Contains("dev") || Glb.dataUrl.Contains("stag") || Glb.dataUrl.Contains("vii") || Glb.dataUrl.Contains("192.168.63.129")))
			{
				if (instanceObject == null)
				{
					instanceObject = new GameObject("DevGUI");
					instanceObject.AddComponent<DevGUI>();
					// Create a collider that covers the whole screen so non-dev panel stuff can't be touched while showing the dev panel.
					BoxCollider boxCollider = instanceObject.AddComponent<BoxCollider>();
					boxCollider.size = new Vector3(4000, 3000, 1);
					instanceObject.layer = Layers.ID_NGUI;

					NGUIExt.attachToAnchor(instanceObject, NGUIExt.SceneAnchor.DIALOG, Vector3.forward);
					NGUIExt.disableAllMouseInput();
				}
				else
				{
					NGUIExt.enableAllMouseInput();
					Destroy(instanceObject);
					instanceObject = null;
				}
			}
#endif
		}
	}

	void Start()
	{
		if (MobileUIUtil.isSmallMobile)
		{
			// Use the full screen space on small devices.
			windowRect = new Rect(0, 0, Screen.width, Screen.height);
		}
		else
		{
			windowRect = new Rect(Screen.width / 5, 0, 4 * Screen.width / 5, Screen.height);
		}

		DevGUIMenu.setDefaultMenu();
	}

	void Update()
	{
		if (gameObject != instanceObject)
		{
			// For safety sake, suicide if not on the singleton object
			Debug.Log("DevGUIController suicided because it wasn't the GameObject it wanted to be.");
			Destroy(gameObject);
			return;
		}
	}

	void OnGUI()
	{
		//string skin = DevGUIMenu.isHiRes ? "Amiga500GUISkin Hi" : "Amiga500GUISkin Low";
		GUI.skin = DevGUIMenu.isHiRes ? GUIScript.instance.devSkinHi : GUIScript.instance.devSkinLow;

		// widen vertical scrollbars so they are easy to use on device
		GUI.skin.verticalScrollbar.fixedWidth = Screen.width * 0.05f;
		GUI.skin.verticalScrollbarThumb.fixedWidth = Screen.width * 0.05f;

		//GUI.skin = AssetDatabase.LoadAssetAtPath("Assets/Data/Common/Other/Amiga500/" + skin, typeof(GUISkin)) as GUISkin;
		windowRect = GUI.Window(WINDOW_ID, windowRect, DevGUIMenu.drawCurrentMenu, windowTitle);
	}
	
	/// Implements IResetGame
	public static void resetStaticClassData()
	{
		instanceObject = null;
	}
}
