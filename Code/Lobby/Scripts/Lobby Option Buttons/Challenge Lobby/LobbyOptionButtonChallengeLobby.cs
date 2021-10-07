using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Controls UI behavior of a menu option button related to Land of Oz.
Just to keep things together, this script handles the portal in the main lobby and the final jackpot option in the LOZ lobby.
This is a very special case, so it derives directly from LobbyOptionButton.
*/

public class LobbyOptionButtonChallengeLobby : LobbyOptionButton
{	
	public GameObject lockElements;
	public TextMeshPro lockLevelLabel;
	public TextMeshPro jackpotLabel;
	public Animator jackpotAnimator;
	public string campaignName;
	public GameObject codeBox;

	public static Dictionary<string, LobbyOptionButtonChallengeLobby> challengePortals = new Dictionary<string, LobbyOptionButtonChallengeLobby>();

	// =============================
	// CONST
	// =============================
	protected const string JACKPOT_CELEBRATION = "Lobby Option Jackpot Celebrate";
	
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);
		refresh();

		if (isPortal && !challengePortals.ContainsKey(campaignName))
		{
			challengePortals.Add(campaignName, this);
		}
	}

	public static LobbyOptionButtonChallengeLobby findByCampaign(string campaignName)
	{
		if (challengePortals.ContainsKey(campaignName))
		{
			return challengePortals[campaignName];
		}
		return null;
	}

	protected override void OnClick()
	{
		ChallengeLobbyCampaign campaign = CampaignDirector.find(campaignName) as ChallengeLobbyCampaign;

		if (campaign == null) {	return;	}

		Bugsnag.LeaveBreadcrumb("Clicked the enter campaign: " + campaignName);
		
		if (campaign.isPortalUnlocked)
		{
			// portal or jackpot option?
			if (isPortal)
			{
				// portal, log click
				StatsManager.Instance.LogCount
				(
					"lobby",
					campaignName,
					"",
					"",
					"",
					"click"
				);
			}
			base.OnClick();
		}
		else if (!campaign.isPromoUnLocked && !campaign.isLevelLocked)
		{
			PromoCodeDialog.showDialog(campaignName, SchedulerPriority.PriorityType.IMMEDIATE, refresh);
		}
	}
	
	public override void refresh(){ refresh(false); }

	public void refresh(bool doShowCelebration = false)
	{
		ChallengeLobbyCampaign campaign = CampaignDirector.find(campaignName) as ChallengeLobbyCampaign;

		if (campaign == null)
		{
			return;
		}

		if (campaign.isPortalUnlocked && codeBox != null)
		{
			codeBox.SetActive(false);
		}
		
		if (doShowCelebration && jackpotAnimator != null && jackpotLabel != null)
		{
			// The lobby is going to roll it up when celebrating, so start with 0.
			jackpotLabel.text = CreditsEconomy.convertCredits(0);
		}
		else if (jackpotLabel != null)
		{
			jackpotLabel.text = CreditsEconomy.convertCredits(campaign.currentJackpot);
		}
				
		// Set the lock status.
		if (option != null && option.action != "")
		{
			// This is the main lobby portal option.
			if (lockElements != null)
			{
				lockElements.SetActive(campaign != null && (!campaign.isPortalUnlocked || campaign.isLevelLocked));

				if (lockLevelLabel != null && campaign.isLevelLocked)
				{
					lockLevelLabel.text = campaign.unlockLevel.ToString();
				}
			}
		}
		else
		{
			// This is the final jackpot node in Land of Oz. There are no lock elements.
		}

		if (campaign != null)
		{
			switch (campaign.state)
			{
				case ChallengeCampaign.INCOMPLETE:
					jackpotLabel.text = Localize.textUpper("event_over");
					break;

				case ChallengeCampaign.COMPLETE:
					jackpotLabel.text = Localize.textUpper(campaign.state);

					if (jackpotAnimator != null)
					{
						jackpotAnimator.Play(JACKPOT_CELEBRATION);
					}
					break;
			}
		}
	}

	protected bool isPortal
	{
		get
		{
			// identifying factor that the reused script is actually on the portal, and not the jackpot option
			return jackpotAnimator == null;	
		}
	}
}