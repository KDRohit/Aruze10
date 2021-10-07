using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Base reel game class for Gen10 Howling Wilds
*/
public class Gen10 : SlotBaseGame
{
	[SerializeField] private Gen10MoonEffect moonEffect;
	
	[HideInInspector] public bool doWildReplacement = false;

	private string mutationTarget = "";

	/// slotOutcomeEventCallback - after a spin occurs, Server calls this with the results.
	protected override void slotOutcomeEventCallback(JSON data)
	{
		if (isSpinTimedOut)
		{
			// Not matching base class because this class has many changes to it.
			return;
		}
		
		// cancel the spin timeout since we recieved a response from the server
		setIsCheckingSpinTimeout(false);

		base.setOutcome(data);

		if (mutationManager.mutations.Count > 0)
		{
			this.StartCoroutine(this.doWilds());
		}
		else
		{
			this.setEngineOutcome(_outcome);
		}

		// IF ANYONE ELSE DOES THIS I WILL HUNT YOU DOWN AND HURT YOU
		// Usually it is done in the base class!  But there is no call to 
		// the base class here so I have to jam the code in here too.
		// This is how we measure the timing between requesting a spin from
		// the server and receieving it
#if ZYNGA_TRAMP
		AutomatedPlayer.spinReceived();
#endif
	}

	/// Brings up the crystal ball and activates the wild overlay for the chosen symbol
	private IEnumerator doWilds()
	{
		// Find the symbol that is being made wild
		JSON mut = outcome.getMutations()[0];
		mutationTarget = mut.getString("replace_symbol", "");

		if (mutationTarget == "M1")
		{
			mutationTarget = "M1-2A";
		}

		yield return StartCoroutine(moonEffect.playMoonFeature(mutationTarget));

		this.setEngineOutcome(_outcome);
	}

	public override SymbolAnimator getSymbolAnimatorInstance(string name, int columnIndex = -1, bool forceNewInstance = false, bool canSearchForMegaIfNotFound = false)
	{
		// Grab the symbol and activate its wild overlay if its the targeted symbol from the mutation
		SymbolAnimator newSymbolAnimator;

		string serverName = SlotSymbol.getServerNameFromName(name);
		if (doWildReplacement && serverName == mutationTarget)
		{
			newSymbolAnimator = base.getSymbolAnimatorInstance(serverName, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
			newSymbolAnimator.showWild();
		}
		else
		{
			newSymbolAnimator = base.getSymbolAnimatorInstance(name, columnIndex, forceNewInstance, canSearchForMegaIfNotFound);
		}
		
		return newSymbolAnimator;
	}

	/// Function to handle changes that derived classes need to do before a new spin occurs
	/// called from both normal spins and forceOutcome
	protected override IEnumerator prespin()
	{
		yield return StartCoroutine(base.prespin());

		foreach (SlotReel reel in engine.getAllSlotReels())
		{
			reel.reelStopSoundOverride = "";
		}

		if (mutationTarget != "")
		{
			hideAllWildOverlays();
		}
		
		mutationTarget = "";
		doWildReplacement = false;
	}

	/// Go thourgh all the symbols and hide all the wild overlays
	private void hideAllWildOverlays()
	{
		foreach (SlotReel reel in engine.getReelArray())
		{
			foreach (SlotSymbol symbol in reel.symbolList)
			{
				symbol.hideWild();
			}
		}
	}
}
