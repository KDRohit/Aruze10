using UnityEngine;
using System.Collections;

using TMPro;

public class WeeklyRaceBottomButton : BottomOverlayButton
{
	public GameObject rankParent;
	public GameObject badgeParent;

	public UISprite divisionBadge;
	public UISprite divisionLabel;
	public TextMeshPro playerRank;
	public UISprite rankArrow;
	public Animator rankAnimator;
	public UISprite tagSprite;

	private const string BURST_ANIMATION = "on";
	private const string BADGE_PREFAB_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Weekly Race/Weekly Race Rank Badge Item.prefab";
	private const string PLAYER_RANK_PREFAB_PATH = "Assets/Data/HIR/Bundles/Initialization/Features/Weekly Race/Player Rank - Bottom Overlay.prefab";

	protected override void Awake()
	{
		sortIndex = 1;
		base.Awake();
		init();
	}

	protected override void init()
	{
		base.init();
		
		//load badge prefab if not baked in
		if (badgeParent != null)
		{
			SkuResources.loadFromMegaBundleWithCallbacks(this, BADGE_PREFAB_PATH, weeklyRaceLoadSuccess, weeklyRaceLoadFailure);
		}

		//load player rank prefab if not baked in
		if(rankParent != null)
		{
			SkuResources.loadFromMegaBundleWithCallbacks(this, PLAYER_RANK_PREFAB_PATH, weeklyRaceLoadSuccess, weeklyRaceLoadFailure);
		}
		else
		{
			playerRank = CommonGameObject.findChild(this.gameObject, "Notification Label").GetComponent<TextMeshPro>();
		}
		
		WeeklyRaceDirector.registerStatHandler(refresh);
		refresh(null, null);
	}

	public void refresh(object sender, System.EventArgs e)
	{
		WeeklyRace race = WeeklyRaceDirector.currentRace;
		
		if (race != null)
		{
			updateRank(race.division, race.competitionRank);
		}
	}

	public void weeklyRaceLoadSuccess(string path, Object obj, Dict args)
	{
		if (this == null)
		{
			return;
		}

		GameObject prefab = obj as GameObject;
		GameObject assetObject = CommonGameObject.instantiate(prefab) as GameObject;

		if (assetObject != null)
		{
			if (path.Contains("Badge"))
			{			
				assetObject.transform.parent = badgeParent.transform;
				assetObject.transform.localPosition = new Vector3(0, 0, -10);
				assetObject.transform.localScale = Vector3.one;
			
				divisionBadge = CommonGameObject.findChild(assetObject, "Rank Badge Sprite").GetComponent<UISprite>();
				divisionLabel = CommonGameObject.findChild(assetObject, "Rank Badge Numeral Sprite").GetComponent<UISprite>();
			}
			else
			{
				assetObject.transform.parent = rankParent.transform;
				assetObject.transform.localPosition = Vector3.zero;
				assetObject.transform.localScale = Vector3.one;
				
				playerRank = CommonGameObject.findChild(assetObject, "Rank Label").GetComponent<TextMeshPro>();
				rankArrow = CommonGameObject.findChild(assetObject, "Zone Arrow Sprite").GetComponent<UISprite>();
				rankAnimator = assetObject.GetComponent<Animator>();
			}
		}

		refresh(null, null);
	}

	public void updateRank(int division, int playerRank)
	{
		if (divisionBadge == null || divisionLabel == null)
		{
			return;
		}
		WeeklyRace race = WeeklyRaceDirector.currentRace;
				
		// going to do a nifty little animation
		if (rankAnimator != null && race.previousRank != race.competitionRank)
		{
			rankAnimator.Play(BURST_ANIMATION);
			StartCoroutine(waitSequence(division, playerRank));
			return;
		}
		
		divisionBadge.spriteName = WeeklyRace.getBadgeSprite(division);
		divisionLabel.spriteName = WeeklyRace.getDivisionTierSprite(division);
		divisionLabel.gameObject.SetActive(!string.IsNullOrEmpty(WeeklyRace.getTierNumeral(division)));

		if (rankParent != null)
		{
			updateRankPrefab(race);
		}
		else if (tagSprite != null)
		{
			updateTagAndRank(race);
		}

		if (this.playerRank != null)
		{
			this.playerRank.text = CommonText.formatContestPlacement(playerRank+1, true);
			SafeSet.gameObjectActive(this.playerRank.transform.parent.gameObject, race.timeRemaining > 0);
		}
	}

	private void updateRankPrefab(WeeklyRace race)
	{
		if (rankArrow != null)
		{
			if (race != null && race.isInPromotion)
			{
				rankArrow.spriteName = "Rank Promotion Zone Indicator";
				SafeSet.gameObjectActive(rankArrow.gameObject, race.timeRemaining > 0);
			}
			else if (race != null && race.isInRelegation)
			{
				rankArrow.spriteName = "Rank Drop Zone Indicator";
				SafeSet.gameObjectActive(rankArrow.gameObject, race.timeRemaining > 0);
			}
			else
			{
				rankArrow.gameObject.SetActive(false);
			}

			
		}
	}

	private void updateTagAndRank(WeeklyRace race)
	{
		//bottom bar version 4
		//set the tag sprite to green if moving up, red if moving down.
		if (race != null && race.isInPromotion)
		{
			rankArrow.spriteName = "Weekly Race Arrow Prom";
			tagSprite.spriteName = "Tag Green Stretchy";
			SafeSet.gameObjectActive(rankArrow.gameObject, race.timeRemaining > 0);

		}
		else if (race != null && race.isInRelegation)
		{
			rankArrow.spriteName = "Weekly Race Arrow Drop";
			tagSprite.spriteName = "Tag Red Stretchy";	
			SafeSet.gameObjectActive(rankArrow.gameObject, race.timeRemaining > 0);
		}
		else
		{
			//just default to green
			rankArrow.gameObject.SetActive(false);
			tagSprite.spriteName = "Tag Orange Stretchy";	
			}
	}

	private IEnumerator waitSequence(int division, int playerRank, float time = 0.2f)
	{
		yield return new WaitForSeconds(time);

		divisionBadge.spriteName = WeeklyRace.getBadgeSprite(division);
		divisionLabel.spriteName = WeeklyRace.getDivisionTierSprite(division);
		divisionLabel.gameObject.SetActive(!string.IsNullOrEmpty(WeeklyRace.getTierNumeral(division)));		

		WeeklyRace race = WeeklyRaceDirector.currentRace;
		if (race != null && race.isInPromotion)
		{
			rankArrow.spriteName = "Rank Promotion Zone Indicator";
		}
		else if (race != null && race.isInRelegation)
		{
			rankArrow.spriteName = "Rank Drop Zone Indicator";
		}
		else
		{
			rankArrow.gameObject.SetActive(false);
		}
		
		this.playerRank.text = CommonText.formatContestPlacement(playerRank+1, true);
		
		yield break;
	}

	public void weeklyRaceLoadFailure(string path, Dict args = null)
	{
		Debug.LogErrorFormat("OverlayTopHIR.cs -- weeklyRaceLoadFailure -- failed to load the overlay button from prefab path: {0}", path);
	}
	
	protected override void onClick(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName:"bottom_nav",
			kingdom:	"weekly_race",
			phylum:		SlotsPlayer.isFacebookUser ? "fb_connected" : "anonymous",
			genus:		"click"
		);

		Audio.play("Dialog1OpenWeeklyRace");
		WeeklyRaceLeaderboard.showDialog(Dict.create(D.OBJECT, WeeklyRaceDirector.currentRace));
		if (WeeklyRaceDirector.currentRace != null)
		{
			StatsWeeklyRace.logBottomOverlayClick(WeeklyRaceDirector.currentRace.division, WeeklyRaceDirector.currentRace.competitionRank);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		WeeklyRaceDirector.unregisterStatHandler(refresh);
	}
}
