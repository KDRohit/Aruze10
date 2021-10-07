using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/*
 The Pick Major Free Spins Class that controls the free spins game for games such as Bev01, Gen06, Ani03 (each game overrides portions of this class for specific effects).
 There are 2 stages to this freespins game.
 1. Is a pickem stage that the player picks a button that reveals a symbol
	This symbol will be the the large symbol in the actual freespins part (done on backend)
	During this stage the normal bonus game wings are shown, and the overlay is hidden (like a challenge game)
 2. Is the actuall freespins game. 34443 offset.
	-123-
	01234
	01234
	01234
 There is no way to get more freespins in this game.
 */

public class PickMajorFreeSpins : FreeSpinGame
{
	
	// Pick Variables:
	[SerializeField] protected GameObject pickemStageObjects;					// The pickem part of the freespin game so we can hide it once we are done.
	[SerializeField] protected List<GameObject> buttonSelections;				// List of the buttons that can be picked
	[SerializeField] protected GameObject banner;								// The banner at the top of the pickem portion of the game
	[SerializeField] protected List<GameObject> revealSprites;					// The sprites from M1 -> M4. So they can be revealed in the right order.
	[SerializeField] protected bool showWingsInForeground = true;				// Some games may want to have the reel game showing under the pick intro that uses a shroud
	[SerializeField] private bool isReelGameVisibleDuringPick = false;			// Some games may want to have the reel game showing under the pick intro that uses a shroud
	[SerializeField] private float enableInputDelay = 0.0f; 					// Time to wait before minipick button starts
	[SerializeField] private bool isSlidingInSpinPanel = false;					// Allow the spin panel to slide in, as that might look better then just having it appear
	[SerializeField] protected bool isShowingWingsForPickStage = true;			// Controls if wings are shown during the picking stage
	protected List<Stage1Type> randomizedStageTypeList = new List<Stage1Type>();	// Allow for a randomized list of stage types during reveals
	protected SkippableWait revealWait;											// We want to be able to skip the reveals.
	private CoroutineRepeater pickMeController;									// Class to call the pickme animation on a loop

	protected Stage1Type stageType;									// Keeps track of the reveal that should be done on this stage.
	
	// Games that care about major rp symbols (pawn01) can look in here to see if the symbol was picked up in the outcome.
	protected string majorRPSymbol = null;
	
	// An easy way to keep track of what we should be revealing.
	protected enum Stage1Type
	{
		NONE = -1,
		M1 = 0,
		M2 = 2,
		M3 = 1,
		M4 = 3,
		M5 = 4
	}

	// Freespin Variables:
	public GameObject freeSpinText;									// The text that needs to keep moving up and down durring the freespins.
	public GameObject spinStageObjects;								// The freespin part of this freespin game so we can hide it once we are done.

	// Should go into another class
	public GameObject megaSymbolText;								// The text that needs to keep moving up and down durring the freespins.
	public GameObject megaSymbolImage;								// The image that holds what the mega symbol is going to be for this game.
	protected Vector3 megaSymbolTextStartingPos;					// Starting pos of the mega text.
	protected Vector3 megaSymbolImageStartingPos;					// Starting pos of the mega image.
	protected Vector3 freeSpinTextStartingPos;						// Starting Pos of the FS text.

	// Game State variables
	protected bool inputEnabled = true;								// Only one input should be able to happen in the first stage of this game.
	protected bool inPickMeStage = true;							// bool to tell which stage we are in so we can delay starting the base freespin stuff.
	protected int revealSpriteIndex = -1; 							// can use same index for reveal sprite and star

	// Constants
	protected float TIME_BETWEEN_REVEALS = 0.5f;
	protected float TIME_AFTER_REVEALS = 1.0f;
	[SerializeField] protected float MIN_TIME_PICKME = 2.0f;						// Minimum time an animation might take to play next
	[SerializeField] protected float MAX_TIME_PICKME = 7.0f;						// Maximum time an animation might take to play next
	private const float TIME_SLIDE_SPIN_PANEL_IN = 0.4f;			// How long the spin panel slide in will take (only used if isSlidingInSpinPanel is true)

	
	public override void initFreespins()
	{
		base.initFreespins();

		randomizeStageTypeList();

		inputEnabled = false; // initially set the buttons to be inactive. We will reactivate them later.
		// Make sure that we have the right stuff visible
		pickemStageObjects.SetActive(true);

		if (isReelGameVisibleDuringPick)
		{
			if(spinStageObjects != null)
			{
				spinStageObjects.SetActive(true);
			}
		}
		else
		{
			if(spinStageObjects != null)
			{
				spinStageObjects.SetActive(false);
			}
		}

		pickMeController = new CoroutineRepeater(MIN_TIME_PICKME, MAX_TIME_PICKME, pickMeCallback);
		revealWait = new SkippableWait();

		// We want the start of this to play like a challenge game so we hide the overlay and show different wings.
		if (isShowingWingsForPickStage)
		{
			BonusGameManager.instance.wings.forceShowNormalWings(showWingsInForeground);
		}

		SpinPanel.instance.showSideInfo(false);
		//SpinPanel.instance.hidePanels();
		setupStage();
		StartCoroutine(enableInputWithDelay(enableInputDelay)); // Enable the pickme buttons after waiting the delay
	}

	/// Allows the button selections to be randomized, for reveals
	protected void randomizeStageTypeList()
	{
		for(int i = 1; i <= buttonSelections.Count; i++)
		{
			if (System.Enum.IsDefined(typeof(Stage1Type), "M"+i))
			{
				randomizedStageTypeList.Add((Stage1Type)System.Enum.Parse(typeof(Stage1Type), "M"+i));
			}
			else
			{
				Debug.LogError("Could not add button for M" + i + " in picking round of the freespin game. Check the size of button selections");
			}
		}

		CommonDataStructures.shuffleList<Stage1Type>(randomizedStageTypeList);
	}

	/// Get the next randomized stage type, used to produce random reveals
	protected Stage1Type getNextRandomizedStageType()
	{
		if (randomizedStageTypeList.Count > 0)
		{
			Stage1Type nextStageType = randomizedStageTypeList[randomizedStageTypeList.Count - 1];
			randomizedStageTypeList.RemoveAt(randomizedStageTypeList.Count - 1);
			return nextStageType;
		}
		else
		{
			return Stage1Type.NONE;
		}
	}

	protected virtual void setupStage()
	{
		if(!string.IsNullOrEmpty(majorRPSymbol))
		{
			if (!string.IsNullOrEmpty(majorRPSymbol))
			{
				switch(majorRPSymbol)
				{
				case "M1":
					stageType = Stage1Type.M1;
					break;
				case "M2":
					stageType = Stage1Type.M2;
					break;
				case "M3":
					stageType = Stage1Type.M3;
					break;
				case "M4":
					stageType = Stage1Type.M4;
					break;
				case "M5":
					stageType = Stage1Type.M5;
					break;
				default:
					Debug.LogError("mutation.majorRPSymbol contained unknown value: " + majorRPSymbol);
					stageType = Stage1Type.NONE;
					break;
				}
			}
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M1") || BonusGameManager.instance.bonusGameName.Contains("_m1"))
		{
			stageType = Stage1Type.M1;
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M2") || BonusGameManager.instance.bonusGameName.Contains("_m2"))
		{
			stageType = Stage1Type.M2;	
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M3") || BonusGameManager.instance.bonusGameName.Contains("_m3"))
		{
			stageType = Stage1Type.M3;
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M4") || BonusGameManager.instance.bonusGameName.Contains("_m4"))
		{
			stageType = Stage1Type.M4;
		}
		else if (BonusGameManager.instance.bonusGameName.Contains("_M5") || BonusGameManager.instance.bonusGameName.Contains("_m5"))
		{
			stageType = Stage1Type.M5;
		}
		else
		{
			stageType = Stage1Type.NONE;
			Debug.LogError("There was an unexpected format for the name of the PickMajorFreeSpins game, don't know what symbol to reveal for " +
						   BonusGameManager.instance.bonusGameName);
		}

		// remove the pick from the randomized list
		if (stageType != Stage1Type.NONE)
		{
			randomizedStageTypeList.Remove(stageType);
		}
	}
	
	protected virtual void setupAnimationComponents()
	{

	}

	/// Get the current stage type as a string that can be appended to things like animations or sounds
	protected string stageTypeAsString()
	{
		return PickMajorFreeSpins.convertStage1TypeToString(stageType);
	}

	/// Static function for converting Stage1Type enums to strings
	protected static string convertStage1TypeToString(Stage1Type passedStageType)
	{
		switch (passedStageType)
		{
			case Stage1Type.NONE:
				return "NONE";
			case Stage1Type.M1:
				return "M1";
			case Stage1Type.M2:
				return "M2";
			case Stage1Type.M3:
				return "M3";
			case Stage1Type.M4:
				return "M4";
			case Stage1Type.M5:
				return "M5";
			default:
				return "";
		}
	}
	
	// used in 'handleSetReelSet()' to find the first occurance of mutation data in the outcome
	private JSON[] searchOutcomeForMutations(SlotOutcome outcome)
	{
		JSON[] mutationData = outcome.getMutations();
		ReadOnlyCollection<SlotOutcome> subOutcomes = outcome.getSubOutcomesReadOnly();
		
		for (int i = 0; i < subOutcomes.Count; i++)
		{
			if (mutationData != null && mutationData.Length > 0)
			{
				break;
			}
			mutationData = searchOutcomeForMutations(subOutcomes[i]);
		}
		return mutationData;
	}
	
	protected override void handleSetReelSet(string reelSetKey)
	{
		_reelSetData = slotGameData.findReelSet(reelSetKey);

		Dictionary<string, string> normalReplacementSymbolMap = new Dictionary<string, string>();
		Dictionary<string, string> megaReplacementSymbolMap = new Dictionary<string, string>();

		// Call this incase this game has multi-replacement symbols
		populateSymbolReplaceMultiReplacementSymbolData(reelSetKey, normalReplacementSymbolMap, megaReplacementSymbolMap);

		// Do some extra work to grab majorRPSymbol if it exits.
		if (reelInfo == null || reelInfo.Length == 0)
		{ 
			// search for the first occurance of majorRPmutation data
			JSON[] mutationData = searchOutcomeForMutations(BonusGameManager.currentBonusGameOutcome);
			if (mutationData != null)
			{
				for (int i = 0; i < mutationData.Length; i++)
				{
					string majorRPSymbolStr = mutationData[i].getString("major_rp_symbol", "");
					if (!string.IsNullOrEmpty(majorRPSymbolStr))
					{
						majorRPSymbol = majorRPSymbolStr;
						break;
					}
				}
			}

			// of we got an majorRPSymbol, we need to manually set the replacement symboldata.
			if (!string.IsNullOrEmpty(majorRPSymbol) && !megaReplacementSymbolMap.ContainsKey("RP1"))
			{
				// Since this is a special kind of pick major game where there is only one kind of replacement symbol, RP1
				//	should be ok. If in the future some game comes along that needs something more elaborate this code would
				//	need to be updated.
				megaReplacementSymbolMap.Add("RP1", majorRPSymbol);
			}
		}

		// Set the games normal and megasymbol replacements.
		setGameReplacementSymbolData(reelSetKey, normalReplacementSymbolMap, megaReplacementSymbolMap);
	}

	/// Convert the enum to a standard index ordering
	/// NOTE: This provides a different index than the enum type was declared as
	protected int stageTypeAsInt()
	{
		return PickMajorFreeSpins.convertStage1TypeToInt(stageType);
	}

	/// Convert the enum to a standard index ordering
	/// NOTE: This provides a different index than the enum type was declared as
	/// static version
	protected static int convertStage1TypeToInt(Stage1Type passedStageType)
	{
		switch (passedStageType)
		{
			case Stage1Type.NONE:
				return -1;
			case Stage1Type.M1:
				return 0;
			case Stage1Type.M2:
				return 1;
			case Stage1Type.M3:
				return 2;
			case Stage1Type.M4:
				return 3;
			case Stage1Type.M5:
				return 4;
			default:
				return -1;
		}
	}
	
	/////////////////////////////////////
	////////// Stage 1 Methods //////////
	/////////////////////////////////////
	
	// After waiting for the delay, re-enable the pickme buttons. 
	// This is if we want to instantiate the pickme window before transititions fully finish and prevent user input
	private IEnumerator enableInputWithDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		inputEnabled = true;
	}

	protected virtual IEnumerator pickMeCallback()
	{
		yield return null;
	}
	
	// Call back called when one of the pickme buttons is clicked in stage 1.
	public void knockerClicked(GameObject clickedKnocker)
	{
		// We only want to let one knocker get selected.
		if (!inputEnabled)
		{
			return;
		}
		
		inputEnabled = false;
		StartCoroutine(knockerClickedCoroutine(clickedKnocker));
	}
	
	protected virtual IEnumerator knockerClickedCoroutine(GameObject button)
	{
		// Reveal the pick that was selected.
		yield return StartCoroutine(showPick(button));
		// Remove the button from the list because we're not using it anymore.
		buttonSelections.Remove(button);
		// Show reveals
		yield return StartCoroutine(showReveals());
		yield return new WaitForSeconds(TIME_AFTER_REVEALS);
		// Transition into the freespins game
		yield return StartCoroutine(transitionIntoStage2());
	}

	protected virtual IEnumerator showPick(GameObject button)
	{
		button.SetActive(false);
		yield return null;
	}

	protected virtual IEnumerator showReveals()
	{
		foreach (GameObject button in buttonSelections)
		{
			StartCoroutine(showReveal(button));
			yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
		}
	}

	protected virtual IEnumerator showReveal(GameObject button)
	{
		button.SetActive(false);
		yield return null;
	}
	
	// For right now this is one of the most bland reveal animations of all time, the knocker basically just transforms into the symbol.
	protected virtual void revealUnpickedKnocker(GameObject unpickedKnocker)
	{
		unpickedKnocker.SetActive(false);
	}

	// Overriding this so that we will skip executeGameStartModules() until transitionIntoStage2() is called
	protected override IEnumerator playGameStartModules()
	{
		// skipping executeGameStartModules() because it will be called in transitionIntoStage2()
		_didInit = true;
		yield break;
	}
	
	// The function call that does the transition from stage 1 -> 2
	protected virtual IEnumerator transitionIntoStage2()
	{
		// Maybe we can do something where we zoom into the large symbol so it fills the whole screen, and zoom out into the freespins game
		pickemStageObjects.SetActive(false);
		// We are going back to a normal freespin game so we need to hide the wings, and show the overlay again.
		if (isShowingWingsForPickStage)
		{
			BonusGameManager.instance.wings.hide();
		}

		if (isSlidingInSpinPanel)
		{
			// slide the spin panel in by first setting the position and then sliding it
			SpinPanel.instance.setSpinPanelPosition(SpinPanel.Type.FREE_SPINS, SpinPanel.SpinPanelSlideOutDirEnum.Down, false);
			SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
			yield return StartCoroutine(SpinPanel.instance.slideSpinPanelInFrom(
				SpinPanel.Type.FREE_SPINS,
				SpinPanel.SpinPanelSlideOutDirEnum.Down,
				TIME_SLIDE_SPIN_PANEL_IN,
				false));
		}
		else
		{
			SpinPanel.instance.showPanel(SpinPanel.Type.FREE_SPINS);
		}

		if (showSideInfo)
		{
			SpinPanel.instance.showSideInfo(true);
		}
		pickemStageObjects.SetActive(false);
		if(spinStageObjects != null)
		{
			spinStageObjects.SetActive(true);
		}
		StartCoroutine(moveFreeSpinText());
		yield return StartCoroutine(executeGameStartModules());
		yield return null;
		inPickMeStage = false;
	}
	
	/////////////////////////////////////
	////////// Stage 2 Methods //////////
	/////////////////////////////////////
	
	// A function that move the freespin text up and down.
	protected virtual IEnumerator moveFreeSpinText()
	{
		yield return null;
	}
	
	// Overriding the update method here so we can have 2 stages.
	protected override void Update()
	{
		if (_didInit)
		{
			// If we are still in the pickme part of the game we want to do our stuff.
			if (inPickMeStage)
			{
				// Wiggle the pickme animation.
				if (inputEnabled && _didInit)
				{
					pickMeController.update();
				}
			}
			else
			{
				base.Update();
			}
		}
	}
}
