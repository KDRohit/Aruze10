using UnityEngine;
using System.Collections;
using TMPro;

public class SmartBetSelector : SelectBet
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private GameObject descriptionLabel;
	[SerializeField] private GameObject betButtonsParent;
	[SerializeField] private GameObject closeButton;
	[SerializeField] private GameObject topButtonsParent;
	[SerializeField] private TextMeshPro bottomLabel;
	[SerializeField] private SmartBetButton[] smartBetButtons;		// Data for the coins and locks, ordered from bottom of the dialog to the top, i.e. [0] is tied to the lowest bet

	[SerializeField] private Renderer gameTexture;
	[SerializeField] private Renderer multiProgressiveGameTexture;

	[SerializeField] private Animator arrowAnimator;
	[SerializeField] private GameObject increasedChanceIcon;

	[SerializeField] private GameObject burstEffect;
	[SerializeField] private Animator burstAnimator;

	[SerializeField] private GameObject jackpotTextPanel;

	[SerializeField] private GameObject decoratorAnchor;
	private string currentFeatureType;
	private ProgressiveJackpot pj = null;

	// =============================
	// CONST
	// =============================
	private const string HIGHLIGHT_ONE = "highlight_1";
	private const string HIGHLIGHT_TWO = "highlight_2";
	private const string HIGHLIGHT_THREE = "highlight_3";
	private const string HIGHLIGHT_FOUR = "highlight_4";
	private static string[] arrowAnimations = new string[]{ HIGHLIGHT_ONE, HIGHLIGHT_TWO, HIGHLIGHT_THREE, HIGHLIGHT_FOUR };

	public override void init()
	{
		Audio.play("minimenuopen0");

		bool isShowingCloseButton = (bool)dialogArgs.getWithDefault(D.SHOW_CLOSE_BUTTON, false);

		if (closeButton != null)
		{
			closeButton.SetActive(isShowingCloseButton);
		}

		string gameKey = dialogArgs.getWithDefault(D.GAME_KEY, "") as string;
		string featureType = dialogArgs.getWithDefault(D.FEATURE_TYPE, "jackpot") as string;

		if (gameKey != "")
		{
			// grab the lobby info for this game
			gameInfo = LobbyGame.find(gameKey);
		}
		else
		{
			// fallback to checking if we are already in a game and can just grab it
			gameInfo = GameState.game;
		}

		string filename = SlotResourceMap.getLobbyImagePath(gameInfo.groupInfo.keyName, gameInfo.keyName, gameInfo.isMultiProgressive ? "" : "1X2");
		if (!gameInfo.isMultiProgressive)
		{
			multiProgressiveGameTexture.gameObject.SetActive(false);
			DisplayAsset.loadTextureToRenderer(gameTexture, filename, skipBundleMapping:true, pathExtension:".png");
		}
		else
		{
			gameTexture.gameObject.SetActive(false);
			DisplayAsset.loadTextureToRenderer(multiProgressiveGameTexture, filename, skipBundleMapping:true, pathExtension:".png");
		}


		long minBet = 0L;
		buttonValues = setInitialSmartWagers(betButtons, ref minBet, gameInfo, smartBetButtons, featureType);

		pj = null;
		if (gameInfo.progressiveJackpots != null)
		{
			jackpotTextPanel.SetActive(true);
			pj = gameInfo.progressiveJackpots[gameInfo.progressiveJackpots.Count - 1];
		}
		else
		{
			jackpotTextPanel.SetActive(false);
		}

		setupFeatureTopper(featureType, gameInfo.isMultiProgressive);

		increasedChanceIcon.SetActive((MysteryGift.isIncreasedMysteryGiftChance && currentFeatureType == "mystery_gift") ||
									  (MysteryGift.isIncreasedBigSliceChance && currentFeatureType == "big_slice"));

		StatsManager.Instance.LogCount
		(
			"dialog",
			"bet_selector",
			currentFeatureType,
			gameInfo.keyName,
			"",
			"view"
		);
	}

	protected void setupFeatureTopper(string featureType, bool isMultiProgressive = false)
	{
		if (featureType == "NONE" || featureType == "jackpot")
		{
			if (isMultiProgressive)
			{
				currentFeatureType = "multi_progressive_jackpot";
			}
			else
			{
				currentFeatureType = "progressive_jackpot";
			}
		}
		else
		{
			currentFeatureType = featureType == MysteryGiftType.MYSTERY_GIFT.ToString() ? "mystery_gift" : "big_slice";
		}
			
		switch(currentFeatureType)
		{
			case "multi_progressive_jackpot":
				if (MultiJackpotLobbyOptionDecorator1x2.overlayPrefab != null)
				{
					setupMultiJackpotHeader();
				}
				else
				{
					AssetBundleManager.load(this, MultiJackpotLobbyOptionDecorator1x2.LOBBY_PREFAB_PATH, multiJackpotPrefabLoadSuccess, multiJackpotPrefabLoadFailed);
				}
				break;

			case "progressive_jackpot":
				if (pj != ProgressiveJackpot.giantJackpot)
				{
					if (JackpotLobbyOptionDecorator1x2.getOverlayPrefabForType(JackpotLobbyOptionDecorator.JackpotTypeEnum.Default) != null)
					{
						setupSingleJackpotHeader();
					}
					else
					{
						AssetBundleManager.load(JackpotLobbyOptionDecorator1x2.LOBBY_DEFAULT_PREFAB_PATH, singleJackpotPrefabLoadSuccess, singleJackpotPrefabLoadFailed);
					}
				}
				else
				{
					GiantJackpotLobbyOptionDecorator1x2.loadPrefab(decoratorAnchor, null);
					GiantJackpotLobbyOptionDecorator jackpotDecorator = decoratorAnchor.GetComponentInChildren<GiantJackpotLobbyOptionDecorator>();
					if (jackpotDecorator != null)
					{
						pj.registerLabel(jackpotDecorator.jackpotTMPro);
					}
				}
				break;

			case "mystery_gift":
				MysteryGiftLobbyOptionDecorator1x2.loadPrefab(decoratorAnchor, null);
				break;

			case "big_slice":
				BigSliceLobbyOptionDecorator1x2.loadPrefab(decoratorAnchor, null);
				break;
		}

		int numValidWagers = -1;
		string wagerSet = SlotsWagerSets.getWagerSetForGame(gameInfo.keyName);
		for (int i = 0; i < buttonValues.Length; ++i)
		{
			int playerLevel = SlotsPlayer.instance.socialMember.experienceLevel;
			int lockedLevel = SlotsWagerSets.getWagerUnlockLevel(wagerSet, buttonValues[i]);

			if (buttonValues[i] >= gameInfo.specialGameMinQualifyingAmount)
			{
				numValidWagers++;
			}
		}

		if (numValidWagers < 0)
		{
			Debug.LogError("SmartBetSelector: No valid wagers for user, this should never happen.");
			return;
		}

		arrowAnimator.Play(arrowAnimations[numValidWagers]);
	}

	public void multiJackpotPrefabLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		MultiJackpotLobbyOptionDecorator1x2.overlayPrefab = obj as GameObject;
		setupMultiJackpotHeader();
	}

	public void multiJackpotPrefabLoadFailed(string assetPath, Dict data = null)
	{
		Debug.LogError("SmartBetSelector::prefab load failure - Failed to load asset at: " + assetPath);
	}

	private void setupMultiJackpotHeader()
	{
		if (decoratorAnchor != null)
		{
			MultiJackpotLobbyOptionDecorator1x2.loadPrefab(decoratorAnchor, null);
			MultiJackpotLobbyOptionDecorator1x2 multiJackpotDecorator = decoratorAnchor.GetComponentInChildren<MultiJackpotLobbyOptionDecorator1x2>();
			if (multiJackpotDecorator != null)
			{
				if (gameInfo != null)
				{
					gameInfo.registerMultiProgressiveLabels(multiJackpotDecorator.jackpotLabels, false);
				}
			}
		}
	}

	public void singleJackpotPrefabLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		JackpotLobbyOptionDecorator1x2.setOverlayPrefabForType(JackpotLobbyOptionDecorator.JackpotTypeEnum.Default, obj as GameObject);
		setupSingleJackpotHeader();
	}

	public void singleJackpotPrefabLoadFailed(string assetPath, Dict data = null)
	{
		Debug.LogError("SmartBetSelector::prefab load failure - Failed to load asset at: " + assetPath);
	}

	private void setupSingleJackpotHeader()
	{
		if (this != null && decoratorAnchor != null)
		{
			JackpotLobbyOptionDecorator1x2.loadPrefab(decoratorAnchor, null, JackpotLobbyOptionDecorator.JackpotTypeEnum.Default);
			JackpotLobbyOptionDecorator1x2 jackpotDecorator = decoratorAnchor.GetComponentInChildren<JackpotLobbyOptionDecorator1x2>();
			if (jackpotDecorator != null && jackpotDecorator.jackpotTMPro != null && pj != null)
			{
				pj.registerLabel(jackpotDecorator.jackpotTMPro);
			}
		}
	}

	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}

	/// Used by UIButtonMessage
	public void closeClicked()
	{
		Dialog.close();

		StatsManager.Instance.LogCount
		(
			"dialog",
			"bet_selector",
			currentFeatureType,
			gameInfo.keyName,
			"",
			"close"
		);
	}

	protected void clickBetButton(GameObject go)
	{
		if (buttonValues != null)
		{
			int index = System.Array.IndexOf(betButtons, go);
			long selectedAmount = 0;

			if (index < buttonValues.Length && index >= 0)
			{
				selectedAmount = buttonValues[index];
			}

			string wagerSet = SlotsWagerSets.getWagerSetForGame(gameInfo.keyName);
			if (SlotsWagerSets.isAbleToWager(wagerSet, selectedAmount))
			{
				Vector3 buttonPos = go.transform.localPosition;
				burstEffect.transform.localPosition = new Vector3(buttonPos.x, buttonPos.y, buttonPos.z);
				burstEffect.SetActive(true);
				burstAnimator.Play("pressed");

				dialogArgs.merge(D.ANSWER, selectedAmount);
				Dialog.close();

				StatsManager.Instance.LogCount
				(
					"dialog",
					"bet_selector",
					currentFeatureType,
					gameInfo.keyName,
					index.ToString(),
					"click",
					CreditsEconomy.multipliedCredits(selectedAmount),
					CreditsEconomy.multipliedCredits(SlotsPlayer.creditAmount).ToString()
				);
			}
		}		
	}

	public static long[] setInitialSmartWagers
	(
		GameObject[] betButtons,
		ref long minBet,
		LobbyGame gameInfo,
		SmartBetButton[] smartBetButtons,
		string type = "jackpot"  
	)
	{
		string wagerSet = SlotsWagerSets.getWagerSetForGame(gameInfo.keyName);
		long[] buttonValues = gameInfo.getSmartBetValues(wagerSet);

		minBet = gameInfo.specialGameMinQualifyingAmount;

		if (smartBetButtons != null)
		{
			for (int i = 0; i < smartBetButtons.Length; ++i)
			{		
				smartBetButtons[i].refresh(wagerSet, buttonValues[i], minBet);
			}
		}

		if (minBet == 0L)
		{
			Debug.LogError("Progressive Bet Dialog: Didn't find a suitable minimum bet amount.");
		}
		else
		{
			// Set the minimum bet label on the overlay jackpot tooltip.
			//Overlay.instance.jackpotMystery.jackpotMinBetTMPro.text = Localize.textUpper("min_bet_{0}", CommonText.formatNumber(minBet));
		}

		return buttonValues;
	}

	/*=========================================================================================
	SHOW DIALOG
	=========================================================================================*/
	public static void showDialog(Dict args)
	{
		string gameKey = args.getWithDefault(D.GAME_KEY, "") as string;
		LobbyGame gameInfo = null;

		if (gameKey != "")
		{
			// grab the lobby info for this game
			gameInfo = LobbyGame.find(gameKey);
		}
		else
		{
			// fallback to checking if we are already in a game and can just grab it
			gameInfo = GameState.game;
		}

		if (gameInfo != null)
		{
			// Multiprogressive mode uses a 1X1 image, which is achieved by passing in "".
			string imageSize = (gameInfo.isMultiProgressive ? "" : "1X2");
			string filename = SlotResourceMap.getLobbyImagePath(gameInfo.groupInfo.keyName, gameInfo.keyName, imageSize);

			Dialog.instance.showDialogAfterDownloadingTextures("smart_bet_select", nonMappedBundledTextures:new string[]{filename}, args:args);
		}
		else
		{
			Debug.LogError("SmartBetSelector::showDialog() - gameInfo is null and couldn't be found!  Dialog will not be shown!");
		}
	}
}

[System.Serializable]
public class SmartBetButton
{
	public GameObject lockState;
	public GameObject unlockState;
	public GameObject qualified;
	public GameObject notQualified;

	public TextMeshPro lockText;
	public TextMeshPro betText;
	public GameObject sheen;

	public const float DEFAULT_TEXT_LEFT = 0f;
	public const float LOCKED_TEXT_LEFT = 93f;

	public void refresh(string wagerSet, long wagerValue, long minQBet)
	{
		int lockedLevel = SlotsWagerSets.getWagerUnlockLevel(wagerSet, wagerValue);
		TextContainer container = betText.gameObject.GetComponent<TextContainer>();

		if (!SlotsWagerSets.isAbleToWager(wagerSet, wagerValue))
		{
			lockState.SetActive(true);
			notQualified.SetActive(true);
			unlockState.SetActive(false);
			qualified.SetActive(false);
			SafeSet.gameObjectActive(sheen, false);

			if (container != null)
			{
				Vector4 lockedMargin = container.margins;
				container.margins = new Vector4(LOCKED_TEXT_LEFT, lockedMargin.y, lockedMargin.z, lockedMargin.w);
			}
		}
		else
		{
			lockState.SetActive(false);
			unlockState.SetActive(true);
			notQualified.SetActive(wagerValue < minQBet);
			qualified.SetActive(wagerValue >= minQBet);
			SafeSet.gameObjectActive(sheen, true);

			if (container != null)
			{
				Vector4 margin = container.margins;
				container.margins = new Vector4(DEFAULT_TEXT_LEFT, margin.y, margin.z, margin.w);
			}
		}

		string wager = CreditsEconomy.convertCredits(wagerValue).ToString();
		betText.text = wager;
		lockText.text = lockedLevel.ToString();
	}
}
