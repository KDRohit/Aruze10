using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//The base offer game variant class
public class ModularAutoPickOfferGameVariant : ModularPickingGameVariant
{	
	[SerializeField] protected float POST_ROLLUP_DELAY = 1.5f;
	[SerializeField] protected float PRE_CHOICE_DELAY = 1.5f;
	[SerializeField] protected float PAUSE_BETWEEN_CHOICES = 1.5f;
	[SerializeField] protected float PAUSE_BEFORE_FINAL_ROUND_ACCEPT = 1.25f;	

	[Header("Selected Item Info")]
	[SerializeField] protected string SELECTED_ANIMATION_NAME = "";

	[Header("Labels")]
	[SerializeField] private LabelWrapperComponent finalWinningsLabel;
	[SerializeField] private List<LabelWrapperComponent> offerLabelList = new List<LabelWrapperComponent>();

	[Header("Offer UI")]
	[SerializeField] private AnimationListController.AnimationInformationList offerUIInAnimationList;	
	[SerializeField] private AnimationListController.AnimationInformationList offerUIOutAnimationList;
	[SerializeField] private bool shouldHideDealButtonsOnGameEnd = true;
	[SerializeField] private AnimationListController.AnimationInformation dealButtonsOutAnimation;
	[SerializeField] private AudioListController.AudioInformationList acceptDealButtonSounds;
	[SerializeField] private AudioListController.AudioInformationList declineDealButtonSounds;
	[Tooltip("Pickme animations for the accpet and/or decline buttons.  One of the animation lists will be played randomly when the pickMeAnimCallback function triggers.")]
	[SerializeField] private List<AnimationListController.AnimationInformationList> buttonPickMeAnimLists; 

	[Header("Winnings Box")]
	[SerializeField] private AnimationListController.AnimationInformationList winningsAnimationList;

	private CoroutineRepeater offerButtonPickMeController;	// Class to call the pickme animation for the offer buttons, since they are linked to the items
	private bool areOfferItemsChoosen = false; // Tracks if the items the player is presented with are choosen, used to control when the pickme for the accept/decline buttons is enabled
	private List<PickingGameOfferPickItem> roundPicks = new List<PickingGameOfferPickItem>(); // A list of the items the round chose to give an offer on
	private ModularOfferChallengeGame offerGameParent;

	// Custom init for picking game specifics
	public override void init(ModularChallengeGameOutcome outcome, int roundIndex, ModularChallengeGame parentGame)
	{
		base.init(outcome, roundIndex, parentGame);	

		//get the offer game so we have access to its properties
		offerGameParent = gameParent as ModularOfferChallengeGame;

		offerButtonPickMeController = new CoroutineRepeater(MIN_PICKME_ANIM_DELAY, MAX_PICKME_ANIM_DELAY, offerButtonPickMeAnimCallback);
	}

	//Override so the enable input on the pick items does isn't enabled.
	public override IEnumerator roundStart()
	{
		//Make sure our our current offer is 0
		BonusGamePresenter.instance.currentPayout = 0;
		
		//Turn the round on
		gameObject.SetActive(true);

		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			if (module.needsToExecuteOnShowCustomWings())
			{
				yield return StartCoroutine(module.executeOnShowCustomWings());
			}

			if (module.needsToExecuteOnRoundStart())
			{
				yield return StartCoroutine(module.executeOnRoundStart());
			}
		}

		//Start the automated picking of the items
		yield return StartCoroutine(chooseOfferItems());

		if (!offerGameParent.isFinalRound())
		{
			// Set the flag letting the game know that the offer buttons can start to animate pickme
			areOfferItemsChoosen = true;

			// Enable input
			offerGameParent.isOfferButtonEnabled = true;
		}
	}


	//We want a different flow from the picking game round end since we have no "leftovers" to reveal on round end
	// NOTE : roundEnd actually triggers after the picking objects are setup (not after an offer button is selected)
	public override IEnumerator roundEnd()
	{
		yield return new TIWaitForSeconds(DELAY_BEFORE_ADVANCE_ROUND);

		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			if (module.needsToExecuteOnRoundEnd(!offerGameParent.willAdvanceRoundEndGame()))
			{
				yield return StartCoroutine(module.executeOnRoundEnd(!offerGameParent.willAdvanceRoundEndGame()));
			}
		}
		//set the offer label
		for (int i = 0; i < offerLabelList.Count; i++)
		{
			LabelWrapperComponent currentLabel = offerLabelList[i];
			if (currentLabel != null)
			{
				currentLabel.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
			}
		}

		//Show Offer Label and Deal Buttons
		if (offerUIInAnimationList != null && offerUIInAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(offerUIInAnimationList));
		}

		// Automatically accept the offer if this is the final round
		if (offerGameParent.isFinalRound())
		{
			yield return new TIWaitForSeconds(PAUSE_BEFORE_FINAL_ROUND_ACCEPT);
			StartCoroutine(acceptOffer());
		}
	}
	
	protected override IEnumerator itemClicked(PickingGameBasePickItem pickItem, ModularChallengeGameOutcomeEntry pickData)
	{
		//Update the list of selected items
		PickingGameOfferPickItem indexItem = pickItem.gameObject.GetComponent<PickingGameOfferPickItem>();
		if (indexItem != null)
		{
			roundPicks.Add(indexItem);			
		}
		else
		{
			Debug.LogWarning("No PickingGamePickItemIndex component found on: " + pickItem.gameObject.name);
		}	
		return base.itemClicked(pickItem, pickData);
	}

	//Pick an item that isnt revealed, and tell it it has been clicked
	private IEnumerator chooseOfferItems()
	{
		//Slight pause as to give the feel of thinking about this picks
		yield return new TIWaitForSeconds(PRE_CHOICE_DELAY);

		ModularChallengeGameOutcomeRound currentRound = getCurrentRoundOutcome();
		for (int i = 0; i < currentRound.entries.Count; i++)
		{			
			PickingGameBasePickItem pickItem = null;
			ModularChallengeGameOutcomeEntry pickData = getCurrentPickOutcome();			
			foreach (PickingGameOfferPickItem offerPick in getOfferPickItemsList())
			{
				if (offerPick.offer == pickData.corePickData.baseCredits)
				{
					//The item we want to select has been found
					pickItem = offerPick.gameObject.GetComponent<PickingGameBasePickItem>();
					continue;
				}
			}

			if(pickItem != null)
			{
				yield return StartCoroutine(itemClicked(pickItem, pickData));
				yield return new TIWaitForSeconds(PAUSE_BETWEEN_CHOICES);
			}
			else
			{
				Debug.LogError("No pick with base credit value of: " + pickData.corePickData.baseCredits + " could be found, double check this outcome with the server team.");
			}			
		}
	}

	public IEnumerator declineOffer()
	{
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(declineDealButtonSounds));

		//execute modules
		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			ModularPickingOfferGameModule offerGameModule = module as ModularPickingOfferGameModule;
			if (offerGameModule != null)
			{
				if (offerGameModule.needsToExecuteOnOfferDeclined())
				{
					yield return StartCoroutine(offerGameModule.executeOnOfferDeclined());
				}
			}
		}

		//Hide Offer Label and Deal Buttons	
		if (offerUIOutAnimationList != null && offerUIOutAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(offerUIOutAnimationList));
		}

		//Play the declined state for the picks
		for (int i = 0; i < roundPicks.Count; i++)
		{
			yield return StartCoroutine(AnimationListController.playAnimationInformation(roundPicks[i].declinedState));
		}


		gameParent.advanceRound();
	}

	public IEnumerator acceptOffer()
	{
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(acceptDealButtonSounds));

		//send the server our choice
		SlotAction.acceptBonusGameCredits(offerGameParent.getEventID(), GameState.game.keyName, (roundIndex + 1));

		//execute modules
		foreach (ChallengeGameModule module in cachedAttachedModules)
		{
			ModularPickingOfferGameModule offerGameModule = module as ModularPickingOfferGameModule;
			if (offerGameModule != null)
			{
				if (offerGameModule.needsToExecuteOnOfferAccepted())
				{
					yield return StartCoroutine(offerGameModule.executeOnOfferAccepted());
				}
			}
		}

		//We may want to hide offer buttons but not the offer banners 
		if (shouldHideDealButtonsOnGameEnd && dealButtonsOutAnimation != null)
		{
			yield return StartCoroutine(AnimationListController.playAnimationInformation(dealButtonsOutAnimation));
		}

		//if we have an accepted state for the picks play it.  Example: Pawn01 the items say "sold" 
		for (int i = 0; i < roundPicks.Count; i++)
		{
			yield return StartCoroutine(AnimationListController.playAnimationInformation(roundPicks[i].acceptedState));
		}

		//If we have any animations to on an accepted offer do that now
		if (winningsAnimationList != null && winningsAnimationList.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(winningsAnimationList));
		}

		//update the win label
		yield return StartCoroutine(rollUpFinalWinnings(0, BonusGamePresenter.instance.currentPayout));
		
		//Pop the summary after the winnings label is updated
		offerGameParent.offerFinalized();
	}

	protected IEnumerator rollUpFinalWinnings(long startScore, long endScore)
	{
		if (finalWinningsLabel != null)
		{
			yield return StartCoroutine(
				SlotUtils.rollup(
					startScore,
					endScore,
					finalWinningsLabel,
					rollupOverrideSound: Audio.tryConvertSoundKeyToMappedValue(ROLLUP_SOUND_LOOP_OVERRIDE),
					rollupTermOverrideSound: Audio.tryConvertSoundKeyToMappedValue(ROLLUP_SOUND_END_OVERRIDE)
				)
			);

			yield return new TIWaitForSeconds(POST_ROLLUP_DELAY);
		}
	}

	public List<PickingGameOfferPickItem> getOfferPickItemsList()
	{
		List<PickingGameOfferPickItem> offerItemList = new List<PickingGameOfferPickItem>();
		foreach (PickingGameBasePickItem pickItem in itemList)
		{
			PickingGameOfferPickItem offerItem = pickItem.gameObject.GetComponent<PickingGameOfferPickItem>();
			if (offerItem!=null)
			{
				offerItemList.Add(offerItem);
			}
		}

		return offerItemList;
	}

	private IEnumerator offerButtonPickMeAnimCallback()
	{
		if (offerGameParent.isOfferButtonEnabled && areOfferItemsChoosen && buttonPickMeAnimLists != null && buttonPickMeAnimLists.Count > 0)
		{
			// play a random button's anims/sounds, since this game has objects as well as buttons
			int randomAnimListIndex = Random.Range(0, buttonPickMeAnimLists.Count);
			AnimationListController.AnimationInformationList randomAnimList = buttonPickMeAnimLists[randomAnimListIndex];
			if (randomAnimList != null && randomAnimList.Count > 0)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(randomAnimList));
			}
		}
	}

	// Handle pick me animations
	protected override void Update()
	{
		// Play the pickme animation.
		base.Update();

		// Play the pickme animation.
		if (offerGameParent.isOfferButtonEnabled && areOfferItemsChoosen && didInit)
		{
			offerButtonPickMeController.update();
		}
	}
}
