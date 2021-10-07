using UnityEngine;
using System.Collections.Generic;
using Com.Scheduler;
using PrizePop;
using Zynga.Payments.IAP;

public class Collectables : IResetGame
{
	public int betIndicatorTimeout = 3;
	public bool hasIncreasedRewards = false;

	//Seasons -> Albums -> Sets -> Cards
	private Dictionary<string, CollectableSeasonData> allSeasons = new Dictionary<string, CollectableSeasonData>();
	private Dictionary<string, CollectableAlbum> allAlbums = new Dictionary <string, CollectableAlbum>();// Season Key Name -> Albums in the season

	// I don't hate these being here, but albums need to know abotu sets too 
	private Dictionary<string, CollectableSetData> allSets = new Dictionary <string, CollectableSetData>(); // Album Key Name -> Sets in the Album 
	private Dictionary<string, CollectableCardData> allCards = new Dictionary <string,CollectableCardData>(); // Set Key Name -> Cards in the Set

	private Dictionary<string, CollectablePackData> allPacks = new Dictionary<string, CollectablePackData>();
	private Dictionary<string, Dictionary<string, Dictionary<int, string>>> challengePackData = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>();

	public string starPackEventId = "";

	private static Collectables instance;
	public static string currentSeason = "";
	public static string currentAlbum = "";
	public static bool showStarMeterToolTip = true; //using this in case we wrap back to 0 so we don't show the star pack tooltip unnecessarily
	public static System.DateTime timeUntilEnd;

	public static int endTimeInt = 0;
	public static GameTimerRange endTimer;

	public static JSON cachedPackJSON;
	public static JSON nextIterationData;
	public static List<string> missingBundles = new List<string>();
	public static bool bundlesReady { get; private set; }
	public static bool usingDynamicAtlas = false;
	private static bool forceFirstPack = false; //Value is set from Livedata key. On webgl this value is always true
	private bool ftueSeen = false;
	public bool hasCards { get; private set; }

	public const string BUY_PAGE_SURFACING_PATH = "Features/Collections/Prefabs/Lobby & Buy Page/Buy Page Packs";
	public const string lobbyButtonPath = "Features/Collections/Prefabs/Lobby & Buy Page/Collection Tab";

	private static event System.EventHandler onCollectionEnd;
	
	public delegate void CollectableDroppedCallback(JSON callbackJson);
	private Dictionary<string, CollectableDroppedCallback> callbackDict = new Dictionary<string, CollectableDroppedCallback>();
	
	public static Collectables Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new Collectables();
			}
			return instance;
		}
	}

	public static bool hasSeenFtue()
	{
		return instance != null && instance.ftueSeen;
	}
	
	public static void markFtueSeen()
	{
		if (instance != null)
		{
			instance.ftueSeen = true;
		}
	}

	public static bool isActive()
	{
		return !string.IsNullOrEmpty(currentAlbum) && isEventTimerActive() && bundlesReady && !isLevelLocked();
	}

	public static bool isLevelLocked()
	{
		return ExperimentWrapper.EUEFeatureUnlocks.isInExperiment &&
		       !EueFeatureUnlocks.isFeatureUnlocked("collections");
	}

	public static bool isEventTimerActive()
	{
		System.DateTime endTime = Common.convertFromUnixTimestampSeconds(endTimeInt);
		bool validTimeRange = endTime > System.DateTime.UtcNow;
		timeUntilEnd = endTime;
		return validTimeRange;
	}

	public static void populateAll(JSON data)
	{
		endTimeInt = Data.liveData.getInt("COLLECTIONS_END_TIME", 0);
		usingDynamicAtlas = Data.liveData.getBool("USE_COLLECTIONS_DYNAMIC_ATLAS", false);
		forceFirstPack = Data.liveData.getBool("COLLECTIONS_FORCE_FIRST_PACK", false);
		//Populate all our global data
		if (data == null)
		{
			Debug.LogError("Null data on login");
			return;
		}

		//Getting all the seasons at the top level
		JSON[] seasonsJson = data.getJsonArray("seasons");
		for (int i = 0; i < seasonsJson.Length; i++)
		{
			CollectableSeasonData collectableSeason = new CollectableSeasonData(seasonsJson[i]);

			if (collectableSeason == null || seasonsJson[i] ==null)
			{
				Debug.LogError("CollectableSeasonData is null");
				continue;
			}
			Instance.allSeasons.Add(collectableSeason.keyName, collectableSeason);

			//Get the list of albums in the current season
			JSON[] albumsJson = seasonsJson[i].getJsonArray("albums");
			for (int j = 0; j < albumsJson.Length; j++)
			{
				CollectableAlbum collectableAlbum = new CollectableAlbum(albumsJson[j]);
				Instance.allAlbums.Add(collectableAlbum.keyName, collectableAlbum);
				collectableSeason.albumsInSeason.Add(collectableAlbum.keyName);
				//Getting all the sets in the current album
				JSON[] setsJson = albumsJson[j].getJsonArray("sets");
				for (int k = 0; k < setsJson.Length; k++)
				{
					CollectableSetData collectableSet = new CollectableSetData(setsJson[k]);
					Instance.allSets.Add(collectableSet.keyName, collectableSet);
					collectableAlbum.setsInAlbum.Add(collectableSet.keyName);
					collectableSet.setPath(collectableAlbum.keyName); // Image path for the album dialog

					//Finally, for each set, populate the cards
					JSON[] cardsJson = setsJson[k].getJsonArray("cards");
					for (int l = 0; l < cardsJson.Length; l++)
					{
						CollectableCardData card = new CollectableCardData(cardsJson[l]);
						card.setPath(collectableAlbum.keyName, collectableSet.keyName);
						Instance.allCards.Add(card.keyName, card);
						collectableSet.cardsInSet.Add(card.keyName);
					}
				}
			}
		}

		JSON packDataJson = data.getJSON("packs");
		if (packDataJson != null)
		{
			List<string> packKeys = packDataJson.getKeyList();
			for (int i = 0; i < packKeys.Count; i++)
			{
				CollectablePackData newPackData = new CollectablePackData(packKeys[i], packDataJson.getJSON(packKeys[i]));
				Instance.allPacks.Add(newPackData.keyName, newPackData);
			}
		}
	}

	public static void populateChallengePackData(JSON data)
	{
		if (data == null)
		{
			return;
		}
		List<string> packIndexKeys = data.getKeyList();
		for (int i = 0; i < packIndexKeys.Count; i++)
		{
			string packIndexKey = packIndexKeys[i];
			JSON packIndexJson = data.getJSON(packIndexKey);

			if (packIndexJson != null)
			{
				List<string> challengePackTypeKeys = packIndexJson.getKeyList();
				Dictionary<string, Dictionary<int, string>> challengePackTypeToVipLevelDict = new Dictionary<string, Dictionary<int, string>>();
				for (int j = 0; j < challengePackTypeKeys.Count; j++)
				{
					string challengePackTypeKey = challengePackTypeKeys[j];
					JSON challengePackJson = packIndexJson.getJSON(challengePackTypeKey);
					if (challengePackJson != null)
					{
						List<string> challengePackJsonKeys = challengePackJson.getKeyList();
						Dictionary<int, string> vipLevelToPackIdDict = new Dictionary<int, string>();

						for (int k = 0; k < challengePackJsonKeys.Count; k++)
						{
							string challengePackVipKey = challengePackJsonKeys[k];
							
							//Keys come down as "vip_0", "vip_1"... so we're just extracting the int
							string[] challengePackSplit = challengePackVipKey.Split('_');
							string vipLevelString = challengePackSplit[1];
							int vipLevel = -1;
							if (int.TryParse(vipLevelString, out vipLevel))
							{
								string packName = challengePackJson.getString(challengePackVipKey, "");
								if (!string.IsNullOrEmpty(packName))
								{
									vipLevelToPackIdDict.Add(vipLevel, packName);
								}
							}
						}

						challengePackTypeToVipLevelDict.Add(challengePackTypeKey, vipLevelToPackIdDict);
					}
					else
					{
						string[] challengePackKeyArray = packIndexJson.getStringArray(challengePackTypeKey);
						Dictionary<int, string> vipLevelToPackIdDict = new Dictionary<int, string>();
						for (int x = 0; x < challengePackKeyArray.Length; x++)
						{
							vipLevelToPackIdDict.Add(x, challengePackKeyArray[x]);
						}
						challengePackTypeToVipLevelDict.Add(challengePackTypeKey, vipLevelToPackIdDict);
					}
				}

				Instance.challengePackData.Add(packIndexKey, challengePackTypeToVipLevelDict);
			}
		}
	}

	public static string getChallengePack(string packIndexKey, string challengePackTypeKey)
	{
		Dictionary<string, Dictionary<int, string>> challengePackTypeToVipLevelDict = null;
		if (Instance.challengePackData.TryGetValue(packIndexKey, out challengePackTypeToVipLevelDict))
		{
			Dictionary<int, string> vipLevelToPackIdDict = null;

			if (challengePackTypeToVipLevelDict.TryGetValue(challengePackTypeKey, out vipLevelToPackIdDict))
			{
				string packName = "";
				if (vipLevelToPackIdDict.TryGetValue(SlotsPlayer.instance.vipNewLevel, out packName))
				{
					return packName;
				}
			}
		}
		
		Bugsnag.LeaveBreadcrumb(string.Format("Couldn't find a valid challenge pack for index {0}, type {1}, vip level {2}", packIndexKey, challengePackTypeKey, SlotsPlayer.instance.vipNewLevel));
		return "";
	}

	public void initPlayerCards(JSON data)
	{
		Server.registerEventDelegate("collectible_season_end", onSeasonEnd);
		
		//Expecting the reward amount for each set/album in here and the cards we've collected
		//Possibly set static references to just the current album here
		if (data == null)
		{
			if (!EueFeatureUnlocks.isFeatureUnlocked("collections"))
			{
				EueFeatureUnlocks.instance.registerForGetInfoEvent("collections", initPlayerCards);
				EueFeatureUnlocks.instance.registerForFeatureLoadEvent("collections", loadBundles);
			}
			return;
		}

		bundlesReady = true;
		if (missingBundles == null)
		{
			missingBundles = new List<string>();
		}

		if (!AssetBundleManager.isBundleCached("collections_common_dialogs"))
		{
			missingBundles.Add("collections_common_dialogs");
		}

		Server.registerEventDelegate("collectible_pack_dropped", onPackDropNew, true);

		string seasonKey = data.getString("season_id", "");
		hasCards = false;
		bool firstIteration = false;
		
		//Login info should only give us card info for the current season, any collected cards from past seasons will be requested for
		currentSeason = seasonKey;
		ftueSeen = data.getBool("ftue_seen", false);
		JSON albumsWithCards = data.getJSON("albums");
		List<string> albumKeyNames = albumsWithCards.getKeyList();

		if (albumKeyNames.Count > 0)
		{
			currentAlbum = albumKeyNames[0];
		}

		for(int i = 0; i < albumKeyNames.Count; i++)
		{
			string albumName = albumKeyNames[i];
			JSON albumJSON = albumsWithCards.getJSON(albumName);
			CollectableAlbum album;
			if (albumJSON != null && allAlbums.TryGetValue(albumName, out album))
			{
				if (!AssetBundleManager.isBundleCached("collections_" + albumName))
				{
					missingBundles.Add("collections_" + albumName);
				}
				album.numCompleted = albumJSON.getInt("iterations", 0);
				firstIteration = album.numCompleted == 0;
				album.currentDuplicateStars = albumJSON.getInt("stars", 0);
				album.starPackName = albumJSON.getString("star_pack", "");
				if (album.currentDuplicateStars > 0)
				{
					showStarMeterToolTip = false;
				}
				album.maxStars = albumJSON.getInt("max_stars", 0);
				album.rewardAmount = albumJSON.getLong("reward", 0);
				JSON setsWithCardsJson = albumJSON.getJSON("sets");
				List<string> setKeyNames = setsWithCardsJson.getKeyList();
				for (int j = 0; j < setKeyNames.Count; j++)
				{
					string setName = setKeyNames[j];
					JSON singleSetJson = setsWithCardsJson.getJSON(setName);
					CollectableSetData singleSet;
					if (singleSetJson != null && allSets.TryGetValue(setName, out singleSet))
					{
						singleSet.rewardAmount = singleSetJson.getLong("reward", 0);
						JSON cardsListJson = singleSetJson.getJSON("cards");
						if (!AssetBundleManager.isBundleCached("collections_" + setName))
						{
							missingBundles.Add("collections_" + setName);
						}
						if (cardsListJson != null)
						{
							List<string> collectedCards = cardsListJson.getKeyList();
							if (singleSet.cardsInSet.Count == collectedCards.Count)
							{
								singleSet.isComplete = true;
							}

							for (int k = 0; k < collectedCards.Count; k++)
							{
								string cardName = collectedCards[k];
								CollectableCardData card;
								if (allCards.TryGetValue(cardName, out card))
								{
									hasCards = true;
									card.isCollected = true;

									JSON cardJson = cardsListJson.getJSON(cardName); //This JSON blob only exists to let us know if the card is seen or not
									if (cardJson != null)
									{
										card.isNew = cardJson.getBool("is_new", false);
										if (card.isNew && !singleSet.isPowerupsSet)
										{
											album.currentNewCards++;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		// If we don't have any cards in our collection then tell the server we want our free pack
		// (May need to be moved to MOTD section);
		if (!hasCards && firstIteration)
		{
#if UNITY_WEBGL
			//Always force the first pack drop for webgl. The indexDB cache isn't reliable since it will have different
			//behaviours for different browsers/settings.
			forceFirstPack = true;
#endif
			bundlesReady = missingBundles != null ? missingBundles.Count == 0 : true;
			bool isValidBundle = true;
			if (missingBundles != null)
			{
				foreach(string bundleName in missingBundles)
				{
					if(!AssetBundleManager.isValidBundle(bundleName))
					{
						isValidBundle = false;
					}
				}	
			}
			
			//Server requires us to call the getFreeFirstPack action before packs are sent to the client from other sources.
			//Sending the action at login if we see the player has 0 cards collected and has all the required bundles cached on device, 
			//or they aren't required to have the bundles pre-cached.
			
			//Not sending the action here if they're in the EUEFeatureUnlock experiment and they haven't seen the feature yet.
			//Starting the collections feature will be handled by the startup logic for that feature instead.
			
			//Also not forcing the first pack if the necessary card bundles aren't cached or don't exist on the client. 
			if ((!EueFeatureUnlocks.hasFeatureUnlockData("collections")  || EueFeatureUnlocks.hasFeatureBeenSeen("collections")) && 
			    (bundlesReady || (forceFirstPack && isValidBundle)))
			{
				startFeature();

				if (!bundlesReady)
				{
					missingBundles.Clear();
					bundlesReady = true;
				}
			}
			else
			{
				Dictionary<string, string> extraFields = new Dictionary<string, string>();
				string combindedString = string.Join( ",", missingBundles.ToArray() );
				extraFields.Add("missing_bundles", combindedString);
				SplunkEventManager.createSplunkEvent("Collectables", "Bundles were not ready", extraFields);
			}
		}
		else if (!string.IsNullOrEmpty(currentAlbum))
		{
			getPowerupsPack();	
		}
		
		if (string.IsNullOrEmpty(currentAlbum))
		{
			Debug.LogError("Collectables::initPlayerCards - MISING ALBUM! COLLECTABLES WON'T WORK CORRECTLY!");
		}

		endTimer = GameTimerRange.createWithTimeRemaining(endTimeInt - GameTimer.currentTime);
		endTimer.registerFunction(Instance.onFeatureEnds);

		if (data.getBool("reward_upgraded", false))
		{
			//Show new dialog if we got put into a new reward tier
			Instance.hasIncreasedRewards = true;
		}

		betIndicatorTimeout = Data.liveData.getInt("COLLECTIONS_BET_INDICATOR_TIMEOUT", 3);
	}

	//Player isn't actually able to receive cards until the first free pack has been requested from the server
	//Usually want to make sure all the bundles are loaded & cached prior to starting the feature so the card pack flow doesn't
	//interrupt the player's gameplay.
	public void startFeature()
	{
		if (isLevelLocked())
		{
			return;
		}

		hasCards = true;
		bundlesReady = true;
		Instance.registerForPackDrop(onFirstPackDrop, "first");
		CollectablesAction.getFreeFirstPack(currentAlbum); //Don't turn on the feature unless the bundles are ready
	}

	public static void getPowerupsPack()
	{
		if (ExperimentWrapper.Powerups.isInExperiment && !CustomPlayerData.getBool(CustomPlayerData.POWERUPS_FTUE_SEEN, false))
		{
			Instance.registerForPackDrop(Instance.onFirstPowerupsPackDrop, "powerups");
			CollectablesAction.getFreeFirstPowerupPack(currentAlbum);
		}
	}

	// Used by the Sort() method to sort the data by index.
	public static int sortCardsBySortId(CollectableCardData a, CollectableCardData b)
	{
		return a.sortOrder.CompareTo(b.sortOrder);
	}

	public List<CollectableSetData> getSetsFromAlbum(string albumKeyName, bool doSort = false)
	{
		CollectableAlbum album;
		if (allAlbums.TryGetValue(albumKeyName, out album))
		{
			List<CollectableSetData> setsInAlbum = new List<CollectableSetData>();
			for (int i = 0; i < album.setsInAlbum.Count; i++)
			{
				CollectableSetData collectableSet;
				collectableSet = findSet(album.setsInAlbum[i]);
				if (collectableSet != null)
				{
					setsInAlbum.Add(collectableSet);
				}
			}

			if (doSort)
			{
				setsInAlbum.Sort(sortSetsBySortId);
			}

			return setsInAlbum;
		}
		else
		{
			Debug.LogErrorFormat("Collectables::getSetsFromAlbum {0} doesn't exist in the AllAlbums dictionary", albumKeyName);
		}

		return null;
	}

	public CollectableAlbum getAlbumByKey(string keyName)
	{
		CollectableAlbum album;
		if (allAlbums.TryGetValue(keyName, out album))
		{
			return album;
		}

		Debug.LogErrorFormat("Collectables::getAlbumByKey - Album {0} doesn't exist in the AllAlbums dictionary", keyName);
		return null;
	}

	public CollectableSeasonData getSeasonByKey(string keyName)
	{
		CollectableSeasonData season;
		if (allSeasons.TryGetValue(keyName, out season))
		{
			return season;
		}

		return null;
	}

	public List<CollectableCardData> getCardsFromSet(string setKeyName, bool doSort = false)
	{
		List<CollectableCardData> cardsInSet = new List<CollectableCardData>();
		CollectableSetData set;
		if (allSets.TryGetValue(setKeyName, out set))
		{
			for (int i = 0; i < set.cardsInSet.Count; i++)
			{
				CollectableCardData collectableCard = findCard(set.cardsInSet[i]);
				if (collectableCard != null)
				{
					cardsInSet.Add(collectableCard);
				}
			}

			if (doSort)
			{
				cardsInSet.Sort(sortCardsBySortId);
			}
		}
		else
		{
			Debug.LogErrorFormat("Album {0} doesn't exist in the AllAlbums dictionary", setKeyName);
		}

		return cardsInSet;
	}

	public CollectableCardData findCard(string cardKeyName)
	{
		CollectableCardData card;
		if (allCards.TryGetValue(cardKeyName, out card))
		{
			return card;
		}
		else
		{
			Debug.LogErrorFormat("Card {0} not found", cardKeyName);
			return null;
		}
	}

	public CollectableSetData findSet(string setKeyName)
	{
		CollectableSetData collectableSet;
		if (allSets.TryGetValue(setKeyName, out collectableSet))
		{
			return collectableSet;
		}
		else
		{
			Debug.LogErrorFormat("Card {0} not found", setKeyName);
			return null;
		}
	}

	public void resetSet(string albumKey, string setKey)
	{
		CollectableAlbum album = allAlbums[albumKey];
		for (int i = 0; i < album.setsInAlbum.Count; i++)
		{
			if (album.setsInAlbum[i] == setKey)
			{
				CollectableSetData collectableSet = allSets[album.setsInAlbum[i]];
				if (collectableSet != null)
				{
					collectableSet.isComplete = false;
					for (int j = 0; j < collectableSet.cardsInSet.Count; j++)
					{
						CollectableCardData card = allCards[collectableSet.cardsInSet[j]];
						card.isNew = false;
						card.isCollected = false;
					}
				}
			}
		}

	}

	public void resetAlbum(string albumKey)
	{
		// Don't get restart with new data? Probably can call init here with this data
		CollectableAlbum completedAlbum = allAlbums[albumKey];
		completedAlbum.numCompleted++;
		completedAlbum.currentNewCards = 0;
		//Need new reward info from somewhere
		//completedAlbum.rewardAmount = ####;

		for (int i = 0; i < completedAlbum.setsInAlbum.Count; i++)
		{
			CollectableSetData collectableSet = allSets[completedAlbum.setsInAlbum[i]];
			collectableSet.isComplete = false;
			//Need new reward info from somewhere
			//collectableSet.rewardAmount = ####;
			for (int j = 0; j < collectableSet.cardsInSet.Count; j++)
			{
				CollectableCardData card = allCards[collectableSet.cardsInSet[j]];
				card.isNew = false;
				card.isCollected = false;
			}
		}

		//Set new reward amounts with server data
		if (nextIterationData != null)
		{
			JSON albumsJSON = nextIterationData.getJSON("albums");
			if (albumsJSON != null)
			{
				List<string> albumsToReset = albumsJSON.getKeyList();
				for (int i = 0; i < albumsToReset.Count; i++)
				{
					CollectableAlbum albumToReset = Instance.getAlbumByKey(albumsToReset[i]);
					JSON newAlbumdata = albumsJSON.getJSON(albumsToReset[i]);
					if (newAlbumdata != null)
					{
						albumToReset.maxStars = newAlbumdata.getInt("max_stars", 0);
						albumToReset.currentDuplicateStars = newAlbumdata.getInt("stars", 0);
						albumToReset.rewardAmount = newAlbumdata.getLong("reward", 0);
						albumToReset.starPackName = newAlbumdata.getString("star_pack", "");
						JSON setsToUpdateData = newAlbumdata.getJSON("sets");
						if (setsToUpdateData != null)
						{
							List<string> setsToUpdate = setsToUpdateData.getKeyList();
							for (int j = 0; j < setsToUpdate.Count; j++)
							{
								CollectableSetData setToUpdate = Instance.findSet(setsToUpdate[j]);
								if (setToUpdate != null)
								{
									JSON newSetData = setsToUpdateData.getJSON(setsToUpdate[j]);
									if (newSetData != null)
									{
										setToUpdate.rewardAmount = newSetData.getLong("reward", 0);
									}
								}
							}
						}
					}
				}
			}
		}

		nextIterationData = null; //Not needed anymore
	}

	public static int sortSetsBySortId(CollectableSetData a, CollectableSetData b)
	{
		return a.sortOrder.CompareTo(b.sortOrder);
	}

	public static void registerCollectionEndHandler(System.EventHandler func)
	{
		
		onCollectionEnd -= func; //prevent duplicate add
		onCollectionEnd += func;
	}

	public static void unregisterCollectionEndHandler(System.EventHandler func)
	{
		onCollectionEnd -= func;
	}

	public void onFeatureEnds(Dict args = null, GameTimerRange sender = null)
	{
		if (onCollectionEnd != null)
		{
			onCollectionEnd(this, null);
		}
	}

	public static void onPackDropStarPackAndAlbumComplete(JSON starPackData, JSON previousRewardData)
	{
		string albumName = starPackData.getString("album", "");
		string eventId = starPackData.getString("event", "");
		string packId = starPackData.getString("pack", "");
		string source = starPackData.getString("source", "");
		JSON additionalStarPackData = starPackData.getJSON("star_pack");
		JSON rewardsJson = starPackData.getJSON("rewards");

		string[] droppedCardsNames = starPackData.getStringArray("cards");
		if (droppedCardsNames.Length <= 0)
		{
			Debug.LogError("No actual cards to show being dropped. Skipping the dialog");
			return;
		}

		List<CollectableCardData> collectedCards = new List<CollectableCardData>();
		for (int i = 0; i < droppedCardsNames.Length; i++)
		{
			string cardKeyName = droppedCardsNames[i];
			CollectableCardData collectedCard = instance.findCard(cardKeyName);
			collectedCards.Add(collectedCard);
		}

		PackDroppedDialog.showDialog(collectedCards, albumName, eventId, packId, source, additionalStarPackData, rewardsJson, previousRewardData);
	}

	private void onFirstPackDrop(JSON data)
	{
		cachedPackJSON = data;
		string albumViewSource = data.getString("pack", "");
		AssetBundleManager.downloadAndCacheBundle("features_ftue"); //Caching ftue the sound bundle so the first clip doesn't fail
		CollectablesMOTD.showDialog(albumViewSource);
	}

	private void onFirstPowerupsPackDrop(JSON data)
	{
		CustomPlayerData.setValue(CustomPlayerData.POWERUPS_FTUE_SEEN, true);
		DoSomething.now("powerups_ftue");
		claimPackDropNow(data, SchedulerPriority.PriorityType.BLOCKING); 
	}

	public static void claimPackDropNow(JSON data, SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW)
	{
		string source = data.getString("source", "");
		bool showNormalPackDrops = shouldShowNormalPack(source);
		JSON starPackData = data.getJSON("star_pack");
		string albumName = data.getString("album", "");
		string eventId = data.getString("event", "");
		string packId = data.getString("pack", "");
		string seasonId = data.getString("season", "");
		string[] droppedCardsNames = data.getStringArray("cards");
		JSON rewardsJson = data.getJSON("rewards");
		
		if (droppedCardsNames.Length <= 0)
		{
			Debug.LogError("No actual cards to show being dropped. Skipping the dialog");
			return;
		}
		
		// This needs to be passed properly
		if (starPackData != null)
		{
			Instance.starPackEventId = eventId;
		}

		bool isInRoyalRush = GameState.game != null && GameState.game.isRoyalRush &&
		                     SpinPanel.instance.shouldShowRoyalRushOverlay;

		List<CollectableCardData> collectedCards = new List<CollectableCardData>();
		for (int i = 0; i < droppedCardsNames.Length; i++)
		{
			string cardKeyName = droppedCardsNames[i];
			CollectableCardData collectedCard = instance.findCard(cardKeyName);
			collectedCards.Add(collectedCard);

			if (collectedCard.isPowerup && !isInRoyalRush)
			{
				showNormalPackDrops = true;
			}
		}

		// If this is a normal pack drop without any set/album reward, and we don't want to show normal pack drops or..
		// if we're in royal rush...
		if ((rewardsJson == null && !showNormalPackDrops) || isInRoyalRush)
		{
			CollectableAlbum album = Instance.getAlbumByKey(currentAlbum);
			if (isInRoyalRush)
			{
				// If we're in royal rush and we have star pack data and pausing in collections is enabled...
				if ((starPackData != null || rewardsJson != null) && ExperimentWrapper.RoyalRush.isPausingInCollections)
				{
					// Show the pack drop dialog
					PackDroppedDialog.showDialog(collectedCards, albumName, eventId, packId, source, starPackData, rewardsJson, null, priority);
					return;
				}

				// Get ready to show collections toaster
				List<string> completedSetsList = null;
				long starPackReward = 0;
				if (rewardsJson != null)
				{
					starPackReward = rewardsJson.getLong("jackpot", 0);

					JSON completedSetsJson = rewardsJson.getJSON("sets");
					if (completedSetsJson != null)
					{
						completedSetsList = completedSetsJson.getKeyList();
					}
				}
				
				showCollectionToaster(completedSetsList, collectedCards, starPackData, packId, source, starPackReward);
				
				//https://jira.corp.zynga.com/browse/HIR-80278
				//mark card pack as seen so the dialog isnt surfaced to the player on the next lobby load
				//This removes the confusion about seeing the dialog but not seeing the coins increase since they were
				//granted silently
				string gameKey = "";
				if (GameState.game != null)
				{
					gameKey = GameState.game.keyName;
				}
				CollectablesAction.cardPackSeen(eventId, gameKey);
			}
			else
			{
				if (starPackData != null)
				{
					claimPackDropNow(starPackData);
				}

				StatsManager.Instance.LogCount(counterName: "dialog",
					kingdom: "hir_collection",
					phylum: "pack_award",
					klass: source,
					family: packId,
					genus: "muted",
					val: collectedCards.Count);
			}


			// If there's no star pack data, then go ahead and claim. If there is star pack data,
			// we don't want to claim silently since we want the user to see the full experience
			if (starPackData == null)
			{
				string gameKey = "";
				if (isInRoyalRush && GameState.game != null)
				{
					gameKey = GameState.game.keyName;
				}

				if (rewardsJson == null)
				{
					CollectablesAction.cardPackSeen(eventId, gameKey);
				}
			}

			if (album != null)
			{
				// Update card "isNew" and "isCollected" settings
				for (int i = 0; i < collectedCards.Count; i++)
				{
					CollectableCardData collectedCard = collectedCards[i];

					if (collectedCard != null)
					{
						if (collectedCard.isCollected)
						{
							album.currentDuplicateStars += collectedCard.rarity;
							if (album.currentDuplicateStars >= album.maxStars)
							{
								album.currentDuplicateStars -= album.maxStars;
							}
						}
						else
						{
							if (!PowerupBase.collectablesPowerupsMap.ContainsKey(collectedCard.keyName))
							{
								album.currentNewCards++;
								collectedCard.isNew = true;
							}

							collectedCard.isCollected = true;
						}
					}
				}
			}
		}
		else
		{
			// show normal pack drop dialog
			PackDroppedDialog.showDialog(collectedCards, albumName, eventId, packId, source, starPackData, rewardsJson, priority:priority);
		}
	}

	private static bool shouldShowNormalPack(string source)
	{
		//Always show normal packs (packs that don't complete a set or duplicate stars meter) if they're on from the settings menu
		//Or if the card pack is dropped from a feature that wants to always show cards,
		//Or if the card pack is dropped from a purchase
		return CustomPlayerData.getBool(CustomPlayerData.SHOW_COLLECT_ALERTS, true) || 
		       source == "rich_pass" || 
		       source == "prize_pop" ||
		       source == "inbox"     ||
		       source == "lottery"   ||
		       source == "board_game" ||
		       PurchasablePackage.isValidPackage(source);
	}
	
	public void onPackDropNew(JSON data)
	{
		//If they don't want to see the dialog then go ahead and update stuff here
		if (data == null)
		{
			return;
		}

		string seasonId = data.getString("season", "");
		if (currentSeason != seasonId)
		{
			string eventId = data.getString("event", "");
			//just claim the pack silently
			if (!eventId.IsNullOrWhiteSpace())
			{
				CollectablesAction.cardPackSeen(eventId);
			}
			return;
		}
		
		//Debug.LogError("On pack drop data " + data);
		
		string source = data.getString("source", "");

		if (callbackDict.ContainsKey(source))
		{
			callbackDict[source](data);
		}
		else
		{
			// Just claim it since no on else wants to do anything here
			claimPackDropNow(data);
		}
	}
	
	public void registerForPackDrop(CollectableDroppedCallback callback, string identifier)
	{
		if (!callbackDict.ContainsKey(identifier))
		{
			callbackDict.Add(identifier, callback);
		}
	}
	
	public void unRegisterForPackDrop(CollectableDroppedCallback callback, string identifier)
	{
		if (callbackDict.ContainsKey(identifier))
		{
			callbackDict.Remove(identifier);
		}
	}
	
	private static void onSeasonEnd (JSON data)
	{
		Server.unregisterEventDelegate("collectible_season_end", onSeasonEnd);
		CollectablesSeasonOverDialog.setEndedAlbums(data.getStringArray("albums"));
		CollectablesSeasonOverDialog.showDialog();
	}

	public static void showCollectionToaster(List<string> rewardSet = null, List<CollectableCardData> collectedCards = null, JSON starData = null, string packId = null, string packSource = null, long starPackReward = 0L)
	{
		Dict packInfo = Dict.create();
		packInfo.Add(D.DATA, rewardSet);
		packInfo.Add(D.COLLECTABLE_CARDS, collectedCards);
		packInfo.Add(D.BONUS_CREDITS, starData);
		packInfo.Add(D.KEY, packSource);
		packInfo.Add(D.PACKAGE_KEY, packId);
		packInfo.Add(D.VALUE, starPackReward);
		ToasterManager.addToaster(ToasterType.COLLECTIONS, packInfo, null);
	}

	public CollectablePackData findPack(string packName)
	{
		if (packName != null)
		{
			CollectablePackData pack;
			if (allPacks.TryGetValue(packName, out pack))
			{
				return pack;
			}
		}

		return null;
	}

	public Dictionary<string, CollectableCardData> getAllCards()
	{
		return allCards;
	}

	public Dictionary<string, CollectableSetData> getAllSets()
	{
		return allSets;
	}

	public Dictionary<string, CollectableAlbum> getAllAlbums()
	{
		return allAlbums;
	}

	public static void loadCollectionsBundles(bool canUnloadInstantly)
	{
#if UNITY_WEBGL
		canUnloadInstantly = false;
#endif
		if (missingBundles != null)
		{
			for (int i = 0; i < missingBundles.Count; i++)
			{
				//Adding callback here to start feature once all bundles are loaded
				AssetBundleManager.downloadAndCacheBundle(missingBundles[i], false, canUnloadInstantly, true, false); //treat these as lazy loaded since we might not need them right away
			}
			missingBundles.Clear();	
		}
		
	}

	private static void loadBundles()
	{
		currentSeason = Data.liveData.getString("COLLECTIONS_SEASON", "");
		currentAlbum = instance.getSeasonByKey(currentSeason).albumsInSeason[0];
		missingBundles = getMissingBundles();
		loadCollectionsBundles(true);
	}

	private static List<string> getMissingBundles()
	{
		List<string> bundles = new List<string>();
		if (!AssetBundleManager.isBundleCached("collections_common_dialogs"))
		{
			bundles.Add("collections_common_dialogs");
		}
		if (!AssetBundleManager.isBundleCached("collections_" + currentAlbum))
		{
			bundles.Add("collections_" + currentAlbum);
		}

		if (string.IsNullOrEmpty(currentAlbum))
		{
			//no missing bundles if we don't have an ablum
			return null;
		}

		if (instance != null)
		{
			CollectableAlbum album = instance.getAlbumByKey(currentAlbum);
			if (album != null)
			{
				List<string> currentSets = album.setsInAlbum;
				if (currentSets != null)
				{
					for (int i = 0; i < currentSets.Count; i++)
					{
						if (!AssetBundleManager.isBundleCached("collections_" + currentSets[i]))
						{
							bundles.Add("collections_" + currentSets[i]);
						}
					}	
				}	
			}
				
		}
		

		return bundles;
	}
	
	public static bool hasValidBundles()
	{
		missingBundles = getMissingBundles();
		if (missingBundles != null)
		{
			for (int i = 0; i < missingBundles.Count; i++)
			{
				if(!AssetBundleManager.isValidBundle(missingBundles[i]))
				{
					return false;
				}
			}	
		}

		return true;
	}
	
	public static void showVideo()
	{
		VideoDialog.showDialog(
			ExperimentWrapper.Collections.videoUrl, 
			"", 
			"Play Now!", 
			summaryScreenImage: ExperimentWrapper.Collections.videoSummaryPath, 
			autoPopped: false,
			statName: "collections"
		);
	}

	public static void resetStaticClassData()
	{
		instance = null;
		currentSeason = "";
		currentAlbum = "";
		cachedPackJSON = null;
		showStarMeterToolTip = true;
		missingBundles = null;
		usingDynamicAtlas = false;
		nextIterationData = null;
	}
}