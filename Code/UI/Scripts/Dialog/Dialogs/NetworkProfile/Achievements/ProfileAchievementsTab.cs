using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ProfileAchievementsTab : NetworkProfileTabBase
{

	public static ProfileAchievementsTab instance;
	
	[SerializeField] private TabManager skuManager;

    [SerializeField] private TextMeshPro pointsLabel;
	[SerializeField] private TextMeshPro filterLabel;
	[SerializeField] private TextMeshPro comingSoonGameLabel;
	[SerializeField] private GameObject trophyGridParent;
	[SerializeField] private GameObject shelfPrefab;
	[SerializeField] private GameObject comingSoonParent;
	[SerializeField] private GameObject loadingParent;
	[SerializeField] private GameObject trophyDisplayParent;
	[SerializeField] private TextMeshProMasker tmProMasker;

	public SlideController trophySlideController;
	public Transform newMarkerTransform; // Used for marking trophies as seen.
	public SpriteMask particleMask;
	
	// Filter Dropdown stuff.
	public GameObject filterDropdownParent;
	[SerializeField] private ToggleManager filterToggleManager;
	[SerializeField] private ClickHandler filterDropdownButton;
	[SerializeField] private ClickHandler infoButton;
	
	private FilterType currentType = FilterType.ALL;
	private string currentSku = "hir";
	public bool isWaitingForData = false;
	[HideInInspector] public NetworkProfileDialog dialog;
	private List<AchievementsShelf> shelves;
	private Dictionary<string, long> cachedAchievementScores;
	private bool hasLoadedData = false;
	
	private const int SHELF_SIZE = 4;
	private const string UNEARNED_LOCALIZATION_FORMAT = "unearned_{0}";
	private const string EARNED_LOCALIZATION_FORMAT = "earned_{0}";
	private const string ALL_LOCALIZATION_FORMAT = "all_{0}";
	private const string STAY_TUNED_FORMAT = "stay_tuned_for_trophies_in_{0}";
	private const float TIMEOUT_LENGTH = 3.0f;
	private const float shelfHeight = 550f;

	//Audio
	private const string FILTER_OPEN = "SprocketOpen";
	private const string FILTER_CLOSE = "SprocketClose";
	
	public enum FilterType:int
	{
		ALL = 0,
		EARNED = 1,
		UNEARNED = 2
	}

	private enum AchievementTabTypes:int
	{
		HIR = 0,
		WOZ = 1,
		WONKA = 2,
		GOT = 3,
		NETWORK = 4
	}
	
	private AchievementTabTypes currentTab;
	private List<Achievement> allAchievements;
	private List<Achievement> earnedAchievements;
	private List<Achievement> unearnedAchievements;
	private bool isWaitingOnFtue = false;
	
	public void init(SocialMember member, NetworkProfileDialog dialog)
	{
		instance = this;
		this.dialog = dialog;
		skuManager.init(typeof(AchievementTabTypes), (int)AchievementTabTypes.HIR, onSkuSelect);
		this.member = member;

		cachedAchievementScores = new Dictionary<string, long>();
		filterDropdownButton.registerEventDelegate(filterDropdownClicked);
		if (null != infoButton)
		{
			infoButton.registerEventDelegate(infoClicked);
			infoButton.gameObject.SetActive(NetworkAchievements.rewardsEnabled);
		}
		filterToggleManager.init(filterClicked);
		currentSku = "hir"; // Default to HIR.
		comingSoonParent.SetActive(false); // Turn this off by default.
	}

	public override IEnumerator onIntro(NetworkProfileDialog.ProfileDialogState fromState, string extraData)
	{
		if (!hasLoadedData)
		{
			// Only do this if this is the first time we are transitioning to this tab.
			TabSelector selector = skuManager.tabs[(int)AchievementTabTypes.HIR];
			skuManager.selectTab(selector);
			currentSku = "hir"; // Default to HIR.
			StartCoroutine(loadDataRoutine());
			yield return null;
		}
	}
	
	private IEnumerator loadDataRoutine()
	{
		if (!hasLoadedData)
		{
			// Turn on loading objects.
			loadingParent.SetActive(true);
			trophyDisplayParent.SetActive(false);
			skuManager.isEnabled = false;
			
			isWaitingForData = true;
			NetworkAchievementAction.getAchievementsForUser(member);
			float totalWaitTime = 0.0f;
			while (isWaitingForData)
			{
				// Continue to wait for the data to come down.
				yield return new WaitForSeconds(1.0f);
				totalWaitTime += Time.deltaTime;
				if (totalWaitTime > TIMEOUT_LENGTH)
				{
					Debug.LogErrorFormat("ProfileAchievementsTab.cs -- loadDataRoutine -- timed out while loading data...");
					break;
				}
			}
			hasLoadedData = true;
		}
		setupAchievementLists();

		// Turn on trophy showing objects.
		skuManager.isEnabled = true;
		loadingParent.SetActive(false);
		trophyDisplayParent.SetActive(true);
		yield return null;
	}
	
	public List<Achievement> getCurrentList()
	{
		return getAchievementList(currentType);
	}

	public void trophyClickedCallback(Dict args)
	{
		if (isWaitingOnFtue)
		{
			// Don't let them break the FTUE flow here.
			return;
		}
		dialog.showSpecificTrophy(args);
	}
	
	private void onSkuSelect(TabSelector tab)
	{
		switch (tab.index)
		{
			case (int) AchievementTabTypes.WOZ:
				currentTab = AchievementTabTypes.WOZ;
				break;
			case (int) AchievementTabTypes.WONKA:
				currentTab = AchievementTabTypes.WONKA;
				break;
			case (int) AchievementTabTypes.GOT:
				currentTab = AchievementTabTypes.GOT;
				break;
			case (int) AchievementTabTypes.NETWORK:
				currentTab = AchievementTabTypes.NETWORK;
				break;
			case (int) AchievementTabTypes.HIR:
			default:
				currentTab = AchievementTabTypes.HIR;
				break;
		}
		
		if (currentSku != getKeyFromTab(currentTab))
		{
		    // No reason to recalculate if we haven't changed SKU.
			currentSku = getKeyFromTab(currentTab);
			setupAchievementLists();
		}
	}

	private string getKeyFromTab(AchievementTabTypes type)
	{
		switch (type)
		{
			case AchievementTabTypes.WOZ:
				return "woz";
			case AchievementTabTypes.WONKA:
				return "wonka";
			case AchievementTabTypes.GOT:
				return "got";
			case AchievementTabTypes.NETWORK:
				return "network";
			case AchievementTabTypes.HIR:
			default:
				return "hir";
		}
	}

	private string getSkuNameFromType(AchievementTabTypes type)
	{
		switch (type)
		{
			case AchievementTabTypes.WOZ:
				return "Wizard of Oz";
			case AchievementTabTypes.WONKA:
				return "Wonka Slots";
			case AchievementTabTypes.GOT:
				return "Game of Thrones Slots";
			case AchievementTabTypes.NETWORK:
				return "All Zynga Slots";
			case AchievementTabTypes.HIR:
			default:
				return "Hit It Rich";
		}
	}
	
	private void setupAchievementLists()
	{
		StatsManager.Instance.LogCount(
			counterName:"dialog",
			kingdom:"ll_profile",
			phylum:"trophy_room",
			klass:"view",
			family:currentSku,
			genus: member.networkID);
		
		currentType = FilterType.ALL;
		allAchievements = new List<Achievement>();
		earnedAchievements = new List<Achievement>();
		unearnedAchievements = new List<Achievement>();

		CommonGameObject.destroyChildren(trophyGridParent);
		bool doTrophiesExist = false;
		if (NetworkAchievements.allAchievements != null && NetworkAchievements.allAchievements.ContainsKey(currentSku))
		{
		    doTrophiesExist = NetworkAchievements.allAchievements[currentSku].Count > 0;
		}

		string stayTunedKey = string.Format(STAY_TUNED_FORMAT, currentSku);
		comingSoonGameLabel.text = Localize.text(stayTunedKey);
		comingSoonParent.SetActive(!doTrophiesExist);
		trophyDisplayParent.SetActive(doTrophiesExist);
		if (!doTrophiesExist)
		{
			// No need to go through any of the rest of this function.
			return;
		}
		
		foreach (KeyValuePair<string, Achievement> pair in NetworkAchievements.allAchievements[currentSku])
		{
			string key = pair.Key;
			allAchievements.Add(pair.Value);
			Achievement achievement = pair.Value;
			if (achievement.isUnlocked(member))
			{
				earnedAchievements.Add(achievement);
			}
			else
			{
				unearnedAchievements.Add(achievement);
			}
		}

		filterToggleManager.handlers[(int)FilterType.ALL].text = createLabel(FilterType.ALL, allAchievements.Count);
		filterToggleManager.handlers[(int)FilterType.EARNED].text = createLabel(FilterType.EARNED, earnedAchievements.Count);
		filterToggleManager.handlers[(int)FilterType.UNEARNED].text = createLabel(FilterType.UNEARNED, unearnedAchievements.Count);
		
		filterToggleManager.toggle((int)FilterType.ALL);
		filterDropdownParent.SetActive(false); // Default this to off.

		setupTrophies(currentType);
	}	
	
	public string createLabel(FilterType type, int count)
	{
		string format = "{0}";
		switch (type)
		{
			case FilterType.ALL:
				format = ALL_LOCALIZATION_FORMAT;
				break;
			case FilterType.EARNED:
				format = EARNED_LOCALIZATION_FORMAT;
				break;
			case FilterType.UNEARNED:
				format = UNEARNED_LOCALIZATION_FORMAT;
				break;
		}
		return Localize.text(format, count);
	}
	
	public void setupTrophies(FilterType type)
	{
		shelves = new List<AchievementsShelf>(); // Clear the shelf list.
		trophySlideController.enabled = true; // If this was off turn it back on.
		// Remove all of the existing children from the grid.
		CommonGameObject.destroyChildren(trophyGridParent);
		trophySlideController.resetEvents(); // Reset this since we are destroying all the children.
		trophyGridParent.transform.localPosition = Vector3.zero;
		List<Achievement> achievements = getAchievementList(type);
		filterLabel.text = createLabel(type, achievements.Count);
		int numShelves = achievements.Count / SHELF_SIZE;
		if ((achievements.Count % SHELF_SIZE) > 0)
		{
			// If there are any remaining achievements then pop open another shelf.
			numShelves++;
		}
		GameObject newShelf;
		for (int i = 0; i < numShelves; i++)
		{
			newShelf = CommonGameObject.instantiate(shelfPrefab, trophyGridParent.transform) as GameObject;
			float height = -1 * shelfHeight * ((i));
			newShelf.transform.localPosition = new Vector3(0, height, -15f);
			if (newShelf != null)
			{
				AchievementsShelf shelf = newShelf.GetComponent<AchievementsShelf>();
				if (shelf != null)
				{
					shelf.init(achievements, SHELF_SIZE * i, SHELF_SIZE, this, trophySlideController);
					shelves.Add(shelf);
					tmProMasker.addObjectArrayToList(shelf.getAllTMPros());
				}
				else
				{
					Debug.LogErrorFormat("ProfileAchievementsTab.cs -- setupTrophies -- succeeded in creating the shelf, but it doesn't have a script on it for whatever reason.");
				}
			}
			else
			{
				Debug.LogErrorFormat("ProfileAchievementsTab.cs -- setupTrophies -- failed to instantiate the shelf object for some reason :(");
			}
		}

		float totalHeight = shelfHeight * numShelves;
	    trophySlideController.content.height = totalHeight;
		float topBound = shelfHeight * (numShelves - 1.5f);
		//float bottomBound = -1 * shelfHeight * (numShelves) - (shelfHeight /2);
		float bottomBound = -1 * shelfHeight * (numShelves) - (shelfHeight / 4);
		trophySlideController.setBounds(topBound, bottomBound);
		// Subtract a quarter of a shelf height to position it so that you can see all of it.
		CommonTransform.addY(trophyGridParent.transform, shelfHeight * -0.15f);
		
		// They want the points to only show for the currently selected SKU, so lets calculate this here.
		if (!cachedAchievementScores.ContainsKey(currentSku))
		{
			cachedAchievementScores.Add(currentSku,
				NetworkAchievements.getCurrentPlayerAchievementScoreForSku(currentSku));
		}
		pointsLabel.text = Localize.textUpper("{0}_pts", cachedAchievementScores[currentSku]);
	}

	public void enableSKUSelect(bool enable)
	{
		skuManager.isEnabled = enable;
	}

	

	// Refreshes the icons on the trophies without reinstantiating them.
	public void refreshTrophies()
	{
		if (shelves != null)
		{
			for (int i = 0; i < shelves.Count; i++)
			{
				shelves[i].refreshTrophies();
			}
		}
	}
	
	private List<Achievement> getAchievementList(FilterType type)
	{
		List<Achievement> result = new List<Achievement>();
		switch (type)
		{
			case FilterType.EARNED:
				return earnedAchievements;
			case FilterType.UNEARNED:
				return unearnedAchievements;
			case FilterType.ALL:
			default:
				return allAchievements;
		}
	}

	private void filterDropdownClicked(Dict args = null)
	{
		bool turningDropdownOn = !filterDropdownParent.activeSelf;
		trophySlideController.enabled = !turningDropdownOn;
		string audioKey = filterDropdownParent.activeSelf ? FILTER_CLOSE : FILTER_OPEN;
		Audio.play(audioKey);
		filterDropdownParent.SetActive(turningDropdownOn);
	}
	
	private void infoClicked(Dict args = null)
	{
		Dialog.close();
		//open the trophies faq
		Common.openSupportUrl(Glb.HELP_LINK_SUPPORT);
	}

	private void filterClicked(ToggleHandler handler)
	{
		Audio.play(FILTER_CLOSE);
		FilterType type = (FilterType)handler.index;
		if (type != currentType)
		{
			currentType = type; //Set the current type;
			setupTrophies(type);
		}
		else
		{
			// Do nothing
		}
		filterDropdownParent.SetActive(false);
		trophySlideController.enabled = true; // If this was off, turn if back on.
	}

	private void OnDestroy()
	{
		instance = null;
	}
}
