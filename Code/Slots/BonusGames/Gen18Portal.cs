using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Gen18Portal : ChallengeGame 
{
	[SerializeField] protected List<Gen18PickGameButton> pickButtons = new List<Gen18PickGameButton>(); 	// Animators for the books, used to
	[SerializeField] protected float minPickMeTime = 1.5f;			// Minimum time an animation might take to play next
	[SerializeField] protected float maxPickMeTime = 2.5f;			// Maximum time an animation might take to play next
	[SerializeField] protected UILabel[] bottomTextLabel;
	[SerializeField] protected bool hasCreditBonus = true;			// Tells if this portal includes a credit bonus
	[SerializeField] protected BonusGameDataTypeEnum bonusOutcomeType = BonusGameDataTypeEnum.WheelOutcome; // Tells what type of data the bonus game outcome should be read as
	[SerializeField] protected GameObject portalPicksBackdrop;		//Game Object surrounding the picks
	[SerializeField] protected Animator portalPicksBackdropTextAnimator;		//Game Object surrounding the picks
	[SerializeField] protected GameObject[] objectsToHideOnPick;
	[SerializeField] protected GameObject[] objectsToShowOnPick;
	[SerializeField] protected Animator backgroundAnimator;
	[SerializeField] protected GameObject[] objectsToHideForTransition;	// List of objects to hide during the transition animation, probably at least the buttons

	protected bool isInputEnabled = false;
	protected SlotOutcome bonusOutcome = null;
	protected PortalTypeEnum portalType = PortalTypeEnum.NONE;
	protected List<PortalTypeEnum> randomPortalTypeList = new List<PortalTypeEnum>();
	protected CoroutineRepeater pickMeController;										// Class to call the pickme animation on a loop
	
	protected enum PortalTypeEnum
	{
		NONE 		= -1,
		PICKING 	= 0,
		FREESPINS 	= 1,
		CREDITS 	= 2
	}

	// When adding game types to this enum always add to the end of the list!
	protected enum BonusGameDataTypeEnum
	{
		WheelOutcome 					= 0,
		PickemOutcome 					= 1,
		NewBaseBonusOutcome 			= 2,
		WheelOutcomeWithChildOutcomes 	= 3,
		CrosswordOutcome 				= 4	// This is a special crossword outcome used for zynga04 and any related games.
	}

	[SerializeField] protected string REVEAL_PICKING_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_FREESPINS_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_PICKING_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FREESPINS_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_CREDITS_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_GRAY_PICKING_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_GRAY_FREESPINS_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_GRAY_CREDITS_ANIM_NAME = "";
	[SerializeField] protected string PICKME_ANIM_NAME = "";
	[SerializeField] protected string PRE_REVEAL_PICK_ANIM_NAME = "";
	[SerializeField] protected string CHALLENGE_OUTCOME_NAME = "";
	[SerializeField] protected string FREESPIN_OUTCOME_NAME = "";
	[SerializeField] protected  string CREDIT_OUTCOME_NAME = "";
	[SerializeField] protected string FREESPIN_PORTAL_OVERRIDE_NAME = "";
	[SerializeField] protected string CHALLENGE_LOCALIZATION_KEY = "";
	[SerializeField] protected string FREESPIN_LOCALIZATION_KEY = "";
	[SerializeField] protected string REVEAL_PICKING_BACKGROUND_ANIM_NAME = "";
	[SerializeField] protected string REVEAL_FREESPINS_BACKGROUND_ANIM_NAME = "";
	[SerializeField] protected string PORTAL_PICKS_BACKDROP_EXIT_ANIM_NAME = "";
	[SerializeField] protected string PORTAL_PICKS_BACKDROP_TEXT_EXIT_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FADE_PICKED_PICKING_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FADE_UNPICKED_PICKING_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FADE_PICKED_FREESPINS_ANIM_NAME = "";
	[SerializeField] protected string POST_REVEAL_FADE_UNPICKED_FREESPINS_ANIM_NAME = "";

	
	[SerializeField] protected float PICK_ME_TIME = 0.5f; // how long to wait before setting _didInit to true after everything happens (for long VOs)
	[SerializeField] protected float SHAKE_AMOUNT = 5.0f; // how long to wait before setting _didInit to true after everything happens (for long VOs)

	[SerializeField] protected float POST_INTRO_VO_WAIT = 0.0f; // how long to wait before setting _didInit to true after everything happens (for long VOs)
	[SerializeField] protected float WAIT_BEFORE_ENDING_PORTAL = 0.0f;
	[SerializeField] protected float REVEAL_PICKING_ANIM_LENGTH = 0.0f;
	[SerializeField] protected float REVEAL_FREESPINS_ANIM_LENGTH = 0.0f;
	[SerializeField] protected float REVEAL_CREDITS_ANIM_LENGTH = 0.0f;
	[SerializeField] protected float REVEAL_NOT_SELECTED_ANIM_LENGTH = 0.0f;
	[SerializeField] protected float PICKME_ANIM_LENGTH = 0.0f;
	[SerializeField] protected float REVEAL_BONUS_VO_SOUND_KEY_DELAY = 0.0f;
	[SerializeField] protected float REVEAL_OTHERS_SOUND_KEY_DELAY = 0.0f;
	[SerializeField] protected float PORTAL_VO_SOUND_KEY_DELAY = 0.0f;

	protected const string PORTAL_VO_SOUND_KEY = "bonus_portal_vo";
	protected const string PORTAL_PICKME_SOUND_KEY = "bonus_portal_pickme";
	protected const string REVEAL_BONUS_VO_SOUND_KEY = "bonus_portal_reveal_bonus_vo";
	protected const string REVEAL_PICK_SOUND_KEY = "bonus_portal_reveal_bonus";
	protected const string REVEAL_PICK_FREESPINS_SOUND_KEY = "bonus_portal_reveal_freespins";
	protected const string REVEAL_PICK_PICKING_SOUND_KEY = "bonus_portal_reveal_picking";
	protected const string REVEAL_PICK_CREDITS_SOUND_KEY = "bonus_portal_reveal_credits";
	protected const string REVEAL_OTHERS_SOUND_KEY = "bonus_portal_reveal_others";
	protected const string REVEAL_OTHERS_FREESPINS_SOUND_KEY = "bonus_portal_reveal_others_freespins";
	protected const string REVEAL_OTHERS_PICKING_SOUND_KEY = "bonus_portal_reveal_others_picking";
	protected const string REVEAL_OTHERS_CREDITS_SOUND_KEY = "bonus_portal_reveal_others_credits";
	protected const string BONUS_PORTAL_BG_MUSIC_KEY = "bonus_portal_bg";
	protected const string POST_REVEAL_PICKING_SOUND_KEY = "bonus_portal_pickme_post_reveal";
	protected const string POST_REVEAL_FREESPINS_SOUND_KEY = "bonus_portal_freespins_post_reveal";
	protected const string PORTAL_TRANSITION_PICKING_SOUND_KEY = "bonus_portal_transition_picking";
	protected const string PORTAL_TRANSITION_FREESPINS_SOUND_KEY = "bonus_portal_transition_freespins";
	protected const string PORTAL_BACKDROP_SOUND_KEY = "bonus_portal_backdrop";
	protected const string PORTAL_END_FREESPINS_SOUND_KEY = "bonus_portal_end_outcome_freespins";
	protected const string PORTAL_END_PICKING_SOUND_KEY = "bonus_portal_end_outcome_picking";
	protected const string BONUS_PORTAL_END_PICKING_BG_MUSIC_KEY = "bonus_portal_end_picking_bg";
	protected const string BONUS_PORTAL_END_FREESPINS_BG_MUSIC_KEY = "bonus_portal_end_freespins_bg";

	/// Init game specific stuff
	public override void init()
	{
		setPseudoConstants();
		Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_PORTAL_BG_MUSIC_KEY));

		if (Audio.canSoundBeMapped(PORTAL_VO_SOUND_KEY))
		{
			Audio.play(Audio.soundMap(PORTAL_VO_SOUND_KEY), 1, 0, PORTAL_VO_SOUND_KEY_DELAY);
		}
		
		// Randomize a list of portal type objects that will reveal
		randomPortalTypeList.Add(PortalTypeEnum.PICKING);
		randomPortalTypeList.Add(PortalTypeEnum.FREESPINS);
				
		if (hasCreditBonus)
		{
			randomPortalTypeList.Add(PortalTypeEnum.CREDITS);
		}

		// Add in any extra "filler" reveals based on the length of the pickButtons list and the entrys already in the randomPortalTypeList
		// This is in case there are more than 2 buttons (or 3 with Credits) that need to play reveals 
		// Example of this is Zynga04 - Words with friends
		List<PortalTypeEnum> gamePortalTypes = new List<PortalTypeEnum>(randomPortalTypeList);
		int remainingPicks = System.Math.Abs(pickButtons.Count - randomPortalTypeList.Count);
		for (int i = 0; i < remainingPicks; ++i)
		{
			PortalTypeEnum randomPortalType = gamePortalTypes[Random.Range(0, gamePortalTypes.Count)];
			randomPortalTypeList.Add(randomPortalType);
		}
		
		CommonDataStructures.shuffleList<PortalTypeEnum>(randomPortalTypeList);

		if (SlotBaseGame.instance.bonusGameOutcomeOverride != null)
		{
			bonusOutcome = SlotBaseGame.instance.bonusGameOutcomeOverride;
			SlotBaseGame.instance.bonusGameOutcomeOverride = null;
		}
		else
		{
			bonusOutcome = SlotBaseGame.instance.outcome;
		}
		// cancel being a portal now that we're in it
		bonusOutcome.isPortal = false;
		
		SlotOutcome challenge_outcome = bonusOutcome.getBonusGameOutcome(CHALLENGE_OUTCOME_NAME);
		if (challenge_outcome != null)
		{
			bonusOutcome.isChallenge = true;
			BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] = createChallengeBonusData(challenge_outcome);
			portalType = PortalTypeEnum.PICKING;
		}
		
		if (CREDIT_OUTCOME_NAME != "" && hasCreditBonus)
		{
			SlotOutcome credit_outcome = bonusOutcome.getBonusGameOutcome(CREDIT_OUTCOME_NAME);
			if (credit_outcome != null)
			{
				bonusOutcome.isCredit = true;
				WheelOutcome creditOutcome = new WheelOutcome(credit_outcome);
				bonusOutcome.winAmount = creditOutcome.getNextEntry().credits;
				BonusGamePresenter.instance.currentPayout = SlotBaseGame.instance.getCreditBonusValue();
				portalType = PortalTypeEnum.CREDITS;
			}
		}
		
		SlotOutcome freespinOutcome = getFreeSpinOutcome();
		if (freespinOutcome != null)
		{
			bonusOutcome.isGifting = true;
			BonusGameManager.instance.outcomes[BonusGameType.GIFTING] = new FreeSpinsOutcome(freespinOutcome);
			portalType = PortalTypeEnum.FREESPINS;

			if (FREESPIN_PORTAL_OVERRIDE_NAME != "")
			{
				BonusGame thisBonusGame = BonusGame.find(FREESPIN_PORTAL_OVERRIDE_NAME);
				BonusGameManager.instance.summaryScreenGameName = FREESPIN_PORTAL_OVERRIDE_NAME;
				BonusGameManager.instance.isGiftable = thisBonusGame.gift;
			}
		}

		if (!wingsIncludedInBackground)
		{
			BonusGameManager.instance.wings.forceShowPortalWings(true);
		}
		
		if (portalType == PortalTypeEnum.NONE)
		{
			// Encountered a major error, just going to exit this portal
			Debug.LogError("Portal Data wasn't in expected format, terminating the portal!");
			BonusGamePresenter.instance.gameEnded();
		}
		else
		{
			randomPortalTypeList.Remove(portalType);
			
			pickMeController = new CoroutineRepeater(minPickMeTime, maxPickMeTime, pickMeAnimCallback);
			StartCoroutine(waitThenSetDidInit());
		}
	}

	private IEnumerator waitThenSetDidInit()
	{
		if(portalPicksBackdrop != null)
		{
			yield return new TIWaitForSeconds(2.0f);
			portalPicksBackdrop.SetActive(true);
		}
		
		if (Audio.canSoundBeMapped(PORTAL_BACKDROP_SOUND_KEY))
		{
			Audio.play(Audio.soundMap(PORTAL_BACKDROP_SOUND_KEY));
		}
		
		if (!Audio.muteSound)
		{
			yield return new TIWaitForSeconds(POST_INTRO_VO_WAIT);
		}

		_didInit = true;
		isInputEnabled = true;
	}

	protected virtual void setPseudoConstants()
	{

	}
	
	protected override void Update()
	{
		base.Update();
		
		if (isInputEnabled && _didInit)
		{
			pickMeController.update();
		}
	}
	
	/// Pick me animation player
	protected IEnumerator pickMeAnimCallback()
	{
		int randomButtonIndex = Random.Range(0, pickButtons.Count);
		// We want to rotate the pick buttons with a tween
		PickGameButton button = pickButtons[randomButtonIndex];
		iTween.ShakeRotation(button.gameObject, iTween.Hash("amount", new Vector3(0, 0, SHAKE_AMOUNT), "time", PICK_ME_TIME));
		
		Audio.play(Audio.soundMap(PORTAL_PICKME_SOUND_KEY));

		yield return new TIWaitForSeconds(PICK_ME_TIME);
	}
	
	/*objectsToHideOnPick
objectsToShowOnPick*/
	
	protected void showObjects()
	{
		foreach(var o in objectsToShowOnPick)
		{
			o.SetActive(true);
		}
	}
	
	protected void hideObjects()
	{
		foreach(var o in objectsToHideOnPick)
		{
			o.SetActive(false);
		}
	}
	
	/// Triggered by UI Button Message when a book is clicked
	public void choiceSelected(GameObject obj)
	{
		if (isInputEnabled)
		{
			isInputEnabled = false;
			showObjects();
			hideObjects();
			StartCoroutine(choiceSelectedCoroutine(obj));
		}
	}
	
	protected virtual IEnumerator gameSpecificSelectedCoroutine(PickGameButton button)
	{
		yield break;
	}

	protected virtual void setCreditText(PickGameButton button, bool isPick)
	{

	}
	
	/// Coroutine to handle timing stuff once a book is picked
	public IEnumerator choiceSelectedCoroutine(GameObject obj)
	{
		Gen18PickGameButton button = obj.GetComponent<Gen18PickGameButton>();
		yield return StartCoroutine(gameSpecificSelectedCoroutine(button));
		Animator pickedAnimator = button.animator;

		Audio.play(Audio.soundMap(REVEAL_PICK_SOUND_KEY));
		Audio.play(Audio.soundMap(REVEAL_BONUS_VO_SOUND_KEY), 1, 0, REVEAL_BONUS_VO_SOUND_KEY_DELAY);
		if (PRE_REVEAL_PICK_ANIM_NAME != "")
		{
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, PRE_REVEAL_PICK_ANIM_NAME));
		}
		if (bonusOutcome.isChallenge)
		{
			// Show the Challange stuff and hide the freespins stuff.
			foreach (GameObject objectToActivate in button.challengeObjectsToActivate)
			{
				if (objectToActivate != null)
				{
					objectToActivate.SetActive(true);
				}
			}
			foreach (GameObject objectToDeactivate in button.challengeObjectsToDeactivate)
			{
				if (objectToDeactivate != null)
				{
					objectToDeactivate.SetActive(false);					
				}
			}
			foreach(UILabel label in bottomTextLabel)
			{
				label.text = Localize.textUpper(CHALLENGE_LOCALIZATION_KEY);
			}

			if (Audio.canSoundBeMapped(REVEAL_PICK_PICKING_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(REVEAL_PICK_PICKING_SOUND_KEY));
			}

			// wait on animation
			yield return new TIWaitForSeconds(REVEAL_PICKING_ANIM_LENGTH);
		}
		else if (bonusOutcome.isGifting)
		{
			// Show the Freespins stuff and hide the challenge stuff.
			foreach (GameObject objectToActivate in button.freespinObjectsToActivate)
			{
				if (objectToActivate != null)
				{
					objectToActivate.SetActive(true);
				}
			}
			foreach (GameObject objectToDeactivate in button.freespinObjectsToDeactivate)
			{
				if (objectToDeactivate != null)
				{
					objectToDeactivate.SetActive(false);					
				}
			}
			foreach(UILabel label in bottomTextLabel)
			{
				label.text = Localize.textUpper(FREESPIN_LOCALIZATION_KEY);
			}

			if (Audio.canSoundBeMapped(REVEAL_PICK_FREESPINS_SOUND_KEY))
			{
				Audio.play(Audio.soundMap(REVEAL_PICK_FREESPINS_SOUND_KEY));
			}

			// wait on animation
			yield return new TIWaitForSeconds(REVEAL_FREESPINS_ANIM_LENGTH);
		}
		else
		{
			// No credits in gen 18
		}
		
		foreach (Gen18PickGameButton currentButton in pickButtons)
		{
			if (currentButton != button && randomPortalTypeList.Count > 0)
			{
				// grab a portal type from the random list
				PortalTypeEnum nextPortal = randomPortalTypeList[randomPortalTypeList.Count - 1];
				randomPortalTypeList.RemoveAt(randomPortalTypeList.Count - 1);
				
				Audio.play(Audio.soundMap(REVEAL_OTHERS_SOUND_KEY), 1.0f, 0.0f, REVEAL_OTHERS_SOUND_KEY_DELAY);
				if (PRE_REVEAL_PICK_ANIM_NAME != "")
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(currentButton.animator, PRE_REVEAL_PICK_ANIM_NAME));
				}

				switch (nextPortal)
				{
				case PortalTypeEnum.PICKING:
					foreach (GameObject objectToActivate in currentButton.challengeObjectsGrey)
					{
						if (objectToActivate != null)
						{
							objectToActivate.SetActive(true);
						}
					}
					foreach (GameObject objectToDeactivate in currentButton.challengeObjectsToDeactivate)
					{
						if (objectToDeactivate != null)
						{
							objectToDeactivate.SetActive(false);
						}
					}
					if (Audio.canSoundBeMapped(REVEAL_OTHERS_PICKING_SOUND_KEY))
					{
						Audio.play(Audio.soundMap(REVEAL_OTHERS_PICKING_SOUND_KEY));
					}
					break;
				case PortalTypeEnum.FREESPINS:
					foreach (GameObject objectToActivate in currentButton.freespinObjectsGrey)
					{
						if (objectToActivate != null)
						{
							objectToActivate.SetActive(true);
						}
					}
					foreach (GameObject objectToDeactivate in currentButton.challengeObjectsToDeactivate)
					{
						if (objectToDeactivate != null)
						{
							objectToDeactivate.SetActive(false);
						}
					}
					if (Audio.canSoundBeMapped(REVEAL_OTHERS_FREESPINS_SOUND_KEY))
					{
						Audio.play(Audio.soundMap(REVEAL_OTHERS_FREESPINS_SOUND_KEY));
					}
					break;
				case PortalTypeEnum.CREDITS:
					// No credits in gen18.
					break;
				}
				
				// wait on animation
				yield return new TIWaitForSeconds(REVEAL_NOT_SELECTED_ANIM_LENGTH);
			}
		}
		
		// let player see their choices for just a bit before starting the bonus
		yield return new TIWaitForSeconds(WAIT_BEFORE_ENDING_PORTAL);

		yield return StartCoroutine(playFadeAnimations(obj));
		//Play exit portal animations
		if (bonusOutcome.isChallenge)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_PORTAL_END_PICKING_BG_MUSIC_KEY));

			if (POST_REVEAL_PICKING_ANIM_NAME != "")
			{
				Audio.play(Audio.soundMap(POST_REVEAL_PICKING_SOUND_KEY));
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, POST_REVEAL_PICKING_ANIM_NAME));

			}

			if (backgroundAnimator != null && REVEAL_PICKING_BACKGROUND_ANIM_NAME != "")
			{
				yield return StartCoroutine(playBackgroundTransition(REVEAL_PICKING_BACKGROUND_ANIM_NAME, PORTAL_TRANSITION_PICKING_SOUND_KEY));
			}
			Audio.play(Audio.soundMap(PORTAL_END_PICKING_SOUND_KEY));
		}
		else if (bonusOutcome.isGifting)
		{
			Audio.switchMusicKeyImmediate(Audio.soundMap(BONUS_PORTAL_END_FREESPINS_BG_MUSIC_KEY));

			if (POST_REVEAL_FREESPINS_ANIM_NAME != "")
			{
				Audio.play(Audio.soundMap(POST_REVEAL_FREESPINS_SOUND_KEY));
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, POST_REVEAL_FREESPINS_ANIM_NAME));
				Audio.stopSound(Audio.findPlayingAudio(Audio.soundMap(POST_REVEAL_FREESPINS_SOUND_KEY)));
			}

			if (backgroundAnimator != null && REVEAL_FREESPINS_BACKGROUND_ANIM_NAME != "")
			{
				yield return StartCoroutine(playBackgroundTransition(REVEAL_FREESPINS_BACKGROUND_ANIM_NAME, PORTAL_TRANSITION_FREESPINS_SOUND_KEY));
			}
			Audio.play(Audio.soundMap(PORTAL_END_FREESPINS_SOUND_KEY));

		}

		beginBonus();
	}

	/// Play a transition animation
	private IEnumerator playBackgroundTransition(string transitionAnimName, string transitionAnimSoundKey)
	{
		if (backgroundAnimator != null && transitionAnimName != "")
		{
			foreach (GameObject objectToHide in objectsToHideForTransition)
			{
				objectToHide.SetActive(false);
			}

			Audio.play(Audio.soundMap(transitionAnimSoundKey));
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(backgroundAnimator, transitionAnimName));
		}
	}

	private IEnumerator playFadeAnimations(GameObject obj)
	{
		PickGameButton button = obj.GetComponent<PickGameButton>();
		Animator pickedAnimator = button.animator;
		Animator portalPicksBackdropAnimator = null;
		if(portalPicksBackdrop != null)
		{
			portalPicksBackdropAnimator = portalPicksBackdrop.GetComponent<Animator>();
		}
		

		if (bonusOutcome.isChallenge && POST_REVEAL_FADE_PICKED_PICKING_ANIM_NAME != "")
		{
			StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, POST_REVEAL_FADE_PICKED_PICKING_ANIM_NAME));

		}
		else if (bonusOutcome.isGifting && POST_REVEAL_FADE_PICKED_FREESPINS_ANIM_NAME != "")
		{
			StartCoroutine(CommonAnimation.playAnimAndWait(pickedAnimator, POST_REVEAL_FADE_PICKED_FREESPINS_ANIM_NAME));

		}

		if (portalPicksBackdropAnimator != null && PORTAL_PICKS_BACKDROP_EXIT_ANIM_NAME != "")
		{
			StartCoroutine(CommonAnimation.playAnimAndWait(portalPicksBackdropAnimator, PORTAL_PICKS_BACKDROP_EXIT_ANIM_NAME));
		}

		if (portalPicksBackdropTextAnimator != null && PORTAL_PICKS_BACKDROP_TEXT_EXIT_ANIM_NAME != "")
		{
			StartCoroutine(CommonAnimation.playAnimAndWait(portalPicksBackdropTextAnimator, PORTAL_PICKS_BACKDROP_TEXT_EXIT_ANIM_NAME));
		}

		foreach (PickGameButton currentButton in pickButtons)
		{
			if (currentButton != button)
			{
				if (bonusOutcome.isChallenge && POST_REVEAL_FADE_UNPICKED_FREESPINS_ANIM_NAME != "")
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(currentButton.animator, POST_REVEAL_FADE_UNPICKED_FREESPINS_ANIM_NAME));

				}
				else if (bonusOutcome.isGifting && POST_REVEAL_FADE_UNPICKED_PICKING_ANIM_NAME != "")
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(currentButton.animator, POST_REVEAL_FADE_UNPICKED_PICKING_ANIM_NAME));

				}
			}
		}

	}
	/// Start the actual bonus revealed in this portal
	protected void beginBonus()
	{
		if (bonusOutcome.isCredit)
		{
			if (SlotBaseGame.instance != null)
			{
				SlotBaseGame.instance.goIntoBonus();
			}
			else
			{
				Debug.LogError("There is no SlotBaseGame instance, can't start bonus game...");
			}
		}
		else
		{
			if (bonusOutcome.isChallenge)
			{
				BonusGameManager.instance.create(BonusGameType.CHALLENGE);
			}
			else
			{
				BonusGameManager.instance.create(BonusGameType.GIFTING);
			}

			BonusGameManager.instance.show();
		}
	}

	protected virtual SlotOutcome getFreeSpinOutcome()
	{
		return bonusOutcome.getBonusGameOutcome(FREESPIN_OUTCOME_NAME);
	}

	/// Overridable function for generating the challenge game data, may use something other than WheelOutcome, if so override and return a new instance of one of those
	protected BaseBonusGameOutcome createChallengeBonusData(SlotOutcome challenge_outcome)
	{
		switch (bonusOutcomeType)
		{
			case BonusGameDataTypeEnum.WheelOutcome:
				return new WheelOutcome(challenge_outcome);
				
			case BonusGameDataTypeEnum.WheelOutcomeWithChildOutcomes:
				return new WheelOutcome(challenge_outcome, false, 0, true);

			case BonusGameDataTypeEnum.PickemOutcome:
				return new PickemOutcome(challenge_outcome);

			case BonusGameDataTypeEnum.NewBaseBonusOutcome:
				return new NewBaseBonusGameOutcome(challenge_outcome);

			case BonusGameDataTypeEnum.CrosswordOutcome:
				return new CrosswordOutcome(challenge_outcome);
		}

		// if we reach here then we have an unhandled outcome type
		Debug.LogError("PickPortal::createChallengeBonusData() - Don't know how to handle bonusOutcomeType = " + bonusOutcomeType);
		return null;
	} 
}
