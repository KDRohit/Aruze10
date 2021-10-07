using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FriendsBottomButton : BottomOverlayButton 
{
	public GameObject collectParent;
	public TextMeshPro collectText;

	protected override void Awake()
	{
		base.Awake();
		init();
		sortIndex = 4;
	}

	protected override void init()
	{
		base.init();

#if UNITY_WEBGL
		collectParent.SetActive(false);     // by default a webgl player is a FB connected player
#else
		if (SlotsPlayer.isFacebookUser || SlotsPlayer.IsFacebookConnected)
		{
			collectParent.SetActive(false);
		}
		else
		{
			collectText.text = CreditsEconomy.convertCredits(SlotsPlayer.instance.mergeBonus);
		}
#endif
	}

	protected override void onClick(Dict args = null)
	{
		StatsManager.Instance.LogCount(
			counterName:"bottom_nav",
			kingdom:	"friends",
			phylum:		SlotsPlayer.isFacebookUser ? "fb_connected" : "anonymous",
			genus:		"click"
		);

		if (SlotsPlayer.isSocialFriendsEnabled)
		{
			// Load open to the friends tab with default behaviour.
			NetworkProfileDialog.showDialog(SlotsPlayer.instance.socialMember, earnedAchievement:null, dialogEntryMode:NetworkProfileDialog.MODE_FIND_FRIENDS);
		}
		else
		{
			StatsManager.Instance.LogCount("bottom_nav", "fbAuth", "", "", "", "click");
			SlotsPlayer.facebookLogin();
		}
	}
}
