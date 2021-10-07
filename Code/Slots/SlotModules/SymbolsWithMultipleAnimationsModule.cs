using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
Class created to handle aruze04 which has intro animations before our standard looped outcome animations for some symbols.
In order to accomplish this we will have teh symbols animate, and then when they finish check if there is a second animation
that they should switch to.

Original Author: Scott Lepthien
Creation Date: November 8, 2017
*/
public class SymbolsWithMultipleAnimationsModule : SlotModule 
{
	[SerializeField] private AdditionalAnimInfo[] animationChangeInfoList;
	[System.Serializable]
	public class AdditionalAnimInfo 
	{
		public string targetSymbolName = "";
		public string symbolMutationName = "";
		public float targetSymbolAnimatorStopPoint = 0.0f;
		
		[Tooltip("Set to True if we want to play the loop symbol anticipation animation")]
		public bool shouldPlayAnimationAfterMutate = false;
	}

	private Dictionary<string, string> animationChangeDictionary = new Dictionary<string, string>(); // Dictionary conversion of AdditionalAnimInfo, so we can quickly lookup what to change symbols into after they are animated by a payline
	private List<SlotSymbol> symbolsAnimated = new List<SlotSymbol>();

	public override void Awake()
	{
		base.Awake();

		for (int i = 0; i < animationChangeInfoList.Length; i++)
		{
			AdditionalAnimInfo currentAdditionalAnimInfo = animationChangeInfoList[i];

			if (!animationChangeDictionary.ContainsKey(currentAdditionalAnimInfo.targetSymbolName))
			{
				animationChangeDictionary.Add(currentAdditionalAnimInfo.targetSymbolName, currentAdditionalAnimInfo.symbolMutationName);
			}
			else
			{
				Debug.LogError("SymbolsWithMultipleAnimationsModule.Awake() - Multiple entries in animationChangeInfoList are sharing the same targetSymbolName = " + currentAdditionalAnimInfo.targetSymbolName);
			}
		}
	}

	// executeOnPaylineHide() section
	// function in this section are accesed by ReelGame.onPaylineHidden()
	public override bool needsToExecuteOnSymbolAnimationFinished(SlotSymbol animatorSymbol)
	{
		if (animationChangeInfoList.Length > 0)
		{
			if (animationChangeDictionary.ContainsKey(animatorSymbol.baseName))
			{
				return true;
			}
		}
		else
		{
			Debug.LogError("SymbolsWithMultipleAnimationsModule.needsToExecuteOnPaylineHide() - animationChangeInfoList is empty, this module will do nothing!");
			return false;
		}

		return false;
	}
	
	public override IEnumerator executeOnSymbolAnimationFinished(SlotSymbol animatorSymbol)
	{
		AdditionalAnimInfo animInfo = getAnimInfoForSymbol(animatorSymbol);
		
		// Convert any symbols over to their next form
		string mutationName = animationChangeDictionary[animatorSymbol.baseName];

		SymbolInfo info = reelGame.findSymbolInfo(mutationName);
		if (info != null)
		{
			animatorSymbol.mutateTo(mutationName, null, true, true);
		}
		else
		{
			Debug.LogError("SymbolsWithMultipleAnimationsModule.executeOnPaylineHide() - Unable to find symbol info for mutationName = " + mutationName);
		}

		// clear the list of animated symbols since they should be handled now
		symbolsAnimated.Clear();
		
		if (animInfo.shouldPlayAnimationAfterMutate)
		{
			animatorSymbol.animateAnticipation();
		}

		yield break;
	}

	// executeOnSymbolAnimatorPlayed() section
	// Module hook for handling something when SymbolAnimator.playAnimation has been called
	// can be useful if you want to track when symbols are animated.
	public override bool needsToExecuteOnSymbolAnimatorPlayed(SlotSymbol symbol)
	{
		// we only care about the animations that need to play one after another
		return animationChangeDictionary.ContainsKey(symbol.baseName);
	}

	public override void executeOnSymbolAnimatorPlayed(SlotSymbol symbol)
	{
		symbolsAnimated.Add(symbol);
	}

	// executeSetSymbolAnimatorStopPoint() section
	// Module that hooks into SymbolAnimator.turnOffAnimators for the purpose of controlling what 
	// point the animator goes to when stopped, useful if you want the animator to stop at the beginning
	// until it has been played, and then stop at the end
	public override bool needsToSetSymbolAnimatorStopPoint(SlotSymbol symbol)
	{
		return symbolsAnimated.Contains(symbol) && animationChangeDictionary.ContainsKey(symbol.baseName);
	}

	public override float executeSetSymbolAnimatorStopPoint(SlotSymbol symbol)
	{
		AdditionalAnimInfo animInfo = getAnimInfoForSymbol(symbol);

		if (animInfo != null)
		{
			return animInfo.targetSymbolAnimatorStopPoint;
		}
		else
		{
			Debug.LogError("SymbolsWithMultipleAnimationsModule.executeSetSymbolAnimatorStopPoint() - Unable to find AdditionalAnimInfo for symbol.baseName = " + symbol.baseName 
				+ "; this shouldn't happen!  Returning 0.0f for animation stop point.");
			return 0.0f;
		}
	}

	// Function to grab AdditionalAnimInfo for a given symbol so we can find out what the
	// targetSymbolAnimatorStopPoint is set to for executeSetSymbolAnimatorStopPoint()
	private AdditionalAnimInfo getAnimInfoForSymbol(SlotSymbol symbol)
	{
		SlotSymbol animatorSymbol = symbol.getAnimatorSymbol();

		if (animatorSymbol != null)
		{
			for (int i = 0; i < animationChangeInfoList.Length; i++)
			{
				AdditionalAnimInfo currentAdditionalAnimInfo = animationChangeInfoList[i];

				if (animatorSymbol.baseName == currentAdditionalAnimInfo.targetSymbolName)
				{
					return currentAdditionalAnimInfo;
				}
			}
		}

		return null;
	}

// executeOnClearOutcomeDisplay() section
// Hook for when ReelGame.clearOutcomeDisplay is called. Note that this module
// hook is not a coroutine since it isn't really safe in all
// cases to have this cause the game to block.
// Ideally this hook should only be used for basic cleanup
// that needs to happen at the same time that the outcome display
// is cleaned.
	public override bool needsToExecuteOnClearOutcomeDisplay()
	{ 
		return true;
	}

	public override void executeOnClearOutcomeDisplay()
	{
		symbolsAnimated.Clear();
	}
}
