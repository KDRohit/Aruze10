using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Free Spin class for Gen06
 * Clone of Bev01, so we just extend that class to make a few modificationss
 */ 
public class Gen06FreeSpins : PickMajorFreeSpins 
{
	// inspector variables
	public Vector3 rotationAmount; // how much to rotate picks during pickme
	public float rotationTime; // how long to take to rotate picks
	public Animator background; // animation component of pick screen background
	public List<UILabel> revealMegaText; // list of 'is mega symbol every spin' labels -  To be removed when prefabs are updated.
	public List<LabelWrapperComponent> revealMegaTextWrapperComponent; // list of 'is mega symbol every spin' labels

	public List<LabelWrapper> revealMegaTextWrapper
	{
		get
		{
			if (_revealMegaTextWrapper == null)
			{
				_revealMegaTextWrapper = new List<LabelWrapper>();

				if (revealMegaTextWrapperComponent != null && revealMegaTextWrapperComponent.Count > 0)
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
	
	public List<UISprite> revealStars; // list of stars to reveal (only reveal 1 of them)
	public List<GameObject> revealHolders; // parent game object of reveals (for attaching stars)
	public GameObject spriteHolder; // parent game object of all sprites (scale it up for zoom in)
	public List<GameObject> megaBannerSymbols; // list of mega symbols (activate 1 to show up in banner)
	public GameObject pickWeaponText; // banner at top during pick weapon phase
	public GameObject backgroundCoverImage; // image of background (to turn to black before transition)
	public List<UISprite> glowSprites; // list of weapon glow sprites
	private List<UISprite> revealedSprites = new List<UISprite>(); // hold the sprites that we revealed
	private List<UILabel> revealedLabels = new List<UILabel>(); // hold the labels that we revealed -  To be removed when prefabs are updated.
	private List<LabelWrapperComponent> revealedLabelsWrapperComponent = new List<LabelWrapperComponent>(); // hold the labels that we revealed

	public List<LabelWrapper> revealedLabelsWrapper
	{
		get
		{
			if (_revealedLabelsWrapper == null)
			{
				_revealedLabelsWrapper = new List<LabelWrapper>();

				if (revealedLabelsWrapperComponent != null && revealedLabelsWrapperComponent.Count > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealedLabelsWrapperComponent)
					{
						_revealedLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealedLabels)
					{
						_revealedLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealedLabelsWrapper;
		}
	}
	private List<LabelWrapper> _revealedLabelsWrapper = null;	
	

	private UISprite pickedSprite, pickedStar; // hold the sprites that we picked
	private UILabel pickedLabel; // hold the label that we picked -  To be removed when prefabs are updated.
	private LabelWrapperComponent pickedLabelWrapperComponent; // hold the label that we picked

	public LabelWrapper pickedLabelWrapper
	{
		get
		{
			if (_pickedLabelWrapper == null)
			{
				if (pickedLabelWrapperComponent != null)
				{
					_pickedLabelWrapper = pickedLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_pickedLabelWrapper = new LabelWrapper(pickedLabel);
				}
			}
			return _pickedLabelWrapper;
		}
		set
		{
			_pickedLabelWrapper = value;
		}
	}
	private LabelWrapper _pickedLabelWrapper = null;
	

	private List<string> spriteNames = new List<string>(); // sprite names in correct order

	// timing constants
	private const float WEAPON_SHRINK_TIME = 0.5f;								// how long to take to shrink weapons after pick
	private const float SPRITE_GROW_TIME = 0.5f;								// how long to take to grow revealed sprites
	private const float ANIM_WAIT_TIME = 1.5f;									// how long to wait before transitioning to stage 2
	private const float SPRITE_ZOOM_TIME = 1.5f;								// how long to take to zoom all sprites (during gate open + zoom animation)
	private const float SPRITE_ZOOM_WAIT_TIME = 0.5f;							// how long to wait before starting zoom
	private const float TIME_SCALE_FS_TEXT = 0.5f;								// How long to move the stage 2 text.
	private const float TIME_TO_SHOW_FS_TEXT = 2.5f;							// After the text is moved how long to hold it there.
	private const float CROWD_LOOP_DELAY = 1.0f;								// how long to wait before starting crowd loop
	private const float INTRO_SPIN_DELAY = 0.5f;								// how long to wait before doing intro spin sound
	private const float SPIN_VO_DELAY = 3.0f;									// how long to wait before doing spin_vo sound
	private const float SPIN_BG_DELAY = 3.0f;									// how long to wait before playing spin bg music

	// constants
	private const float STAR_Y_OFFSET = -148.0f;								// how far down to place revealed star
	private const float SPRITE_HOLDER_GROWTO_SIZE = 3.0f;						// how big to grow the sprite holder to

	// sound constants
	private const string WEAPON_BG = "PickAWeaponBg";							// background sound of weapon pick screen
	private const string WEAPON_CROWD = "PickAWeaponCrowd";						// background crowd noise of weapon pick screen
	private const string WEAPON_PICKED = "PickAWeapon";							// sound played when weapon picked
	private const string REVEAL_STINGER = "PickAWeaponRevealSymbol";			// Name of the sound played when the big symbol is revealed.
	private const string REVEAL_OTHERS = "PickAWeaponRevealOthers";				// Colection name played when the other symbols are revealed.
	private const string PICK_ME_SOUND = "PickMeWeapon";						// Sound played when the weapon starts to shake for the pick me.
	private const string INTRO_VO = "DMPickAWeaponVO";							// Sound name played when the games starts.
	private const string SUMMARY_VO = "FreespinSummaryVOGladiator";				// Sound name played once the summary screen comes up for this game.
	private const string TRANSITION_SOUND = "Transition2FreespinGladiator";		// sound played when we go to free spins
	private const string CROWD_LOOP_SOUND = "CrowdLoopFreespinGladiator";		// looping crowd sound played during free spins
	private const string INTRO_SPIN = "IntroFreespinGladiator";					// intro to free spins
	private const string SPIN_VO = "FreespinIntroVOGladiator";					// intro VO to free spins
	private const string SPIN_BG = "FreespinGladiator";							// free spin bg music

	// take care of any setup required for reveals/transition
	protected override void setupStage()
	{
		Audio.play(WEAPON_BG);
		Audio.play (WEAPON_CROWD);

		spriteNames.Add("ee04_M1_1x1");
		spriteNames.Add("ee04_M2_1x1");
		spriteNames.Add("ee04_M3_1x1");
		spriteNames.Add("ee04_M4_1x1");

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

		megaBannerSymbols[revealSpriteIndex].SetActive(true);

		// Check and make sure that we got into one of the normal stage.
		if (revealSpriteIndex != -1)
		{
			Audio.play(INTRO_VO);
			setupAnimationComponents();
		}
		else // If these wasn't an image to load we should just jump right into the freespins game.
		{
			inputEnabled = false;
			StartCoroutine(transitionIntoStage2());
		}


		Color[] colors = new Color[2];
		colors[0] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		colors[1] = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		foreach (UISprite glow in glowSprites) // oscillate glow on all weapons
		{
			CommonEffects.addOscillateSpriteColorEffect(glow, colors, 1.5f);
		}
	}

	// do pickme animation
	protected override IEnumerator pickMeCallback()
	{
		// Get one of the available weapon game objects
		int randomKnockerIndex = 0;
		
		randomKnockerIndex = Random.Range(0, buttonSelections.Count);
		GameObject randomWeapon = buttonSelections[randomKnockerIndex];

		Audio.play(PICK_ME_SOUND);
		iTween.PunchRotation(randomWeapon, rotationAmount, rotationTime);
		yield return new TIWaitForSeconds(rotationTime);
	}

	// handle weapon being picked
	protected override IEnumerator knockerClickedCoroutine(GameObject clickedKnocker)
	{
		yield return null;
		pickWeaponText.SetActive(false);
		Audio.play(WEAPON_PICKED);
		iTween.ScaleTo(clickedKnocker, iTween.Hash("scale", Vector3.zero, "time", WEAPON_SHRINK_TIME, "easetype", iTween.EaseType.easeInCubic, "islocal", true));
		yield return new TIWaitForSeconds(WEAPON_SHRINK_TIME);

		int spriteIndex = buttonSelections.IndexOf(clickedKnocker);
		GameObject winningSpriteObject = revealSprites[spriteIndex];
		revealSprites.RemoveAt(spriteIndex);
		if (winningSpriteObject != null)
		{
				UISprite winningSprite = winningSpriteObject.GetComponent<UISprite>();
				if (winningSprite != null)
				{
					pickedSprite = winningSprite;
					winningSprite.spriteName = spriteNames[revealSpriteIndex];
				}
		}
		Vector3 localStarScale = revealStars[revealSpriteIndex].gameObject.transform.localScale;


		revealStars[revealSpriteIndex].gameObject.SetActive(true);
		revealStars[revealSpriteIndex].gameObject.transform.parent = revealHolders[spriteIndex].transform;
		revealStars[revealSpriteIndex].gameObject.gameObject.transform.localScale = localStarScale;
		revealStars[revealSpriteIndex].gameObject.gameObject.transform.localPosition = new Vector3(0.0f, STAR_Y_OFFSET, 0.0f);

		iTween.ScaleTo(revealHolders[spriteIndex].gameObject, iTween.Hash("scale", Vector3.one, "time", SPRITE_GROW_TIME, "easetype", iTween.EaseType.easeInCubic, "islocal", true));
		Audio.play(REVEAL_STINGER);
		yield return new TIWaitForSeconds(SPRITE_GROW_TIME);

		pickedStar = revealStars[revealSpriteIndex];
		pickedLabelWrapper = revealMegaTextWrapper[spriteIndex];

		buttonSelections.RemoveAt(spriteIndex);
		spriteNames.RemoveAt(revealSpriteIndex);
		revealHolders.RemoveAt(spriteIndex);
		revealStars.RemoveAt(revealSpriteIndex);
		revealMegaTextWrapper.RemoveAt(spriteIndex);

		yield return StartCoroutine(revealOthers());

		StartCoroutine(fadeEverythingToBlack());

		yield return new TIWaitForSeconds(0.15f);
		background.Play("ee04 Gladiator Pick A Weapon_BKG set Transition Animation");
		Audio.play(TRANSITION_SOUND);
		Audio.play(INTRO_SPIN);
		yield return new TIWaitForSeconds(SPRITE_ZOOM_WAIT_TIME);

		Audio.play(CROWD_LOOP_SOUND, 1.0f, 0.0f, CROWD_LOOP_DELAY, 0.0f);
		yield return new TIWaitForSeconds(ANIM_WAIT_TIME);

		// Transition into the freespins game
		yield return StartCoroutine(transitionIntoStage2());

		Audio.play(SPIN_VO, 1.0f, 0.0f, SPIN_VO_DELAY, 0.0f);
		Audio.play(SPIN_BG, 1.0f, 0.0f, SPIN_BG_DELAY);
	}

	// fade all of our sprites/labels to black so the transition looks correct
	private IEnumerator fadeEverythingToBlack()
	{
		iTween.ValueTo(this.gameObject, iTween.Hash("from", 0.5f, "to", 0.0f, "time", 1.0f, "onupdate", "updateGrayFade")); // some things are already gray
		iTween.ValueTo(this.gameObject, iTween.Hash("from", 1.0f, "to", 0.0f, "time", 1.0f, "onupdate", "updateWhiteFade")); // some things are white

		yield return new TIWaitForSeconds(1.0f);
		Destroy (spriteHolder);
	}

	// update the things that started out white
	private void updateWhiteFade(float value)
	{
		pickedStar.color = new Color(value, value, value, 1.0f);
		pickedSprite.color = new Color(value, value, value, 1.0f);
		pickedLabelWrapper.color = new Color(value, value, value, 1.0f);
	}

	// update the things that started out gray
	private void updateGrayFade(float value)
	{
		foreach (UISprite sprite in revealedSprites)
		{
			sprite.color = new Color(value, value, value, 1.0f);
		}
		foreach (LabelWrapper label in revealedLabelsWrapper)
		{
			label.color = new Color(value, value, value, 1.0f);
		}
	}

	// reveal the rest of the weapons
	private IEnumerator revealOthers()
	{
		while (revealSprites.Count > 0)
		{
			int spriteIndex = 0;
			GameObject clickedKnocker = buttonSelections[spriteIndex].gameObject;
			iTween.ScaleTo(clickedKnocker, iTween.Hash("scale", Vector3.zero, "time", WEAPON_SHRINK_TIME, "easetype", iTween.EaseType.easeInCubic, "islocal", true));
			yield return new TIWaitForSeconds(WEAPON_SHRINK_TIME);
			Audio.play(REVEAL_OTHERS);

			GameObject revealedSpriteObject = revealSprites[spriteIndex];
			revealSprites.RemoveAt(spriteIndex);
			if (revealedSpriteObject != null)
			{
					UISprite revealedSprite = revealedSpriteObject.GetComponent<UISprite>();
					if (revealedSprite != null)
					{
						revealedSprites.Add(revealedSprite);
						revealedSprite.spriteName = spriteNames[spriteIndex];
						revealedSprite.color = Color.gray;
					}
			}

			Vector3 localStarScale = revealStars[spriteIndex].gameObject.transform.localScale;
			
			
			revealStars[spriteIndex].gameObject.SetActive(true);
			revealStars[spriteIndex].gameObject.transform.parent = revealHolders[spriteIndex].transform;
			revealStars[spriteIndex].gameObject.gameObject.transform.localScale = localStarScale;
			revealStars[spriteIndex].gameObject.gameObject.transform.localPosition = new Vector3(0.0f, STAR_Y_OFFSET, 0.0f);
			revealStars[spriteIndex].color = Color.gray;

			revealMegaTextWrapper[spriteIndex].color = Color.gray;

			iTween.ScaleTo(revealHolders[spriteIndex].gameObject, iTween.Hash("scale", Vector3.one, "time", SPRITE_GROW_TIME, "easetype", iTween.EaseType.easeInCubic, "islocal", true));
			yield return new TIWaitForSeconds(SPRITE_GROW_TIME);

			revealedSprites.Add(revealStars[spriteIndex]);
			revealedLabelsWrapper.Add(revealMegaTextWrapper[spriteIndex]);

			buttonSelections.RemoveAt(spriteIndex);
			spriteNames.RemoveAt(spriteIndex);
			revealHolders.RemoveAt(spriteIndex);
			revealStars.RemoveAt(spriteIndex);
			revealMegaTextWrapper.RemoveAt(spriteIndex);
		}
	}

	// play the summary sound and end the game
	protected override void gameEnded()
	{
		Audio.play(Audio.soundMap("prespin_idle_loop"));
		// Play the summary VO .6 seconds after the game has ended.
		Audio.play(SUMMARY_VO, 1.0f, 0.0f, 0.6f);
		base.gameEnded();
	}
}
