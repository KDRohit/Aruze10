using UnityEngine;
using System.Collections;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class BuyPagePerkMOTD : DialogBase
{
	public TextMeshPro timerLabel;
	public TextMeshPro titleTextLabel;
	public TextMeshPro motdInfoTextLabel;
	public Renderer backgroundRenderer;
	public Renderer logoRenderer;
	
	private bool isEventExpiryDialog;
	private bool shouldCloseOnExpiration = false;
	private string statKey;
	
	public override void init()
	{
		downloadedTextureToRenderer(backgroundRenderer, 0);
		downloadedTextureToRenderer(logoRenderer, 1);

		if (!isValidPlayer())
		{
			return;
		}

		isEventExpiryDialog = (bool) dialogArgs.getWithDefault(D.OPTION, false);
		statKey = isEventExpiryDialog ? "buy_again"  : "motd";
		StatsManager.Instance.LogCount("dialog", "buy_page_perk", statKey, "", "view", "");

		updateText();

		updateTimer();
		
		// If the feature is active, then close this dialog when it expires.
		// If it's not active, they opened it from the Dev GUI, so don't close it.
		shouldCloseOnExpiration = MysteryGift.isIncreasedMysteryGiftChance;

		MOTDFramework.markMotdSeen(dialogArgs);
	}
	
	public void Update()
	{
		if (!isValidPlayer())
		{
			return;
		}
		AndroidUtil.checkBackButton(clickClose);
		updateTimer();
	}

	private void updateText()
	{
		string titleLocKey = isEventExpiryDialog ? "get_another_perk"  : "buy_page_perk_title";

		titleTextLabel.text = Localize.textUpper(titleLocKey);

		string motdInfoTextLocKey = isEventExpiryDialog ? "buy_page_perk_expired_description" 
								: "buy_page_perk_description";

		motdInfoTextLabel.text = Localize.text(motdInfoTextLocKey);

	}
	
	private bool isValidPlayer()
	{
		if (SlotsPlayer.instance == null)
		{
			// This should never happen since SlotsPlayer.instance is designed to always return a new instance if one doesn't exist.
			enabled = false;
			Debug.LogError("BuyPagePerkMOTD: SlotsPlayer.instance is null somehow.");
			return false;
		}
		return true;
	}
	
	public void updateTimer()
	{
		if (BuyPagePerk.isActive)
		{
			timerLabel.text = BuyPagePerk.timerRange.timeRemainingFormatted;
		}
		else if (shouldCloseOnExpiration)
		{
			Dialog.close();
		}
	}

	public void clickBuyNow()
	{
		Dialog.close();
		if (BuyPagePerk.isActive)
		{
			StatsManager.Instance.LogCount("dialog", "buy_page_perk", statKey, "", "buy_now", "click");
			BuyCreditsDialog.showDialog();
		}
	}
	
	public void clickClose()
	{
		StatsManager.Instance.LogCount("dialog", "buy_page_perk", statKey, "", "close", "click");
		Dialog.close();
	}
	
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
	}
		
	public static bool showDialog(bool isEventExpiryDialog = false, string motdKey = "")
	{
		string[] texturePaths = new string[]
		{
			"misc_dialogs/buy_page_perk/Panel MOTD Bkgd.png",
			"misc_dialogs/buy_page_perk/Icon Perks.png",
		};

		Dict args = Dict.create(
			D.IS_LOBBY_ONLY_DIALOG, GameState.isMainLobby,
			D.MOTD_KEY, motdKey,
			D.OPTION, isEventExpiryDialog
		);

		Dialog.instance.showDialogAfterDownloadingTextures("buy_page_perk", texturePaths, args);
		return true;
	}
}
