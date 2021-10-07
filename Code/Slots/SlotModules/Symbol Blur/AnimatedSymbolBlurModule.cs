using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Enables/disables different game objects to turn motion blur on/off for animated symbols
(that cannot use the SymbolBlurModule method)

Original Author: Frederic My
*/
public class AnimatedSymbolBlurModule : SlotModule 
{
	private SlotReel[] allReels;
/*
	private void Update()
	{
		SlotReel[] reels = getReels();
		if(reels == null)
		{
			return;
		}

		bool doBlur = DevGUIMenuPerformance.doMotionBlur;
		foreach(var reel in reels)
		{
			enableBlur(reel, reelIsBlurry(reel) && doBlur);
		}
	}

	private SlotReel[] getReels()
	{
		return allReels;
	}

	private bool reelIsBlurry(SlotReel reel)
	{
		SpinReel spinReel = reel as SpinReel;
		if(spinReel != null)
		{
			if(spinReel.isAnimatedAnticipationPlaying)
			{
				return false;
			}
		}

		return reel.isSpinning || reel.isSpinEnding;
	}

// executeOnSlotGameStarted() section
// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true; 
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		SlotEngine engine = reelGame.engine;
		allReels = engine.getAllSlotReels();
		yield break;
	}

// executeAfterSymbolSetup() section
// Functions in this section are called once a symbol has been setup.
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		return true;
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		SymbolAnimator animator = symbol.animator;
		GameObject scalingPart = symbol.scalingSymbolPart;
		if(animator == null || scalingPart == null)
		{
			return;
		}

		if(animator.blurData == null)
		{
			animator.blurData = scalingPart.GetComponentInChildren<AnimatedSymbolBlur>();
		}
	}

//
//

	private void enableBlur(SlotReel reel, bool enable)
	{
		List<SlotSymbol> reelSymbols = reel.symbolList;
		foreach(var symbol in reelSymbols)
		{
			enableBlur(symbol, enable);
		}
	}
		
	private void enableBlur(SlotSymbol symbol, bool enable)
	{
		SymbolAnimator animator = symbol.animator;
		AnimatedSymbolBlur data = animator != null ? animator.blurData : null;
		if(data == null)
		{
			return;
		}

		foreach(var gameObject in data.standardObjects)
		{
			if(gameObject.activeSelf == enable)		// object and blur should be in opposite active states
			{
				gameObject.SetActive(!enable);
			}
		}

		foreach(var gameObject in data.blurredObjects)
		{
			if(gameObject.activeSelf != enable)		// object and blur should be in the same active state
			{
				gameObject.SetActive(enable);
			}
		}
	}
	*/
}
