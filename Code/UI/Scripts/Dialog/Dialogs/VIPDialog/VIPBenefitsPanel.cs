using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Holds properties for a page scroller panel used on the VIP benefits dialog.
*/

public class VIPBenefitsPanel : TICoroutineMonoBehaviour
{
	public GameObject background;
	public GameObject currentTierLabel;
	public VIPNewIcon icon;
	public TextMeshPro pointsRequired;
	public TextMeshPro extraPurchases;
	public TextMeshPro extraDailyBonus;
	public TextMeshPro extraFromGifts;
	public TextMeshPro extraSentToFriends;
	public TextMeshPro dailyFreeSpinLimit;
	public TextMeshPro dailyCreditsGiftLimit;
	public GameObject earlyGameAccessBullet;
	public GameObject accountManagerBullet;
	public GameObject invitationsBullet;

	public VIPLevel level { get; private set; }
	
	public void setVIPLevel(int levelNumber)
	{
		setVIPLevel(VIPLevel.find(levelNumber));
	}

	public virtual void setVIPLevel(VIPLevel level)
	{
		if (level.levelNumber != VIPLevel.getEventAdjustedLevel())
		{
			if (background != null)
			{				
				background.SetActive(false);
			}

			if (currentTierLabel != null)
			{				
				currentTierLabel.SetActive(false);
			}
		}

		this.level = level;
		
		icon.setLevel(level);
		
		pointsRequired.text = CommonText.formatNumber(level.vipPointsRequired);
		extraPurchases.text = Localize.text("{0}_percent", CommonText.formatNumber(level.purchaseBonusPct));
		extraDailyBonus.text = Localize.text("{0}_percent", CommonText.formatNumber(level.dailyBonusPct));
		extraFromGifts.text = Localize.text("{0}_percent", CommonText.formatNumber(level.receiveGiftBonusPct));

		if (extraSentToFriends != null)
		{
			extraSentToFriends.text = Localize.text("{0}_percent", CommonText.formatNumber(level.sendGiftBonusPct));
		}
		
		dailyFreeSpinLimit.text = CommonText.formatNumber(level.freeSpinLimit);
		dailyCreditsGiftLimit.text = CommonText.formatNumber(level.creditsGiftLimit);

		if (earlyGameAccessBullet != null)
		{			
			SafeSet.gameObjectActive(earlyGameAccessBullet, VIPLevel.earlyAccessMinLevel != null && level.levelNumber >= VIPLevel.earlyAccessMinLevel.levelNumber);
		}

		if (accountManagerBullet != null)
		{
			accountManagerBullet.SetActive(level.dedicatedAccountManager);
		}

		if (invitationsBullet != null)
		{			
			invitationsBullet.SetActive(level.invitationToSpecialEvents);
		}
	}
}
