using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
A dev panel.
*/

public class DevGUIMenuDialogAnimations : DevGUIMenu
{
	private Color selectedButtonColor = new Color(0.6f, 1.0f, 0.6f);

	public override void drawGuts()
	{
		GUILayout.BeginHorizontal();
		
		Dialog.animInTime = floatInputField("Anim In Time", Dialog.animInTime.ToString(), 0.05f);
		Dialog.animOutTime = floatInputField("Anim Out Time", Dialog.animOutTime.ToString(), 0.05f);
	
		GUILayout.EndHorizontal();
		
		// The ease filter argument is the opposite of the animation, since we need to see
		// the ease type at the opposite end of the animation. For example, when we're
		// sliding "out" of view, we need to see the ease type that has something
		// happening at the beginning, or "in" part of the animation.
		Dialog.animInPos = drawPositionButtons("Slide In From...", Dialog.animInPos);
		Dialog.animInScale = drawScaleButtons("Scale In From...", Dialog.animInScale);
		Dialog.animInEase = drawEaseButtons("Anim In Ease Type...", Dialog.animInEase);

		Dialog.animOutPos = drawPositionButtons("Slide Out To...", Dialog.animOutPos);
		Dialog.animOutScale = drawScaleButtons("Scale Out To...", Dialog.animOutScale);
		Dialog.animOutEase = drawEaseButtons("Anim Out Ease Type...", Dialog.animOutEase);
	}
	
	private Dialog.AnimPos drawPositionButtons(string title, Dialog.AnimPos current)
	{
		Color normalButtonColor = GUI.color;
		
		drawHeader(title);

		GUILayout.BeginHorizontal();

		for (int i = 0; i < (int)Dialog.AnimPos.COUNT; i++)
		{
			Dialog.AnimPos pos = (Dialog.AnimPos)i;
			
			if (current == pos)
			{
				GUI.color = selectedButtonColor;
			}
			
			if (GUILayout.Button(pos.ToString()))
			{
				current = pos;
			}
			
			GUI.color = normalButtonColor;
		}
		
		GUILayout.EndHorizontal();
		
		GUILayout.Space(20);
		
		return current;
	}
	
	private Dialog.AnimScale drawScaleButtons(string title, Dialog.AnimScale current)
	{
		Color normalButtonColor = GUI.color;

		drawHeader(title);

		GUILayout.BeginHorizontal();

		for (int i = 0; i < (int)Dialog.AnimScale.COUNT; i++)
		{
			Dialog.AnimScale scale = (Dialog.AnimScale)i;
			
			if (current == scale)
			{
				GUI.color = selectedButtonColor;
			}
			
			if (GUILayout.Button(scale.ToString()))
			{
				current = scale;
			}
			
			GUI.color = normalButtonColor;
		}
		
		GUILayout.EndHorizontal();
		
		GUILayout.Space(20);
	
		return current;
	}

	private Dialog.AnimEase drawEaseButtons(string title, Dialog.AnimEase current)
	{
		Color normalButtonColor = GUI.color;

		drawHeader(title);

		GUILayout.BeginHorizontal();

		for (int i = 0; i < (int)Dialog.AnimEase.COUNT; i++)
		{
			Dialog.AnimEase ease = (Dialog.AnimEase)i;
			
			if (current == ease)
			{
				GUI.color = selectedButtonColor;
			}
			
			if (GUILayout.Button(ease.ToString()))
			{
				current = ease;
			}
			
			GUI.color = normalButtonColor;
		}
		
		GUILayout.EndHorizontal();

		GUILayout.Space(20);
		
		return current;
	}
	
	private void drawHeader(string title)
	{
		Color normalButtonColor = GUI.color;

		GUILayout.BeginHorizontal();
		GUI.enabled = false;
		GUI.color = new Color(0.5f, 0.5f, 0.5f);
		GUILayout.Button(title);
		GUI.enabled = true;
		GUI.color = normalButtonColor;
		GUILayout.EndHorizontal();
	}
	
	// Implements IResetGame
	new public static void resetStaticClassData()
	{
	}
}
