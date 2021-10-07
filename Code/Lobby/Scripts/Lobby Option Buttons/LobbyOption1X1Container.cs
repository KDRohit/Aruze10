using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Serves as the container for two 1X1 options that are stacked in the same column in the SIR ListScroller-style lobby.
*/

public class LobbyOption1X1Container : MonoBehaviour
{
	private const int SPACING_Y = -376;	// How far the 1X1's are vertically spaced in the container.

	public GameObject optionPrefabGeneric;
	public GameObject optionPrefabComingSoon;
	
	private List<LobbyOptionButton> buttons = new List<LobbyOptionButton>() { null, null };
	private List<bool> isComingSoon = new List<bool>() { false, false };	// Whether each button in buttons is a "coming soon" button.
	
	public void setup(List<LobbyOption> options)
	{
		if (buttons[0] == null)
		{
			// If this is the first time using this panel, pre-populate the two 1X1 panels with generic buttons.
			for (int i = 0; i < 2; i++)
			{
				createPanel(optionPrefabGeneric, i);
			}
		}
		
		for (int i = 0; i < options.Count; i++)
		{
			LobbyOption option = options[i];
			
			// Only create a new panel if the existing one for this index doesn't match what we need.
			if (option.type == LobbyOption.Type.COMING_SOON && !isComingSoon[i])
			{
				createPanel(optionPrefabComingSoon, i);
				isComingSoon[i] = true;
			}
			else if (option.type != LobbyOption.Type.COMING_SOON && isComingSoon[i])
			{
				createPanel(optionPrefabGeneric, i);
				isComingSoon[i] = false;
			}

			option.button = buttons[i];
			option.button.setup(option);
			CommonTransform.setY(option.button.transform, i * SPACING_Y);
			
			if (option.type != LobbyOption.Type.COMING_SOON)
			{
				StartCoroutine(option.loadImages());
			}
		}
	}
	
	private void createPanel(GameObject prefab, int index)
	{
		if (buttons[index] != null)
		{
			Destroy(buttons[index].gameObject);
		}
		GameObject panel = NGUITools.AddChild(gameObject, prefab);
		buttons[index] = panel.GetComponent<LobbyOptionButton>();
	}
}
