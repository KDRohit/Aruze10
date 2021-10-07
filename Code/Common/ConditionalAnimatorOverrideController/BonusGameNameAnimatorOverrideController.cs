using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class that allows for an Animator's controller to be overridden with an AnimatorOverrideController
 * which changes the animations when the object it is attached to meets conditions.  This class does this
 * based on what the current bonus game name is, for instance if different versions of the bonus need
 * different animations (specifically default states, but could be used for more complex anim swaps).
 * See the following link for more info: https://docs.unity3d.com/2018.4/Documentation/Manual/AnimatorOverrideController.html?_ga=2.34165857.197747741.1582248448-945471438.1544752098
 *
 * Original Author: Scott Lepthien
 * Creation Date: 2/21/2020
 */
public class BonusGameNameAnimatorOverrideController : ConditionalAnimatorOverrideController
{
	[Tooltip("List of the bonus games that need to swap animations using this script")]
	[SerializeField] private List<string> bonusGameNames;

	private bool isNameListFormatted = false;

	// Override this to control if the AnimatorOverrideController is used or not
	// Check if this bonus game matches the names we are looking to swap on
	protected override bool needsToExecuteOnAwake()
	{
		if (BonusGameManager.instance == null || BonusGameManager.instance.outcomes == null)
		{
			return false;
		}

		formatGameKeyIntoBonusGameNames();

		foreach (BaseBonusGameOutcome bonusGameOutcome in BonusGameManager.instance.outcomes.Values)
		{
			if (bonusGameNames.Contains(bonusGameOutcome.bonusGameName))
			{
				return true;
			}
		}

		return false;
	}

	private void formatGameKeyIntoBonusGameNames()
	{
		if (!isNameListFormatted)
		{
			if (GameState.game != null)
			{
				// Try to auto add the game key, so we can use strings that will work directly in cloned prefabs
				for (int i = 0; i < bonusGameNames.Count; i++)
				{
					bonusGameNames[i] = string.Format(bonusGameNames[i], GameState.game.keyName);
				}
			}

			isNameListFormatted = true;
		}
	}
}
