using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zynga.Zdk;

public class DevGUIMenuSlideControlTuner : DevGUIMenu
{

	private List<SlideController> slideControllers = new List<SlideController>();
	private List<bool> isCollapsedControls = new List<bool>();

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Grab Slide Controllers", GUILayout.Width(100)))
		{
			refreshActiveSlideControllers();
		}
		GUILayout.EndHorizontal();

		for (int i = slideControllers.Count - 1; i >= 0; i--)
		{
			if (slideControllers[i] != null)
			{
				GUILayout.BeginHorizontal();
				isCollapsedControls[i] = GUILayout.Toggle(isCollapsedControls[i], slideControllers[i].name);
				GUILayout.EndHorizontal();
				if (isCollapsedControls[i])
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label("Momentum Modifier ");
					slideControllers[i].momentumModifier = floatInputField("", slideControllers[i].momentumModifier.ToString(), 0.1f, 0);
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Max Momentum ");
					slideControllers[i].maxMomentum = floatInputField("", slideControllers[i].maxMomentum.ToString(), 0.1f, 0);
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Friction ");
					slideControllers[i].friction = floatInputField("", slideControllers[i].friction.ToString(), 0.1f, 0);
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Use Mouse Scroll ");
					slideControllers[i].shouldUseMouseScroll = GUILayout.Toggle(slideControllers[i].shouldUseMouseScroll, "");
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					GUILayout.Label("Mouse Scroll Speed");
					slideControllers[i].mouseScrollSpeed = floatInputField("", slideControllers[i].mouseScrollSpeed.ToString(), 0.1f, 0);
					GUILayout.EndHorizontal();

				}
			}
			else
			{
				slideControllers.RemoveAt(i);
				isCollapsedControls.RemoveAt(i);
			}
		}
	}


	private void refreshActiveSlideControllers()
	{
		slideControllers = new List<SlideController>();
		slideControllers.AddRange(NGUIExt.uiRoot.GetComponentsInChildren<SlideController>());

		isCollapsedControls = new List<bool>(new bool[slideControllers.Count]);

		for (int i = slideControllers.Count - 1; i >= 0; i--)
		{
			if (!slideControllers[i].isActiveAndEnabled)
			{
				slideControllers.RemoveAt(i);
				isCollapsedControls.RemoveAt(i);
			}
			else
			{
				isCollapsedControls[i] = false;
			}
		}
	}

// Implements IResetGame
	new static void resetStaticClassData()
	{
		//no static data need to be reset.
	}

}
