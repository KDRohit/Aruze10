using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

public class WeeklyRacePlayerListItem : MonoBehaviour, IResetGame
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private UISprite chestSprite;
	[SerializeField] private ButtonHandler chestButton;
	
	[SerializeField] private GameObject playerView;
	[SerializeField] private GameObject friendView;
	[SerializeField] private GameObject listItemGlow;

	[SerializeField] private UISprite friendRankBadge;
	[SerializeField] private UISprite friendRankLabel;

	[SerializeField] private UISprite playerBg;
	[SerializeField] private UITexture profileImage;
	[SerializeField] private ObjectSwapper swapper;

	[SerializeField] private GameObject rivalObject;
	[SerializeField] private GameObject rivalDropShadow;

	[SerializeField] private ClickHandler viewProfileButton;
	
	private const int PICTURE_CACHE_LIMIT = 50; //limit texture amount so we don't run out of memory
	protected static Dictionary<string, Material> profilePicCache = new Dictionary<string, Material>();

	private bool isSetup = false;

	private SocialMember member {get; set;}

	// =============================
	// PUBLIC
	// =============================
	public TextMeshPro playerRank;
	public TextMeshPro playerName;
	public TextMeshPro playerScore;
	public TextMeshPro friendScore;
	public TextMeshPro divisionName;

	// =============================
	// CONST
	// =============================
	private const string PLAYER_BG = "Player Panel Bar Blue Stretchy";
	private const string GREEN_BG = "Player Panel Bar Green Stretchy";
	private const string ORANGE_BG = "Player Panel Bar Orange Stretchy";
	private const string PURPLE_BG = "Player Panel Bar Purple Stretchy";
	private const string RED_BG = "Player Panel Bar Red Stretchy";
	public const float SCALE_SIZE = 1.12f;

	private WeeklyRaceLeaderboard leaderboard;
	private Material clonedMaterial = null;

	void Awake()
	{
		init();
	}

	protected void init()
	{
		viewProfileButton.registerEventDelegate(viewProfileClicked);
		chestButton.registerEventDelegate(onChestClick);
	}

	protected void onChestClick(Dict args = null)
	{
		if (leaderboard != null)
		{
			leaderboard.showPrizes();
		}
	}
	
	public void setup
	(
		WeeklyRaceLeaderboard leaderboard,
		WeeklyRaceRacer racer,
		WeeklyRace race,
		WeeklyRaceSlideController controller = null,
		bool showRival = true
	)
	{
		member = racer.member;

		// only do this the first time we setup the weekly race list item
		if (!isSetup)
		{
			string playerImageURL = racer.member.getImageURL;
			if (profilePicCache.TryGetValue(playerImageURL, out Material mat))
			{
				profileImage.material = mat;
			}
			else
			{
				member.loadProfileImageToUITexture(profileImage, onNewMaterialCreated);	
			}
			isSetup = true;
		}

		this.leaderboard = leaderboard;	
		playerRank.text = CommonText.formatContestPlacement(racer.competitionRank+1, true);
		playerName.text = racer.name;
		long scoreToDisplay = (race.isScoreInflated || SlotsPlayer.instance == null) ? racer.score :
			(long)System.Math.Ceiling(racer.score * SlotsPlayer.instance.currentBuyPageInflationFactor);
		playerScore.text = CreditsEconomy.convertCredits(scoreToDisplay);

		playerView.SetActive(true);
		friendView.SetActive(false);
		SafeSet.gameObjectActive(rivalObject, false);

		if (racer.id != SlotsPlayer.instance.socialMember.zId)
		{
			listItemGlow.SetActive(false);
			if (race.isRankWithinPromotion(racer.competitionRank))
			{
				if (playerBg.spriteName != GREEN_BG)
				{
					playerBg.spriteName = GREEN_BG;
				}

				swapper.setState("promotion");
			}
			else if (race.isRankWithinRelegation(racer.competitionRank))
			{
				if (playerBg.spriteName != ORANGE_BG)
				{
					playerBg.spriteName = ORANGE_BG;
				}

				swapper.setState("relegation");
			}
			else if (playerBg.spriteName != PURPLE_BG)
			{
				playerBg.spriteName = PURPLE_BG;

				swapper.setState("neutral");
			}
		}
		else if (playerBg.spriteName != PLAYER_BG)
		{
			playerBg.spriteName = PLAYER_BG;
			swapper.setState("player");
		}

		if (showRival && racer.isRival)
		{
			SafeSet.gameObjectActive(rivalObject, racer.isRival);

			if (playerBg.spriteName != RED_BG)
			{
				playerBg.spriteName = RED_BG;

				if (controller != null)
				{
					controller.onPin += onItemPinned;
					controller.onUnpin += onItemUnpinned;
				}

				swapper.setState("rival");
			}
		}

		if (race.getChestForRank(racer.competitionRank) < 0)
		{
			chestButton.gameObject.SetActive(false);
		}
		else
		{
			chestSprite.spriteName = WeeklyRace.getChestSpriteName(race.getChestForRank(racer.competitionRank));
		}
	}
	
	private void onNewMaterialCreated(Material mat)
	{
		string fullURL = member.getImageURL;

		if (profilePicCache.Count < PICTURE_CACHE_LIMIT)
		{
			
#if UNITY_EDITOR
			//sanity check this in dev builds.  Overhead is not worth it in production builds
			//the leaked material will get cleaned up on scene change
			if (profilePicCache.TryGetValue(fullURL, out Material toDestroy))
			{
				Debug.LogError("Invalid profile image load");
				if (toDestroy != mat)
				{
					Destroy(mat);
				}
			}
#endif
			profilePicCache[fullURL] = mat;	
		}
		else
		{
			clonedMaterial = mat;
		}
	}

	public void setupFriend(SocialMember member, int rank = 0)
	{
		playerBg.spriteName = PURPLE_BG;
		swapper.setState("neutral");
		playerName.text = member.firstNameLastInitial;
		playerRank.text = CommonText.formatContestPlacement(rank+1, true);

		friendScore.gameObject.SetActive(false);
		friendRankBadge.spriteName = WeeklyRace.getBadgeSprite(member.weeklyRaceDivision);
		friendRankLabel.spriteName = WeeklyRace.getDivisionTierSprite(member.weeklyRaceDivision);
		divisionName.text = WeeklyRace.getFullDivisionName(member.weeklyRaceDivision);

		playerView.SetActive(false);
		friendView.SetActive(true);

		this.member = member;

		string imageURL = member.getImageURL;
		if (profilePicCache.TryGetValue(imageURL, out Material mat))
		{
			profileImage.material = mat;
		}
		else
		{
			member.loadProfileImageToUITexture(profileImage, onNewMaterialCreated);	
		}
		
	}

	public void onItemPinned(GameObject item)
	{
		item.transform.localScale = new Vector3(SCALE_SIZE, SCALE_SIZE, 1);
		SafeSet.gameObjectActive(rivalDropShadow, true);
	}

	public void onItemUnpinned(GameObject item)
	{
		item.transform.localScale = Vector3.one;
		SafeSet.gameObjectActive(rivalDropShadow, false);
	}

	private void OnDestroy()
	{
		if (leaderboard != null && leaderboard.slideController != null)
		{
			leaderboard.slideController.onPin -= onItemPinned;
			leaderboard.slideController.onUnpin -= onItemUnpinned;
		}
		viewProfileButton.unregisterEventDelegate(viewProfileClicked);
		
		if (clonedMaterial != null)
		{
			Destroy(clonedMaterial);
			clonedMaterial = null;
		}
	}

	private void viewProfileClicked(Dict args = null)
	{
		NetworkProfileDialog.showDialog(member, SchedulerPriority.PriorityType.IMMEDIATE);
	}
	
	public static void clearProfilePictures()
	{
		if (profilePicCache == null)
		{
			return;
		}
		
		//memory clean up
		foreach (Material mat in profilePicCache.Values)
		{
			Destroy(mat);
		}

		profilePicCache.Clear();
	}

	public static void resetStaticClassData()
	{
		clearProfilePictures();
	}
}
