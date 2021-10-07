using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class handles deciding which prefab to use for the free spin game and sending out the appropriate action.
public class Gen05Portal : ChallengeGame {

	public GameObject[] gameIcons;
	public GameObject[] revealObjects;
	public GameObject[] pickMeObjects;
	public UILabel[] gameDescriptions;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] gameDescriptionsWrapperComponent;

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
	

	public GameObject planeTransition;

	public UIPanel gamePanel;

	private CoroutineRepeater pickMeController;

	private bool selectionComplete = false;
	private string[] listOfPosibleGames = new string[3];

	private const float MIN_TIME_PICKME = 1.0f;								// Minimum time pickme animation might take to play next
	private const float MAX_TIME_PICKME = 5.0f;								// Maximum time pickme animation might take to play next
	private const float PICK_ME_TIMING = 2.0f;
	private const float TIME_BETWEEN_PICK_ME = 1.0f;
	private const float FLY_TIME = 1.5f;
	private const float FLY_TWEEN_TIME = 2.0f;

	private const string PORTAL_BONUS_LOOP = "PortalBgLivingLarge";

	private string portalSummaryID = "";

	public override void init()
	{
		// Populates our data and loc. depending on the game choices from the server.
		foreach (PayTable.BonusGameChoice choice in BonusGameManager.instance.possibleBonusGameChoices.gameChoices)
		{
			if (choice.keyName.Contains("5_spin"))
			{
				listOfPosibleGames[0] = choice.keyName;
				gameDescriptionsWrapper[0].text = Localize.text("{0}_free_spins_with_{1}_wilds", "5", choice.extraInfo);
			}
			else if (choice.keyName.Contains("7_spin"))
			{
				listOfPosibleGames[1] = choice.keyName;
				gameDescriptionsWrapper[1].text = Localize.text("{0}_free_spins_with_{1}_wilds", "7", choice.extraInfo);
			}
			else
			{
				listOfPosibleGames[2] = choice.keyName;
				gameDescriptionsWrapper[2].text = Localize.text("{0}_free_spins_with_{1}_wilds", "9", choice.extraInfo);
			}
		}
		
		//Audio.playMusic(PORTAL_BONUS_LOOP);
		Audio.switchMusicKey(PORTAL_BONUS_LOOP);
		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);
		_didInit = true;
	}

	// Shows the pick me animations, and endlessly cycles it.
	private IEnumerator pickMeCallback()
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
		if (!selectionComplete && _didInit)
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

	private IEnumerator showReveal(int indexArray)
	{
		// Show the glow behind the icon once we've selected it.
		revealObjects[indexArray].SetActive(true);

		Audio.play("RevealBonusLivingLarge");
		if (indexArray == 0)
		{
			Audio.play("RevealRivieraVO");
		}
		else if (indexArray == 1)
		{
			Audio.play("RevealChaletVO");
		}
		else
		{
			Audio.play("RevealIslandVO");
		}

		yield return new TIWaitForSeconds(1.0f);

		SlotResourceMap.freeSpinType = (SlotResourceMap.FreeSpinTypeEnum)indexArray;

		BonusGameManager.instance.summaryScreenGameName = listOfPosibleGames[indexArray];
		portalSummaryID = BonusGamePresenter.NextBonusGameIdentifier();
		SlotAction.chooseBonusGame(GameState.game.keyName, GameState.game.keyName, listOfPosibleGames[indexArray], BonusGameManager.instance.possibleBonusGameChoices.keyName,(int)SlotBaseGame.instance.multiplier, enterGame);
	}

	// Our server callback after the game has been chosen.
	public void enterGame(JSON data)
	{
		if (portalSummaryID != "")
		{
			//We don't need to send this because SlotAction.chooseBonusGame clears out the bonus Summary ID on the server, and this will do it twice causing an error.
			//SlotAction.seenBonusSummaryScreen(portalSummaryID);
		}
		Server.unregisterEventDelegate("slots_outcome", enterGame);
		StartCoroutine(transitionToBonusGame(data));
	}

	private IEnumerator transitionToBonusGame(JSON data)
	{
		//if (BonusGamePresenter.bonusEventIdentifier != "")
		//{
		//	SlotAction.seenBonusSummaryScreen(BonusGamePresenter.bonusEventIdentifier);
		//	BonusGamePresenter.bonusEventIdentifier = "";
		//}

		// Let's activate the plane and make if fly! Let it goooooooo, let it goooooooo
		planeTransition.SetActive(true);
		Audio.play("TransitionLearJetFlyby");
		iTween.MoveTo(planeTransition, iTween.Hash("x", 900, "y", -600,"time", FLY_TWEEN_TIME, "islocal", true, "easetype", iTween.EaseType.linear));
		iTween.ScaleTo(planeTransition, iTween.Hash("scale", new Vector3(200.0f ,200.0f, 1.0f), "time", FLY_TWEEN_TIME, "easetype", iTween.EaseType.linear));

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

