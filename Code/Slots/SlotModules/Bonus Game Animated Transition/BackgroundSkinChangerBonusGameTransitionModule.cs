/*
 * BackgroundSkinChangerBonusGameTransitionModule.cs
 * This module sets the 'skin' of the game based on the outcome of the free spin battle mode.
 * The following attributes can be set for each skin:
 * 1. Reel, frame and background materials
 * 2. Reel offsets
 * 3. Accompanying animations like ambient FX, mask offsets
 * 4. Transitions
 * 5. Anticipation reels
 *
 * The active skin is saved across multiple sessions using the PlayerPref key: GAME_BACKGROUND_SKIN
 *
 * Original author - Abhishek Singh
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundSkinChangerBonusGameTransitionModule : BaseTransitionModule
{
	// Internal class to hold information related to the skin of the game
	[System.Serializable]
	private class GameSkinInfo
	{
		public string skinName;
		[Tooltip("The 'feature_anticipation_prefabs' key defined in slot_resource_map.txt")]
		public string anticipationPrefabKey;
		public string transitionName;
		public bool shouldReelsBeOffset;
		public Material backgroundMaterial;
		public Material reelBackgroundMaterial;
		public Material reelBackgroundAlphaMaterial;
		[Tooltip("Set attributes like masks, ambient fx and transitions")]
		public AnimationListController.AnimationInformationList skinSpecificAnimations;
		[Header("Skin Specific Audio")]
		public string prespinIdleLoop = "prespin_idle_loop";
		public string reelspinBase = "reelspin_base";
		public string prewinBase = "prewin_base";
		public string bonusFreespinWipeTransition = "bonus_freespin_wipe_transition";
		public string bonusFreespinWipeTransitionVO = "bonus_freespin_wipe_transition_vo";
		public CommonDataStructures.SerializableDictionaryOfStringToString symbolReplacements; // Special way of handling symbols that need to swap out based on skin
		[SerializeField] private SymbolSoundOverrideEntry[] symbolSoundOverrideArray; // Sound overrides for symbols when using this skin
		[System.NonSerialized] public Dictionary<string, SymbolSoundOverrideEntry> symbolSoundOverrideDict; // Dictionary lookup of the sound overrides

		public void init()
		{
			for (int i = 0; i < symbolSoundOverrideArray.Length; i++)
			{
				if (symbolSoundOverrideDict == null)
				{
					symbolSoundOverrideDict = new Dictionary<string, SymbolSoundOverrideEntry>();
				}

				SymbolSoundOverrideEntry entry = symbolSoundOverrideArray[i];
				symbolSoundOverrideDict.Add(entry.symbolName, entry);
			}
		}
	}

	[System.Serializable]
	private class SymbolSoundOverrideEntry
	{
		public string symbolName;
		public AudioListController.AudioInformationList soundList;
	}

	// Internal class to hold information related to reel offsets
	[System.Serializable]
	public class ReelOffsetInfo
	{
		public GameObject reelPrefab;
		public int offsetCount;	// Number of symbols to be moved up or down
	}

	[SerializeField] private GameSkinInfo[] gameSkinList;			// List of possible skins the game can switch between
	[SerializeField] private bool deleteSavedSkinOnStart = false;	// If true, clear Player Prefs value at start

	// Reel prefab elements to be modified
	[SerializeField] private bool shouldSwapMaterials = false;	// Reel, bg and frame objects have materials swapped instead of animator handling it
	[SerializeField] private GameObject backgroundPrefab;
	[SerializeField] private GameObject reelBackgroundPrefab;
	[SerializeField] private GameObject reelFramePrefab;
	[Tooltip("Offsets for the second skin of the reels")]
	[SerializeField]private ReelOffsetInfo[] reelOffsetList;

	// Transition related variables
	[SerializeField] private bool shouldFadeSymbols = true;
	[SerializeField] private bool shouldFadeWings;
	[SerializeField] private bool shouldFadeTopOverlay;
	[SerializeField] private bool shouldFadeSpinPanel;
	[SerializeField] private bool shouldPlayTransitionSound = true;
	[SerializeField] private float fadeTime;
	[SerializeField] private List<GameObject> objectsToFade;
	[SerializeField] private List<GameObject> objectsToDeactivateImmediately;
	[SerializeField] private List<GameObject> objectsToDeactivateOnTransitionEnd; //Useful when you want to leave the basegame activated during the freespins
	[SerializeField] private AnimationListController.AnimationInformationList transitionAnimationList;	// Set blocking to be true
	[SerializeField] private bool shouldSlideOutOverlay;
	[SerializeField] private bool shouldSlideOutOverlayWithAnimation;
	[SerializeField] private bool shouldSlideOutSpinPanelWithAnimation;
	[SerializeField] private bool shouldHideAllUIDuringTransition;

	//If we want to update the base game background post transition
	[SerializeField] private bool updateBasegameBackgroundAnimationFromOutcome = false;
	[SerializeField] private AnimationListController.AnimationInformationList goodWinAnimations = new AnimationListController.AnimationInformationList();
	[SerializeField] private AnimationListController.AnimationInformationList badWinAnimations = new AnimationListController.AnimationInformationList();

	private Vector3[] originalReelsPosition;
	private GameSkinInfo currentSkinInfo;

	private List<Dictionary<Material, float>> fadeObjectAlphaMaps = new List<Dictionary<Material, float>>();
	private int startingObjectsToFadeCount;
	private ReelGameBackground.WingTypeOverrideEnum originalWingType;

	// Need to play these animations here or the animator will play it's default state.
	protected override void OnEnable()
	{
		base.OnEnable();
		playAnimsForCurrentSkin();
	}

	private void storeOriginalReelsPositions()
	{
		if (originalReelsPosition == null)
		{
			originalReelsPosition = new Vector3[reelOffsetList.Length];
		}

		for (int i = 0; i < reelOffsetList.Length; i++)
		{
			originalReelsPosition[i] = reelOffsetList[i].reelPrefab.transform.position;
		}
	}

	private void Start()
	{
		if (background != null)
		{
			originalWingType = background.wingType;
		}

		if (shouldFadeWings)
		{
			objectsToFade.Add(background.wings.gameObject);
		}

		if (objectsToFade.Count > 0)
		{
			startingObjectsToFadeCount = objectsToFade.Count;
		}

		// Clear the saved player prefs key for the active skin if reset box is checked
		if (deleteSavedSkinOnStart)
		{
			PlayerPrefsCache.DeleteKey(string.Format(Prefs.GAME_BACKGROUND_SKIN, GameState.game.keyName));
		}

		for (int i = 0; i < gameSkinList.Length; i++)
		{
			gameSkinList[i].init();
		}

		// Force music to start again, since changing skins may affect it
		ReelGame.activeGame.playBgMusic();
	}

	// executeOnSlotGameStarted() section
	// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return true;
	}

	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		// Store starting positions of the reels
		if (originalReelsPosition == null)
		{
			storeOriginalReelsPositions();
		}

		yield return StartCoroutine(validateActiveSkin());
	}

	// Retrieve the skin to be set for the game, otherwise choose the first defined one
	private IEnumerator validateActiveSkin()
	{
		if (gameSkinList == null)
		{
			Debug.LogError("Game skin list is empty. Define at least one skin! Destroying script: " + this.GetType().Name);
			Destroy(this);
		}

		if (shouldSwapMaterials &&
			(backgroundPrefab == null || reelFramePrefab == null || reelBackgroundPrefab == null))
		{
			Debug.LogError("Prefabs not assigned for material swap! Destroying script: " + this.GetType().Name);
			Destroy(this);
		}

		// Retrieve value from player prefs if already available
		string chosenActiveSkin = PlayerPrefsCache.GetString(string.Format(Prefs.GAME_BACKGROUND_SKIN, GameState.game.keyName), "");

		if (chosenActiveSkin == "")
		{
			// If no skin has been marked as active, set the first one to be active
			chosenActiveSkin = gameSkinList[0].skinName;
		}

		if (currentSkinInfo == null || chosenActiveSkin != currentSkinInfo.skinName)
		{
			// Save value in playerPrefs then set skin
			PlayerPrefsCache.SetString(string.Format(Prefs.GAME_BACKGROUND_SKIN, GameState.game.keyName), chosenActiveSkin);
			yield return StartCoroutine(setNewSkin(chosenActiveSkin));
		}
		else
		{
			// Otherwise ensure animators are on the right state
			playAnimsForCurrentSkin();
		}
	}

	// Sets the specified game skin assets and clears the other one
	private IEnumerator setNewSkin(string newSkinName)
	{
		// to keep the reel position offset accurate
		// store the latest updated reel positions again (reelBackground could have resized the reels since the slot game started)
		storeOriginalReelsPositions();
		
		bool chosenSkinFound = false;

		foreach (GameSkinInfo gameSkin in gameSkinList)
		{
			if (gameSkin.skinName == newSkinName)
			{
				bool skipFirstOffsetForDefaultSkin = false;
				
				// if this is setting the "sun" skin at the start of a session, we ignore offset
				// because we use the default transform of the reels prefab, and it is setup for sun
				if (currentSkinInfo == null && !gameSkin.shouldReelsBeOffset)
				{
					skipFirstOffsetForDefaultSkin = true;
				}

				// Save skin information
				currentSkinInfo = gameSkin;
				chosenSkinFound = true;

				// Set the reel positions
				for (int i = 0; i < reelOffsetList.Length; i++)
				{
					Vector3 position = originalReelsPosition[i];
					if (!skipFirstOffsetForDefaultSkin)
					{
						// moon skin
						if (gameSkin.shouldReelsBeOffset)
						{
							position.y += reelOffsetList[i].offsetCount * reelGame.getSymbolVerticalSpacingAt(i);
						}
						else  // sun skin
						{
							position.y -= reelOffsetList[i].offsetCount * reelGame.getSymbolVerticalSpacingAt(i);
						}
					}
					reelOffsetList[i].reelPrefab.transform.position = position;
				}

				// Swap symbols to overrides
				convertSymbolsToMatchCurrentSkin();

				// Change materials
				if (shouldSwapMaterials)
				{
					backgroundPrefab.GetComponent<Renderer>().material = gameSkin.backgroundMaterial;
					reelBackgroundPrefab.GetComponent<Renderer>().material = gameSkin.reelBackgroundMaterial;
					reelFramePrefab.GetComponent<Renderer>().material = gameSkin.reelBackgroundMaterial;
					reelFramePrefab.GetComponent<Renderer>().material = gameSkin.reelBackgroundAlphaMaterial;
				}

				// Update audio for selected skin
				ReelGame.activeGame.BASE_GAME_BG_MUSIC_KEY = gameSkin.prespinIdleLoop;
				ReelGame.activeGame.BASE_GAME_SPIN_MUSIC_KEY = gameSkin.reelspinBase;
				ReelGame.activeGame.outcomeDisplayController.PRE_WIN_BASE_KEY = gameSkin.prewinBase;
				this.FREESPIN_TRANSITION_SOUND_KEY = gameSkin.bonusFreespinWipeTransition;
				this.TRANSITION_VO_KEY = gameSkin.bonusFreespinWipeTransitionVO;


				// Activate other related animations
				if (gameSkin.skinSpecificAnimations != null)
				{
					yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(gameSkin.skinSpecificAnimations));
				}
			}
		}

		if (!chosenSkinFound)
		{
			Debug.LogError("Specified game skin does not exist in the gameSkinList: " + newSkinName);
		}
	}

	protected override IEnumerator doTransition()
	{
		if (shouldPlayTransitionSound)
		{
			playTransitionSounds();
		}

		// Build a new alpha map for objects to be faded especially as skin objects change
		for (int i = 0; i < objectsToFade.Count; i++)
		{
			fadeObjectAlphaMaps.Add(CommonGameObject.getAlphaValueMapForGameObject(objectsToFade[i]));
		}

		if (shouldFadeSymbols)
		{
			foreach (SlotReel reel in reelGame.engine.getAllSlotReels())
			{
				List<SlotSymbol> symbolList = reel.symbolList;
				foreach (SlotSymbol symbol in symbolList)
				{
					if (symbol.animator != null)
					{
						if (reelGame.isGameUsingOptimizedFlattenedSymbols && !symbol.isFlattenedSymbol)
						{
							symbol.mutateToFlattenedVersion();
						}
						objectsToFade.Add(symbol.animator.gameObject);
						fadeObjectAlphaMaps.Add(CommonGameObject.getAlphaValueMapForGameObject(symbol.animator.gameObject));
					}
				}
			}
		}

		// Trigger the objects to be faded before transition occurs
		StartCoroutine(fadeOutObjects());

		if (shouldSlideOutOverlayWithAnimation)
		{
			// have routine runner do this since this object will be deactivated before it finishes potentially
			RoutineRunner.instance.StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Up, OVERLAY_TRANSITION_SLIDE_TIME, false));
			// hide the jackpot/mystery panel before the slide (it should be brought back when the Spin Panel is enabled in the base game again)
			Overlay.instance.jackpotMystery.hide();
		}

		if (shouldSlideOutSpinPanelWithAnimation)
		{
			// have routine runner do this since this object will be deactivated before it finishes potentially
			RoutineRunner.instance.StartCoroutine(SpinPanel.instance.slideSpinPanelOut(SpinPanel.Type.NORMAL, SpinPanel.SpinPanelSlideOutDirEnum.Down, OVERLAY_TRANSITION_SLIDE_TIME, false));
			SpinPanel.instance.showFeatureUI(false);
		}

		if (shouldHideAllUIDuringTransition)
		{
			SpinPanel.instance.showSideInfo(false);
			SpinPanel.instance.showFeatureUI(false);
		}

		if (transitionAnimationList != null)
		{
			AnimationListController.AnimationInformation transitionAnim = transitionAnimationList.animInfoList.Find(anim => anim.ANIMATION_NAME == currentSkinInfo.transitionName);
			yield return StartCoroutine(AnimationListController.playAnimationInformation(transitionAnim));
		}

		SlotBaseGame.instance.createBonus();
		if (reelGame is LayeredMultiSlotBaseGame)
		{
			SlotBaseGame.instance.startBonus(); //This will remove the skin from the layeredBonusSkins list and show the bonus
		}
		else
		{
			BonusGameManager.instance.show(null, true);
		}

		if (shouldSlideOutOverlay)
		{
			// have routine runner do this since this object will be deactivated before it finishes potentially
			RoutineRunner.instance.StartCoroutine(Overlay.instance.top.slideOut(OverlayTop.SlideOutDir.Up, OVERLAY_TRANSITION_SLIDE_TIME, false));
		}

		BonusGameManager.instance.startTransitionedBonusGame();
		if (BonusGamePresenter.instance.isHidingBaseGame)
		{
			BonusGameManager.currentBaseGame.gameObject.SetActive(false);
		}

		StartCoroutine(onTransitionFinished());
	}

	protected IEnumerator onTransitionFinished()
	{
		if (updateBasegameBackgroundAnimationFromOutcome)
		{
			bool goodWonBattle = false;

			//Look at the outcome to see who won
			FreeSpinsOutcome freeSpinsOutcomes = (FreeSpinsOutcome)BonusGameManager.instance.outcomes[BonusGameType.GIFTING];
			foreach (SlotOutcome outcome in freeSpinsOutcomes.entries)
			{
				List<JSON> jsonOutcomes = new List<JSON>(outcome.getMutations());
				for (int i = 0; i < jsonOutcomes.Count; i++)
				{
					if (jsonOutcomes[i].hasKey("battle_result"))
					{
						string result = jsonOutcomes[i].getString("battle_result", null);
						if (result != null && result == "win")
						{
							goodWonBattle = true;
						}
					}
				}
			}

			if (goodWonBattle)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(goodWinAnimations));
			}
			else
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(badWinAnimations));
			}
		}

		foreach (GameObject objectToDeactivate in objectsToDeactivateOnTransitionEnd)
		{
			if (objectToDeactivate != null)
			{
				objectToDeactivate.SetActive(false);
			}
			else
			{
				Debug.LogWarning("objectsToDeactivateOnTransitionEnd was null! Please adjust the size of the list you're using.");
			}
		}
	}

	protected IEnumerator fadeOutObjects()
	{
		float elapsedTime = 0;

		if (shouldFadeTopOverlay)
		{
			StartCoroutine(Overlay.instance.fadeOut(fadeTime));
		}

		if (shouldFadeSpinPanel)
		{
			StartCoroutine(SpinPanel.instance.fadeOut(fadeTime));
		}

		foreach (GameObject objectToDeactivateImmediately in objectsToDeactivateImmediately)
		{
			if (objectToDeactivateImmediately != null)
			{
				objectToDeactivateImmediately.SetActive(false);
			}
			else
			{
				Debug.LogWarning("objectToDeactivateImmediately was null! Please adjust the size of the list you're using.");
			}
		}

		while (elapsedTime < fadeTime)
		{
			elapsedTime += Time.deltaTime;
			foreach (GameObject objectToFade in objectsToFade)
			{
				CommonGameObject.alphaGameObject(objectToFade, 1 - (elapsedTime / fadeTime));
			}
			yield return null;
		}

		foreach (GameObject objectToFade in objectsToFade)
		{
			if (objectToFade != null)
			{
				CommonGameObject.alphaGameObject(objectToFade, 0.0f);
			}
			else
			{
				Debug.LogError("objectToFade is null! Please adjust the size of the list you're using.");
			}
		}

		if (shouldFadeTopOverlay)
		{
			Overlay.instance.top.show(false);
			Overlay.instance.fadeInNow();
		}

		if (shouldFadeSpinPanel)
		{
			SpinPanel.instance.hidePanels();
			SpinPanel.instance.restoreAlpha();
		}

	}

	// Skin validation once bonus game is completed and also reset base game objects
	public override IEnumerator executeOnBonusGameEnded()
	{
		foreach (GameObject objectToDeactivateImmediately in objectsToDeactivateImmediately)
		{
			if (objectToDeactivateImmediately != null)
			{
				objectToDeactivateImmediately.SetActive(true);
			}
			else
			{
				Debug.LogWarning("objectToDeactivateImmediately was null! Please adjust the size of the list you're using.");
			}
		}

		foreach (GameObject objectToDeactivate in objectsToDeactivateOnTransitionEnd)
		{
			if (objectToDeactivate != null)
			{
				objectToDeactivate.SetActive(true);
			}
			else
			{
				Debug.LogWarning("objectsToDeactivateOnTransitionEnd was null! Please adjust the size of the list you're using.");
			}
		}

		for (int i = 0; i < objectsToFade.Count; i++)
		{
			if (objectsToFade[i] != null && i < fadeObjectAlphaMaps.Count)
			{
				CommonGameObject.restoreAlphaValuesToGameObjectFromMap(objectsToFade[i], fadeObjectAlphaMaps[i]);
			}
			else
			{
				if (objectsToFade[i] == null)
				{
					Debug.LogError("objectToFade is null! Please adjust the size of the list you're using.");
				}
				else
				{
					Debug.LogError("BonusGameAnimatedTransition.executeOnBonusGameEnded() - objectsToFade[" + i + "].name = " + objectsToFade[i].name + "; i = " + i + "; is out of bounds of fadeObjectAlphaMaps.Count = " + fadeObjectAlphaMaps.Count);
				}
			}
		}

		if (shouldFadeTopOverlay)
		{
			Overlay.instance.top.show(true);
		}

		if (shouldFadeSpinPanel)
		{
			SpinPanel.instance.showPanel(SpinPanel.Type.NORMAL);
		}

		SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);

		if (shouldFadeSymbols)
		{
			SlotReel[] reelArray = reelGame.engine.getReelArray();

			for (int reelID = 0; reelID < reelArray.Length; reelID++)
			{
				List<SlotSymbol> symbolList = reelGame.engine.getSlotReelAt(reelID).symbolList;
				foreach (SlotSymbol symbol in symbolList)
				{
					if (symbol.animator != null)
					{
						symbol.animator.gameObject.SetActive(true);
					}
				}
			}

			//Remove the symbols from our fade objects list so it doesn't continually get larger
			objectsToFade.RemoveRange(startingObjectsToFadeCount, objectsToFade.Count - startingObjectsToFadeCount);
			// Reset the alpha map as we build it fresh on every single transition
			fadeObjectAlphaMaps.RemoveRange(0, fadeObjectAlphaMaps.Count);
		}

		if (reelGame.GetComponent<HideSpinPanelMetersUIModule>() == null)
		{
			SpinPanel.instance.showFeatureUI(true);
		}

		Overlay.instance.top.restorePosition();
		SpinPanel.instance.restoreSpinPanelPosition(SpinPanel.Type.NORMAL);
		SpinPanel.instance.resetAutoSpinUI();

		// Set skin
		yield return StartCoroutine(validateActiveSkin());

		if (background != null && background.wings != null)
		{
			CommonGameObject.setLayerRecursively(background.wings.gameObject, Layers.ID_SLOT_FRAME); //hack to keep wings on top of frame during transition
			background.setWingsTo(reelGame.reelGameBackground.wingType); //wings are now reset back to what the basegame wants them to be
			background.forceUpdate(); //Does a one time update to the position and scale of the wings while the application is running
		}

		//Making sure to set our wings back to regular since we're in the base game
		if (background != null && originalWingType == ReelGameBackground.WingTypeOverrideEnum.Basegame)
		{
			background.wingType = ReelGameBackground.WingTypeOverrideEnum.Basegame;
		}
		resetWings();
	}

	// Handle multiple anticipation prefabs
	public override bool needsToGetFeatureAnicipationNameFromModule()
	{
		return (!string.IsNullOrEmpty(currentSkinInfo.anticipationPrefabKey));
	}

	public override string getFeatureAnticipationNameFromModule()
	{
		return currentSkinInfo.anticipationPrefabKey;
	}

	// Restore the animators as all state data is lost whenever the base game is hidden
	public override bool needsToExecuteOnShowSlotBaseGame()
	{
		return true;
	}

	public override void executeOnShowSlotBaseGame()
	{
		playAnimsForCurrentSkin();
	}

	// Ensure animators are in the right state
	private void playAnimsForCurrentSkin()
	{
		if (currentSkinInfo != null && currentSkinInfo.skinSpecificAnimations != null)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(currentSkinInfo.skinSpecificAnimations));
		}
	}

	// Goes through all symbols in use (including ones in buffer area) and
	// converts them to the current skin override for the symbol (if one exists)
	private void convertSymbolsToMatchCurrentSkin()
	{
		if (currentSkinInfo != null && currentSkinInfo.symbolReplacements != null && currentSkinInfo.symbolReplacements.Count > 0)
		{
			// Go through all symbols in use (including ones in buffer area) and convert
			// them to the current skin version
			List<SlotSymbol> allSymbols = reelGame.engine.getAllSymbolsOnReels();

			for (int i = 0; i < allSymbols.Count; i++)
			{
				SlotSymbol currentSymbol = allSymbols[i];

				if (currentSkinInfo.symbolReplacements.ContainsKey(currentSymbol.serverNameWithVariant))
				{
					string newSymbolName = currentSkinInfo.symbolReplacements[currentSymbol.serverNameWithVariant];
					if (currentSymbol.isFlattenedSymbol)
					{
						// need to convert to the flattened version of this skin replacement symbol
						Vector2 symbolSize = currentSymbol.getWidthAndHeightOfSymbol();
						newSymbolName = SlotSymbol.constructNameFromDimensions(newSymbolName + SlotSymbol.FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
					}

					currentSymbol.mutateTo(newSymbolName, null, false, true);
				}
			}
		}
	}

	// executeAfterSymbolSetup() secion
	// Functions in this section are called once a symbol has been setup.
	public override bool needsToExecuteAfterSymbolSetup(SlotSymbol symbol)
	{
		// check if the current skin has symbol overrides that need to apply to this symbol
		if (currentSkinInfo != null && currentSkinInfo.symbolReplacements != null && currentSkinInfo.symbolReplacements.ContainsKey(symbol.serverName))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override void executeAfterSymbolSetup(SlotSymbol symbol)
	{
		// swap the symbol for the current skin replacement
		string newSymbolName = currentSkinInfo.symbolReplacements[symbol.serverName];

		if (symbol.serverName != newSymbolName && newSymbolName != "")
		{
			if (symbol.isFlattenedSymbol)
			{
				// need to convert to the flattened version of this skin replacement symbol
				Vector2 symbolSize = symbol.getWidthAndHeightOfSymbol();
				newSymbolName = SlotSymbol.constructNameFromDimensions(newSymbolName + SlotSymbol.FLATTENED_SYMBOL_POSTFIX, (int)symbolSize.x, (int)symbolSize.y);
			}

			if (symbol.name != newSymbolName)
			{
				symbol.mutateTo(newSymbolName, null, false, true);
			}
		}
	}

	// needsToOverridePaytableSymbolName() section
	// Used for games to override what symbol prefab gets loaded for a given symbol
	// for the paytable.  Useful for games like billions01 that have two versions of
	// majors, or games where you want to swap to another prefab for some reason.
	public override bool needsToOverridePaytableSymbolName(string name)
	{
		if (currentSkinInfo != null && currentSkinInfo.symbolReplacements != null && currentSkinInfo.symbolReplacements.ContainsKey(name))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public override string getOverridePaytableSymbolName(string name)
	{
		return currentSkinInfo.symbolReplacements[name];
	}

	// executeOverridePaylineSounds(string symbolName)
	// allow payline sound to be overridded
	public override bool needsToOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		if (currentSkinInfo != null && currentSkinInfo.symbolSoundOverrideDict != null && currentSkinInfo.symbolSoundOverrideDict.ContainsKey(winningSymbolName))
		{
			SymbolSoundOverrideEntry soundOverrideEntry = currentSkinInfo.symbolSoundOverrideDict[winningSymbolName];
			return soundOverrideEntry.soundList != null && soundOverrideEntry.soundList.Count > 0;
		}
		else
		{
			return false;
		}
	}

	public override void executeOverridePaylineSounds(List<SlotSymbol> slotSymbols, string winningSymbolName)
	{
		SymbolSoundOverrideEntry soundOverrideEntry = currentSkinInfo.symbolSoundOverrideDict[winningSymbolName];
		StartCoroutine(AudioListController.playListOfAudioInformation(soundOverrideEntry.soundList));
	}
}
