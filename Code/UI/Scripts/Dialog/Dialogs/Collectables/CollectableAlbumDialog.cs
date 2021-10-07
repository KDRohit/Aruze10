using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using Com.Scheduler;

public class CollectableAlbumDialog : DialogBase
{
	public const string FTUE_SOUND_KEY = "MessageIn1FTUE";

	public CollectableTrophy[] trophyCheckMarks;
	public AlbumDialogSetView setView;
	public UISprite albumCompleteShroud;
	public BonusPackInfoDialog starPackInfoDialog; // Not really a dialog.
	public GameObject trophyParent;
	public TextMeshPro trophyMessage;

	public GameObject powerupsContainer;

	public TextMeshPro eventEndsText;
	public TextMeshPro jackpotAmountText;
	public TextMeshPro progressText;
	public TextMeshPro headerText;

	public ButtonHandler closeButton;
	public ButtonHandler meterButton;
	public ButtonHandler ftueReviewButton;
	public ClickHandler ftueFullScreenClickHandler;
	public ButtonHandler ftueSkipButton;
	// So we can create sets as needed.
	public CollectableSet setLink;
	private CollectableSet[] createdSets;

	public GameObject setParent;

	private ForcedFlow ftue;

	private const int SET_SPACING_X = 800;
	private const int SET_SPACING_Y = 500;
	private const int SETS_PER_ROW = 3;
	private const int TROPHY_PARENT_OFFSET = 600;
	private const int POWERUPS_CONTAINER_SPACING = 500;
	private const int SLIDE_CONTROLLER_OFFSET = 350;

	public SlideController slideController;
	public SwipeArea swipeArea;
	public TextMeshProMasker masker;

	public CollectionsDuplicateMeter starMeter;

	private CollectableSet currentFtueSet;
	private CollectableCard cardToPointAt;

	public Animator fingerAnim;
	public Animator eventEndingAnim;

	// These are screens that get layered over the dialog. They show specficis of sets/cards
	public GameObject albumCompleteViewParent;
	public GameObject setViewParent;
	public GameObject normalViewParent;

	// FTUE pieces
	public GameObject pointingFinger;
	public GameObject ftueBottomPanel;
	public ButtonHandler ftueButton;
	public TextMeshPro ftueBottomText;
	public TextMeshPro stepCounter;

	public static event System.Action ftueSkipped;

	public Renderer[] packLogos;

	[SerializeField] private Renderer background;

	private CollectableAlbum albumToCheck;

	private string albumName = "";
	private string currentFtueStepPhylum = "";

	private Dictionary<string, Texture2D> loadedSetTexturesDict = new Dictionary<string, Texture2D>();
	private Texture2D setContainerTexture;
	private bool isComplete = false;
	public bool manualFtueStart { get; private set; }

	private const float CARD_POINTER_OFFSET = 0.2f;
	private const string TROPHY_BASE_NAME = "hir_collection_{0}_";
	private const string FLASHING_ANIM = "flashing";

	private List<CollectableSetData> setDatas = null;
	private bool allContainerBundlesLoaded = false;
	
	private bool isPowerupsSetIncluded = false;
	private bool isFtue = false;

	public override void init()
	{
		allContainerBundlesLoaded = false;

		closeButton.registerEventDelegate(onClickClose);

		albumName = (string)dialogArgs.getWithDefault(D.KEY, "");

		if (string.IsNullOrEmpty(albumName))
		{
			Debug.LogError("CollectableAlbumDialog::init - missing album name we can't show album dialog.");
			Dialog.close(this);
			return;
		}
		
		Collectables.registerCollectionEndHandler(onCollectionsEnd);

		string viewSource = (string)dialogArgs.getWithDefault(D.REASON, "");
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "collection",
			klass: albumName,
			family: viewSource,
			genus: "view");
		setDatas = Collectables.Instance.getSetsFromAlbum(albumName);
		if (setDatas == null)
		{
			Debug.LogError("Null sets for: " + albumName);
		}
		albumToCheck = Collectables.Instance.getAlbumByKey(albumName);

		CollectableSet.dialogHandle = this;
		starMeter.init(albumToCheck);


		if (Collectables.isActive())
		{
			if (Collectables.endTimer.timeRemaining <= Common.SECONDS_PER_DAY)
			{
				eventEndingAnim.Play(FLASHING_ANIM);
			}

			if (Collectables.endTimer.timeRemaining <= Common.SECONDS_PER_DAY * 7)
			{
				eventEndsText.text = "Event Ends: ";
				Collectables.endTimer.registerLabel(eventEndsText, GameTimerRange.TimeFormat.REMAINING, true);
			}
			else
			{
				eventEndsText.text = "Event Ends: " + CommonText.formatDate(Collectables.timeUntilEnd).Split(',')[0];
			}	
		}
		else
		{
			eventEndsText.text = "Event Over";
		}

		jackpotAmountText.text = CreditsEconomy.convertCredits(albumToCheck.rewardAmount);

		int completedAlbumCount = 0;
		int totalAlbumCount = setDatas.Count;
		if (setDatas != null)
		{
			for (int i = 0; i < setDatas.Count; i++)
			{
				if (setDatas[i].isComplete && setDatas[i].countsTowardAlbumCompletion)
				{
					completedAlbumCount++;
				}

				if (!setDatas[i].countsTowardAlbumCompletion)
				{
					totalAlbumCount--;
				}
				
				if (setDatas[i].isPowerupsSet && ExperimentWrapper.Powerups.isInExperiment)
				{
					isPowerupsSetIncluded = true;
					break;
				}
			}

			progressText.text = string.Format("{0}/{1} Sets Completed", completedAlbumCount, totalAlbumCount);
			int powerupsOffset = isPowerupsSetIncluded ? POWERUPS_CONTAINER_SPACING : 0;
			slideController.setBounds((setDatas.Count / SETS_PER_ROW) * SET_SPACING_Y + powerupsOffset, 0);
		}
		else
		{
			progressText.text = "";
		}


		long rewardAmount = (long)dialogArgs.getWithDefault(D.AMOUNT, 0L);
		if (rewardAmount > 0)
		{
			isComplete = true;
			normalViewParent.SetActive(false);
			loadAlbumCompleteAnimations(rewardAmount);
		}

		if (isComplete || (bool)dialogArgs.getWithDefault(D.OPTION, false) || setDatas == null || completedAlbumCount == setDatas.Count)
		{
			ftueReviewButton.gameObject.SetActive(false);
			progressText.color = new Color(0.0f, 1.0f, 0.0f);

			headerText.color = new Color(0.0f, 1.0f, 0.0f);
			headerText.text = "Collection Complete!";
		}


		AssetBundleManager.load(albumToCheck.backgroundTexturePath, backgroundLoadedSuccess, bundleLoadFail);
		AssetBundleManager.load(albumToCheck.setContainerTexturePath, setContainerLoadSuccess, setContainerLoadFail);
		if (setDatas != null)
		{
			AssetBundleManager.load(albumToCheck.logoTexturePath, logoLoadedSuccess, bundleLoadFail);
		}
		else
		{
			allContainerBundlesLoaded = true;
		}

		meterButton.registerEventDelegate(onClickMeterButton);
		
		ftueReviewButton.registerEventDelegate(onClickReviewButton);
	
		Achievement achievement = null;
		string achievementAlbumKey = albumToCheck.completionAchievement.Replace("complete_collection_album_", "");
		string trophyName = string.Format(TROPHY_BASE_NAME, achievementAlbumKey);
		if (NetworkAchievements.allAchievements.ContainsKey("hir"))
		{
			for (int i = 0; i < trophyCheckMarks.Length; i++)
			{
				trophyCheckMarks[i].gameObject.SetActive(false);
				if (NetworkAchievements.allAchievements["hir"].TryGetValue(trophyName + (i + 1), out achievement))
				{
					StartCoroutine(DisplayAsset.loadTextureFromBundle(
							primaryPath:achievement.localURL,
							callback:trophyTextureLoaded,
							data:Dict.create(D.INDEX, i, D.ACHIEVEMENT, achievement),
							secondaryPath:achievement.trophyURL,
							isExplicitPath:false,
							loadingPanel:true,
							onDownloadFailed:null,
							skipBundleMapping:true,
							pathExtension:".png"
						));
				}
				else
				{
					Debug.LogError("Missing key in all achievements for collectables trophy: " + (trophyName + (i + 1)));
					trophyCheckMarks[i].checkMark.SetActive(false);
					trophyCheckMarks[i].trophyImage.color = new Color32(82, 36, 92, 255);
					trophyCheckMarks[i].sparkles.SetActive(false);
					trophyCheckMarks[i].trophyTitle.text = string.Empty;
				}
			}
		}
		else
		{
			GameObject.Destroy(trophyParent);
			slideController.Update();
			
			
		}
	}

	private void trophyTextureLoaded(Texture2D tex, Dict args)
	{
		int index = (int)args.getWithDefault(D.INDEX, -1);
		Achievement achievement = (Achievement)args.getWithDefault(D.ACHIEVEMENT, null);

		if (this == null || index == -1 || achievement == null)
		{
			return;
		}
		
		trophyCheckMarks[index].checkMark.SetActive(achievement.isUnlocked());
		trophyCheckMarks[index].sparkles.SetActive(achievement.isUnlocked());
		if (!achievement.isUnlocked())
		{
			trophyCheckMarks[index].trophyImage.color = new Color32(82, 36, 92, 255);
		}
		trophyCheckMarks[index].trophyTitle.text = achievement.name;
		
		trophyCheckMarks[index].trophyImage.material = new Material(trophyCheckMarks[index].trophyImage.material);
		trophyCheckMarks[index].trophyImage.mainTexture = tex;
		trophyCheckMarks[index].gameObject.SetActive(true);
	}


	private void onCollectionsEnd(object sender, System.EventArgs e)
	{
		if (this.gameObject != null)
		{
			Dialog.close(this);
		}
	}
	void Update()
	{
		//finish init when texture atlas is built
		if (allContainerBundlesLoaded)
		{
			onLoadAllContainerBundles();
		}

		if (Collectables.endTimer == null)
		{
			eventEndsText.text = "event Ends: ";
		}
		else if (Collectables.endTimer.timeRemaining <= Common.SECONDS_PER_DAY * 7)
		{
			eventEndsText.text = "Event Ends: " + Collectables.endTimer.timeRemainingFormatted;
		}
	}

	public bool loadAndShowSetCards(CollectableSetData dataToUse)
	{
		if (this == null || this.gameObject == null)
		{
			return false;
		}
		normalViewParent.SetActive(false);
		GameObject createdSetView = NGUITools.AddChild(setViewParent.gameObject, setView.gameObject);
		setViewParent.SetActive(true);

		if (createdSetView != null)
		{
			AlbumDialogSetView setViewHandle = createdSetView.GetComponent<AlbumDialogSetView>();
			if (setViewHandle != null)
			{
				setViewHandle.init(dataToUse, ftue);
				setViewHandle.closeButton.registerEventDelegate(onHideSetView);
			}
			else
			{
				Debug.LogError("Missing created set view reference");
				return false;
			}
		}
		else
		{
			Debug.LogError("Missing created gameobject reference");
			return false;
		}

		return true;
	}

	public void loadAlbumCompleteAnimations(long rewardAmount)
	{
		albumCompleteViewParent.SetActive(true);
		albumCompleteShroud.gameObject.SetActive(true); //Turn on the shroud to also eat inputs while we load
		AssetBundleManager.load(albumToCheck.albumRewardPrefabPath, completeScreenLoadSuccess, completeScreenLoadFailed, Dict.create(D.AMOUNT, rewardAmount));
	}

	private void completeScreenLoadFailed(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load set image at " + assetPath);
	}

	private void completeScreenLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this != null && this.gameObject != null)
		{
			GameObject completePresentation = NGUITools.AddChild(albumCompleteViewParent, obj as GameObject, true);
			if (completePresentation != null)
			{
				long rewardAmount = (long)data.getWithDefault(D.AMOUNT, 0);
				AlbumDialogCompleteView completeViewHandle = completePresentation.GetComponent<AlbumDialogCompleteView>();
				completeViewHandle.init(rewardAmount, normalViewParent, albumCompleteShroud);
			}
		}
	}

	private void onClickClose(Dict args = null)
	{
		Audio.play("ClickXCollections");

		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "collection",
			klass: albumName,
			family: "close",
			genus: "click");
		
		if (isComplete)
		{
			string eventId = (string)dialogArgs.getWithDefault(D.EVENT_ID, "");
			CollectablesPlayAgainDialog.showDialog(eventId);
		}

		if ((bool)dialogArgs.getWithDefault(D.OPTION, false)) //True if we're intentionally reviewing the Collection after the Play Again dialog
		{
			Collectables.Instance.resetAlbum(Collectables.currentAlbum);
		}

		if (MainLobbyBottomOverlay.instance != null)
		{
			MainLobbyBottomOverlay.instance.initNewCardsAlert();
		}

		Dialog.close();
	}

	private void onClickReviewButton(Dict args)
	{
		AssetBundleManager.downloadAndCacheBundle("features_ftue"); //load the ftue sounds
		manualFtueStart = true;
		ftueSkipButton.gameObject.SetActive(true);
		ftueSkipButton.registerEventDelegate(onFtueSkipClicked);
		displayFtue();
	}

	private void onFtueSkipClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: currentFtueStepPhylum,
			klass: albumName,
			family: "skip",
			genus: "click");
		ftue.clearSteps();
		ftue = null;
		if (ftueSkipped != null)
		{
			ftueSkipped();
		}
		meterButton.enabled = true;
		ftueReviewButton.enabled = true;
		CollectableSet.setButtonEnabled = true;
		swipeArea.enabled = true;
		ftueBottomPanel.SetActive(false);
		pointingFinger.SetActive(false);
		manualFtueStart = false;
		ftueFullScreenClickHandler.gameObject.SetActive(false);
	}

	private void displayFtue()
	{
		ftue = new ForcedFlow();
		ftue.addStep(ftueStart);
		ftue.addStep(showSetTooltips);
		ftue.addStep(showCardInfo);
		ftue.addStep(closeCardInfo);
		ftue.addStep(returnSet);
		ftue.addStep(returnToColection);
		ftue.addStep(returnToLobby);
		ftue.addStep(finishFTUE);

		// This will call FTUE start. That'll setup the initial step. Once we call complete on it,
		// it should call showSetTooltips, which will bring us to the set view.
		ftue.completeCurrentStep();
	}

	private void onClickMeterButton(Dict args = null)
	{
		if (starPackInfoDialog != null && starPackInfoDialog.gameObject != null)
		{
			setViewParent.SetActive(true);
			GameObject starPackInfo = NGUITools.AddChild(setViewParent, starPackInfoDialog.gameObject);

			StatsManager.Instance.LogCount(counterName:"dialog",
				kingdom: "hir_collection",
				phylum: "extras_bonus",
				genus: "view");
		}
		else
		{
			Debug.LogError("Star Pack Info Dialog is missing");
		}
	}

	private void onHideSetView(Dict args = null)
	{
		if (MainLobbyBottomOverlay.instance != null)
		{
			MainLobbyBottomOverlay.instance.initNewCardsAlert();
		}

		if (createdSets == null)
		{
			Debug.LogError("CollectableAlbumDialog::onHideSetView - cached card set list was null! badging won't update!");
		}

		for (int i = 0; i < createdSets.Length; i++)
		{
			if (setDatas[i].isPowerupsSet)
			{
				//special set; do nothing
				continue;
			}
			if (createdSets[i] != null)
			{
				createdSets[i].updateSetCounts();
			}
			else 
			{
				Debug.LogError("CollectableAlbumDialog::onHideSetView - Card set at index " + i + " was null and will not be updated");
			}
		}
		normalViewParent.SetActive(true);
	}

	private void bundleLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load set image at " + assetPath);
	}

	private void logoLoadedSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this != null && this.gameObject != null)
		{
			if (packLogos.Length > 0)
			{
				for (int i = 0; i < packLogos.Length; i++)
				{
					Material material = new Material(packLogos[i].material.shader);
					material.mainTexture = obj as Texture2D;
					packLogos[i].material = material;
					packLogos[i].gameObject.SetActive(true);
				}
			}
		}
	}

	private void setContainerLoadFail(string assetPath, Dict data = null)
	{
		allContainerBundlesLoaded = true;
	}

	private void setContainerLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this != null && this.gameObject != null)
		{
			Texture2D loadedTexture = obj as Texture2D;
			loadedTexture.name = Path.GetFileName(assetPath);
			loadedSetTexturesDict.Add(loadedTexture.name, loadedTexture);
			if (loadedTexture != null)
			{
				for (int i = 0; i < setDatas.Count; i++)
				{
					//Powerups set dont have set container texture
					if (setDatas[i].isPowerupsSet)
					{
						continue;
					}
					AssetBundleManager.load(setDatas[i].texturePath, containerBundleLoadSuccess,
							containerBundleLoadFail, Dict.create(D.DATA, i));
				}
			}
			else
			{
				allContainerBundlesLoaded = true;
			}
		}
	}

	private void backgroundLoadedSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this != null && this.gameObject != null)
		{
			Material material = new Material(background.material.shader);
			material.mainTexture = obj as Texture2D;
			background.material = material;
		}
	}

	private void containerBundleLoadFail(string assetPath, Dict data = null)
	{
		//call basic load fail for logging
		bundleLoadFail(assetPath, data);

		//add null entry to texture dict and call callback if we've completed all bundle loads
		string fileName = Path.GetFileName(assetPath) ?? CommonText.formatNumber((int)data[D.DATA]);  //ensure no duplicates by using index as default
		loadedSetTexturesDict.Add(fileName, null);

		//set bundles load finished flag if last bundle
		if (loadedSetTexturesDict.Count == setDatas.Count + 1)
		{
			allContainerBundlesLoaded = true;
		}
	}

	private void containerBundleLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		Texture2D loadedTexture = obj as Texture2D;
		string fileName = Path.GetFileName(assetPath) ?? CommonText.formatNumber((int)data[D.DATA]);  //ensure no duplicates by using index as default
		if (loadedTexture != null)
		{
			loadedTexture.name = fileName;
		}
		loadedSetTexturesDict.Add(fileName, loadedTexture);

		//set bundles load finished flag if last bundle
		int count = isPowerupsSetIncluded ? setDatas.Count : setDatas.Count - 1;
		if (loadedSetTexturesDict.Count == count)
		{
			allContainerBundlesLoaded = true;
		}
	}

	private bool areAllTexturesValid()
	{
		if (setDatas == null || loadedSetTexturesDict == null)
		{
			return false;
		}

		foreach (Texture2D tex in loadedSetTexturesDict.Values)
		{
			if (tex == null)
			{
				return false;
			}
		}

		return true;
	}

	private void onLoadAllContainerBundles()
	{
		//turn off so we don't re-run the load routine
		allContainerBundlesLoaded = false;

		//validate the textures
		if (!areAllTexturesValid())
		{
			Debug.LogError("Closing album dialog because some textures did not load");
			Dialog.immediateClose(this);
			return;
		}

		//setup the atlas if using dynamic atlas
		UIAtlas atlas = null;
		if (Collectables.usingDynamicAtlas)
		{
			List<Texture2D> texturesToPack = new List<Texture2D>();
			foreach(KeyValuePair<string, Texture2D> kvp in loadedSetTexturesDict)
			{
				if (kvp.Value == null)
				{
					continue;
				}
				texturesToPack.Add(kvp.Value);
			}
			atlas = DynamicAtlas.createAndAttachAtlas(texturesToPack.ToArray(), gameObject);
		}

		int xLocationToPlaceSet = 0;
		List<TextMeshPro> setTMProObjects = new List<TextMeshPro>();
		for (int i = 0; i < trophyCheckMarks.Length; i++)
		{
			setTMProObjects.Add(trophyCheckMarks[i].trophyTitle);
		}
		setTMProObjects.Add(trophyMessage);
		GameObject setGameobject;
		CollectableSet setHandle;
		createdSets = new CollectableSet[setDatas.Count];
		int powerupsContainerOffset = 0;
		
		if (isPowerupsSetIncluded)
		{
			CommonTransform.setY(powerupsContainer.transform, 0); // negative because it decends downtrophyParent
			powerupsContainer.SetActive(true);
			powerupsContainerOffset = POWERUPS_CONTAINER_SPACING;
		}
		
		for (int i = 0; i < setDatas.Count; i++)
		{
			if (setDatas[i].isPowerupsSet)
			{
				CollectableSet powerupsSet = powerupsContainer.GetComponentInChildren<CollectableSet>();
				if (powerupsSet != null)
				{
					powerupsSet.setup(setDatas[i], atlas, loadedSetTexturesDict);
				}
				setTMProObjects.AddRange(powerupsContainer.GetComponentsInChildren<TextMeshPro>());
				
			}
			else
			{
				// create a set image.
				setGameobject = NGUITools.AddChild(setParent.gameObject, setLink.gameObject);
				setHandle = setGameobject.GetComponent<CollectableSet>();

				if (setHandle != null)
				{
					// Setup should load whatever we need. But we could avoid doing a dynamic atlas and just put
					// set images on their own atlas along with their background images and whatnot.
					setHandle.setup(setDatas[i], atlas, loadedSetTexturesDict);
					createdSets[i] = setHandle;
				}

				CommonTransform.setX(setGameobject.transform, xLocationToPlaceSet);
				CommonTransform.setY(setGameobject.transform,-((i / SETS_PER_ROW) * SET_SPACING_Y) - powerupsContainerOffset); // negative because it decends down
				
				//subtract 2 because this works with or without powerups set present in the album since we show 3 sets on a row the second to last set has the same y value as the last set
				if (i == setDatas.Count - 2 && trophyParent != null)
				{
					//slideController.safleySetYLocation(Mathf.Abs(setGameobject.transform.localPosition.y - SLIDE_CONTROLLER_OFFSET));
					CommonTransform.setY(trophyParent.transform,
						setGameobject.transform.localPosition.y - TROPHY_PARENT_OFFSET); // negative because it decends downtrophyParent
					trophyParent.gameObject.SetActive(NetworkAchievements.allAchievements.Count != 0);
				}
				

				xLocationToPlaceSet += SET_SPACING_X;

				if (xLocationToPlaceSet >= SET_SPACING_X * SETS_PER_ROW)
				{
					xLocationToPlaceSet = 0;
				}

				setTMProObjects.AddRange(setGameobject.GetComponentsInChildren<TextMeshPro>());
			}
		}

		masker.addObjectArrayToList(setTMProObjects.ToArray());

		// If we're in a FTUE
		isFtue = (bool) dialogArgs.getWithDefault(D.DATA, false);
		if (isFtue)
		{
			displayFtue();
		}
		else
		{
			ftueFullScreenClickHandler.gameObject.SetActive(false);
			//GameTimerRange delayTimer = GameTimerRange.createWithTimeRemaining(1);
			Dict args = Dict.create(D.DATA, 0);
			playIntroAfterDelay(args);
			//delayTimer.registerFunction(playIntroAfterDelay, args);
		}

		//Update the bottom overlay in case collections are active now
		if (MainLobbyBottomOverlayV4.instance != null)
		{
			MainLobbyBottomOverlayV4.instance.onCollectionBundleFinished();
		}
		
	}

	#region FTUE

	// TODO: neaten up this ftue
	// A lot of these objects we're doing get component on could be cached or otherwise passed between the functions via the args dict available
	// to the forced flow.
	private void ftueStart(Dict args = null)
	{
		Audio.playWithDelay(FTUE_SOUND_KEY, 0.1f);
		Audio.play("ClickToOpenCollections");
		swipeArea.enabled = false;
		pointingFinger.SetActive(false);
		ftueBottomPanel.SetActive(false);
		ftueBottomText.text = Localize.text("cards_are_in_sets");
		stepCounter.text = Localize.text("step_{0}_of_{1}", 1, 7);

		CollectableSet[] allCollectableSets = setParent.gameObject.GetComponentsInChildren<CollectableSet>();
		
		for (int i = 0; i < allCollectableSets.Length; i++)
		{
			if (allCollectableSets[i] is PowerupCollectionSetUI)
			{
				continue;
			}
			currentFtueSet = allCollectableSets[i];
			if (allCollectableSets[i].count > 0)
			{
				currentFtueSet = allCollectableSets[i];
				break;
			}
		}

		if (currentFtueSet != null)
		{
			Dict locationArgs = Dict.create(D.DATA, (int)currentFtueSet.transform.localPosition.y);
			playIntroAfterDelay(locationArgs);
		}
	}

	private void playIntroAfterDelay(Dict args = null, GameTimerRange sender = null)
	{
		int targetLocation = 0;
		if (args != null && args.containsKey(D.DATA))
		{
			targetLocation = (int)args[D.DATA];
		}

		if (ftue != null)
		{
			slideController.onEndAnimation += onEndFtueScrollup;
		}

		slideController.scrollToAbsoluteVerticalPosition(Mathf.Abs(targetLocation), -18, true);
	}

	private void onEndFtueScrollup(Dict args = null)
	{
		currentFtueStepPhylum = "ftue_collection_step_1";
		
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: currentFtueStepPhylum,
			klass: albumName,
			family: manualFtueStart ? "help" : "ftue",
			genus: "view");
		
		slideController.onEndAnimation -= onEndFtueScrollup;
		CommonTransform.setX(pointingFinger.transform, currentFtueSet.transform.position.x, Space.World);
		CommonTransform.setY(pointingFinger.transform, currentFtueSet.transform.position.y, Space.World);
		pointingFinger.transform.localPosition = new Vector3(pointingFinger.transform.localPosition.x + 300, pointingFinger.transform.localPosition.y - 100, pointingFinger.transform.localPosition.z);
		ftueButton.text = Localize.text("open_set");
		ftueButton.registerEventDelegate(onClickNextFtueButton);
		
		showAfterDelay(null, null);
		
	}

	private void showSetTooltips(Dict args = null)
	{
		currentFtueStepPhylum = "ftue_collection_step_2";

		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: currentFtueStepPhylum,
			klass: albumName,
			family: manualFtueStart ? "help" : "ftue",
			genus: "view");
		
		Audio.playWithDelay(FTUE_SOUND_KEY, 0.1f);
		Audio.play("ClickMoreInfoCollections");
		pointingFinger.SetActive(false);
		ftueBottomPanel.SetActive(false);

		bool loadStarted = false;
		if (currentFtueSet != null)
		{
			if (currentFtueSet.notifParent != null)
			{
				currentFtueSet.notifParent.SetActive(false);
			}
			loadStarted = loadAndShowSetCards(currentFtueSet.data);
		}

		if (loadStarted)
		{
			StartCoroutine(waitForCardsToLoad());
		}
		else
		{
			showAfterDelay(null,null);
		}

		ftueBottomText.text = Localize.text("lets_check_out_card");
		stepCounter.text = Localize.text("step_{0}_of_{1}", 2, 7);
		ftueButton.text = Localize.text("view_card");
		// Step 2, tear down whatever we had from step one, setup step two, done.
	}


	private IEnumerator waitForCardsToLoad()
	{
		AlbumDialogSetView setViewHandle = GetComponentInChildren<AlbumDialogSetView>();
		if (setViewHandle != null)
		{
			while (setViewHandle.content == null || !setViewHandle.content.isFinishedLoadedCards)
			{
				yield return null;
			}

			cardToPointAt = setViewHandle.content.firstCard;

			if (cardToPointAt != null)
			{
				CommonTransform.setX(pointingFinger.transform, cardToPointAt.transform.position.x + CARD_POINTER_OFFSET, Space.World);
				CommonTransform.setY(pointingFinger.transform, cardToPointAt.transform.position.y - CARD_POINTER_OFFSET, Space.World);
			}
		}
		showAfterDelay(null,null);
	}

	private void showCardInfo(Dict args = null)
	{
		Audio.playWithDelay(FTUE_SOUND_KEY, 0.1f);
		pointingFinger.SetActive(false);
		ftueBottomPanel.SetActive(false);
		showAfterDelay(null,null);

		AlbumDialogSetView setViewHandle = GetComponentInChildren<AlbumDialogSetView>();

		if (cardToPointAt != null)
		{
			Dict cardDict = Dict.create(D.DATA, cardToPointAt.data);
			setViewHandle.onClickCard(cardDict);

			AlbumDialogCardView createdCardView = setViewHandle.GetComponentInChildren<AlbumDialogCardView>();
			createdCardView.closeButton.registerEventDelegate(onClickNextFtueButton);

			CommonTransform.setX(pointingFinger.transform, createdCardView.closeButton.transform.position.x, Space.World);
			CommonTransform.setY(pointingFinger.transform, createdCardView.closeButton.transform.position.y, Space.World);
		}
		else
		{
			Debug.LogError("Didn't find a valid card for the FTUE");
		}

		currentFtueStepPhylum = "ftue_collection_step_3";

		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: currentFtueStepPhylum,
			klass: albumName,
			family: manualFtueStart ? "help" : "ftue",
			genus: "view");
		
		ftueBottomText.text = Localize.text("info_on_cards");
		stepCounter.text = Localize.text("step_{0}_of_{1}", 3, 7);
		ftueButton.text = Localize.text("okay");
	}

	private void closeCardInfo(Dict args = null)
	{
		Audio.playWithDelay(FTUE_SOUND_KEY, 0.1f);
		Audio.play("ClickMoreInfoCollections");
		pointingFinger.SetActive(false);
		ftueBottomPanel.SetActive(false);
		showAfterDelay(null,null);

		AlbumDialogSetView setViewHandle = GetComponentInChildren<AlbumDialogSetView>();
		AlbumDialogCardView createdCardView = setViewHandle.GetComponentInChildren<AlbumDialogCardView>();
		pointingFinger.SetActive(true);
		fingerAnim.Play("point intro");
		if (createdCardView != null)
		{
			createdCardView.onClickCloseCardView();
		}
		CommonTransform.setX(pointingFinger.transform, jackpotAmountText.transform.position.x, Space.World);
		CommonTransform.setY(pointingFinger.transform, jackpotAmountText.transform.position.y, Space.World);
		CommonTransform.setY(pointingFinger.transform, pointingFinger.transform.localPosition.y - 30f, Space.Self);

		currentFtueStepPhylum = "ftue_collection_step_4";

		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: currentFtueStepPhylum,
			klass: albumName,
			family: manualFtueStart ? "help" : "ftue",
			genus: "view");
		
		ftueBottomText.text = Localize.text("collect_all_cards_to_win_award");
		stepCounter.text = Localize.text("step_{0}_of_{1}", 4, 7);
		ftueButton.text = Localize.text("continue");
	}

	private void returnSet(Dict args = null)
	{
		Audio.playWithDelay(FTUE_SOUND_KEY, 0.1f);
		Audio.play("ClickGotItCollections");
		pointingFinger.SetActive(false);
		ftueBottomPanel.SetActive(false);
		showAfterDelay(null,null);

		AlbumDialogSetView setViewHandle = GetComponentInChildren<AlbumDialogSetView>();
		pointingFinger.SetActive(true);
		Quaternion rot = Quaternion.Euler(0, 0, -70);
		pointingFinger.transform.localRotation = rot;
		CommonTransform.setX(pointingFinger.transform, setViewHandle.closeButton.transform.position.x, Space.World);
		CommonTransform.setY(pointingFinger.transform, setViewHandle.closeButton.transform.position.y, Space.World);
		CommonTransform.setX(pointingFinger.transform, pointingFinger.transform.localPosition.x + 50f, Space.Self);
		CommonTransform.setY(pointingFinger.transform, pointingFinger.transform.localPosition.y + 20f, Space.Self);

		currentFtueStepPhylum = "ftue_collection_step_5";

		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: currentFtueStepPhylum,
			klass: albumName,
			family: manualFtueStart ? "help" : "ftue",
			genus: "view");

		ftueBottomText.text = Localize.text("lets_go_to_collection");
		stepCounter.text = Localize.text("step_{0}_of_{1}", 5, 7);
		ftueButton.text = Localize.text("close_set");
	}

	private void returnToColection(Dict args = null)
	{
		Audio.playWithDelay(FTUE_SOUND_KEY, 0.1f);
		pointingFinger.SetActive(false);
		ftueBottomPanel.SetActive(false);

		meterButton.enabled = false;
		CollectableSet.setButtonEnabled = false;
		ftueReviewButton.enabled = false;
		Quaternion rot = Quaternion.Euler(0, 0, 0);
		pointingFinger.transform.localRotation = rot;
		AlbumDialogSetView setViewHandle = GetComponentInChildren<AlbumDialogSetView>();
		if (setViewHandle != null)
		{
			setViewHandle.onClickCloseSetView();
		}
		normalViewParent.SetActive(true);
		slideController.scrollToAbsoluteVerticalPosition(slideController.topBound, 20, true);

		if (ftue != null)
		{
			slideController.onEndAnimation += onEndTrophyScrollup;
		}

		currentFtueStepPhylum = "ftue_collection_step_6";

		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: currentFtueStepPhylum,
			klass: albumName,
			family: manualFtueStart ? "help" : "ftue",
			genus: "view");

		ftueBottomText.text = Localize.text("complete_all_sets_for_trophy");
		stepCounter.text = Localize.text("step_{0}_of_{1}", 6, 7);
		ftueButton.text = Localize.text("continue");
	}

	private void onEndTrophyScrollup(Dict args = null)
	{
		slideController.onEndAnimation -= onEndTrophyScrollup;
		CommonTransform.setX(pointingFinger.transform, jackpotAmountText.transform.position.x, Space.World);
		CommonTransform.setY(pointingFinger.transform, jackpotAmountText.transform.position.y, Space.World);
		CommonTransform.setY(pointingFinger.transform, pointingFinger.transform.localPosition.y - 30f, Space.Self);
		showAfterDelay(null,null);
		pointingFinger.SetActive(true);
		fingerAnim.Play("point intro");
	}

	private void returnToLobby(Dict args = null)
	{
		Audio.playWithDelay(FTUE_SOUND_KEY, 0.1f);
		Audio.play("ClickGotItCollections");
		meterButton.enabled = true;
		ftueReviewButton.enabled = true;
		CollectableSet.setButtonEnabled = true;
		pointingFinger.SetActive(false);
		ftueBottomPanel.SetActive(false);
		showAfterDelay(null,null);

		pointingFinger.SetActive(true);
		Quaternion rot = Quaternion.Euler(0, 0, -70);
		pointingFinger.transform.localRotation = rot;
		CommonTransform.setX(pointingFinger.transform, closeButton.transform.position.x, Space.World);
		CommonTransform.setY(pointingFinger.transform, closeButton.transform.position.y, Space.World);

		currentFtueStepPhylum = "ftue_collection_step_7";

		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: currentFtueStepPhylum,
			klass: albumName,
			family: manualFtueStart ? "help" : "ftue",
			genus: "view");

		ftueBottomText.text = Localize.text("play_any_slot_to_collect");
		stepCounter.text = Localize.text("step_{0}_of_{1}", 7, 7);
		ftueButton.text = Localize.text("return_to_lobby");
	}

	private void finishFTUE(Dict args = null)
	{
		Audio.play("ClickXCollections");
		swipeArea.enabled = true;
		Collectables.getPowerupsPack();
		//Call close before StartCollectingDialog.showDialog since both dialogs are marked as BLOCKING
		//otherwise you will be stuck
		Dialog.close();
		if ((manualFtueStart || isFtue) && GameState.game == null)
		{
			StartCollectingDialog.showDialog(manualFtueStart);
		}
		manualFtueStart = false;
	}

	private void showAfterDelay(Dict args, GameTimerRange sender)
	{
		if (this == null)
		{
			return;
		}

		if (ftueFullScreenClickHandler != null && ftueFullScreenClickHandler.gameObject != null)
		{
			ftueFullScreenClickHandler.gameObject.SetActive(true);
			ftueFullScreenClickHandler.registerEventDelegate(onClickNextFtueButton);
		}
		else
		{
			//force user through so the game doesn't soft lock
			Debug.LogError("No click handler on collections ftue");
			onClickNextFtueButton();
		}

		pointingFinger.SetActive(true);
		ftueBottomPanel.SetActive(true);
	}

	public override void close()
	{
		// These are static references that get passed around so we can easily stack these prefabs on top of eachother
		// and share data between them
		CollectableSet.dialogHandle = null;
		CollectableSet.setButtonEnabled = true;
		
		Collectables.unregisterCollectionEndHandler(onCollectionsEnd);

		if (Collectables.endTimer != null)
		{
			Collectables.endTimer.removeLabel(eventEndsText);
		}
	}

	private void onClickNextFtueButton(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: currentFtueStepPhylum,
			klass: albumName,
			family: "continue",
			genus: "click");
		
		if (ftueFullScreenClickHandler != null)
		{
			ftueFullScreenClickHandler.clearAllDelegates();
		}

		ftue.completeCurrentStep();
	}
	#endregion


	// If we ever had multiple albums active at once, knowing what album we were going into would be nice. 
	public static void showDialog(string albumKeyName, string viewSource, bool isFTUE = false, long rewardAmount = 0L, bool isCompleteReview = false, string eventId = "", bool isTopOfList = false)
	{
		Dict args = Dict.create(D.KEY, albumKeyName,
								D.REASON, viewSource,
		                        D.DATA, isFTUE,
								D.AMOUNT, rewardAmount,
								D.OPTION, isCompleteReview,
								D.EVENT_ID, eventId,
								D.IS_TOP_OF_LIST, isTopOfList);
		
		// Make this dialog's priority as blocking for FTUE and for board game
		Scheduler.addTask(new CollectionsAlbumDialogTask("collectables_album", args), isFTUE || (CasinoEmpireBoardGameDialog.isDialogOpen()) ? SchedulerPriority.PriorityType.BLOCKING :SchedulerPriority.PriorityType.HIGH);
	}
}
