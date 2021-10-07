using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Round variant module specific to "choose items" type of picking games
 */
public class PickingGameModule : ChallengeGameModule
{
	protected ModularPickingGameVariant pickingVariantParent;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	// Overrides HAVE TO call base.executeOnRoundInit!
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		pickingVariantParent = round as ModularPickingGameVariant;
	}
	
	// executes when the first pick is revealed, happens right before executeOnItemClick()
	// module hook happens on the first reveal
	public virtual bool needsToExecuteOnFirstPickItemClicked()
	{
		return false;
	}

	public virtual IEnumerator executeOnFirstPickItemClicked()
	{
		yield break;
	}

	// executes when a player clicks / taps on an item
	public virtual bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		return false;
	}

	public virtual IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		yield break;
	}

	// executes when a player clicks / taps on an item
	public virtual bool needsToExecuteOnAdvancePick()
	{
		return false;
	}

	public virtual IEnumerator executeOnAdvancePick()
	{
		yield break;
	}
	
	// execute when input is enabled (for example, turn on glows when you can pick).
	public virtual bool needsToExecuteOnInputEnabled()
	{
		return false;
	}
	
	public virtual IEnumerator executeOnInputEnabled()
	{
		yield break;
	}
	
	// execute when input is disabled (for example, turn off glows right after you pick).
	public virtual bool needsToExecuteOnInputDisabled()
	{
		return false;
	}

	public virtual IEnumerator executeOnInputDisabled()
	{
		yield break;
	}
	
	// executes on an item reveal, after the pick has been advanced and the picks remaining label has been updated
	// but before the isRoundOver() check is called.  Basically allows you to block a pick being considered handled
	// and the round ending or input being unlocked, until you've handled whatever you want to handle in the module
	// that implements this.
	public virtual bool needsToExecuteOnItemRevealedPreIsRoundOverCheck()
	{
		return false;
	}

	public virtual IEnumerator executeOnItemRevealedPreIsRoundOverCheck()
	{
		yield break;
	}
}
