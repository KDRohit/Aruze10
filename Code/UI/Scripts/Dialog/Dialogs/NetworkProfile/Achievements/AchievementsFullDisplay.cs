using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class AchievementsFullDisplay : MonoBehaviour
{
	[SerializeField] private AnimationListController.AnimationInformationList favoriteSelectedAnimation;
	[SerializeField] private AnimationListController.AnimationInformationList defaultAnimation;

	[SerializeField] private PageController pageController;
	[SerializeField] private ImageButtonHandler addToProfileButton;
	[SerializeField] private ImageButtonHandler closeButton;
	[SerializeField] private GameObject trophyInfoPrefab;
	[SerializeField] private Animator addToProfileAnimator;

	private int currentIndex = 0;
	private List<Achievement> achievementList;
	
	private ProfileAchievementsTab tab;
	[HideInInspector] public SocialMember member; // For referencing the progress
	
	private const string OPEN_AUDIO = "PointsEarnedAlertNetworkAchievements";
	private const string CLOSE_AUDIO = "minimenuclose0";
	private const string LEFT_AUDIO = "FriendsLeftArrow";
	private const string RIGHT_AUDIO = "FriendsRightArrow";
	private const string TROPHY_ADDED_AUDIO = "TrophyAddedNetworkAchievements";

	public void init(ProfileAchievementsTab tab)
	{
		// One time init stuff.
		this.tab = tab;
		closeButton.registerEventDelegate(closeClicked);
		pageController.onPageViewed += onPageView;
		pageController.onPageHide += onPageHide;
		pageController.onSwipeLeft += onSwipeLeft;
		pageController.onSwipeRight += onSwipeRight;
	}

	public void show(Achievement achievement)
	{		
		this.achievementList = new List<Achievement>(){achievement};
		this.currentIndex = 0;
	    setup(achievementList[currentIndex]);
	}

	public void show(List<Achievement> achievementList, int index)
	{
		this.achievementList = achievementList;
		this.currentIndex = index;
		setup(achievementList[index]);
	}

	private void setup(Achievement currentAchievement)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "trophy_detail",
			klass: "view",
			family: currentAchievement.id,
			genus: tab.member.networkID);
		
		Audio.play(OPEN_AUDIO);
		if (tab != null && tab.trophySlideController != null)
		{
			tab.trophySlideController.enabled = false;
		}

		gameObject.SetActive(true); // Turn it on.
		if (member != null && member.isUser)
		{
			// We won't let other people set your display achievement.
			addToProfileButton.gameObject.SetActive(true);
			addToProfileButton.registerEventDelegate(addClicked);	
		}
		else
		{
			addToProfileButton.gameObject.SetActive(false);
		}
		
		pageController.pageWidth = NGUIExt.effectiveScreenWidth;
		pageController.init(trophyInfoPrefab, achievementList.Count, onPageSetup, currentIndex);
		
		addToProfileButton.isEnabled = false; // Disable this so that we properly animate it in the first time.

		AnimationListController.playListOfAnimationInformation(defaultAnimation);
	}


	
	public void hide()
	{
		gameObject.SetActive(false);
		if (tab != null && tab.trophySlideController != null)
		{
			tab.trophySlideController.enabled = true;
		}
		Audio.play(CLOSE_AUDIO);
		Destroy (FTUEManager.Instance.Go);
		tab.trophySlideController.enabled = true; // If this was off, turn if back on.
	}
	
	private void addClicked(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "trophy_detail",
			klass: "favorite",
			family: achievementList[currentIndex].id,
			genus: member.networkID);
		
		Audio.play(TROPHY_ADDED_AUDIO);
		Destroy (FTUEManager.Instance.Go);
		StartCoroutine(favoriteSelected(achievementList[currentIndex]));
	}

	private void closeClicked(Dict args = null)
	{
		if (FTUEManager.isFtueEnabled (CustomPlayerData.ACHIEVEMENTS_FTUE_3)) {
			StatsManager.Instance.LogCount(
				counterName: "dialog",
				kingdom: "ll_trophy_ftue",
				phylum: "favorite_trophy",
				klass: "skip",
				family: "",
				genus: member.networkID,
				val: 3);
		}
		hide();
	}
	
	private IEnumerator favoriteSelected(Achievement achievement)
	{
		yield return AnimationListController.playListOfAnimationInformation(favoriteSelectedAnimation);
		NetworkAchievementAction.setDisplayAchievement(achievement);
		tab.dialog.profileDisplay.setFavoriteTrophy(achievement);
		tab.dialog.achievementDisplay.refreshTrophies();
		addToProfileAnimator.Play("outro");
	}

	private void onPageSetup(GameObject page, int index)
	{
		AchievementsDisplayPanel displayPanel = page.GetComponent<AchievementsDisplayPanel>();
		if (displayPanel != null)
		{
			Achievement achievement = achievementList[index];
			if (achievement == null)
			{
				Debug.LogErrorFormat("AchievementsFullDisplay.cs -- pageSetup -- trying to init an info panel with a null achievement from index: {0}", index);
				return;
			}
			displayPanel.init(achievement, member, false, null);
		}
	}

	private bool shouldShowAddProfile(Achievement achievement)
	{
		bool shouldShow = member.isUser && 
			achievement != member.networkProfile.displayAchievement &&
			achievement.isUnlocked(member);
			
		if (NetworkAchievements.rewardsEnabled)
		{
			shouldShow = shouldShow && achievement.hasCollectedReward(member);
		}
		return  shouldShow;
	}
	
	private string getAddProfileAnimation(Achievement achievement)
	{
		if (shouldShowAddProfile(achievement))
		{
			return addToProfileButton.isEnabled ? "hold" : "intro";
		}
		else
		{
		    return addToProfileButton.isEnabled ? "outro" : "default";
		}
	}
	
    private void onPageView(GameObject page, int index)
	{
		AchievementsDisplayPanel displayPanel = page.GetComponent<AchievementsDisplayPanel>();
		AnimationListController.playListOfAnimationInformation(defaultAnimation);
		if (displayPanel != null)
		{
			Achievement achievement = displayPanel.achievement;
			displayPanel.showTrophy();
			bool shouldShow = shouldShowAddProfile(achievement);
			string buttonAnimation = getAddProfileAnimation(achievement);
			addToProfileAnimator.Play(buttonAnimation);
			addToProfileButton.isEnabled = shouldShow;

			bool doesNeedRefresh = false;
			if (achievement.isUnlockedNotClicked)
			{
				NetworkAchievements.markUnlockedAchievementClicked(achievement);
				doesNeedRefresh = true;
			}
			
			if (achievement.isUnlockedNotSeen)
			{
				NetworkAchievements.markUnlockedAchievementSeen(achievement);
				doesNeedRefresh = true;				
			}

			if (achievement.isNew)
			{
				NetworkAchievements.markAchievementSeen(achievement);
				doesNeedRefresh = true;				
			}
			
			if (doesNeedRefresh)
			{
				tab.dialog.achievementDisplay.refreshTrophies();
			}
			currentIndex = index;
		}
		else
		{
			Debug.LogErrorFormat("AchievementsFullDisplay.cs -- onPageView -- displayPanel was null");
		}
	}

    private void onPageHide(GameObject page, int index)
	{
		AchievementsDisplayPanel displayPanel = page.GetComponent<AchievementsDisplayPanel>();
		if (displayPanel != null)
		{
			displayPanel.hideTrophy();
		}
		else
		{
			Debug.LogErrorFormat("AchievementsFullDisplay.cs -- onPageHide -- displayPanel was null");
		}
	}

	private void onSwipeLeft(GameObject page, int index)
	{
		Audio.play(LEFT_AUDIO);		
		string id = "notyet";
		AchievementsDisplayPanel displayPanel = page.GetComponent<AchievementsDisplayPanel>();
		if (displayPanel != null)
		{
			id = displayPanel.achievement.id;
		}		
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "trophy_detail",
			klass: "click_left",
			family: id,
			genus: tab.member.networkID);
	}

	private void onSwipeRight(GameObject page, int index)
	{
		Audio.play(RIGHT_AUDIO);
		string id = "notyet";
		AchievementsDisplayPanel displayPanel = page.GetComponent<AchievementsDisplayPanel>();
		if (displayPanel != null)
		{
			id = displayPanel.achievement.id;
		}		
		StatsManager.Instance.LogCount(
			counterName: "dialog",
			kingdom: "ll_profile",
			phylum: "trophy_detail",
			klass: "click_right",
			family: id,
			genus: tab.member.networkID);
	}
}