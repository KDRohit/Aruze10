using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/*
 * Free Spin class for ee03
 * Clone of gen06, so we basically do the same things as that class
 */ 
public class ee03FreeSpins : PickMajorFreeSpins 
{
	// inspector variables
	public Vector3 rotationAmount; // how much to rotate picks during pickme
	public float rotationTime; // how long to take to rotate picks
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
	public List<UISprite> brightEggs; // bright versions of eggs to show on top of other during pickme
	public List<GameObject> revealHolders; // parent game object of reveals (for attaching stars)
	public GameObject spriteHolder; // parent game object of all sprites (scale it up for zoom in)
	public List<GameObject> megaBannerSymbols; // list of mega symbols (activate 1 to show up in banner)
	public GameObject pickWeaponText; // banner at top during pick weapon phase
	public GameObject explosion; // egg explosion
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
	


	private List<string> animationNames = new List<string>();
	private List<string> spriteNames = new List<string>(); // sprite names in correct order
	
	// timing constants
	private const float TIME_SCALE_FS_TEXT = 0.5f;								// How long to move the stage 2 text.
	private const float TIME_TO_SHOW_FS_TEXT = 2.5f;							// After the text is moved how long to hold it there.
	private const float INTRO_SPIN_DELAY = 0.5f;								// how long to wait before doing intro spin sound
	private const float SPIN_VO_DELAY = 1.0f;									// how long to wait before doing spin_vo sound
	private const float SPIN_BG_DELAY = 4.029f;									// how long to wait before playing spin bg music
	private const float SMALL_MUTATE_WAIT = 0.75f;								// how long to wait before doing small symbol mutations
	private const float BIG_MUTATE_WAIT = 1.5f;									// how long to wait after doing large symbol mutations
	private const float ALL_MUTATIONS_WAIT = 1.75f;								// how long to wait before continuing (start timer when we start doing mutations)
	
	// constants
	private const float STAR_Y_OFFSET = -148.0f;								// how far down to place revealed star
	private const float SPRITE_HOLDER_GROWTO_SIZE = 3.0f;						// how big to grow the sprite holder to
	
	// sound constants
	private const string EGG_BG = "PickAnEggBg";							// background sound of egg pick screen
	private const string REVEAL_STINGER = "PickAnEggRevealSymbol";			// Name of the sound played when the big symbol is revealed.
	private const string REVEAL_OTHERS = "PickAnEggRevealOthers";				// Colection name played when the other symbols are revealed.
	private const string PICK_ME_SOUND = "PickMeEgg";						// Sound played when the weapon starts to shake for the pick me.
	private const string INTRO_VO = "PortalVODragon";							// Sound name played when the games starts.
	private const string SUMMARY_VO = "FreespinSummaryVODragon";				// Sound name played once the summary screen comes up for this game.
	private const string INTRO_SPIN = "IntroFreeSpinDragon";					// intro to free spins
	private const string SPIN_VO = "FreespinIntroVODragon";					// intro VO to free spins
	private const string SPIN_BG = "FreespinDragon";							// free spin bg music
	
	private ReadOnlyCollection<SlotOutcome> subOutcomes;
	
	// take care of any setup required for reveals/transition
	protected override void setupStage()
	{
		Audio.switchMusicKeyImmediate(EGG_BG);
		spriteNames.Add("EE03_M1_Fire");
		animationNames.Add("ee03_DragonsBounty_FreeSpins_BannerM1Animation");
		spriteNames.Add("EE03_M2_Ice");
		animationNames.Add("ee03_DragonsBounty_FreeSpins_BannerM2Animation");
		spriteNames.Add("EE03_M3_Guy");
		animationNames.Add("ee03_DragonsBounty_FreeSpins_BannerM3Animation");
		spriteNames.Add("EE03_M4_Girl");
		animationNames.Add("ee03_DragonsBounty_FreeSpins_BannerM4Animation");
		
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
	}
	
	// when the reels stop, setup a couple of things and the launch the WD splitting co-routine 
	protected override void reelsStoppedCallback ()
	{
		_outcomeDisplayController.setupPaytable(_outcome);
		subOutcomes = _outcome.getSubOutcomesReadOnly();
		
		StartCoroutine(splitOffscreenWilds());
	}
	
	// split up WD symbols that are part of a win but not wholly on screen
	// Then start the mutations
	private IEnumerator splitOffscreenWilds()
	{
		bool foundMutations = false;		
		HashSet<SlotSymbol> symbolsAnimated = new HashSet<SlotSymbol>();
		SlotReel[] reelArray = engine.getReelArray();

		foreach (SlotOutcome outcome in subOutcomes) // we only care about things that are part of a win
		{
			if (outcome.getPayLine() == null || outcome.getPayLine() == "") // this happens for bonus outcomes
			{
				continue;
			}
			
			Payline line = Payline.find(outcome.getPayLine());
			int[] winReels = _outcomeDisplayController.getWinningReels(outcome);
			for (int i = 0; winReels != null && i < winReels.Length; i++)
			{
				int reelIndex = winReels[i];
				SlotReel reel = reelArray[reelIndex];
				int symbolIndex = line.positions[reelIndex];
				
				SlotSymbol symbol = reel.visibleSymbols[reel.visibleSymbols.Length - 1 - symbolIndex];
				
				if (symbolsAnimated.Contains(symbol)) // don't split a symbol twice
				{
					continue;
				}
				symbolsAnimated.Add(symbol);
				
				if (symbol.name.Contains("WD") && !symbol.isWhollyOnScreen)
				{
					foundMutations = true;
					symbol.splitSymbol();
				}
			}
		}
		
		if (foundMutations) // wait a few frames for the split to be obvious
		{
			yield return null;
			yield return null;
			yield return null;
		}
		
		// after splitting symbols, now we can do the mutations
		doSpecialWildMutations();
	}
	
	// mutate all WDs (ice) to their fire counterparts
	private void doSpecialWildMutations ()
	{
		bool foundMutations = false;		
		HashSet<SlotSymbol> symbolsAnimated = new HashSet<SlotSymbol>();
		SlotReel[] reelArray = engine.getReelArray();

		foreach (SlotOutcome outcome in subOutcomes)
		{
			if (outcome.getPayLine() == null || outcome.getPayLine() == "") // this happens for bonus outcomes
			{
				continue;
			}
			Payline line = Payline.find(outcome.getPayLine());
			int[] winReels = _outcomeDisplayController.getWinningReels(outcome);
			for (int i = 0; winReels != null && i < winReels.Length; i++)
			{
				int reelIndex = winReels[i];
				SlotReel reel = reelArray[reelIndex];
				int symbolIndex = line.positions[reelIndex];
				
				SlotSymbol symbol = reel.visibleSymbols[reel.visibleSymbols.Length - 1 - symbolIndex];
				
				if (symbolsAnimated.Contains(symbol))
				{
					continue;
				}
				symbolsAnimated.Add(symbol);
				
				if (symbol.name.Contains("WD-2A-2A"))
				{
					foundMutations = true;
					StartCoroutine(playAnimThenMutate(symbol,"WD_FIRE-2A-2A"));
				}
				else if (symbol.name.Contains("WD-4A-3A"))
				{
					foundMutations = true;
					StartCoroutine(playAnimThenMutate(symbol,"WD_FIRE-4A-3A"));
				}
				else if(symbol.name == "WD")
				{
					foundMutations = true;
					StartCoroutine(delayThenMutateSmallSymbol(symbol));
				}
			}
		}
		if (foundMutations)
		{
			StartCoroutine(waitForMutationsThenCallback());
		}
		else
		{
			base.reelsStoppedCallback ();
		}
	}
	
	// wait a tiny bit before mutating the small symbols (so they line up with the big symbol mutations)
	private IEnumerator delayThenMutateSmallSymbol(SlotSymbol symbol)
	{
		yield return new TIWaitForSeconds(SMALL_MUTATE_WAIT);		
		symbol.mutateTo(symbol.name + "_FIRE");
	}
	
	// play the outcome animation for the big symbol, then mutate it to the fire version
	private IEnumerator playAnimThenMutate(SlotSymbol symbol, string mutateTo)
	{
		symbol.animator.playOutcome(symbol);
		
		yield return new TIWaitForSeconds(BIG_MUTATE_WAIT);
		symbol.mutateTo(mutateTo);
	}
	
	// wait for mutations to happen, then continue on with outcome displaying
	private IEnumerator waitForMutationsThenCallback()
	{
		yield return new TIWaitForSeconds(ALL_MUTATIONS_WAIT);
		base.reelsStoppedCallback ();
	}
	
	// do pickme animation
	protected override IEnumerator pickMeCallback()
	{
		// Get one of the available weapon game objects
		int randomKnockerIndex = 0;
		
		randomKnockerIndex = Random.Range(0, buttonSelections.Count);
		GameObject randomWeapon = buttonSelections[randomKnockerIndex];
		Color[] colors = new Color[2];
		colors[0] = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		colors[1] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		CommonEffects.addOscillateSpriteColorEffect(brightEggs[randomKnockerIndex], colors, 0.5f); // oscillate egg on alpha value to get it to show up on top

		
		Audio.play(PICK_ME_SOUND);
		iTween.PunchRotation(randomWeapon, rotationAmount, rotationTime);
		yield return new TIWaitForSeconds(rotationTime);

		Destroy(brightEggs[randomKnockerIndex].gameObject.GetComponent<OscillateSpriteColor>());
	}
	
	// handle weapon being picked
	protected override IEnumerator knockerClickedCoroutine(GameObject clickedKnocker)
	{
		yield return null;
		pickWeaponText.SetActive(false);
		
		Audio.play(REVEAL_STINGER);
		//Audio.play(WEAPON_PICKED);

		explosion.transform.position = clickedKnocker.transform.position;
		explosion.SetActive(true);
		yield return new TIWaitForSeconds(0.4f);

		int spriteIndex = buttonSelections.IndexOf(clickedKnocker);

		GameObject winningSpriteObject = revealSprites[spriteIndex];
		revealSprites.RemoveAt(spriteIndex);
		if (winningSpriteObject != null)
		{
				UISprite winningSprite = winningSpriteObject.GetComponent<UISprite>();
				if (winningSprite != null)
				{
					winningSprite.spriteName = spriteNames[revealSpriteIndex];
				}
		}
		Vector3 localStarScale = revealStars[revealSpriteIndex].gameObject.transform.localScale;
		
		
		revealStars[revealSpriteIndex].gameObject.SetActive(true);
		revealStars[revealSpriteIndex].gameObject.transform.parent = revealHolders[spriteIndex].transform;
		revealStars[revealSpriteIndex].gameObject.gameObject.transform.localScale = localStarScale;
		revealStars[revealSpriteIndex].gameObject.gameObject.transform.localPosition = new Vector3(0.0f, STAR_Y_OFFSET, 0.0f);

		revealHolders[spriteIndex].gameObject.SetActive(true);
		clickedKnocker.SetActive(false);
		yield return new TIWaitForSeconds(0.6f);
		Destroy (explosion);
		
		buttonSelections.RemoveAt(spriteIndex);
		spriteNames.RemoveAt(revealSpriteIndex);
		revealHolders.RemoveAt(spriteIndex);
		revealStars.RemoveAt(revealSpriteIndex);
		revealMegaTextWrapper.RemoveAt(spriteIndex);
		
		yield return StartCoroutine(revealOthers());

		// Transition into the freespins game
		yield return StartCoroutine(transitionIntoStage2());

		
		banner.GetComponent<Animator>().Play(animationNames[revealSpriteIndex]);
		Audio.play(INTRO_SPIN, 1.0f, 0.0f, INTRO_SPIN_DELAY, 0.0f);
		Audio.play(SPIN_VO, 1.0f, 0.0f, SPIN_VO_DELAY, 0.0f);
		Audio.play(SPIN_BG, 1.0f, 0.0f, SPIN_BG_DELAY);
	}

	// reveal the rest of the weapons
	private IEnumerator revealOthers()
	{
		while (revealSprites.Count > 0)
		{
			yield return new TIWaitForSeconds(1.0f);

			int spriteIndex = 0;
			GameObject clickedKnocker = buttonSelections[spriteIndex].gameObject;

			clickedKnocker.SetActive(false);
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

			Destroy(revealMegaTextWrapper[spriteIndex].gameObject);
			
			revealHolders[spriteIndex].gameObject.SetActive(true);
			
			revealedSprites.Add(revealStars[spriteIndex]);
			revealedLabelsWrapper.Add(revealMegaTextWrapper[spriteIndex]);
			
			buttonSelections.RemoveAt(spriteIndex);
			spriteNames.RemoveAt(spriteIndex);
			revealHolders.RemoveAt(spriteIndex);
			revealStars.RemoveAt(spriteIndex);
			revealMegaTextWrapper.RemoveAt(spriteIndex);
		}

		yield return new TIWaitForSeconds(1.0f);
	}
	
	// play the summary sound and end the game
	protected override void gameEnded()
	{
		// Play the summary VO .6 seconds after the game has ended.
		Audio.play(SUMMARY_VO, 1.0f, 0.0f, 0.6f);
		base.gameEnded();
	}
}
