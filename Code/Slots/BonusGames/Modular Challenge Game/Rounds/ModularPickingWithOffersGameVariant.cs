using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Class to handle a picking game round where the player is offered a choice to collect or continue after
 * each selection they make (unless the selection is a special one, for instance some may just add additional picks
 * but not provide the chance to collect/continue since the value being offered isn't changing).  This code was originally
 * written to be used with tpir01 (The Price is Right) Cliffhanger bonus.
 *
 * Creation Date: 1/21/2021
 * Original Author: Scott Lepthien
 */
public abstract class ModularPickingWithOffersGameVariant : ModularPickingGameVariant
{
	private bool isOfferUiInputEnabled = false; // Controls when the offer UI will accept input (this will ensure that the player can't click buttons until they are ready, and can only click a button once)
	[SerializeField] private AnimationListController.AnimationInformationList showOfferButtonsAnims;
	[SerializeField] private AnimationListController.AnimationInformationList hideOfferButtonsAnims;
	[Tooltip("Animations played when the collect button is pressed saying the player wants to collect the current win amount")]
	[SerializeField] private AnimationListController.AnimationInformationList collectOfferButtonPressedAnims;
	[Tooltip("Animations played when the decline button is pressed saying the player wants to continue revealing picks")]
	[SerializeField] private AnimationListController.AnimationInformationList declineOfferButtonPressedAnims;
	[SerializeField] private AnimationListController.AnimationInformationList[] offerButtonsPickMeAnimationListArray;
	
	private string eventID; // The EventID that we will send with actions to the server based on which button the player is clicking
	
	// Custom init for picking game specifics
	public override void init(ModularChallengeGameOutcome outcome, int roundIndex, ModularChallengeGame parentGame)
	{
		base.init(outcome, roundIndex, parentGame);

		eventID = outcome.getEventID();
	}

	// Sends the complete action to the server, telling it what choiceIndex the player is wishing to end on
	protected void sendOfferCompleteActionToServer(int choiceIndex)
	{
		SlotAction.progressBonusGameAccumulative(eventID, GameState.game.keyName, SlotAction.ProgressBonusGameAccumulativeChoiceEnum.COMPLETE, choiceIndex);
	}
	
	// Send the progress action to the server, telling it that the player wants to keep going
	protected void sendOfferProgressActionToServer(int choiceIndex)
	{
		SlotAction.progressBonusGameAccumulative(eventID, GameState.game.keyName, SlotAction.ProgressBonusGameAccumulativeChoiceEnum.PROGRESS, choiceIndex);
	}
	
	// Shows the offer UI and enables the buttons to click after it is shown
	public IEnumerator showOfferButtons()
	{
		if (showOfferButtonsAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(showOfferButtonsAnims));
		}

		isOfferUiInputEnabled = true;
	}
	
	// Handle pick me animations
	// Need to override this so that it triggers whether standard input or offer input is enabled
	protected override void Update()
	{
		// Play the pickme animation.
		if (isDoingPickMes && (inputEnabled || isOfferUiInputEnabled) && didInit)
		{
			pickMeController.update();
		}
	}

	protected override IEnumerator pickMeAnimCallback()
	{
		if (isOfferUiInputEnabled)
		{
			if (offerButtonsPickMeAnimationListArray.Length > 0)
			{
				// Handle pick me animations for the offer UI
				// @todo : For now just going to use the same repeater for both of these pickme animations.
				// It is possible we might want to split them up though if we wanted the delay between repeating to be different
				// between the picks and the offer buttons.  This way will be simpler to setup though, with less additional fields.
				int randomItemIndex = Random.Range(0, offerButtonsPickMeAnimationListArray.Length);
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(offerButtonsPickMeAnimationListArray[randomItemIndex]));
			}
		}
		else
		{
			// Do the standard pick me stuff
			yield return StartCoroutine(base.pickMeAnimCallback());
		}
	}

	// Hide the offer buttons and determine if the pick objects should be enabled or the game should end
	private IEnumerator hideOfferButtons(bool isEnablingPickItemInput)
	{
		// Make sure this is false (although it should already be set false right after an offer button is pressed)
		isOfferUiInputEnabled = false;
		
		if (hideOfferButtonsAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(hideOfferButtonsAnims));
		}
		
		if (isEnablingPickItemInput)
		{
			inputEnabled = true;
		}
	}

	// Function that allows the player choice to be skipped.  For now all this does is enable input
	// on the pick objects again (which is only re-enabled after the choice flow completes)
	public void skipPlayerChoice()
	{
		inputEnabled = true;
	}
	
	//The collect offer button
	public void collectOfferPressed()
	{
		if (isOfferUiInputEnabled)
		{
			isOfferUiInputEnabled = false;
			StartCoroutine(collectOfferCoroutine());
		}
	}

	protected IEnumerator collectOfferCoroutine()
	{
		// Need to send the message to the server that the offer is being collected
		// NOTE: We can use the pickIndex here even though it will have been incremented from the last reveal because the
		// index expected by the server is 1 based
		sendOfferCompleteActionToServer(pickIndex);
		
		// Play the pressed animation
		if (collectOfferButtonPressedAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(collectOfferButtonPressedAnims));
		}

		// if the round isn't over we need to hide the buttons which are showing.  If the round is over
		// that means the offer was auto accepted, so we need to skip these animations
		if (!isRoundOver())
		{
			// Hide the offer buttons so that they know that their selection was made
			yield return StartCoroutine(hideOfferButtons(false));
		}

		yield return StartCoroutine(awardOfferToPlayer());
		
		// Need to transfer any remaining picks to leftovers (since we want to make sure
		// that we show the player everything they could have gotten)
		transferPicksToLeftovers(false);
		
		// If the round isn't over we need to terminate the game here
		if (!isRoundOver())
		{
			yield return StartCoroutine(roundEnd());
		}
	}

	// Derived class should implement this (since the actual workings of the specific game implementing this class
	// and what animations it wants to play, will be dictated by how that game works).
	protected abstract IEnumerator awardOfferToPlayer();

	//The decline offer button
	public void declineOfferPressed()
	{
		if (isOfferUiInputEnabled)
		{
			isOfferUiInputEnabled = false;
			StartCoroutine(declineOfferCoroutine());
		}
	}

	private IEnumerator declineOfferCoroutine()
	{
		// Send message to server saying that the offer was declined and the player is continuing
		sendOfferProgressActionToServer(pickIndex);
		
		// Play the pressed animation
		if (declineOfferButtonPressedAnims.Count > 0)
		{
			yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(declineOfferButtonPressedAnims));
		}
		
		// Hide the offer buttons and enable the pick objects again, the player wants to keep picking
		yield return StartCoroutine(hideOfferButtons(true));
	}
}
