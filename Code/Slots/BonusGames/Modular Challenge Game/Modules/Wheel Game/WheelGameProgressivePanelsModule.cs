using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * A module for displaying a set of progressive panels with their values.
 * Assumes that credit values will be received as the "extraData" parameter.
 */
public class WheelGameProgressivePanelsModule : WheelGameModule
{
	public enum ProgressiveCreditValueLocationEnum
	{
		ExtraData = 0,
		Credits
	}

	[SerializeField] private WheelGameProgressiveSymbolSlicesModule.SymbolMapping[] panelMappings; // store bonus game / animator link

	public string ANIMATION_INTRO_NAME = "intro";
	public string ANIMATION_OUTRO_NAME = "fade";
	public string ANIMATION_SELECTED_NAME = "moveUp"; // highlight a specific panel
	public bool shouldPlayOnEndGame = true;
	public bool shouldPlayOnAdvanceRound = true;
	public bool shouldBlockOnSelectedAnimation = true;

	public bool playIntroInSquence = false; // yield on each intro animation
	public bool playOutroInSquence = false; // yield on each outro animation

	[SerializeField] private ProgressiveCreditValueLocationEnum creditLocation = ProgressiveCreditValueLocationEnum.ExtraData;

	private List<ModularChallengeGameOutcomeEntry> fullWheelEntryList;
	private List<ModularChallengeGameOutcomeEntry> filteredProgressiveList;

	private Dictionary<string, ModularChallengeGameOutcomeEntry> progressiveLookupDictionary = new Dictionary<string, ModularChallengeGameOutcomeEntry>();

	// Enable round init override
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}
	
	// Executes on round init & populate the wheel values
	public override void executeOnRoundInit(ModularWheelGameVariant roundParent, ModularWheel wheel)
	{
		base.executeOnRoundInit(roundParent, wheel);
		
		// Format add the gamekey to panelMappings if possible, that way we can reuse a setup
		// and don't have hard coded gamekeys that need updating when cloning.
		// try to insert the game key, this is needed for games which use the same math, like wonka03/wonka06
		if (GameState.game != null)
		{
			for (int i = 0; i < panelMappings.Length; i++)
			{
				panelMappings[i].pickemName = string.Format(panelMappings[i].pickemName, GameState.game.keyName);
			}
		}
		
		populatePanelLabels();
	}

	// New version of funciton taht relies on a dicitonary of progressive info to fill in the data
	private void populatePanelLabels()
	{
		// generate an ordered outcome list from the wins & leftovers
		fullWheelEntryList = wheelRoundVariantParent.outcome.getAllWheelPaytableEntriesForRound(wheelRoundVariantParent.outcome.outcomeIndex);
			
		// filter out the bonus results only
		foreach (ModularChallengeGameOutcomeEntry wheelOutcome in fullWheelEntryList)
		{
			if (!string.IsNullOrEmpty(wheelOutcome.bonusGame))
			{
				progressiveLookupDictionary.Add(wheelOutcome.bonusGame, wheelOutcome);
			}
		}

		for (int i = 0; i < panelMappings.Length; i++)
		{
			long creditValue = 0;

			// Find the correct progressive that we need to get the data from
			ModularChallengeGameOutcomeEntry currentProgressive = progressiveLookupDictionary[panelMappings[i].pickemName];
			
			switch (creditLocation)
			{
				case ProgressiveCreditValueLocationEnum.ExtraData:
					creditValue = long.Parse(currentProgressive.wheelExtraData);
					break;
				case ProgressiveCreditValueLocationEnum.Credits:
					creditValue = currentProgressive.baseCredits;
					break;
				default:
					Debug.LogWarning("WheelGameProgressivePanelsModule.populatePanelLabels() - Unknown credit location set, please define how to get the credit value.  Using zero for credit value!");
					break;
			}
		
			panelMappings[i].label.text = CreditsEconomy.convertCredits(creditValue * GameState.baseWagerMultiplier * GameState.bonusGameMultiplierForLockedWagers);
		}
	}
		

	// Should execute on round start?
	public override bool needsToExecuteOnRoundStart()
	{
		return !string.IsNullOrEmpty(ANIMATION_INTRO_NAME);
	}

	
	// Execute on round start, animate the progressive panels in.
	public override IEnumerator executeOnRoundStart()
	{
		yield return StartCoroutine(animatePanels(ANIMATION_INTRO_NAME, playIntroInSquence));
	}


	// Should execute on round end?
	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		if (isEndOfGame)
		{
			return shouldPlayOnEndGame && (!string.IsNullOrEmpty(ANIMATION_OUTRO_NAME) || !string.IsNullOrEmpty(ANIMATION_SELECTED_NAME));
		}
		else
		{
			return shouldPlayOnAdvanceRound && (!string.IsNullOrEmpty(ANIMATION_OUTRO_NAME) || !string.IsNullOrEmpty(ANIMATION_SELECTED_NAME));
		}
	}

	// Execute on round end, fade out
	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		if (!string.IsNullOrEmpty(ANIMATION_OUTRO_NAME))	
		{
			yield return StartCoroutine(animatePanels(ANIMATION_OUTRO_NAME, playOutroInSquence, true));
		}

		if (!string.IsNullOrEmpty(ANIMATION_SELECTED_NAME))
		{
			yield return StartCoroutine(animateSelectedPanel());
		}
	}
	
	public IEnumerator playAnimationWithAudioList(string bonusGameName, string animName, bool isBlockingModule)
	{
		List<TICoroutine> runningCoroutines = new List<TICoroutine>();
		WheelGameProgressiveSymbolSlicesModule.SymbolMapping panelMapping =
			WheelGameProgressiveSymbolSlicesModule.findSymbolMappingForBonusGame(bonusGameName, panelMappings);
		if (isBlockingModule)
		{
			runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(panelMapping.animator, animName)));
		}
		else
		{
			RoutineRunner.instance.StartCoroutine(CommonAnimation.playAnimAndWait(panelMapping.animator, animName));
		}
		
		if (AudioListController.isAnyOfListBlocking(panelMapping.revealSounds))
		{
			runningCoroutines.Add(RoutineRunner.instance.StartCoroutine(AudioListController.playListOfAudioInformation(panelMapping.revealSounds)));
		}
		else
		{
			RoutineRunner.instance.StartCoroutine(AudioListController.playListOfAudioInformation(panelMapping.revealSounds));
		}
		yield return RoutineRunner.instance.StartCoroutine(Common.waitForCoroutinesToEnd(runningCoroutines));
	}

	// Animate each panel with optional pause & filtering for the "winning" element
	protected IEnumerator animatePanels(string animationName, bool yieldOnEach, bool ignoreSelected = false)
	{
		ModularChallengeGameOutcomeEntry currentEntry = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).entries[0];
		string bonusGameName = currentEntry.bonusGame;

		foreach (WheelGameProgressiveSymbolSlicesModule.SymbolMapping symbol in panelMappings)
		{
			// skip the selected symbol if fading out
			if(ignoreSelected && symbol.pickemName.CompareTo(bonusGameName) == 0)
			{
				continue;
			}
			symbol.animator.Play(animationName);
			AudioListController.playListOfAudioInformation(symbol.revealSounds);
			
			// wait for each animation
			if (yieldOnEach)
			{
				yield return StartCoroutine(CommonAnimation.waitForAnimDur(symbol.animator));
			}
		}
	}

	// Highlight the selected panel & transition to the top of the screen
	protected IEnumerator animateSelectedPanel()
	{
		ModularChallengeGameOutcomeEntry currentEntry = wheelRoundVariantParent.outcome.getRound(wheelRoundVariantParent.roundIndex).entries[0];
		string bonusGameName = currentEntry.bonusGame;

		if (string.IsNullOrEmpty(bonusGameName))
		{
			// dud, this is a credit value with no bonus
			yield break;
		}
		else
		{
			if (shouldBlockOnSelectedAnimation)
			{
				yield return StartCoroutine(playAnimationWithAudioList(bonusGameName, ANIMATION_SELECTED_NAME, playOutroInSquence));
			}
			else
			{
				StartCoroutine(playAnimationWithAudioList(bonusGameName, ANIMATION_SELECTED_NAME, playOutroInSquence));
			}
		}
	}
}
