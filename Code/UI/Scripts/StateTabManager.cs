using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Very simplified class for managing selections of StateImageButtonHandlers, as per a typical navigation
/// or "tab" setup.
/// </summary>
public class StateTabManager : MonoBehaviour
{
	public List<StateImageButtonHandler> tabHandlers;
	private List<UIStateImageButton> buttons;

	/// <summary>
	/// Once a tab is selected, pass the handler that was used. This will disable the current handler from being
	/// pressed again, set the correct selection sprite, and enable the other tabs to be selected
	/// </summary>
	/// <param name="tabButton"></param>
	public void onTabSelected(StateImageButtonHandler tabButton)
	{
		if (buttons == null || buttons.Count != tabHandlers.Count)
		{
			buttons = new List<UIStateImageButton>();
			for (int i = 0; i < tabHandlers.Count; ++i)
			{
				UIStateImageButton button = tabHandlers[i].GetComponent<UIStateImageButton>();
				buttons.Add(button);
			}
		}

		for (int i = 0; i < tabHandlers.Count; ++i)
		{
			tabHandlers[i].enabled = true;
			buttons[i].SetSelected();
		}

		if (tabButton != null)
		{
			UIStateImageButton selectedButton = tabButton.GetComponent<UIStateImageButton>();

			if (selectedButton != null)
			{
				selectedButton.SetSelected(true);
			}
		}
	}
}