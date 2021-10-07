using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LevelUpDialogHIR : LevelUpDialog
{
	// Variables specific to HIR version.
	public GameObject levelStar;
	public Transform coinIconLarge;
	public Transform coinIconSmall;
	public GameObject creditsMeterParent;
	public GameObject buttonsParent;
	public TextMeshPro walletLabel;
	
	public override void init()
	{
		base.init();
		creditsMeterParent.SetActive(!_isPassiveMode);
		
		// Show the new player level:
		this.levelLabel.text = newLevel.ToString();

		// This is how many coins we currently have:

		this.startingCoinBeforeLevel = SlotsPlayer.creditAmount;
		this.walletLabel.text = CreditsEconomy.convertCredits(SlotsPlayer.creditAmount);


		// This is how many coins we're going to collect:
		this.rewardCoinsLabel.text = CreditsEconomy.convertCredits(bonusCreditsForLevel);

		StatsManager.Instance.LogCount("dialog", "level_up", newLevel.ToString(), "", "", "view", bonusCreditsForLevel);
			
		// This is how many VIP points we're going to collect:
		this.rewardPointsLabel.text = CommonText.formatNumber(bonusVipForLevel);
		
		// Fancy scale of the star + level counter:
		this.levelStar.transform.localScale = Vector3.zero;
		iTween.ScaleTo(this.levelStar, iTween.Hash("scale", Vector3.one, "islocal", true, "time", 3.0f, "easetype", iTween.EaseType.easeOutElastic));

		// Level Up audio.
		Audio.play("levelup_short");
		Audio.play("cheer_c");
	}


	protected override void collectClicked()
	{	
		cancelAutoClose();
		if (_isPassiveMode)
		{
			// When using passive level up mode, clicking this button is actually the OK button,
			// which is the same as clicking the close button.
			closeClicked();
		}
		else
		{
			StatsManager.Instance.LogCount("dialog", "level_up", "collect", "click");
			buttonsParent.SetActive(false);
			StartCoroutine(closeAfterCollect());
		}
	}
	/// Show the flying coin and rollup before closing.
	private IEnumerator closeAfterCollect()
	{
		// Show the flying coin to the credits meter.
		
		// Create the coin as a child of "sizer", at the position of "coinIconSmall",
		// with a local offset of (0, 0, -100) so it's in front of everything else with room to spin in 3D.
		CoinScript coin = CoinScript.create(
			sizer,
			coinIconSmall.position,
			new Vector3(0, 0, -100)
	    );
		
		// Calculate the local coordinates of the destination, which is where "coinIconLarge" is positioned relative to "sizer".
		Vector2 destination = NGUIExt.localPositionOfPosition(sizer, coinIconLarge.position);
		yield return StartCoroutine(coin.flyTo(destination));
		// Hide the coin after it reaches the destination, but don't destroy it
		// because we need to let the sparkles fade off first.
		coin.coin.SetActive(false);
			
		applyLevelUp(oldLevel, newLevel, bonusCreditsForLevel, bonusVipForLevel);	// Do this here so the player's overlay credits count is in sync with the following rollup...

		yield return StartCoroutine(SlotUtils.rollup(this.startingCoinBeforeLevel, SlotsPlayer.creditAmount, this.walletLabel));

		// Celebrate Audio
		Audio.play("DialogCelebrate");
		
		Dialog.close();
	}
}
