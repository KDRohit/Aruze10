using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using QuestForTheChest;

/*
Handles display of dialog for telling a player that he still won credits after a bonus game was interrupted.
*/

public class BonusGameErrorDialog : DialogBase
{
	public TextMeshPro gameName;
	public TextMeshPro credits;
	public UITexture gameTexture;
	public UISprite qfcLogo;
	public UISprite coinRewardSprite;
	public GameObject QFCKeyRoot;

	
	private LobbyGame game = null;
	private long jsonKeyAmount = 0;
	private string featureName = "";

	/// Initialization
	public override void init()
	{
		// Let's make sure that eventID doesn't come back to haunt us.
		JSON data = dialogArgs.getWithDefault(D.CUSTOM_INPUT, null) as JSON;

		// acknowledge that the player has seen the info about the bonus they missed
		sendServerAcknowledgement(data);

		string bonusGameKey = data.getString("bonus_game_key", "");
		featureName = data.getString("feature_name", "");
		Userflows.addExtraFieldToFlow(userflowKey, "game_key", bonusGameKey);
		// Now let's set our vars as needed...
		long jsonCreditAmount = data.getLong("credits", 0L);
		jsonKeyAmount = data.getLong("reward_keys", 0);
		bool isGrantingCreditsNow = data.getBool("grant", false);

		// This is a bonus that couldn't have the value granted until now,
		// due to requiring a calculation to be made after login
		// so grant it right away
		if (isGrantingCreditsNow && jsonCreditAmount > 0)
		{
			SlotsPlayer.addCredits(jsonCreditAmount, "missed bonus grant", playCreditsRollupSound: false, reportToGameCenterManager: false, shouldSkipOnTouch: true);
		}

		game = dialogArgs.getWithDefault(D.GAME_KEY, null) as LobbyGame;
		if (game != null)
		{
			switch (bonusGameKey)
			{
				case "mystery_gift":
					gameName.text = Localize.textUpper("mystery_gift");
					break;
				case "mystery_gift_2":
					gameName.text = Localize.textUpper("big_slice");
					break;
				default:
					gameName.text = Localize.toUpper(game.name);
					break;
			}

			downloadedTextureToUITexture(gameTexture, 0);
		}
		else if (!string.IsNullOrEmpty(featureName))
		{
			switch (featureName)
			{
				case "hir_lotto_blast":
					gameName.text = Localize.text("lotto_blast_missed_bonus");
					break;
				default:
					gameName.text = Localize.text(featureName + "_missed_bonus");
					break;
			}
			downloadedTextureToUITexture(gameTexture, 0);
		}
		else
		{
			Debug.LogError("BonusGameErrorDialog dialog: No game provided.");
		}

		if (jsonKeyAmount > 0)
		{
			string keyPath = "Features/Quest for the Chest/Prefabs/Instanced Prefabs/Quest for the Chest Key Item";
			AssetBundleManager.load(this, keyPath, keyLoadSuccess, keyLoadFailure, isSkippingMapping:true, fileExtension:".prefab");
			AssetBundleManager.load(this, string.Format(QuestForTheChestFeature.THEMED_ATLAS_PATH, ExperimentWrapper.QuestForTheChest.theme), atlasLoadSuccess, assetLoadFailed, isSkippingMapping:true, fileExtension:".prefab");
			credits.text = CommonText.formatNumber(jsonKeyAmount);
			coinRewardSprite.gameObject.SetActive(false);
		}
		else
		{
			coinRewardSprite.gameObject.SetActive(true);
			qfcLogo.gameObject.SetActive(false);
			QFCKeyRoot.gameObject.SetActive(false);
			credits.text = CreditsEconomy.convertCredits(jsonCreditAmount);
		}

		Audio.play("minimenuopen0");
	}

	private void atlasLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		UIAtlas themedAtlas = ((GameObject)obj).GetComponent<UIAtlas>();
		qfcLogo.atlas = themedAtlas;
		qfcLogo.spriteName = "Logo Toaster";
	}

	private void assetLoadFailed(string assetPath, Dict data = null)
	{
		Bugsnag.LeaveBreadcrumb("QFC Themed Asset failed to load: " + assetPath);
	}

	private void keyLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		GameObject gameObject = obj as GameObject;
		if (gameObject != null)
		{
			QFCKeyObject keyObj = gameObject.GetComponent<QFCKeyObject>();
			if (keyObj != null)
			{
				keyObj.setupKeyObjects((int)jsonKeyAmount);
				NGUITools.AddChild(QFCKeyRoot, gameObject);
			}
		}
		else
		{
			Debug.LogError("Invalid prefab");
		}

	}

	private void keyLoadFailure(string assetPath, Dict data = null)
	{
		Bugsnag.LeaveBreadcrumb("QFC Key Asset failed to load: " + assetPath);
	}

	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();

		if (game == null && string.IsNullOrEmpty(featureName))
		{
			// If no game is assigned and no feature name is specified, something went wrong, so just close immediately.
			// The specific error message has already been logged in init().
			Dialog.close();
		}
	}
	
	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked);
	}
	
	public void okClicked()
	{
		Dialog.close();
	}

	public void closeClicked()
	{
		Dialog.close();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}

	private static void sendServerAcknowledgement(JSON data)
	{
		string eventId = data.getString("event", null);

		if (!string.IsNullOrEmpty(eventId))
		{
			SlotAction.seenBonusSummaryScreen(eventId);
		}
		else
		{
			// This is possible when testing this dialog without a real event.
			Debug.LogWarning("BonusGameErrorDialog dialog: No eventId provided.");
		}
	}

	public static void handlePresentation(JSON data)
	{
		handlePresentation(data, false);
	}

	public static void handlePresentation(JSON data, bool forceShow)
	{
		if (data == null)
		{
			Debug.LogError("Invalid bonus data");
			return;
		}

		long creditAmount = data.getLong("credits", 0L);
		long keyAmount = data.getLong("reward_keys", 0L);
		string featureName = data.getString("feature_name", "");

		//Don't show the dialog if we don't have any items to give
		if (!forceShow && keyAmount == 0 && creditAmount == 0)
		{
			// acknowledge that the player has seen the info about the bonus they missed
			sendServerAcknowledgement(data);
		}
		else if (!string.IsNullOrEmpty(featureName))
		{
			handleFeatureMissedBonus(featureName, data);
		}
		else
		{
			showDialog(data);
		}
	}
	
	private static void showDialog(JSON data)
	{
		string gameKey = data.getString("slots_game_key", null);
		if (gameKey == null)
		{
			// there will be no 'slots_game_key' for missed LikelyToLapses sweet surprize bonuses and PTR final wheel events
			// TODO: this dialog is not setup to display those kind of msgs yet
			Debug.LogWarningFormat("BonusGameErrorDialog: Missing slots_game_key in {0}", data.ToString());
			sendServerAcknowledgement(data);   // prevent server from sending down this message again
			return;
		}
		LobbyGame game = LobbyGame.find(gameKey);
		if (game == null)
		{
			Debug.LogWarningFormat("No LobbyGame found for key '{0}'. This could happen for web games that don't exist for mobile, and is probably ok to ignore most of the time.", gameKey);
			sendServerAcknowledgement(data);  // prevent server from sending down this message again
			return;
		}

		Dict args = Dict.create(
			D.CUSTOM_INPUT, data,
			D.GAME_KEY, game,
			D.STACK, false
		);

		string pathName = SlotResourceMap.getLobbyImagePath(null == game ? "" : game.groupInfo.keyName, game == null ? "" : game.keyName, "");
		Dialog.instance.showDialogAfterDownloadingTextures("bonus_error", nonMappedBundledTextures:new string[]{pathName}, args:args);
	}

	private static void handleFeatureMissedBonus(string featureName, JSON data)
	{
		Dict args = Dict.create(
			D.CUSTOM_INPUT, data,
			D.STACK, false
		);

		string texturePath = "";
		switch (featureName)
		{
			case "hir_lotto_blast":
				texturePath = "Features/Lotto Blast/Textures/lottoblast_missedbonus_1x1";
				break;
			case "casino_empire":
				texturePath = "Features/Board Game/Common/Textures/Missed Bonus/boardgame_missedbonus_1x1";
				break;
		}
		Dialog.instance.showDialogAfterDownloadingTextures("bonus_error", nonMappedBundledTextures:new string[]{texturePath}, args:args, isExplicitPath: true);
	}
}
