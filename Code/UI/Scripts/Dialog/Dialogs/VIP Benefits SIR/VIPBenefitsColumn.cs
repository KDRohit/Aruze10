using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
/*
Holds properties for a page scroller panel used on the VIP benefits dialog.
*/

public class VIPBenefitsColumn : TICoroutineMonoBehaviour
{
	public TextMeshPro extraPurchaseCoins;
	public TextMeshPro extraDailyBonus;
	public TextMeshPro extraFromGifts;
	public TextMeshPro extraSentToFriends;
	public TextMeshPro dailyFreeSpinLimit;
	public TextMeshPro dailyCreditsGiftLimit;
	public GameObject earlyGameAccess;
	public GameObject accountManagerBullet;
	public GameObject invitationsBullet;
	public VIPNewIcon icon;

	public void init(int levelNumber)
	{
		VIPLevel level = VIPLevel.find(levelNumber);

		if (levelNumber != SlotsPlayer.instance.vipNewLevel)
		{
		    //currentTierLabel.SetActive(false);
		}

		icon.setLevel(level);
		extraPurchaseCoins.text = Localize.text("{0}_percent", CommonText.formatNumber(level.purchaseBonusPct));
		extraDailyBonus.text = Localize.text("{0}_percent", CommonText.formatNumber(level.dailyBonusPct));
		extraFromGifts.text = Localize.text("{0}_percent", CommonText.formatNumber(level.receiveGiftBonusPct));
		extraSentToFriends.text = Localize.text("{0}_percent", CommonText.formatNumber(level.sendGiftBonusPct));
		dailyFreeSpinLimit.text = CommonText.formatNumber(level.freeSpinLimit);
		dailyCreditsGiftLimit.text = CommonText.formatNumber(level.creditsGiftLimit);

		earlyGameAccess.SetActive(VIPLevel.earlyAccessMinLevel.levelNumber <= levelNumber);
		accountManagerBullet.SetActive(level.dedicatedAccountManager);
		invitationsBullet.SetActive(level.invitationToSpecialEvents);
	}
}
