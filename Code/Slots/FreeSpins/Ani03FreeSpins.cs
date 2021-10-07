using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * Free Spin class for Ani01 - Grizzlies Gold
 * Clone of Bev01, so we just extend that class to make a few modificationss
 */ 
public class Ani03FreeSpins : PickMajorFreeSpins 
{
	// inspector variables
	[SerializeField] private Animation[] wagonPickMeAnimations = null;			// Animations for the wagon pick mes
	[SerializeField] private UISprite[] backgroundSprites = null;				// Background textures for the major symbols
	[SerializeField] private UILabel[] revealMegaText = null;					// The texts for all the mega symbol reveals, need references to them so unpicked ones can be grayed out -  To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent[] revealMegaTextWrapperComponent = null;					// The texts for all the mega symbol reveals, need references to them so unpicked ones can be grayed out

	public List<LabelWrapper> revealMegaTextWrapper
	{
		get
		{
			if (_revealMegaTextWrapper == null)
			{
				_revealMegaTextWrapper = new List<LabelWrapper>();

				if (revealMegaTextWrapperComponent != null && revealMegaTextWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealMegaTextWrapperComponent)
					{
						_revealMegaTextWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealMegaText)
					{
						_revealMegaTextWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealMegaTextWrapper;
		}
	}
	private List<LabelWrapper> _revealMegaTextWrapper = null;	
	
	[SerializeField] private GameObject[] revealHolders = null;					// Holders that are scaled in to reveal what symbol was picked
	[SerializeField] private UISprite megaBannerSymbol = null; 					// texture in the banner which can be set with one of the major materials
	[SerializeField] private UISprite megaBannerBkg = null;						// backround for the mega banner symbol
	[SerializeField] private GameObject pickWagonText = null; 					// banner at top during pick wagon phase

	private readonly string[] REVEAL_SPRITE_NAMES = { "ANI03_M1_Bear", "ANI03_M2_Buffalo", "ANI03_M3_Hawk", "ANI03_M4_Fox" };
	private readonly string[] REVEAL_BKG_SPRITE_NAMES = { "ANI03_M1_BKG", "ANI03_M2_BKG", "ANI03_M3_BKG", "ANI03_M4_BKG" };

	// timing constants
	private const float WAGON_SHRINK_TIME = 0.5f;								// how long to take to shrink wagons after pick
	private const float SPRITE_GROW_TIME = 0.5f;								// how long to take to grow revealed sprites
	private const float WAIT_AFTER_REVEALS_TIME = 3.5f;							// short wait after the reveals so the user can see what the other picks were
	private const float FREESPIN_INTRO_MUSIC_DELAY = 0.0f;						// how long to wait before doing intro spin sound
	private const float SPIN_VO_SOUND_DELAY = 0.0f;								// how long to wait before doing spin_vo sound
	private const float SUMMARY_VO_SOUND_DELAY = 0.6f;							// delay before the summary VO is played after the game has ended
	private const float TRANSITION_SOUND_WAIT_TIME = 2.1f;						// delay showing the freespins till this VO is done

	private const float TIME_MOVE_FS_TEXT = 0.75f;					// How long to move the stage 2 text.
	private const float TIME_TO_SHOW_FS_TEXT = 2.5f;				// After the text is moved how long to hold it there.

	// sound constants
	private const string ITEM_PICKED_SOUND = "PickAWagonRevealSymbol";			// sound played when weapon picked
	private const string REVEAL_OTHERS = "PickAWagonRevealOthers";				// Colection name played when the other symbols are revealed.
	private const string INTRO_VO = "DMPickAWagonVO";							// Sound name played when the games starts.
	private const string SUMMARY_VO_SOUND = "FreespinSummaryVOBuffalo";			// Sound name played once the summary screen comes up for this game.
	private const string PICK_A_WAGON_MUSIC = "PickAWagonBg";					// Music for the pick a wagon section of free spins
	private const string FREESPIN_INTRO_MUSIC = "IntroFreespinBuffalo";			// intro to free spins
	private const string SPIN_VO_SOUND = "FreespinIntroVOBuffalo";				// intro VO to free spins

	// take care of any setup required for reveals/transition
	protected override void setupStage()
	{
		if (BonusGameManager.instance.bonusGameName.Contains("_M1"))
		{
			revealSpriteIndex = 0;
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M2"))
		{
			revealSpriteIndex = 1;	
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M3"))
		{
			revealSpriteIndex = 2;
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M4"))
		{
			revealSpriteIndex = 3;
		}
		else
		{
			revealSpriteIndex = -1;
			Debug.LogError("There was an unexpected format for the name of the Bev01Freespins game, don't know what symbol to reveal for " +
			               BonusGameManager.instance.summaryScreenGameName);
		}

		// Set up the stage 2 stuff
		freeSpinTextStartingPos = freeSpinText != null ? freeSpinText.transform.position : Vector3.zero;
		megaSymbolTextStartingPos = megaSymbolText != null ? megaSymbolText.transform.position : Vector3.zero;
		megaSymbolImageStartingPos = megaSymbolImage != null ? megaSymbolImage.transform.position : Vector3.zero;

		// Check and make sure that we got into one of the normal stage.
		if (revealSpriteIndex != -1)
		{
			Audio.play(INTRO_VO);
			Audio.switchMusicKeyImmediate(PICK_A_WAGON_MUSIC, 0.0f);
			setupAnimationComponents();
		}
		else // If these wasn't an image to load we should just jump right into the freespins game.
		{
			inputEnabled = false;
			StartCoroutine(transitionIntoStage2());
		}
	}

	// do pickme animation
	protected override IEnumerator pickMeCallback()
	{
		// Get one of the available weapon game objects
		int randomWagonIndex = 0;
		
		randomWagonIndex = Random.Range(0, wagonPickMeAnimations.Length);
		Animation randomWagonAnimation = wagonPickMeAnimations[randomWagonIndex];

		randomWagonAnimation.Play();

		while (randomWagonAnimation.isPlaying)
		{
			yield return null;
		}
	}

	// handle weapon being picked
	protected override IEnumerator knockerClickedCoroutine(GameObject clickedObject)
	{
		yield return null;
		pickWagonText.SetActive(false);
		Audio.play(ITEM_PICKED_SOUND);
		iTween.ScaleTo(clickedObject, iTween.Hash("scale", Vector3.zero, "time", WAGON_SHRINK_TIME, "easetype", iTween.EaseType.easeInCubic, "islocal", true));
		yield return new TIWaitForSeconds(WAGON_SHRINK_TIME);

		int textureIndex = buttonSelections.IndexOf(clickedObject);

		GameObject winningSpriteObject = revealSprites[textureIndex];
		if (winningSpriteObject != null)
		{
				UISprite winningSprite = winningSpriteObject.GetComponent<UISprite>();
				if (winningSprite != null)
				{
					winningSprite.spriteName = REVEAL_SPRITE_NAMES[revealSpriteIndex];
				}
		}

		backgroundSprites[textureIndex].spriteName = REVEAL_BKG_SPRITE_NAMES[revealSpriteIndex];

		iTween.ScaleTo(revealHolders[textureIndex].gameObject, iTween.Hash("scale", Vector3.one, "time", SPRITE_GROW_TIME, "easetype", iTween.EaseType.easeInCubic, "islocal", true));
		yield return new TIWaitForSeconds(SPRITE_GROW_TIME);

		Collider buttonCollider = clickedObject.GetComponent<Collider>();
		buttonCollider.enabled = false;

		yield return StartCoroutine(revealOthers());

		Audio.playMusic(FREESPIN_INTRO_MUSIC, 0.0f, FREESPIN_INTRO_MUSIC_DELAY);
		Audio.play(SPIN_VO_SOUND, 1.0f, 0.0f, SPIN_VO_SOUND_DELAY, 0.0f);

		// play audio here so it is done before the transition
		yield return new TIWaitForSeconds(WAIT_AFTER_REVEALS_TIME);

        if(megaBannerSymbol != null)
        {
		    megaBannerSymbol.spriteName = REVEAL_SPRITE_NAMES[revealSpriteIndex];
        }
        if(megaBannerBkg != null)
        {
		    megaBannerBkg.spriteName = REVEAL_BKG_SPRITE_NAMES[revealSpriteIndex];
        }

		// Transition into the freespins game
		yield return StartCoroutine(transitionIntoStage2());
	}

	// reveal the rest of the weapons
	private IEnumerator revealOthers()
	{
		for (int i = 0; i < REVEAL_SPRITE_NAMES.Length; i++)
		{
			if (i != revealSpriteIndex)
			{
				// find an open slot
				for (int k = 0; k < buttonSelections.Count; ++k)
				{
					Collider buttonCollider = buttonSelections[k].GetComponent<Collider>();

					if (buttonCollider.enabled)
					{
						iTween.ScaleTo(buttonSelections[k], iTween.Hash("scale", Vector3.zero, "time", WAGON_SHRINK_TIME, "easetype", iTween.EaseType.easeInCubic, "islocal", true));
						yield return new TIWaitForSeconds(WAGON_SHRINK_TIME);

						Audio.play(REVEAL_OTHERS);


						GameObject revealedSpriteObject = revealSprites[k];
						if (revealedSpriteObject != null)
						{
								UISprite revealedSprite = revealedSpriteObject.GetComponent<UISprite>();
								if (revealedSprite != null)
								{
									revealedSprite.spriteName = REVEAL_SPRITE_NAMES[i];
									revealedSprite.color = Color.gray;
								}
						}

						backgroundSprites[k].spriteName = REVEAL_BKG_SPRITE_NAMES[i];
						backgroundSprites[k].color = Color.gray;

						revealMegaTextWrapper[k].color = Color.gray;

						buttonCollider.enabled = false;

						iTween.ScaleTo(revealHolders[k].gameObject, iTween.Hash("scale", Vector3.one, "time", SPRITE_GROW_TIME, "easetype", iTween.EaseType.easeInCubic, "islocal", true));
						yield return new TIWaitForSeconds(SPRITE_GROW_TIME);
						break;
					}
				}
			}
		}
	}

	// A function that move the freespin text up and down.
	protected override IEnumerator moveFreeSpinText()
	{
		if (freeSpinText == null || megaSymbolImage == null || megaSymbolText == null)
		{
			yield break;
		}
		// Set up the starting position of everything, freespin on top megasymbols on bottom.
		Vector3 centerMegaTextPos = megaSymbolText.transform.position;
		centerMegaTextPos.y = 0.0f;
		Vector3 centerMegaImagePos = megaSymbolImage.transform.position;
		centerMegaImagePos.y = 0.0f;
		Vector3 centerFreeSpinText = freeSpinText.transform.position;
		centerFreeSpinText.y = 0.0f;
		megaSymbolText.transform.position = centerMegaTextPos;
		megaSymbolImage.transform.position = centerMegaImagePos;
		megaBannerBkg.gameObject.transform.position = centerMegaImagePos;

		while (true)
		{
			// Show off the freespin text.
			yield return new WaitForSeconds(TIME_TO_SHOW_FS_TEXT);
			// FS text down Mega text up
			iTween.MoveTo(freeSpinText, iTween.Hash("y", centerFreeSpinText.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeInQuad));
			iTween.MoveTo(megaSymbolText, iTween.Hash("y", megaSymbolTextStartingPos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeOutQuad));
			iTween.MoveTo(megaSymbolImage, iTween.Hash("y", megaSymbolImageStartingPos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeOutQuad));
			iTween.MoveTo(megaBannerBkg.gameObject, iTween.Hash("y", megaSymbolImageStartingPos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeOutQuad));
			yield return new WaitForSeconds(TIME_MOVE_FS_TEXT);
			// Show the change that was made
			yield return new WaitForSeconds(TIME_TO_SHOW_FS_TEXT);
			// FS text up, Mega text down.
			iTween.MoveTo(freeSpinText, iTween.Hash("y", freeSpinTextStartingPos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeOutQuad));
			iTween.MoveTo(megaSymbolText, iTween.Hash("y", centerMegaTextPos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeInQuad));
			iTween.MoveTo(megaSymbolImage, iTween.Hash("y", centerMegaImagePos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeInQuad));
			iTween.MoveTo(megaBannerBkg.gameObject, iTween.Hash("y", centerMegaImagePos.y, "time", TIME_MOVE_FS_TEXT, "easetype", iTween.EaseType.easeInQuad));
			yield return new WaitForSeconds(TIME_MOVE_FS_TEXT);

			// Loop back through and do it all again.
		}
	}

	// play the summary sound and end the game
	protected override void gameEnded()
	{
		// Play the summary VO 0.6 seconds after the game has ended.
		Audio.play(SUMMARY_VO_SOUND, 1.0f, 0.0f, SUMMARY_VO_SOUND_DELAY);
		base.gameEnded();
	}
}

