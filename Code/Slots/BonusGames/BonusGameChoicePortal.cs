using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class handles deciding which prefab to use for the free spin game and sending out the appropriate action.
public class BonusGameChoicePortal : ChallengeGame
{
	public GameObject[] gameIcons;
	public GameObject[] revealObjects;
	public GameObject[] pickMeObjects;
	public UILabel[] gameDescriptions;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] gameDescriptionsWrapperComponent;
	
	private bool hasGottenBonusResponse = false; // Tells if the server response for the bonus game choice has been received yet (used for timeout)
	private string bonusGameChoiceTransactionName = "";
	
	private const float SELECTION_SENT_TIMEOUT = 20.0f;

	public List<LabelWrapper> gameDescriptionsWrapper
	{
		get
		{
			if (_gameDescriptionsWrapper == null)
			{
				_gameDescriptionsWrapper = new List<LabelWrapper>();

				if (gameDescriptionsWrapperComponent != null && gameDescriptionsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in gameDescriptionsWrapperComponent)
					{
						_gameDescriptionsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in gameDescriptions)
					{
						_gameDescriptionsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _gameDescriptionsWrapper;
		}
	}
	private List<LabelWrapper> _gameDescriptionsWrapper = null;	
	

	public GameObject transitionObj;

	public UIPanel gamePanel;

	protected CoroutineRepeater pickMeController;

	protected bool selectionComplete = false;
	protected string[] listOfPosibleGames = new string[3];

	protected float MIN_TIME_PICKME = 1.0f;								// Minimum time pickme animation might take to play next
	protected float MAX_TIME_PICKME = 5.0f;								// Maximum time pickme animation might take to play next
	protected float PICK_ME_TIMING = 2.0f;
	protected float TIME_BETWEEN_PICK_ME = 1.0f;
	protected float FLY_TIME = 1.5f;
	protected float FLY_TWEEN_TIME = 2.0f;

	protected string PORTAL_BONUS_LOOP = "PortalBgLivingLarge";
	protected string PORTAL_REVEAL = "RevealBonusLivingLarge";
	protected string PORTAL_CHOICE_1_REVEAL = "RevealRivieraVO";
	protected string PORTAL_CHOICE_2_REVEAL = "RevealChaletVO";
	protected string PORTAL_CHOICE_3_REVEAL = "RevealIslandVO";
	protected string TRANSITION_SFX = "TransitionLearJetFlyby";

	private string portalSummaryID = "";

	public override void init()
	{
		// Populates our data and loc. depending on the game choices from the server.
		foreach (PayTable.BonusGameChoice choice in BonusGameManager.instance.possibleBonusGameChoices.gameChoices)
		{
			if (choice.keyName.Contains("5_spin"))
			{
				listOfPosibleGames[0] = choice.keyName;

				if (gameDescriptionsWrapper != null && gameDescriptionsWrapper[0] != null)
				{
					gameDescriptionsWrapper[0].text = Localize.text("{0}_free_spins_with_{1}_wilds", "5", choice.extraInfo);
				}
			}
			else if (choice.keyName.Contains("7_spin"))
			{
				listOfPosibleGames[1] = choice.keyName;

				if (gameDescriptionsWrapper != null && gameDescriptionsWrapper[1] != null)
				{
					gameDescriptionsWrapper[1].text = Localize.text("{0}_free_spins_with_{1}_wilds", "7", choice.extraInfo);
				}
			}
			else
			{
				listOfPosibleGames[2] = choice.keyName;

				if (gameDescriptionsWrapper != null && gameDescriptionsWrapper[2] != null)
				{
					gameDescriptionsWrapper[2].text = Localize.text("{0}_free_spins_with_{1}_wilds", "9", choice.extraInfo);
				}
			}
		}
		
		//Audio.playMusic(PORTAL_BONUS_LOOP);
		Audio.switchMusicKey(PORTAL_BONUS_LOOP);
		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);
		_didInit = true;
	}

	// Shows the pick me animations, and endlessly cycles it.
	protected virtual IEnumerator pickMeCallback()
	{
		if (!selectionComplete)
		{
			int pickMeIndex = Random.Range(0, pickMeObjects.Length);
			pickMeObjects[pickMeIndex].SetActive(true);
			yield return new TIWaitForSeconds(PICK_ME_TIMING);

			if (pickMeObjects[pickMeIndex] != null)
			{
				pickMeObjects[pickMeIndex].SetActive(false);
			}
			yield return new TIWaitForSeconds(TIME_BETWEEN_PICK_ME);
		}
	}

	protected override void Update()
	{
		base.Update();
		// We only want to be able to play the pickme animations if we actually pick stuff.
		if (!selectionComplete && pickMeController != null)
		{
			pickMeController.update();
		}
	}

	// Calback to selecting an icon.
	public void gameSelected(GameObject obj)
	{
		if (selectionComplete)
		{
			return;
		}
		selectionComplete = true;
		StartCoroutine(showReveal(System.Array.IndexOf(gameIcons, obj)));
	}

	protected virtual IEnumerator showReveal(int indexArray)
	{
		// Show the glow behind the icon once we've selected it.
		revealObjects[indexArray].SetActive(true);

		Audio.play(PORTAL_REVEAL);
		if (indexArray == 0)
		{
			Audio.play(PORTAL_CHOICE_1_REVEAL);
		}
		else if (indexArray == 1)
		{
			Audio.play(PORTAL_CHOICE_2_REVEAL);
		}
		else
		{
			Audio.play(PORTAL_CHOICE_3_REVEAL);
		}

		yield return new TIWaitForSeconds(1.0f);

		SlotResourceMap.freeSpinType = (SlotResourceMap.FreeSpinTypeEnum)indexArray;

		BonusGameManager.instance.summaryScreenGameName = listOfPosibleGames[indexArray];
		portalSummaryID = BonusGamePresenter.NextBonusGameIdentifier();
		hasGottenBonusResponse = false;
		
		// Adding bonus game choice user flow tracking so we can track successful selections and which games players are picking
		bonusGameChoiceTransactionName = "slot-" + GameState.game.keyName + "-bgc";
		Userflows.flowStart(bonusGameChoiceTransactionName);
		Userflows.logStep(listOfPosibleGames[indexArray], bonusGameChoiceTransactionName);
		
		SlotAction.chooseBonusGame(GameState.game.keyName, GameState.game.keyName, listOfPosibleGames[indexArray], BonusGameManager.instance.possibleBonusGameChoices.keyName,(int)SlotBaseGame.instance.multiplier, enterGame);
		
		// Wait to see if we timeout, if so we are going to assume that the user is not going to get a response due to connectivity
		// issues and reset the game after popping a dialog.
		float timePassedSinceMessageSent = 0;

		// wait for the server response
		while (!hasGottenBonusResponse && timePassedSinceMessageSent < SELECTION_SENT_TIMEOUT)
		{
			yield return null;
			// added a timeout here in case the server never sends a response
			timePassedSinceMessageSent += Time.deltaTime;
		}

		if (!hasGottenBonusResponse)
		{
			// The server message and bonus creation timed out, going to have the game terminate, this will cause a desync, but the player will not be stuck
			Userflows.flowEnd(bonusGameChoiceTransactionName, false, "timeout");
			// Let's make sure we cleanup the server callback so it doesn't get callback if it actually does come in but super delayed
			Server.unregisterEventDelegate("slots_outcome", enterGame);
			Debug.LogWarning("BonusGameChoicePortal.showReveal() - SlotAction.chooseBonusGame() call timed out! Terminating this bonus.");

			// Kill the spin transaction for the base game if one exists since we are going to restart the game
			if (Glb.spinTransactionInProgress)
			{
				string errorMsg = "Bonus Game Selection exceeded timeout of: " + SELECTION_SENT_TIMEOUT;
				Glb.failSpinTransaction(errorMsg, "bonus-selection-timeout");
			}

			// Launch a dialog explaining the error that will force the game to restart
			// so we don't receive the response at a later time when we aren't expecting it
			string userMsg = "";
			if (Data.debugMode)
			{
				userMsg = "Bonus selection timed out after " + SELECTION_SENT_TIMEOUT + " seconds.";
			}
			Server.forceGameRefresh(
				"Bonus selection timed out.", 
				userMsg,
				reportError: false,
				doLocalization: false);

			// Stall this bonus until the player clicks the dialog
			// Technically they are probably frozen anyways, but just to be safe
			while (!Glb.isResetting)
			{
				yield return null;
			}
		}
	}

	// Our server callback after the game has been chosen.
	public void enterGame(JSON data)
	{
		// Mark the Userflow as complete
		Userflows.flowEnd(bonusGameChoiceTransactionName);
		
		// Cancel the timeout
		hasGottenBonusResponse = true;
		
		Server.unregisterEventDelegate("slots_outcome", enterGame);
		StartCoroutine(transitionToBonusGame(data));
	}

	protected virtual IEnumerator transitionToBonusGame(JSON data)
	{
		// Let's activate the plane and make if fly! Let it goooooooo, let it goooooooo
		transitionObj.SetActive(true);
		Audio.play(TRANSITION_SFX);
		iTween.MoveTo(transitionObj, iTween.Hash("x", 900, "y", -600,"time", FLY_TWEEN_TIME, "islocal", true, "easetype", iTween.EaseType.linear));
		iTween.ScaleTo(transitionObj, iTween.Hash("scale", new Vector3(200.0f ,200.0f, 1.0f), "time", FLY_TWEEN_TIME, "easetype", iTween.EaseType.linear));

		// Now let's alpha that bastard panel away while our plane is flying in.
		float age = 0f;
		while (age < FLY_TIME)
		{
			gamePanel.alpha = 1 - (age/FLY_TIME);
			age += Time.deltaTime;
			yield return null;
		}

		// Plane is done, let's create our game data with the json, re-register the server callback, and create the game.
		SlotOutcome bonusGameOutcome = new SlotOutcome(data);
		BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(bonusGameOutcome);
		BonusGamePresenter.instance.endBonusGameImmediately();
		BonusGameManager.instance.create(BonusGameType.GIFTING);
		BonusGameManager.instance.show();
	}
}

