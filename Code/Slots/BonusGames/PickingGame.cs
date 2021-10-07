using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Helper class to allow for a 2-dimensional array for roundButtonList in PickingGame
*/
public enum PickGameButtonDataFlags
{
	All = 0,
	Number = 1,
	GrayNumber = 2,
	Multiplier = 4,
	Extra = 8
}
	
[System.Serializable]
public class PickGameButtonDataList
{
	public GameObject[] goList;               // Game object that contains the button, animator, reveal number, etc.
	public GameObject[] buttonList;				// The button container where our box collider/button message resides.
	public Animator[] animatorList;				// The animator reference is often needed to be acquired from the button. We can store the reference here instead of finding it in-game.
	public Animation[] animationList;			// In case there are animaitons instead of animators
	public UILabel[] revealNumberList;			// Some pick buttons have a revealed number that needs to get modified. If it has a list of those, store the refrences here.
	public LabelWrapperComponent[] revealNumberWrapperList;
	public LabelWrapperComponent[] revealNumberOutlineWrapperList;
	public UILabel[] revealNumberOutlineList;	// And SOMETIMES they put the outline on a separate label. I'd argue this more, but visually, it does cause less of the breaking around the font, so it does do something different than the default outline.
	public UILabel[] revealGrayNumberList;			// Some pick buttons have a revealed number that needs to get modified. If it has a list of those, store the refrences here.
	public UILabel[] revealGrayNumberOutlineList;
	public LabelWrapperComponent[] revealGrayNumberWrapperList;
	public UILabel[] multiplierLabelList;        // The multiplier may be a separate label with its own font style.
	public UILabel[] multiplierOutlineLabelList; // Sometimes the multiplier has a separate label for the outline.
	public UILabel[] extraLabelList;             // A separate label for extra bonus.
	public UILabel[] extraOutlineLabelList;      // Outline for extra bonus.
	public GameObject[] extraGoList;			// Extra game objects for misc purposes.
	public UISprite[] imageRevealList;			// Some pick buttons have an image reveal that might need to be grayed out. Store the references here.
	public UISprite[][] multipleImageReveals;	// Need to support multiple revealed images for some games
	public Material[] materialList;				// If we can't do standard reveals using NGUI, stored the materials here for modifying to gray. Remember each item will need its own material.
	public string[] pickMeSoundNameList;        // Play this sound on the pick me animation.
	public UILabelStyle grayedOutStyle;			// If the game needs to gray out its texts, store the label style needed here.
	public UILabelStyle grayedOutlineStyle;   	// Gray out the outline, too.
	public UILabelStyle secondaryGrayOutStyle;	// If a game needs two variants of grayed out styles, store the second one here.
	public Color grayedOutLabelWrapperColor = Color.gray;	// Color to turn the label wrappers when they go gray
	public Color grayedOutOutlineLabelWrapperColor = Color.gray; // Color to turn the outline label wrappers when they go gray
	
	public MeshRenderer[][] glowList;             // List of glows that we can turn on when it's your turn to pick.
	public MeshRenderer[][] glowShadowList;       // List of glow shadows that we can turn OFF hwen it's your turn to pick.
	
	private void setLabelText(UILabel[] lbl, int index, string txt)
	{
		if(index < lbl.Length && lbl[index] != null)
		{
			lbl[index].text = txt;
		}
	}

	private void setLabelWrapperText(LabelWrapperComponent[] lbl, int index, string txt)
	{
		if (index < lbl.Length && lbl[index] != null)
		{
			lbl[index].text = txt;
		}
	}


	private void grayOutLabelWrapper(LabelWrapperComponent[] wrapperList, int index, Color grayColor)
	{
		if (index >= 0 && wrapperList.Length > 0 && index < wrapperList.Length && wrapperList[index] != null)
		{
			wrapperList[index].color = grayColor;
		}
	}
	
	private void grayOutLabel(UILabel[] lbl, int index, UILabelStyle styler)
	{
		if(styler != null && index < lbl.Length && lbl[index] != null)
		{
			UILabelStyler curStyler = lbl[index].gameObject.GetComponent<UILabelStyler>();
			if(curStyler != null)
			{
				curStyler.style = styler;
				curStyler.updateStyle();
			}
		}
	}
	
	public void setText(string txt, int index, PickGameButtonDataFlags flags = PickGameButtonDataFlags.All)
	{
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.Number) != 0)
		{
			setLabelText(revealNumberList, index, txt);
			setLabelText(revealNumberOutlineList, index, txt);

			setLabelWrapperText(revealNumberWrapperList, index, txt);
		}
		
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.GrayNumber) != 0)
		{
			setLabelText(revealGrayNumberList, index, txt);
			setLabelText(revealGrayNumberOutlineList, index, txt);
		}
		
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.Multiplier) != 0)
		{
			setLabelText(multiplierLabelList, index, txt);
			setLabelText(multiplierOutlineLabelList, index, txt);
		}
		
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.Extra) != 0)
		{
			setLabelText(extraLabelList, index, txt);
			setLabelText(extraOutlineLabelList, index, txt);
		}
	}
	
	public void grayOutText(int index, PickGameButtonDataFlags flags = PickGameButtonDataFlags.All)
	{
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.Number) != 0)
		{
			grayOutLabel(revealNumberList, index, grayedOutStyle);
			grayOutLabel(revealNumberOutlineList, index, grayedOutlineStyle);

			grayOutLabelWrapper(revealNumberWrapperList, index, grayedOutLabelWrapperColor);
			grayOutLabelWrapper(revealNumberOutlineWrapperList, index, grayedOutOutlineLabelWrapperColor);
		}
		
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.GrayNumber) != 0)
		{
			grayOutLabel(revealGrayNumberList, index, grayedOutStyle);
			grayOutLabel(revealGrayNumberOutlineList, index, grayedOutlineStyle);
		}
		
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.Multiplier) != 0)
		{
			grayOutLabel(multiplierLabelList, index, grayedOutStyle);
			grayOutLabel(multiplierOutlineLabelList, index, grayedOutlineStyle);
		}
		
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.Extra) != 0)
		{
			grayOutLabel(extraLabelList, index, grayedOutStyle);
			grayOutLabel(extraOutlineLabelList, index, grayedOutlineStyle);
		}
	}
	
	public void setNumberText(string txt, int index)
	{
		setText(txt, index, PickGameButtonDataFlags.Number);
	}
	
	public void setGrayNumberText(string txt, int index)
	{
		setText(txt, index, PickGameButtonDataFlags.GrayNumber);
	}
	
	public void setMultiplierText(string txt, int index)
	{
		setText(txt, index, PickGameButtonDataFlags.Multiplier);
	}
	
	public void setExtraText(string txt, int index)
	{
		setText(txt, index, PickGameButtonDataFlags.Extra);
	}
	
	public void grayOutNumberText(int index)
	{
		grayOutText(index, PickGameButtonDataFlags.Number);
	}
	
	public void grayOutGrayNumberText(int index)
	{
		grayOutText(index, PickGameButtonDataFlags.GrayNumber);
	}
	
	public void grayOutMultiplierText(int index)
	{
		grayOutText(index, PickGameButtonDataFlags.Multiplier);
	}
	
	public void grayOutExtraText(int index)
	{
		grayOutText(index, PickGameButtonDataFlags.Extra);
	}
	
	public void grayOutImage(int index, Color color)
	{
		if(index < imageRevealList.Length && imageRevealList[index] != null)
		{
			imageRevealList[index].color = color;
		}
	}
	
	public void grayOutImage(int index)
	{
		grayOutImage(index, Color.gray);
	}
}

//  This class was at one point intended to be temporary, but it is now deeply entrenched in quite a few games.
//  At some point we need to revisit this design and streamline things further.
public class PickGameButtonData
{
	public GameObject go;
	public GameObject button;                  // The button is the part that has the collider and the UI button message.
	
	public Animator animator;
	public Animation animation;
	
	public UILabel revealNumberLabel;
	public LabelWrapperComponent revealNumberWrapper;
	public LabelWrapperComponent revealNumberOutlineWrapper;
	public UILabel revealNumberOutlineLabel;
	public UILabel revealGrayNumberLabel;
	public UILabel revealGrayNumberOutlineLabel;
	public LabelWrapperComponent revealGrayNumberWrapper;
	public UILabel multiplierLabel;
	public UILabel multiplierOutlineLabel;
	public UILabel extraLabel;
	public UILabel extraOutlineLabel;
	
	public GameObject extraGo;
	
	public UISprite imageReveal;
	public UISprite[] multipleImageReveals;
	public Material material;
	public string pickMeSoundName;
	
	public MeshRenderer[] glowList;
	public MeshRenderer[] glowShadowList;
	
	private void setLabelText(UILabel lbl, string txt)
	{
		if (lbl != null)
		{
			lbl.text = txt;
		}
	}

	private void setLabelWrapperText(LabelWrapperComponent lbl, string txt)
	{
		if (lbl != null)
		{
			lbl.text = txt;
		}
	}
	
	public void setText(string txt, PickGameButtonDataFlags flags = PickGameButtonDataFlags.All)
	{
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.Number) != 0)
		{
			setLabelText(revealNumberLabel, txt);
			setLabelText(revealNumberOutlineLabel, txt);
			setLabelWrapperText(revealNumberWrapper, txt);
			setLabelWrapperText(revealNumberOutlineWrapper, txt);
		}
		
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.GrayNumber) != 0)
		{
			setLabelText(revealGrayNumberLabel, txt);
			setLabelText(revealGrayNumberOutlineLabel, txt);
			setLabelWrapperText(revealGrayNumberWrapper, txt);
		}
		
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.Multiplier) != 0)
		{
			setLabelText(multiplierLabel, txt);
			setLabelText(multiplierOutlineLabel, txt);
		}
		
		if(flags == PickGameButtonDataFlags.All || (flags & PickGameButtonDataFlags.Extra) != 0)
		{
			setLabelText(extraLabel, txt);
			setLabelText(extraOutlineLabel, txt);
		}
	}
	
	public void setNumberText(string txt)
	{
		setText(txt, PickGameButtonDataFlags.Number);
	}
	
	public void setGrayNumberText(string txt)
	{
		setText(txt, PickGameButtonDataFlags.GrayNumber);
	}
	
	public void setMultiplierText(string txt)
	{
		setText(txt, PickGameButtonDataFlags.Multiplier);
	}
	
	public void setExtraText(string txt)
	{
		setText(txt, PickGameButtonDataFlags.Extra);
	}

	public void grayOutImage(Color color)
	{
		if(imageReveal != null)
		{
			imageReveal.color = color;
		}
	}

	public void grayOutImage()
	{
		grayOutImage(Color.gray);
	}
}

[System.Serializable]
public class NewPickGameButtonRound
{
	public GameObject[] pickGameObjects; // This is either a PickGameButton or a game object that contains a PickGameButton.
}

/**
Define types of PickingGame that can be serialized so we can set them in the insepctor
*/
public abstract class PickingGameUsingPickemOutcome : PickingGame<PickemOutcome> {}
public abstract class PickingGameUsingWheelOutcome : PickingGame<WheelOutcome> {}

/**
New base class for picking games, attempts to unifies variables and funcitonality that all picking games rely on
*/
public abstract class PickingGame<T> : ChallengeGame where T : BaseBonusGameOutcome
{
	[SerializeField] protected float minPickMeTime = 1.5f;							// Minimum time an animation might take to play next
	[SerializeField] protected float maxPickMeTime = 4.0f;							// Maximum time an animation might take to play next
	[SerializeField] protected float revealWaitTime = 0.2f;							// Time between reveals
	[SerializeField] protected UILabel currentWinAmountText = null;					// Sets the text box that will rollup with the score, can be changed in code if it changes between stages

	public UILabel[] currentWinAmountTexts = new UILabel[1];						// All possible win amounts for the given stages - To be removed when prefabs are updated.
	public LabelWrapperComponent[] currentWinAmountTextsWrapperComponent;

	public List<LabelWrapper> currentWinAmountTextsWrapper
	{
		get
		{
			if (_currentWinAmountTextsWrapper == null)
			{
				_currentWinAmountTextsWrapper = new List<LabelWrapper>();

				if (currentWinAmountTextsWrapperComponent != null && currentWinAmountTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in currentWinAmountTextsWrapperComponent)
					{
						_currentWinAmountTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in currentWinAmountTexts)
					{
						_currentWinAmountTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _currentWinAmountTextsWrapper;
		}
		set
		{
			_currentWinAmountTextsWrapper = value;
		}
	}
	private List<LabelWrapper> _currentWinAmountTextsWrapper = null;	


	//  Sets the text box that will rollup with the score, can be changed in code if it changes between stages
	[SerializeField] protected LabelWrapperComponent currentWinAmountTextWrapper = null;	// To be removed when prefabs are updated.

	// Sets the text box that will rollup with the score, can be changed in code if it changes between stages
	public LabelWrapper currentWinAmountTextWrapperNew	// Had to name this with "New" since the component version already existed with this name.
	{
		get
		{
			if (_currentWinAmountTextWrapperNew == null)
			{
				if (currentWinAmountTextWrapper != null)
				{
					_currentWinAmountTextWrapperNew = currentWinAmountTextWrapper.labelWrapper;
				}
				else
				{
					_currentWinAmountTextWrapperNew = new LabelWrapper(currentWinAmountText);
				}
			}
			return _currentWinAmountTextWrapperNew;
		}
		set
		{
			_currentWinAmountTextWrapperNew = value;
		}
	}
	private LabelWrapper _currentWinAmountTextWrapperNew = null;


	public LabelWrapperComponent[] currentWinAmountTextWrappers = new LabelWrapperComponent[1];							// All possible win amounts for the given stages
	protected long currentWinAmount = 0;

	[SerializeField] protected int currentNumPicks = -1;
	[SerializeField] protected UILabel currentNumPicksText = null;
	[SerializeField] protected LabelWrapperComponent currentNumPicksLabelWrapperComponent;
	[SerializeField] protected LabelWrapper currentNumPicksLabelWrapper
	{
		get
		{
			if (_currentNumPicksLabelWrapper == null)
			{
				if(currentNumPicksLabelWrapperComponent != null)
				{
					_currentNumPicksLabelWrapper = currentNumPicksLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_currentNumPicksLabelWrapper = new LabelWrapper(currentNumPicksText);
				}
			}
			return _currentNumPicksLabelWrapper;
		}
	}
	private LabelWrapper _currentNumPicksLabelWrapper;

	protected int numPicksSoFar = 0;                                                // Number of picks you've made so far (starting at 0 and counting up).

	[SerializeField] public UILabel currentMultiplierLabel = null;
	[SerializeField] public UILabel[] currentMultiplierLabels;						// To be removed when prefabs are updated.
	[SerializeField] public LabelWrapperComponent[] currentMultiplierLabelsWrapperComponent;	// All possible win amounts for the given stages

	public List<LabelWrapper> currentMultiplierLabelsWrapper
	{
		get
		{
			if (_currentMultiplierLabelsWrapper == null)
			{
				_currentMultiplierLabelsWrapper = new List<LabelWrapper>();

				if (currentMultiplierLabelsWrapperComponent != null && currentMultiplierLabelsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in currentMultiplierLabelsWrapperComponent)
					{
						_currentMultiplierLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in currentMultiplierLabels)
					{
						_currentMultiplierLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _currentMultiplierLabelsWrapper;
		}
	}
	private List<LabelWrapper> _currentMultiplierLabelsWrapper = null;	


	[HideInInspector] public long currentMultiplier = 0;
	
	public PickGameButtonDataList[] roundButtonList = new PickGameButtonDataList[1];	// Button lists for multiple rounds (visibility in editor controlled by isMultiRound)
	public NewPickGameButtonRound[] newPickGameButtonRounds = new NewPickGameButtonRound[1];
	
	public GameObject[] stageObjects = new GameObject[0];							// List of stages objects for toggling during stage changes

	// If you pick all the picks, then should the last pick default to the next round, or should it end the game?
	[SerializeField] protected bool shouldLastPickContinueToNextRound = true;
	[SerializeField] protected bool shouldLastPickEndGame = true;

	// Beginning section that contains game type specific objects that don't necessarily necessitate a whole new class for holding things yet.

	// Used in gem type games: bettie01, gen07
	public PickGameButtonDataList[] gemList = new PickGameButtonDataList[0];				// Need access to gameobjects and animators of the gems.
	public PickGameButtonDataList[] iconList = new PickGameButtonDataList[0];				// Need access to the icons that play animations after find

	public GameObject bonusSparkleTrail = null;												// A bunch of games needs a sparkle trail! Store that shit here mofo.
	public GameObject bonusSparkleExplosion = null;

	[SerializeField] protected GameObject[] jackpotWinEffects = null;						// Some games have effects that go off when you trigger a jackpot condition, stored by stage

	// End of section for game type specific objects

	[SerializeField] protected bool SHOULD_USE_ALTERNATE_CHALLENGE_OUTCOME;
	protected T outcome = null;															// Stores the outcome of the game based on the generic T (i.e. WheelOutcome or PickemOutcome)
	[HideInInspector] public int currentStage = 0;														// Allows us to key off of what stage we're currently on. 0-indexed atm, and we should always begin at the 1st stage object entry.
	[HideInInspector] public bool inputEnabled = true;													// Tells if the game buttons are currently accepting input
	[HideInInspector] public bool hasGameEnded = false;												// Bool in case the game needs to know if end conditions have been met yet.
	protected SkippableWait revealWait = new SkippableWait();							// Class for handling reveals that can be skipped
	protected List<List<GameObject>> pickmeButtonList = new List<List<GameObject>>();	// Track button objects that can still have pick me triggered on them

	protected CoroutineRepeater pickMeController;										// Class to call the pickme animation on a loop

	protected string pickMeAnimName = "pickme";
	protected string pickMeSoundName = "";
	protected string grayoutAnimName = "button_grayed";
	[SerializeField] protected int numPickMeAnims = 1;  // You can have more than one pick me animation (named "pickme", "pickme2", "pickme3", etc).
	
	private GameObjectCacher bonusSparkleTrailCacher = null;
	private GameObjectCacher bonusSparkleExplosionCacher = null;
	
	// Default sound keys.
	protected const string DEFAULT_BG_MUSIC_KEY = "bonus_bg";
	protected const string DEFAULT_INTRO_VO_KEY = "bonus_intro_vo";
	protected const string DEFAULT_SUMMARY_VO_KEY = "bonus_summary_vo";
	
	// Pick-me sound keys.
	protected const string DEFAULT_PICK_ME_SOUND_KEY = "pickem_pickme";
	
	// Player picked this button sound and VO keys.
	protected const string DEFAULT_REVEAL_WIN_SOUND_KEY = "pickem_reveal_win";
	protected const string DEFAULT_REVEAL_WIN_VO_KEY = "pickem_reveal_win_vo";
	protected const string DEFAULT_REVEAL_BAD_SOUND_KEY = "pickem_reveal_bad";
	protected const string DEFAULT_REVEAL_BAD_VO_KEY = "pickem_reveal_bad_vo";
	
	// Reveal remaining picks sound keys.
	protected const string DEFAULT_NOT_CHOSEN_SOUND_KEY = "reveal_not_chosen";
		
	// Other common sound keys.
	public const string DEFAULT_ADVANCE_MULTIPLIER_SOUND_KEY = "pickem_advance_multiplier";
	public const string DEFAULT_REACHED_MAX_MULTIPLIER_SOUND_KEY = "pickem_reached_max_multiplier";
	protected const string DEFAULT_MULTIPLIER_TRAVEL_SOUND_KEY = "pickem_multiplier_travel";
	protected const string DEFAULT_MULTIPLIER_ARRIVE_SOUND_KEY = "pickem_multiplier_arrive";
	
	public struct SparkleTrailParams
	{
		public GameObject startObject;
		public GameObject endObject;
		
		public float dur;
		public float holdDur;
		
		public bool shouldRotate; // rotate trail towards end object.
	};
	
	/// Init that can be called by something other than BonusGameManager, like a combo type bonus which will pass in the outcome
	public virtual void init(T passedOutcome)
	{
		outcome = passedOutcome;

		if (currentWinAmountTextWrapperNew != null)
		{
			currentWinAmountTextWrapperNew.text = "0";
		}
		
		currentMultiplier = 1;
		if (currentMultiplierLabel != null)
		{
			currentMultiplierLabel.text = Localize.text("{0}X", CommonText.formatNumber(currentMultiplier));
		}
		
		if (stageObjects.Length > 0)
		{
			for (int i = 0; i < stageObjects.Length; i++)
			{
				if (i == currentStage)
				{
					stageObjects[i].SetActive(true);
				}
				else
				{
					if (stageObjects[i] != null)
					{
						stageObjects[i].SetActive(false);
					}
				}
			}
		}
		
		foreach (LabelWrapper winAmount in currentWinAmountTextsWrapper)
		{
			if (winAmount != null)
			{
				winAmount.text = "0";
			}
		}

		foreach (LabelWrapperComponent winAmount in currentWinAmountTextWrappers)
		{
			if (winAmount != null)
			{
				winAmount.text = "0";
			}
		}
		
		pickMeSoundName = Audio.soundMap(DEFAULT_PICK_ME_SOUND_KEY);

		if (bonusSparkleTrail != null)
		{
			bonusSparkleTrailCacher = new GameObjectCacher(this.gameObject, bonusSparkleTrail);
		}
		
		if (bonusSparkleExplosion != null)
		{
			bonusSparkleExplosionCacher = new GameObjectCacher(this.gameObject, bonusSparkleExplosion);
		}

		// call derived init now that the outcome is set
		derivedInit();

		pickMeController = new CoroutineRepeater(minPickMeTime, maxPickMeTime, pickMeAnimCallback);

		convertNewPickGameButtonRounds();
		initializePickmeButtonList();
		
		_didInit = true;
	}

	/// Allows for a derived class to handle init, without fully overriding and having to duplicate code because of how _didInit is being set
	protected virtual void derivedInit()
	{
		// override in derived classes to handle init stuff, prefer using this over overriding init() directly
	}

	/// Init stuff for the game, derived classes SHOULD call base.init(); so the outcome is set and the pickme animation controller is setup
	public override void init() 
	{
		if (ReelGame.activeGame.outcome.isScatter)
		{
			init(BonusGameManager.instance.outcomes[BonusGameType.SCATTER] as T);
		}
		else
		{
			if (!SHOULD_USE_ALTERNATE_CHALLENGE_OUTCOME)
			{
				init(BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as T);
			}
			else
			{
				init(BonusGameManager.instance.outcomes[BonusGameType.CREDIT] as T);			
			}
		}
	}

	// Convert the NewPickGameButtonRounds into the old PickGameButtonDataList.
	private void convertNewPickGameButtonRounds()
	{
		if (newPickGameButtonRounds != null && newPickGameButtonRounds.Length > 0 && 
			newPickGameButtonRounds[0].pickGameObjects != null &&
			newPickGameButtonRounds[0].pickGameObjects.Length > 0 ||
			newPickGameButtonRounds.Length > 1)
		{
			roundButtonList = new PickGameButtonDataList[newPickGameButtonRounds.Length];	
				
			for (int round = 0; round < newPickGameButtonRounds.Length; round++)
			{
				NewPickGameButtonRound newPickGameButtonRound = newPickGameButtonRounds[round];
				
				roundButtonList[round] = new PickGameButtonDataList();
				roundButtonList[round].goList = new GameObject[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].buttonList = new GameObject[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].animatorList = new Animator[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].animationList = new Animation[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].revealNumberList = new UILabel[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].revealNumberWrapperList = new LabelWrapperComponent[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].revealNumberOutlineWrapperList = new LabelWrapperComponent[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].revealGrayNumberList = new UILabel[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].revealGrayNumberOutlineList = new UILabel[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].revealGrayNumberWrapperList = new LabelWrapperComponent[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].revealNumberOutlineList = new UILabel[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].multiplierLabelList = new UILabel[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].multiplierOutlineLabelList = new UILabel[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].extraLabelList = new UILabel[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].extraOutlineLabelList = new UILabel[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].extraGoList = new GameObject[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].imageRevealList = new UISprite[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].multipleImageReveals = new UISprite[newPickGameButtonRound.pickGameObjects.Length][];
				roundButtonList[round].materialList = new Material[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].pickMeSoundNameList = new string[newPickGameButtonRound.pickGameObjects.Length];
				roundButtonList[round].glowList = new MeshRenderer[newPickGameButtonRound.pickGameObjects.Length][];
				roundButtonList[round].glowShadowList = new MeshRenderer[newPickGameButtonRound.pickGameObjects.Length][];
				
				for (int index = 0; index < newPickGameButtonRound.pickGameObjects.Length; index++)
				{
					GameObject pickGo = newPickGameButtonRound.pickGameObjects[index];
					
					// Is this game object a pick game button?
					PickGameButton pickGameButton = pickGo.GetComponent<PickGameButton>();
					
					// If not, then does this game object contains the pick game button.
					if (pickGameButton == null)
					{
						// slight hack so we can grab the PickGameButton even if the object holding it is not active
						PickGameButton[] pickGameButtonArray = pickGo.GetComponentsInChildren<PickGameButton>(true);
						if (pickGameButtonArray.Length == 1)
						{
							pickGameButton = pickGameButtonArray[0];
						}
						else if (pickGameButtonArray.Length == 0)
						{
							Debug.LogError("Object called: " + pickGo.name + " didn't contain a PickGameButton!");
						}
						else if (pickGameButtonArray.Length > 1)
						{
							Debug.LogError("Object called: " + pickGo.name + " contained more than one PickGameButton!");
						}
					}
	
					if (pickGameButton.button.GetComponent<BoxCollider>() == null)
					{
						pickGameButton.button.AddComponent<BoxCollider>();
					}
	
					UIButtonMessage uiButtonMessage = pickGameButton.button.GetComponent<UIButtonMessage>();
	
					if (uiButtonMessage == null)
					{
						uiButtonMessage = pickGameButton.button.AddComponent<UIButtonMessage>();
					}

					if (uiButtonMessage.target == null)
					{
						uiButtonMessage.target = this.gameObject;
					}

					if (string.IsNullOrEmpty(uiButtonMessage.functionName))
					{
						uiButtonMessage.functionName = "pickemButtonPressed";
					}
				
					roundButtonList[round].goList[index] = pickGo;
					roundButtonList[round].buttonList[index] = pickGameButton.button;
					roundButtonList[round].animatorList[index] = pickGameButton.animator;
					roundButtonList[round].animationList[index] = pickGameButton.GetComponent<Animation>();
					roundButtonList[round].revealNumberList[index] = pickGameButton.revealNumberLabel;
					roundButtonList[round].revealNumberWrapperList[index] = pickGameButton.revealNumberWrapper;
					roundButtonList[round].revealNumberOutlineWrapperList[index] = pickGameButton.revealNumberOutlineLabelWrapperComponent;
					roundButtonList[round].revealGrayNumberList[index] = pickGameButton.revealGrayNumberLabel;
					roundButtonList[round].revealGrayNumberOutlineList[index] = pickGameButton.revealGrayNumberOutlineLabel;
					roundButtonList[round].revealGrayNumberWrapperList[index] = pickGameButton.revealGrayNumberWrapper;
					roundButtonList[round].revealNumberOutlineList[index] = pickGameButton.revealNumberOutlineLabel;
					roundButtonList[round].multiplierLabelList[index] = pickGameButton.multiplierLabel;
					roundButtonList[round].multiplierOutlineLabelList[index] = pickGameButton.multiplierOutlineLabel;
					roundButtonList[round].extraLabelList[index] = pickGameButton.extraLabel;
					roundButtonList[round].extraOutlineLabelList[index] = pickGameButton.extraOutlineLabel;
					roundButtonList[round].extraGoList[index] = pickGameButton.extraGo;
					roundButtonList[round].imageRevealList[index] = pickGameButton.imageReveal;
					roundButtonList[round].multipleImageReveals[index] = pickGameButton.multipleImageReveals;
					roundButtonList[round].materialList[index] = pickGameButton.material;
					roundButtonList[round].pickMeSoundNameList[index] = pickGameButton.pickMeSoundName;
					roundButtonList[round].glowList[index] = pickGameButton.glowList;
					roundButtonList[round].glowShadowList[index] = pickGameButton.glowShadowList;
				}
			}
		}
	}
			
	public void initializePickmeButtonList()
	{
		pickmeButtonList.Clear();

		foreach (PickGameButtonDataList roundData in roundButtonList)
		{
			List<GameObject> pickButtons = new List<GameObject>();
			pickmeButtonList.Add(pickButtons);
			int index = pickmeButtonList.IndexOf(pickButtons);
			for (int k = 0; k < roundData.buttonList.Length; ++k)
			{
				pickmeButtonList[index].Add(roundData.buttonList[k]);
			}
		}
	}

	// If the picking game has multiple rounds,
	// then continue to the next stage.
	public virtual void continueToNextStage()
	{
		switchToStage(currentStage + 1, false);
	}

	// Allow the round to be selected, some games may jump over certain rounds, or use rounds to represent game variations
	public virtual void switchToStage(int changeStageTo, bool keepShowingCurrentStage = true)
	{
		if (!keepShowingCurrentStage)
		{
			if (stageObjects[currentStage] != null)
			{
				stageObjects[currentStage].SetActive(false);
			}
		}

		currentStage = changeStageTo;
		stageObjects[currentStage].SetActive(true);
	}

	/// Gets the index from a supplied button in the round list
	public int getButtonIndex(GameObject button, int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
		if (round < roundButtonList.Length)
		{
			PickGameButtonDataList pickGameButtonList = roundButtonList[round];
			for (int k = 0; k < pickGameButtonList.buttonList.Length; k++)
			{
				if (button == pickGameButtonList.buttonList[k])
				{
					return k;
				}
			}
		}

		return -1;
	}

	/// We can use this method to determine if a button is selectable for pickme and traditional selection.
	/// This also means you better have your selectable buttons set in the pickme button list.
	public virtual bool isButtonAvailableToSelect(GameObject button)
	{
		for (int i = 0; i < pickmeButtonList.Count; ++i)
		{
			for (int k = 0; k < pickmeButtonList[i].Count; ++k)
			{
				if (button == pickmeButtonList[i][k])
				{
					return true;
				}

				PickGameButton pickGameButton = button.GetComponent<PickGameButton>();
				if( pickGameButton != null)
				{
					GameObject pickButtonObject = pickGameButton.button;
					if (pickButtonObject != null && pickmeButtonList[i][k] == pickButtonObject)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	public bool isButtonAvailableToSelect(PickGameButtonData pick)
	{
		return isButtonAvailableToSelect(pick.button);
	}
	
	/// Secondary way of seeing if a button is available to select!
	public bool isButtonAvailableToSelect(int index, int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
		
		GameObject button = getButtonUsingIndexAndRound(index, round);
		return isButtonAvailableToSelect(button);
	}

	public bool anyButtonsAvailableToSelect()
	{
		List<GameObject> pickmeButtons = pickmeButtonList[currentStage];

		if (pickmeButtons.Count > 0)
		{
			return true;
		}

		return false;
	}
	
	/// Check the round buttons and see the lengths!
	public int getButtonLengthInRound(int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
		
		if (round < roundButtonList.Length)
		{
			PickGameButtonDataList pickGameButtonList = roundButtonList[round];
			return (int)pickGameButtonList.buttonList.Length;
		}

		return 0;
	}

	public PickGameButtonData getPickGameButtonAndRemoveIt(GameObject button)
	{
		removeButtonFromSelectableList(button);

		int index = getButtonIndex(button);
		PickGameButtonData pick = getPickGameButton(index);
		return pick;
	}
	
	public PickGameButtonData getPickGameButton(int index, int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
		
		PickGameButtonData pickGameButton = null;
		if (round < roundButtonList.Length)
		{
			PickGameButtonDataList pickGameButtonList = roundButtonList[round];
			
			if (0 <= index && index < pickGameButtonList.buttonList.Length)
			{
				pickGameButton = new PickGameButtonData();
				
				if (pickGameButtonList.goList != null && index < pickGameButtonList.goList.Length )
				{
					pickGameButton.go = pickGameButtonList.goList[index];
				}
				
				pickGameButton.button = pickGameButtonList.buttonList[index];
				
				if (pickGameButtonList.animatorList != null && index < pickGameButtonList.animatorList.Length)
				{
					pickGameButton.animator = pickGameButtonList.animatorList[index];
				}

				if (pickGameButtonList.animationList != null && index < pickGameButtonList.animationList.Length)
				{
					pickGameButton.animation = pickGameButtonList.animationList[index];
				}
				
				if (pickGameButtonList.revealNumberList != null && index < pickGameButtonList.revealNumberList.Length)
				{
					pickGameButton.revealNumberLabel = pickGameButtonList.revealNumberList[index];
				}

				if (pickGameButtonList.revealNumberWrapperList != null && index < pickGameButtonList.revealNumberWrapperList.Length)
				{
					pickGameButton.revealNumberWrapper = pickGameButtonList.revealNumberWrapperList[index];
				}

				if (pickGameButtonList.revealNumberOutlineWrapperList != null && index < pickGameButtonList.revealNumberOutlineWrapperList.Length)
				{
					pickGameButton.revealNumberOutlineWrapper = pickGameButtonList.revealNumberOutlineWrapperList[index];
				}

				if (pickGameButtonList.revealGrayNumberList != null && index < pickGameButtonList.revealGrayNumberList.Length)
				{
					pickGameButton.revealGrayNumberLabel = pickGameButtonList.revealGrayNumberList[index];
				}
				
				if (pickGameButtonList.revealGrayNumberOutlineList != null && index < pickGameButtonList.revealGrayNumberOutlineList.Length)
				{
					pickGameButton.revealGrayNumberOutlineLabel = pickGameButtonList.revealGrayNumberOutlineList[index];
				}

				if (pickGameButtonList.revealGrayNumberWrapperList != null && index < pickGameButtonList.revealGrayNumberWrapperList.Length)
				{
					pickGameButton.revealGrayNumberWrapper = pickGameButtonList.revealGrayNumberWrapperList[index];
				}


				if (pickGameButtonList.revealNumberOutlineList != null && index < pickGameButtonList.revealNumberOutlineList.Length)
				{
					pickGameButton.revealNumberOutlineLabel = pickGameButtonList.revealNumberOutlineList[index];
				}
				
				if (pickGameButtonList.multiplierLabelList != null && index < pickGameButtonList.multiplierLabelList.Length)
				{
					pickGameButton.multiplierLabel = pickGameButtonList.multiplierLabelList[index];
				}
				
				if (pickGameButtonList.multiplierOutlineLabelList != null && index < pickGameButtonList.multiplierOutlineLabelList.Length)
				{
					pickGameButton.multiplierOutlineLabel = pickGameButtonList.multiplierOutlineLabelList[index];
				}
				
				if (pickGameButtonList.extraLabelList != null && index < pickGameButtonList.extraLabelList.Length)
				{
					pickGameButton.extraLabel = pickGameButtonList.extraLabelList[index];
				}
				
				if (pickGameButtonList.extraOutlineLabelList != null && index < pickGameButtonList.extraOutlineLabelList.Length)
				{
					pickGameButton.extraOutlineLabel = pickGameButtonList.extraOutlineLabelList[index];
				}
				
				if (pickGameButtonList.extraGoList != null && index < pickGameButtonList.extraGoList.Length)
				{
					pickGameButton.extraGo = pickGameButtonList.extraGoList[index];
				}
				
				if (pickGameButtonList.imageRevealList != null && index < pickGameButtonList.imageRevealList.Length)
				{
					pickGameButton.imageReveal = pickGameButtonList.imageRevealList[index];
				}

				if (pickGameButtonList.multipleImageReveals != null && index < pickGameButtonList.multipleImageReveals.Length)
				{
					pickGameButton.multipleImageReveals = pickGameButtonList.multipleImageReveals[index];
				}
				
				if (pickGameButtonList.materialList != null && index < pickGameButtonList.materialList.Length)
				{
					pickGameButton.material = pickGameButtonList.materialList[index];
				}
				
				if (pickGameButtonList.pickMeSoundNameList != null && index < pickGameButtonList.pickMeSoundNameList.Length)
				{
					pickGameButton.pickMeSoundName = pickGameButtonList.pickMeSoundNameList[index];
				}
				
				if (pickGameButtonList.glowList != null && index < pickGameButtonList.glowList.Length)
				{
					pickGameButton.glowList = pickGameButtonList.glowList[index];
				}
				
				if (pickGameButtonList.glowShadowList != null && index < pickGameButtonList.glowShadowList.Length)
				{
					pickGameButton.glowShadowList = pickGameButtonList.glowShadowList[index];
				}
			}
		}
		
		return pickGameButton;
	}

	/// Checks against the selectable list and removes it in the pickmes, if necessary.
	/// Note, if you want to get the next pick game button and remove that,
	/// then use removeNextPickGameButton.
	public GameObject grabNextButtonAndRemoveIt(int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
		
		if (round < roundButtonList.Length)
		{
			List<GameObject> pickmeButtons = pickmeButtonList[round];

			if (pickmeButtons.Count > 0)
			{
				GameObject pickMeFound = pickmeButtons[0];
				pickmeButtonList[round].Remove(pickMeFound);
				return pickMeFound;
			}
		}

		return null;
	}

	public int getNumPickMes(int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
	
		int numPickMes = 0;
		
		if (round < pickmeButtonList.Count)
		{
			List<GameObject> pickmeButtons = pickmeButtonList[round];
			numPickMes = pickmeButtons.Count;
		}
		
		return numPickMes;
	}
	
	public PickGameButtonData removeNextPickGameButton(int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
		
		GameObject button = grabNextButtonAndRemoveIt(round);
		int index = getButtonIndex(button);
		PickGameButtonData pick = getPickGameButton(index);
		
		return pick;
	}

	/// Checks against the selectable list and removes it.
	public void removeButtonFromSelectableList(GameObject button)
	{
		for (int i = 0; i < pickmeButtonList.Count; ++i)
		{
			for (int k = 0; k < pickmeButtonList[i].Count; ++k)
			{
				if (button == pickmeButtonList[i][k])
				{
					pickmeButtonList[i].Remove(button);
				}
			}
		}
	}

	public void removePickGameButtonFromSelectableList(PickGameButton pick)
	{
		removeButtonFromSelectableList(pick.button);
	}
	
	/// Our rounds technically start at one in this scenario.
	public GameObject getButtonUsingIndexAndRound(int index, int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
		
		if (round < roundButtonList.Length)
		{
			PickGameButtonDataList pickGameButtonList = roundButtonList[round];
			if (index < pickGameButtonList.buttonList.Length)
			{
				return pickGameButtonList.buttonList[index];
			}
		}

		return null;
	}

	/// Our rounds technically start at one in this scenario.
	public Animator getAnimatorUsingIndexAndRound(int index, int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
		
		if (round < roundButtonList.Length)
		{
			PickGameButtonDataList pickGameButtonList = roundButtonList[round];
			if (index < pickGameButtonList.animatorList.Length)
			{
				return pickGameButtonList.animatorList[index];
			}
		}

		return null;
	}

	public void updateRevealText(int index, int round, string newText)
	{
		if (round < roundButtonList.Length)
		{
			roundButtonList[round].setNumberText(newText, index);
		}
	}

	public void updateExtraText(int index, int round, string newText)
	{
		if (round < roundButtonList.Length)
		{
			roundButtonList[round].setExtraText(newText, index);
		}
	}

	public void updateMultiplierText(int index, int round, string newText)
	{
		if (round < roundButtonList.Length)
		{
			roundButtonList[round].setMultiplierText(newText, index);
		}
	}

	// If we have the gray styler and either of the reveal labels, let's update it for reveals
	public void grayOutRevealText(int index, int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
		
		if (round < roundButtonList.Length)
		{
			roundButtonList[round].grayOutText(index);
		}
	}

	public void grayOutEndSprite(int index, int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}

		if (round < roundButtonList.Length)
		{
			roundButtonList[round].grayOutImage(index);
		}
	}

	/// Animate the score rolling up
	protected virtual IEnumerator animateScore(long startScore, long endScore)
	{
		if (currentWinAmountTextWrapperNew != null)
		{
			yield return StartCoroutine(SlotUtils.rollup(startScore, endScore, currentWinAmountTextWrapperNew));
			// Introduced a slight delay here so the click of the button doesn't immediately force the rollup to stop.
			yield return new WaitForSeconds(0.1f);
		}
		else
		{
			yield break;
		}
	}

	public IEnumerator addCredits(long credits)
	{
		long currentPayout = BonusGamePresenter.instance.currentPayout;
		yield return StartCoroutine(
			animateScore(
				currentPayout,
				currentPayout + credits));
				
		BonusGamePresenter.instance.currentPayout += credits;
	}
	
	/// Default overridable mehtod for Unity update, base handles updating the controller for pick me animations
	protected override void Update()
	{
		// Play the pickme animation.
		if (inputEnabled && _didInit)
		{
			pickMeController.update();
		}

		base.Update();
	}

	/// Callback function for one of the buttons in the pickem being pressed
	public void pickemButtonPressed(GameObject buttonObj)
	{
		if (inputEnabled && isButtonAvailableToSelect(buttonObj)) 
		{
			StartCoroutine(pickemButtonPressedCoroutine(buttonObj));
		}
	}

	/// Coroutine called when a button is pressed, used to handle timing stuff that may need to happen
	protected abstract IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj);

	/// Pick me animation player
	/// Make sure the pick me animation transitions to the idle animation.
	/// Initialize pickMeAnimName and pickMeSoundName if they're not the default names.
	protected virtual IEnumerator pickMeAnimCallback()
	{
		if (inputEnabled)
		{
			PickGameButtonData pickMe = getRandomPickMe();
			
			if (pickMe != null)
			{
				if (pickMe.pickMeSoundName != "")
				{
					Audio.play(pickMe.pickMeSoundName);
				}
			
				Audio.play(pickMeSoundName);

				string animName = pickMeAnimName;
				if (numPickMeAnims > 1)
				{
					int pickMeAnimIndex = Random.Range(0,numPickMeAnims);
					
					if (pickMeAnimIndex > 0)
					{
						animName = string.Format("{0}{1}", pickMeAnimName, pickMeAnimIndex + 1);
					}
				}	
				
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(pickMe.animator, animName));
			}
		}
	}
	
	/// Use this in the pickMeAnimCallback to get a PickGameButton that is available for pick-me-ing.
	protected PickGameButtonData getRandomPickMe(int round = -1)
	{
		if (round == -1)
		{
			round = currentStage;
		}
		
		PickGameButtonData pick = null;
		
		if (round < pickmeButtonList.Count)
		{
			List<GameObject> pickmeList = pickmeButtonList[round];
			int numPickmes = pickmeList.Count;
			
			if (numPickmes > 0)
			{
				int pickmeIndex = Random.Range(0, numPickmes);
				GameObject pickmeObject = pickmeList[pickmeIndex];
				
				int pickIndex = getButtonIndex(pickmeObject);
				pick = getPickGameButton(pickIndex);
			}
		}
		
		return pick;
	}
	
	protected virtual IEnumerator animateSparkleTrail(SparkleTrailParams trailParams)
	{
		if (bonusSparkleTrailCacher != null)
		{
			GameObject sparkleTrail = bonusSparkleTrailCacher.getInstance();
			
			sparkleTrail.transform.parent = bonusSparkleTrail.transform.parent;
			sparkleTrail.transform.localScale = bonusSparkleTrail.transform.localScale;
						
			Vector3 startPos = new Vector3(
				trailParams.startObject.transform.position.x,
				trailParams.startObject.transform.position.y,
				bonusSparkleTrail.transform.position.z);
			sparkleTrail.transform.position = startPos;
			
			sparkleTrail.SetActive(true);
			
			Vector3 endPos = trailParams.endObject.transform.position;
			endPos.z = bonusSparkleTrail.transform.position.z;

			if (trailParams.shouldRotate)
			{
				Vector3 diff = endPos - startPos;
				float angle = Mathf.Atan2(diff.x, -diff.y) * 180.0f / Mathf.PI;
				sparkleTrail.transform.localEulerAngles = new Vector3(0.0f , 0.0f, angle);
			}
			
			iTween.MoveTo(
				sparkleTrail,
				iTween.Hash(
				"position", endPos,
				"time", trailParams.dur,
				"islocal", false,
				"easetype",
				iTween.EaseType.linear));
			
			yield return new TIWaitForSeconds(trailParams.dur);
			
			if (trailParams.holdDur != 0.0f)
			{
				yield return new TIWaitForSeconds(trailParams.holdDur);
			}
			
			sparkleTrail.SetActive(false);
			bonusSparkleTrailCacher.releaseInstance(sparkleTrail);
		}
	}

	protected virtual IEnumerator animateSparkleExplosion(GameObject go, float dur = 0.0f)
	{
		if (bonusSparkleExplosionCacher != null)
		{
			GameObject sparkleExplosion = bonusSparkleExplosionCacher.getInstance();
			
			sparkleExplosion.transform.parent = bonusSparkleExplosion.transform.parent;
			sparkleExplosion.transform.localScale = bonusSparkleExplosion.transform.localScale;
			
			sparkleExplosion.transform.position = new Vector3(
				go.transform.position.x,
				go.transform.position.y,
				bonusSparkleExplosion.transform.position.z);
				
			sparkleExplosion.SetActive(true);

			if (dur != 0.0f)
			{
				yield return new WaitForSeconds(dur);
			}
			else
			{
				Animator animator = sparkleExplosion.GetComponent<Animator>();
				
				if (animator != null)
				{
					yield return StartCoroutine(CommonAnimation.waitForAnimDur(animator));
				}
			}
			
			sparkleExplosion.SetActive(false);
			bonusSparkleExplosionCacher.releaseInstance(sparkleExplosion);	
		}
	}
	
	// When this round is over, automatically gray-out remaining picks and reveal them one at a time.
	// If your gray-out animation isn't the default animation, then make sure to reassign grayoutAnimName.
	// If you don't want to gray-out everything before the reveal, then assign grayoutAnimName to the empty string.
	// Override finishRevealingPick (singular) and implement your specific reveal remaining pick.
	protected virtual IEnumerator finishRevealingPicks(float dur)
	{
		revealWait.reset();
		
		// Gray-out all the remaining picks right now.
		if (grayoutAnimName != "")
		{
			List<GameObject> pickmeList = pickmeButtonList[currentStage];
			int numPickMes = pickmeList.Count;
			
			for (int iPickMe = 0; iPickMe < numPickMes; iPickMe++)
			{
				GameObject pickMeObject = pickmeList[iPickMe];
				
				int otherIndex = getButtonIndex(pickMeObject);
				PickGameButtonData otherPick = getPickGameButton(otherIndex);
				
				if (otherPick != null)
				{
					otherPick.animator.Play(grayoutAnimName);
				}
			}
		}
		
		// Reveal the remaining picks one at a time.
		while (true)
		{
			PickGameButtonData pick = removeNextPickGameButton();
			
			if (pick == null)
			{
				break;
			}
		
			yield return StartCoroutine(revealWait.wait(dur));
			finishRevealingPick(pick);
		}
	}
	
	protected virtual void finishRevealingPick(PickGameButtonData pick)
	{
	}
}
