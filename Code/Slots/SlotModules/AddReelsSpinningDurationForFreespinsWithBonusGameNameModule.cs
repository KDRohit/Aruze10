using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module that allows freespin spin times to be increased.  For instance on orig004 for a Spin Till You Win
 * freespin Lance requested that the actual duration of the spins be increased.  Standard Freespin times don't
 * have an actual set duration we wait, so this module can be used to target a specific freespin and make the spin time longer.
 * I need to specifically target the bonus because orig004 Crafty Coyote shares its freespin prefab with 3 versions of freespins,
 * but we only want the freespin times to be delayed on one specific version.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 1/4/2021
 */
public class AddReelsSpinningDurationForFreespinsWithBonusGameNameModule : SlotModule
{
	[Tooltip("List of the bonus games that should have reel spins delayed by a set duration")]
	[SerializeField] private List<string> bonusGameNames;
	[SerializeField] private float reelSpinDuration = 1.5f;
	
	private bool isNameListFormatted = false;
	private bool isBonusGameNameMatched = false;

	public override void Awake()
	{
		base.Awake();
		// Need to perform this once on Awake() since the bonus game info we are checking will be cleared after that
		isBonusGameNameMatched = checkForBonusGameNameMatch();
	}

	// executeOnReelsSpinning() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) immediately after the reels start spinning
	public override bool needsToExecuteOnReelsSpinning()
	{
		return isBonusGameNameMatched;
	}
	
	public override IEnumerator executeOnReelsSpinning()
	{
		if (reelSpinDuration > 0.0f)
		{
			yield return new TIWaitForSeconds(reelSpinDuration);
		}
	}

	private bool checkForBonusGameNameMatch()
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
