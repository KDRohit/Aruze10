using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class to serve as the base class for ModularCliffhangerGameVariant related modules.
 * We'll see what additional needs to be added, but having this base class will allow
 * a uniform way of accessing the ModularCliffhangerGameVariant (which we'll need some
 * level of communication between modules and that variant class in order to correctly
 * advance the game, this is because the variant is managing the cliffhanger meter and
 * the character that moves along it).
 *
 * Creation Date: 1/26/2021
 * Original Author: Scott Lepthien
 */
public class CliffhangerGameModule : PickingGameRevealModule
{
	protected ModularCliffhangerGameVariant cliffhangerVariantParent;

	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}

	// Overrides HAVE TO call base.executeOnRoundInit!
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		cliffhangerVariantParent = round as ModularCliffhangerGameVariant;
	}
}
