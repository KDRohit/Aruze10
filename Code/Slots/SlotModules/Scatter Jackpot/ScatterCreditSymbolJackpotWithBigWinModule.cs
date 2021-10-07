using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Extension of ScatterCreditSymbolJackpotModule which is intended to allow a big
win to be displayed.  Basically changes the presentation to allow the value won
from the scatter at the same time as the line wins so that the big win can display
normally.

Original Author: Scott Lepthien
Creation Date: 05/25/2018
*/
public class ScatterCreditSymbolJackpotWithBigWinModule : ScatterCreditSymbolJackpotModule 
{
	[SerializeField] private string scatterSymbolWithValueSymbolName = "SC_Value";
	[SerializeField] private float showScatterWinAnimationDuration = 2.0f;
	[SerializeField] private AudioListController.AudioInformationList revealScValueSymbolSounds;
	[Header("Particle Trail")]
	[SerializeField] private ParticleTrailController particleTrail = null;
	[SerializeField] private float sparkleTrailZPosition = 0.0f;
	[SerializeField] private Camera sparkleTrailCamera;

	protected SlotBaseGame baseGame = null;
	private SlotSymbol collectSymbol = null;
	private long finalScatterPayout = 0;
	
	public override void Awake()
	{
		base.Awake();
		baseGame = reelGame as SlotBaseGame;
		
		if (sparkleTrailCamera == null && particleTrail != null)
		{
			Debug.LogError("ScatterCreditSymbolJackpotWithBigWinModule.Awake() - particleTrail is set but sparkleTrailCamera isn't without sparkleTrailCamera we can't target the spin panel win box correctly!  Killing this module.");
			Destroy(this);
		}
	}
	
// executeOnPreSpin() section
// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		collectSymbol = null;
		finalScatterPayout = 0;
		yield break;
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		finalScatterPayout = scatterCreditsAwarded * reelGame.multiplier;

		collectSymbol = null;
		rollupFinished = false;
		HashSet<string> uniqueScatterSymbolNames = new HashSet<string>();
		List<SlotSymbol> visibleSymbols = reelGame.engine.getAllVisibleSymbols();
		for (int i = 0; i < visibleSymbols.Count; i++)
		{
			SlotSymbol symbol = visibleSymbols[i];
			if (symbol.isScatterSymbol)
			{
				// track unique symbol names for use in jackpot animaitons check
				if (!uniqueScatterSymbolNames.Contains(symbol.serverName))
				{
					uniqueScatterSymbolNames.Add(symbol.serverName);
				}
			
				if (symbol.serverName == "SC")
				{
					collectSymbol = symbol;
				}

				StartCoroutine(playSymbolAnimation(symbol));
			}
		}
		
		// play any jackpot animations for triggered symbols
		List<TICoroutine> scatterJackpotAnimationCoroutines = new List<TICoroutine>();
		foreach (string symbolName in uniqueScatterSymbolNames)
		{
			ScatterJackpotAnimationsData animationsData = getScatterJackpotAnimationsDataForSymbolName(symbolName);
			scatterJackpotAnimationCoroutines.Add(StartCoroutine(playScatterJackpotWonAnimations(animationsData)));
		}
		
		if (scatterJackpotAnimationCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(scatterJackpotAnimationCoroutines));
		}

		yield return StartCoroutine(playFeatureStartSounds());
		
		// Add a delay here so the player can celebrate and understand what they won before we rollup
		yield return new TIWaitForSeconds(showScatterWinAnimationDuration);
		
		// cancel the "rollup" here since we aren't actually doing one here,
		// just showing the winning symbols and we want the symbols to stop playing
		// before we proceed to the next part of the presentation
		rollupFinished = true;

		while (!symbolsDonePlaying)
		{
			// Wait for the symbols to stop playing.
			yield return null;
		}
		
		// Play the value reveal animaiton on the collect symbol
		SymbolInfo info = reelGame.findSymbolInfo(scatterSymbolWithValueSymbolName);
		if (info != null)
		{
			collectSymbol.mutateTo(scatterSymbolWithValueSymbolName, null, true, true);
			List<TICoroutine> coroutineList = new List<TICoroutine>();
			
			if (revealScValueSymbolSounds.Count > 0)
			{
				coroutineList.Add(StartCoroutine(AudioListController.playListOfAudioInformation(revealScValueSymbolSounds)));
			}
			
			coroutineList.Add(StartCoroutine(collectSymbol.playAndWaitForAnimateOutcome()));

			yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}
		else
		{
			Debug.LogError("ScatterCreditSymbolJackpotWithBigWinModule.executeOnReelsStoppedCallback() - Unable to find symbol info for scatterSymbolWithValueSymbolName = " + scatterSymbolWithValueSymbolName);
		}
		
		// reset the any animating jackpot animations to idle
		yield return StartCoroutine(playScatterJackpotIdleAnimationsOnAnyAnimatingJackpots());
		
		// if we have a particle trail, move that from the collect symbol to the winbox on the spin meter
		if (particleTrail != null && collectSymbol != null)
		{
			Vector3 startPos = collectSymbol.getSymbolWorldPosition();
			Vector3 spinPanelWinAmountScreenPosition = SpinPanel.instance.uiCamera.WorldToScreenPoint(SpinPanel.instance.winningsAmountLabel.transform.position);
			Vector3 sparkleTrailEndPosition = sparkleTrailCamera.ScreenToWorldPoint(spinPanelWinAmountScreenPosition);
			sparkleTrailEndPosition.z = sparkleTrailZPosition;
			Vector3 endPos = SpinPanel.instance.winningsAmountLabel.transform.position;

			yield return StartCoroutine(particleTrail.animateParticleTrail(startPos, sparkleTrailEndPosition, reelGame.gameObject.transform));
		}

		if (baseGame)
		{
			if (baseGame.getSubOutcomeCount() == 0)
			{
				// Credits aren't going to be paid out so pay them out here
				baseGame.addCreditsToSlotsPlayer(finalScatterPayout, "scatter win spin outcome", shouldPlayCreditsRollupSound: false);

				if (reelGame.willPayoutTriggerBigWin(finalScatterPayout))
				{
					// Big win is going to trigger, this means the big win will
					// be in charge of continuing the game, so we aren't going to
					// block on this rollup call
					StartCoroutine(reelGame.rollupCredits(0,
						finalScatterPayout,
						reelGame.onPayoutRollup,
						isPlayingRollupSounds: true));
				}
				else
				{
					// No big win, so we should block until this rollup is over
					// so the player can't spin again until the rollup is complete
					yield return StartCoroutine(reelGame.rollupCredits(0,
						finalScatterPayout,
						reelGame.onPayoutRollup,
						isPlayingRollupSounds: true));
				}
			}
			else
			{
				// Skip the pre-win since it isn't going to make sense with our presentation for the SC rollup
				reelGame.isSkippingPreWinThisSpin = true;
			
				// Set a value so that the final rollup for the paylines we do includes 
				// the bonus we won here
				baseGame.jackpotWinToPayOut = finalScatterPayout;
			}	
		}
	}
	
	// Set the text on the scatter collect symbol
	private void setCollectSymbolLabelToJackpotTotal(SlotSymbol symbol)
	{
		SymbolAnimator symbolAnimator = symbol.getAnimator();
		if (symbolAnimator != null)
		{
			LabelWrapperComponent symbolLabel = symbolAnimator.getDynamicLabel();

			if (symbolLabel != null)
			{
				symbolLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(finalScatterPayout, 1, shouldRoundUp: false);
			}
			else
			{
				Debug.LogWarning("ScatterCreditSymbolJackpotWithBigWinModule.setCollectSymbolLabelToJackpotTotal() - Unable to find symbolLabel for symbol.name = " + symbol.name);
			}
		}
	}
	
// executeAfterSymbolSetup() secion
// Functions in this section are called once a symbol has been setup.
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		if (collectSymbol != null && collectSymbol == symbol)
		{
			SymbolAnimator symbolAnimator = symbol.getAnimator();
			if (symbolAnimator != null)
			{
				LabelWrapperComponent symbolLabel = symbolAnimator.getDynamicLabel();
				if (symbolLabel != null)
				{
					return true;
				}
			}
		}

		return false;
	}
	
	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		setCollectSymbolLabelToJackpotTotal(symbol);
	}
}
