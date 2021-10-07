using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This class works with the multi_personal_jackpot reevaluation/modifier_export.
// JackpotEvents from the reevaluation can increase, reset, or win a jackpot from
// landing a trigger symbol on the reels. Jackppot containers contain label and
// animation data to respond to each event.
//
// Author : Nick Saito nsaito@zynga.com
// Date : Dec 1st 2020
// Games : orig010
//
public class PersonalJackpotFromSymbolModule : SlotModule
{
#region serialized member variables

	[Tooltip("Animation data for each jackpot")]
	[SerializeField] private List<JackpotContainer> jackpotContainers;

#endregion

#region private member variables

	// winning jackpot surpresses bigwin
	private bool didWinJackpot;

	// reevaluation that contains trigger symbols and jackpot events
	private ReevaluationMultiPersonalJackpot multiPersonalJackpot;

	// the reevaluation and modifier export key
	private const string MULTI_PERSONAL_JACKPOT_TYPE = "mystery_progressive_jackpot_evaluation";

	// Dictionary of the jackpotContainers initialized at the start for quick lookups
	private Dictionary<string, JackpotContainer> jackpotContainerMap;

#endregion

#region slot module overrides

	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return true;
	}

	// initialize the jackpots from modifier exports data that has jackpot key and wager multiplier.
	// For freesping we copy the values of from the same basegame module.
	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		initJackpotContainerMap();

		if (reelGame.isFreeSpinGame())
		{
			if (SlotBaseGame.instance != null)
			{
				copyValuesFromBasegame();
			}
		}
		else if (reelGame.modifierExports != null)
		{
			initJackpotsFromModiferExport();
		}
	}

	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		didWinJackpot = false;
		yield break;
	}

	// check if there is a MultiPersonalJackpot Reevaluation
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		multiPersonalJackpot = getMultiPersonalJackpotReevaluation();
		return multiPersonalJackpot != null;
	}

	// Collect SC symbols in the proper order and handle the jackpot events
	public override IEnumerator executeOnReelsStoppedCallback()
	{
		List<ReevaluationMultiPersonalJackpot.JackpotEvent> jackpotEvents = multiPersonalJackpot.getJackpotEventsOrderedByFirstReelIncreaseWinResetPosition();

		foreach (ReevaluationMultiPersonalJackpot.JackpotEvent jackpotEvent in jackpotEvents)
		{
			yield return StartCoroutine(handleJackpotEvent(jackpotEvent));
		}
	}

	// prevent bigwin if a jackpot was won since it has it's own presentation
	public override bool isModuleHandlingBigWin()
	{
		return didWinJackpot;
	}

	public override bool needsToExecuteOnWagerChange(long currentWager)
	{
		return true;
	}

	// scale jackpots to relative to the wager
	public override void executeOnWagerChange(long currentWager)
	{
		foreach (JackpotContainer jackpotContainer in jackpotContainers)
		{
			jackpotContainer.updateJackpotMultiplier(reelGame.multiplier);
		}
	}

	public override bool needsToExecuteOnBonusGamePresenterFinalCleanup()
	{
		return reelGame.isFreeSpinGame();
	}

	// update jackpots with new values from freespins back to the basegame
	public override IEnumerator executeOnBonusGamePresenterFinalCleanup()
	{
		copyValuesToBasegame();
		yield break;
	}

#endregion

#region private methods

	private void initJackpotContainerMap()
	{
		jackpotContainerMap = new Dictionary<string, JackpotContainer>();

		foreach (JackpotContainer jackpotContainer in jackpotContainers)
		{
			jackpotContainerMap[jackpotContainer.formattedJackpotKey] = jackpotContainer;
		}
	}

	// initialize the jackpots from modifier_export data setting the jackpot multiplier and
	// the personal jackpot data.
	private void initJackpotsFromModiferExport()
	{
		PersonalJackpotModifierExport personalJackpotModifierExport = getPersonalJackpotModifierExport();

		foreach (PersonalJackpotData personalJackpotData in personalJackpotModifierExport.personalJackpotDatas)
		{
			JackpotContainer jackpotContainer = getJackpotContainer(personalJackpotData.jackpotKey);

			if (jackpotContainer != null)
			{
				jackpotContainer.init(personalJackpotData, reelGame.multiplier);
			}
		}
	}

	// Personal Jackpot data is buried in modifier_exports with a key that varies by game, but we can find it
	// using the type within the JSON.
	private PersonalJackpotModifierExport getPersonalJackpotModifierExport()
	{
		foreach (JSON modifierExportJSON in reelGame.modifierExports)
		{
			if (modifierExportJSON.getString("type", "") == MULTI_PERSONAL_JACKPOT_TYPE)
			{
				return new PersonalJackpotModifierExport(modifierExportJSON);
			}
		}

		return null;
	}

	// Get the jackpot data from the basegame module and apply the same values in freespins
	private void copyValuesFromBasegame()
	{
		PersonalJackpotFromSymbolModule baseGameModule = getBaseGameModule();
		if (baseGameModule == null)
		{
			return;
		}

		foreach (JackpotContainer baseGameJackpotContainer in baseGameModule.jackpotContainers)
		{
			JackpotContainer jackpotContainer = getJackpotContainer(baseGameJackpotContainer.formattedJackpotKey);
			jackpotContainer.init(baseGameJackpotContainer.personalJackpotData, reelGame.multiplier);
		}
	}

	// copy the updated jackpot values from all the jackpot events back in to the basegame module
	private void copyValuesToBasegame()
	{
		PersonalJackpotFromSymbolModule baseGameModule = getBaseGameModule();
		if (baseGameModule == null)
		{
			return;
		}

		foreach (JackpotContainer baseGameJackpotContainer in baseGameModule.jackpotContainers)
		{
			JackpotContainer jackpotContainer = getJackpotContainer(baseGameJackpotContainer.formattedJackpotKey);
			baseGameJackpotContainer.init(jackpotContainer.personalJackpotData, reelGame.multiplier);
		}
	}

	// Helper function to get the same module attached to the base game
	private PersonalJackpotFromSymbolModule getBaseGameModule()
	{
		if (SlotBaseGame.instance != null)
		{
			for (int i = 0; i < SlotBaseGame.instance.cachedAttachedSlotModules.Count; i++)
			{
				PersonalJackpotFromSymbolModule module = SlotBaseGame.instance.cachedAttachedSlotModules[i] as PersonalJackpotFromSymbolModule;
				if (module != null)
				{
					return module;
				}
			}
		}

		return null;
	}

	// Looks through reevaluations to get the multi_personal_jackpot reeval
	private ReevaluationMultiPersonalJackpot getMultiPersonalJackpotReevaluation()
	{
		JSON[] arrayReevaluations = reelGame.outcome.getArrayReevaluations();

		for (int i = 0; i < arrayReevaluations.Length; i++)
		{
			string reevalType = arrayReevaluations[i].getString("type", "");

			if (reevalType == MULTI_PERSONAL_JACKPOT_TYPE)
			{
				return new ReevaluationMultiPersonalJackpot(arrayReevaluations[i]);
			}
		}

		return null;
	}

	private JackpotContainer getJackpotContainer(string jackpotKey)
	{
		JackpotContainer jackpotContainer;
		jackpotContainerMap.TryGetValue(jackpotKey, out jackpotContainer);
		return jackpotContainer;
	}

	// handles a jackpot event
	private IEnumerator handleJackpotEvent(ReevaluationMultiPersonalJackpot.JackpotEvent jackpotEvent)
	{
		JackpotContainer jackpotContainer = getJackpotContainer(jackpotEvent.jackpotKey);
		JackpotEventAnimation jackpotEventAnimation = jackpotContainer.getJackpotEventAnimation(jackpotEvent.eventType);

		switch (jackpotEvent.eventType)
		{
			case ReevaluationMultiPersonalJackpot.JackpotEvent.EVENT_TYPE.INCREASE:
				yield return StartCoroutine(increaseJackpot(jackpotEvent, jackpotEventAnimation, jackpotContainer));
				break;
			case ReevaluationMultiPersonalJackpot.JackpotEvent.EVENT_TYPE.WIN:
				yield return StartCoroutine(winJackpot(jackpotEvent, jackpotEventAnimation, jackpotContainer));
				break;
			case ReevaluationMultiPersonalJackpot.JackpotEvent.EVENT_TYPE.RESET:
				yield return StartCoroutine(resetJackpot(jackpotEvent, jackpotEventAnimation, jackpotContainer));
				break;
		}
	}

	// gets the jackpot and trigger symbols affected by this jackpot event and plays the animations and
	// increase the value of the jackpot.
	private IEnumerator increaseJackpot(ReevaluationMultiPersonalJackpot.JackpotEvent jackpotEvent, JackpotEventAnimation jackpotEventAnimation, JackpotContainer jackpotContainer)
	{
		yield return StartCoroutine(startJackpotAnimations(jackpotEventAnimation, jackpotEvent));
		jackpotContainer.increaseJackpot(jackpotEvent.addCredits);
		yield return StartCoroutine(endJackpotAnimations(jackpotEventAnimation));
	}

	// gets the jackpot and trigger symbols affected by this jackpot event and plays the animations and
	// resets the jackpot back to its base value with no added contribution.
	private IEnumerator resetJackpot(ReevaluationMultiPersonalJackpot.JackpotEvent jackpotEvent, JackpotEventAnimation jackpotEventAnimation, JackpotContainer jackpotContainer)
	{
		yield return StartCoroutine(startJackpotAnimations(jackpotEventAnimation, jackpotEvent));
		jackpotContainer.resetJackpot();
		yield return StartCoroutine(endJackpotAnimations(jackpotEventAnimation));
	}

	// gets the jackpot and trigger symbols affected by this jackpot event and plays the animations and
	// awards the player the value of the jackpot rolling up to the win meter. After the jackpot is won,
	// it resets to its base value.
	private IEnumerator winJackpot(ReevaluationMultiPersonalJackpot.JackpotEvent jackpotEvent, JackpotEventAnimation jackpotEventAnimation, JackpotContainer jackpotContainer)
	{
		didWinJackpot = true;
		addJackpotToPlayerPayout(jackpotContainer);
		yield return StartCoroutine(startJackpotAnimations(jackpotEventAnimation, jackpotEvent));
		yield return StartCoroutine(reelGame.rollupCredits(0, jackpotContainer.jackpotValue, reelGame.onPayoutRollup, true));
		yield return StartCoroutine(endJackpotAnimations(jackpotEventAnimation));
		jackpotContainer.resetJackpot();
	}

	// adds the jackpot value to the players coin total
	private void addJackpotToPlayerPayout(JackpotContainer jackpotContainer)
	{
		if (SlotBaseGame.instance != null && !reelGame.isFreeSpinGame())
		{
			// in the base game we need to add the credits to the player.
			reelGame.addCreditsToSlotsPlayer(jackpotContainer.jackpotValue, "award jackpot", shouldPlayCreditsRollupSound: false);
		}
		else if (reelGame.isFreeSpinGame())
		{
			// in the freespins we rollup, but credits are added to the player at the end of the bonus game.
			BonusGamePresenter.instance.currentPayout += jackpotContainer.jackpotValue;
		}
	}

	private IEnumerator startJackpotAnimations(JackpotEventAnimation jackpotEventAnimation, ReevaluationMultiPersonalJackpot.JackpotEvent jackpotEvent)
	{
		if (jackpotEventAnimation.triggerSymbolAnimatedParticleEffect != null)
		{
			List<TICoroutine> particleEffectCoroutines = new List<TICoroutine>();
			foreach (ReevaluationMultiPersonalJackpot.TriggerSymbol triggerSymbol in jackpotEvent.triggerSymbols)
			{
				SlotSymbol slotSymbol = reelGame.engine.getSlotReelAt(triggerSymbol.reelIndex).visibleSymbolsBottomUp[triggerSymbol.pos];
				slotSymbol.animateOutcome();
				particleEffectCoroutines.Add(StartCoroutine(jackpotEventAnimation.triggerSymbolAnimatedParticleEffect.animateParticleEffect(slotSymbol.transform)));

				if (jackpotEventAnimation.triggerSymbolDelayBetweenCollect > 0.0f)
				{
					yield return new WaitForSeconds(jackpotEventAnimation.triggerSymbolDelayBetweenCollect);
				}
			}

			yield return StartCoroutine(Common.waitForCoroutinesToEnd(particleEffectCoroutines));
		}

		if (jackpotEventAnimation.introAnimation != null && jackpotEventAnimation.introAnimation.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotEventAnimation.introAnimation));
		}
	}

	private IEnumerator endJackpotAnimations(JackpotEventAnimation jackpotEventAnimation)
	{
		if (jackpotEventAnimation.outroAnimation != null && jackpotEventAnimation.outroAnimation.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(jackpotEventAnimation.outroAnimation));
		}
	}

	#endregion

#region data classes

	[System.Serializable]
	public class JackpotContainer
	{
		[Tooltip("Jackpot key should match keys sent down by the server. Name can be formatted with gameKey if server requires it. Example : {0}_mini")]
		[SerializeField] private string jackpotKey;

		[Tooltip("Label to display the value of the jackpot to the player.")]
		[SerializeField] private LabelWrapperComponent jackpotLabel;

		[Tooltip("Label that contains the amount the jackpot value increased")]
		[SerializeField] private LabelWrapperComponent jackpotIncreaseLabel;

		[Tooltip("Format string for increase label ex: +{0}")]
		[SerializeField] protected string jackpotIncreaseLabelFormat;

		[Tooltip("Animations to play for jackpot events like increase, win, and reset.")]
		public List<JackpotEventAnimation> jackpotEventAnimations;

		// map to each events animation data for easy lookups
		public Dictionary<ReevaluationMultiPersonalJackpot.JackpotEvent.EVENT_TYPE, JackpotEventAnimation> jackpotAnimationMap = new Dictionary<ReevaluationMultiPersonalJackpot.JackpotEvent.EVENT_TYPE, JackpotEventAnimation>();

		// jackpot data contains raw_contribution and base amount used
		// when the wager changes we will need the total to add to the
		// scaled wager increase
		public PersonalJackpotData personalJackpotData;

		// multiplier for the jackpot's baseAmount
		private long jackpotMultiplier;

		// sometimes jackpots will have the gamekey prefixed. This allows us to use a formatted string
		// in the name to make clones easier. Name should formatted something like {0}_mini.
		public string formattedJackpotKey
		{
			get
			{
				if (string.IsNullOrEmpty(_formattedJackpotKey))
				{
					_formattedJackpotKey = string.Format(jackpotKey, GameState.game.keyName);
				}

				return _formattedJackpotKey;
			}
		}

		private string _formattedJackpotKey;

		public long jackpotValue
		{
			get
			{
				return _jackpotValue;
			}
		}

		private long _jackpotValue;

		// Init the jackpot with jackpot data and the wager set multiplier
		public void init(PersonalJackpotData newPersonalJackpotData, long multiplier)
		{
			personalJackpotData = newPersonalJackpotData;
			jackpotMultiplier = multiplier;
			updateJackpotValue();
			updateJackpotLabel();

			// create an easy lookup map for jackpotAnimataData
			foreach (JackpotEventAnimation jackpotEventAnimation in jackpotEventAnimations)
			{
				jackpotAnimationMap[jackpotEventAnimation.eventType] = jackpotEventAnimation;
			}
		}

		public void increaseJackpot(long increaseAmount)
		{
			personalJackpotData.rawContribution += increaseAmount;
			updateJackpotValue();
			updateJackpotLabel();
			updateJackpotIncreaseLabel(increaseAmount);
		}

		public void updateJackpotMultiplier(long newJackpotMultiplier)
		{
			jackpotMultiplier = newJackpotMultiplier;
			updateJackpotValue();
			updateJackpotLabel();
		}

		public void resetJackpot()
		{
			personalJackpotData.rawContribution = 0;
			updateJackpotValue();
			updateJackpotLabel();
		}

		private void updateJackpotLabel()
		{
			jackpotLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(_jackpotValue, shouldRoundUp: false);
		}

		private void updateJackpotIncreaseLabel(long increaseAmount)
		{
			if (jackpotIncreaseLabel != null)
			{
				if (!string.IsNullOrEmpty(jackpotIncreaseLabelFormat))
				{
					jackpotIncreaseLabel.text = string.Format(jackpotIncreaseLabelFormat, CreditsEconomy.multiplyAndFormatNumberAbbreviated(increaseAmount, shouldRoundUp: false));
				}
				else
				{
					jackpotIncreaseLabel.text = CreditsEconomy.multiplyAndFormatNumberAbbreviated(increaseAmount, shouldRoundUp: false);
				}
			}
		}

		private void updateJackpotValue()
		{
			if (personalJackpotData != null)
			{
				_jackpotValue = personalJackpotData.baseAmount * jackpotMultiplier + personalJackpotData.rawContribution;
			}
		}

		public JackpotEventAnimation getJackpotEventAnimation(ReevaluationMultiPersonalJackpot.JackpotEvent.EVENT_TYPE eventType)
		{
			JackpotEventAnimation jackpotEventAnimation;
			jackpotAnimationMap.TryGetValue(eventType, out jackpotEventAnimation);
			return jackpotEventAnimation;
		}
	}

	[System.Serializable]
	public class JackpotEventAnimation
	{
		[Tooltip("The type of jackpot event win/increase/reset")]
		public ReevaluationMultiPersonalJackpot.JackpotEvent.EVENT_TYPE eventType;

		[Tooltip("Animations to play at the start of the jackpot event")]
		public AnimationListController.AnimationInformationList introAnimation;

		[Tooltip("Animations to play at after jackpot event has completed")]
		public AnimationListController.AnimationInformationList outroAnimation;

		[Tooltip("Particle effect that plays from the symbols that triggered the jackpot event")]
		public AnimatedParticleEffect triggerSymbolAnimatedParticleEffect;

		[Tooltip("Adds a delay between each particle effect")]
		public float triggerSymbolDelayBetweenCollect;
	}

	public class PersonalJackpotModifierExport
	{
		public List<PersonalJackpotData> personalJackpotDatas;

		public PersonalJackpotModifierExport(JSON modifierExportJSON)
		{
			personalJackpotDatas = new List<PersonalJackpotData>();
			JSON[] personalJackpotsJSON = modifierExportJSON.getJsonArray("jackpots");

			foreach (JSON personalJackpotJSON in personalJackpotsJSON)
			{
				PersonalJackpotData personalJackpotData = new PersonalJackpotData(personalJackpotJSON);
				personalJackpotDatas.Add(personalJackpotData);
			}
		}
	}

	public class PersonalJackpotData
	{
		public string jackpotKey;
		public long baseAmount;
		public long rawContribution;

		public PersonalJackpotData(JSON modifierExportJSON)
		{
			jackpotKey = modifierExportJSON.getString("jackpot_key", "");
			baseAmount = modifierExportJSON.getLong("base_amount", 0);
			rawContribution = modifierExportJSON.getLong("raw_contribution", 0);
		}
	}

#endregion
}
