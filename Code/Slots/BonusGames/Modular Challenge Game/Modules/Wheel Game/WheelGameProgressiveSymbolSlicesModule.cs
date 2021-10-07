using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/**
 * Module for handling progressive symbols on wheels
 */
public class WheelGameProgressiveSymbolSlicesModule : WheelGameModule
{
	[SerializeField] private string HIGHLIGHT_ANIM_NAME = "anim";

	[Serializable]
	public class SymbolMapping
	{
		public string pickemName;
		public Animator animator;
		public AudioListController.AudioInformationList revealSounds;
		public LabelWrapperComponent label;
	}

	[SerializeField] private SymbolMapping[] wheelSymbolMappings;
	[SerializeField] protected Animator wheelWedgeCelebrationAnimator = null;
	[SerializeField] protected string WHEEL_WEDGE_CELEBRATION_ANIM_NAME = "anim";

	private List<ModularChallengeGameOutcomeEntry> fullWheelEntryList;
	private List<ModularChallengeGameOutcomeEntry> filteredProgressiveList;

	
	// Enable round init override
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}
	
	// Executes on round init & populate the wheel values
	public override void executeOnRoundInit(ModularWheelGameVariant roundParent, ModularWheel wheel)
	{
		base.executeOnRoundInit(roundParent, wheel);
		populateWheelSequence();
	}
	
	// Populates the wheel sequence from the outcomes available
	private void populateWheelSequence()
	{
		// generate an ordered outcome list from the wins & leftovers
		fullWheelEntryList = wheelRoundVariantParent.outcome.getAllWheelPaytableEntriesForRound(wheelRoundVariantParent.roundIndex);

		// filter out the bonus results only
		filteredProgressiveList = new List<ModularChallengeGameOutcomeEntry>();
		foreach (ModularChallengeGameOutcomeEntry wheelOutcome in fullWheelEntryList)
		{
			if (!string.IsNullOrEmpty(wheelOutcome.bonusGame))
			{
				filteredProgressiveList.Add(wheelOutcome);
			}
		}
	}

	// Enable spin complete action
	public override bool needsToExecuteOnSpinComplete()
	{
		// ensure we got a progressive slice
		ModularChallengeGameOutcomeEntry currentEntry = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).entries[0];
		string bonusGameName = currentEntry.bonusGame;
		return !string.IsNullOrEmpty(bonusGameName);
	}

	// On spin complete, animate the winning symbol
	public override IEnumerator executeOnSpinComplete()
	{
		// play the wedge celebration
		if (wheelWedgeCelebrationAnimator != null)
		{
			wheelWedgeCelebrationAnimator.Play(WHEEL_WEDGE_CELEBRATION_ANIM_NAME);
		}
		
		ModularChallengeGameOutcomeEntry currentEntry = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).entries[0];
		string bonusGameName = currentEntry.bonusGame;
		
		SymbolMapping symbolMapping = findSymbolMappingForBonusGame(bonusGameName, wheelSymbolMappings);
		yield return StartCoroutine(playAnimationWithAudioList(symbolMapping.animator, HIGHLIGHT_ANIM_NAME, symbolMapping.revealSounds));
	}

	public static IEnumerator playAnimationWithAudioList(Animator animator, string animName, AudioListController.AudioInformationList infos)
	{
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(animator, animName)));
		
		if (AudioListController.isAnyOfListBlocking(infos))
		{
			runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(AudioListController.playListOfAudioInformation(infos)));
		}
		else
		{
			RoutineRunner.instance.StartCoroutine(AudioListController.playListOfAudioInformation(infos));
		}
		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
	}

	// Look up one symbolMapping item from our symbolMappings based on the target game
	public static SymbolMapping findSymbolMappingForBonusGame(string targetGameName, SymbolMapping[] targetMappings)
	{
		foreach (SymbolMapping mapping in targetMappings)
		{
			if (mapping.pickemName.CompareTo(targetGameName) == 0)
			{
				return mapping;
			}
		}

		Debug.LogError("No symbolMapping found for bonus game target: " + targetGameName);
		return null;
	}
}
