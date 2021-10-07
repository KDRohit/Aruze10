using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SlotventuresObjectiveList : ObjectivesGrid
{
	public List<SlotventuresChallengeTab> tabs;
	public Animator winAnimation;
	public Animator slideOut; // maybe just tween this
	public ButtonHandler slideButton;
	public GameObject tabParent;
	public GameObject panelBackground;
	public GameObject clickCatcher;
	private const string SLIDE_OUT = "Slide_Open";
	private const string SLIDE_IN = "Slide_Closed";
	private const string WIN_ANIMATION = "Celebration_New";
	private const int LOAD_TIMEOUT = 30; //30 seconds

	private const float OBJECTIVE_PARENT_OFFSET = 97.5f;
	private const float OBJECTIVE_SPCAING = 195f;
	private const int PANEL_Z_LOCATION = -50;

	private bool isSlidOut = false;

	private int missionIndex = 0;

	private void Start()
	{
		StartCoroutine(loadCampaignData());
	}

	private IEnumerator loadCampaignData()
	{
		int progressLoadTimer = GameTimer.currentTime + LOAD_TIMEOUT;
		
		CommonTransform.setZ(gameObject.transform, PANEL_Z_LOCATION);
		ChallengeLobbyCampaign slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as ChallengeLobbyCampaign;
		if (slotventureCampaign == null)
		{
			Debug.LogError("No slotventure campaign available");
			yield break;
		}

		//When we init a campaign we do a progress update call.  wait for that call to complete before we try to load this data
		while (!slotventureCampaign.hasRecievedProgressUpdate)
		{
			if (GameTimer.currentTime > progressLoadTimer)
			{
				Debug.LogError("Timeout waiting for campaign progress.  Campaign has no progress and is not valid");
				yield break;
			}
			yield return null;
		}
		
		if (GameState.game != null)
		{
			slideOut.Play(SLIDE_OUT);
			slideButton.registerEventDelegate(onClickSlideIn);
			isSlidOut = true;
		}
		else
		{
			slideButton.registerEventDelegate(onClickSlideOut);
		}

		if (slotventureCampaign.currentEventIndex == slotventureCampaign.missions.Count - 1)
		{
			gameObject.SetActive(false);
			yield break;
		}
		
		missionIndex = slotventureCampaign.currentEventIndex;
		if (slotventureCampaign.currentMission != null && slotventureCampaign.currentMission.objectives != null)
		{
			CommonTransform.setY(tabParent.gameObject.transform, 0 + ((slotventureCampaign.currentMission.objectives.Count - 1) * OBJECTIVE_PARENT_OFFSET));
			CommonTransform.setHeight(panelBackground.transform, panelBackground.transform.localScale.y - ((tabs.Count - slotventureCampaign.currentMission.objectives.Count) * OBJECTIVE_PARENT_OFFSET));

			for (int i = 0; i < tabs.Count; i++)
			{
				if (i < slotventureCampaign.currentMission.objectives.Count)
				{
					tabs[i].init(slotventureCampaign.currentMission.objectives[i]);
					CommonTransform.setY(tabs[i].gameObject.transform, i * -OBJECTIVE_SPCAING);
				}
				else
				{
					tabs[i].gameObject.SetActive(false);
				}
			}
		}

		// We didn't want to be able to click through or scroll through the panel, so this blocks it in the lobby.
		if (clickCatcher != null)
		{
			CommonTransform.setHeight(clickCatcher.transform, panelBackground.transform.localScale.y);
		}
	}

	public override void init(LobbyGame game, Mission mission = null)
	{
		// This is getting called weather we like it or not.
	}

	public override void playSpinAnimations()
	{
		if (isSlidOut)
		{
			onClickSlideIn();
		}
	}

	public override void refresh(Mission mission = null)
	{
		// Sound only wanted us to play one or the other so now our for loop below is kind of an abomination
		bool shouldPlayComplete = false;
		bool shouldPlayIncrement = false;
		bool shouldPlayReset = false;

		// Any update calls this, we'll have to check for completion.
		ChallengeLobbyCampaign slotventureCampaign = CampaignDirector.find(SlotventuresChallengeCampaign.CAMPAIGN_ID) as ChallengeLobbyCampaign;
		if (missionIndex == slotventureCampaign.currentEventIndex)
		{
			for (int i = 0; i < tabs.Count; i++)
			{
				if (i < slotventureCampaign.currentMission.objectives.Count)
				{
					Objective objective = slotventureCampaign.missions[missionIndex].objectives[i];

					// determine which sound we should play.
					if (objective.currentAmount < objective.amountNeeded)
					{
						if (tabs[i].currentProgress < objective.currentAmount)
						{
							shouldPlayIncrement = true;
						}
						else if (tabs[i].currentProgress > objective.currentAmount)
						{
							shouldPlayReset = true;
						}
					}
					else if (!tabs[i].wasComplete)
					{
						shouldPlayComplete = true;
					}

					tabs[i].updateCounts(slotventureCampaign.missions[missionIndex].objectives[i], isSlidOut);
				}
			}
		}
		else
		{
			shouldPlayComplete = true;
			for (int i = 0; i < tabs.Count; i++)
			{
				if (i < slotventureCampaign.missions[missionIndex].objectives.Count)
				{
					tabs[i].forceFinishedState(slotventureCampaign.missions[missionIndex].objectives[i], isSlidOut);
				}
			}
			StartCoroutine(playWinLoopAfterDelay());
		}

		if (shouldPlayComplete)
		{
			if (missionIndex != slotventureCampaign.currentEventIndex)
			{
				Audio.playWithDelay(SlotventuresLobby.assetData.audioMap[LobbyAssetData.ALL_OBJECTIVES_COMPLETE], 2.25f);
			}
			else
			{
				Audio.play("ChallengeSingleCompleteSlotVenturesCommon");
			}
			Audio.playWithDelay("ChallengeSlideSlotVenturesCommon", 1.25f);
			Audio.playWithDelay("ChallengeSlideSlotVenturesCommon", 4.25f);
		}
		else if (shouldPlayIncrement)
		{
			Audio.play("ChallengeIncrementSlotVenturesCommon");
		}
		else if (shouldPlayReset)
		{
			//TODO: play something else here
			Audio.play("ChallengeIncrementSlotVenturesCommon");
		}
	}

	public void setIdleAnimstates()
	{
		for (int i = 0; i < tabs.Count; i++)
		{
			if (tabs[i].wasComplete)
			{
				tabs[i].playCompleteIdleAnimation();
			}
		}
	}

	private IEnumerator playWinLoopAfterDelay()
	{
		yield return new WaitForSeconds(2.5f);
		winAnimation.Play(WIN_ANIMATION);

		yield return null;
	}
	public void onClickSlideOut(Dict args = null)
	{
		if (SlotBaseGame.instance == null || !SlotBaseGame.instance.isGameBusy)
		{
			Audio.play("minimenuopen0");
			isSlidOut = true;
			slideButton.unregisterEventDelegate(onClickSlideOut);
			slideButton.registerEventDelegate(onClickSlideIn);
			slideOut.Play(SLIDE_OUT);
		}		
	}

	public void onClickSlideIn(Dict args = null)
	{
		if (isSlidOut)
		{
			Audio.play("minimenuclose0");
			isSlidOut = false;
			slideButton.unregisterEventDelegate(onClickSlideIn);
			slideButton.registerEventDelegate(onClickSlideOut);
			slideOut.Play(SLIDE_IN);
		}
		
	}

	public override void onSelectAutoSpin()
	{
		onClickSlideIn();
	}

}
