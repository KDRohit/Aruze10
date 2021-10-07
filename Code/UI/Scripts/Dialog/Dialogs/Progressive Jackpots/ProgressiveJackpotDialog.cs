using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Controls the dialog for showing progressive jackpot win celebration and sharing the news about it.
*/

public abstract class ProgressiveJackpotDialog : DialogBase
{
	public GameObject celebrationParent;
	public GameObject shareParent;
	public UIInput messageInput;
	public FacebookFriendInfo friendInfo;
	public Renderer gameTexture;
	public GameObject poolAmountSizer;
	public TextMeshPro poolAmountLabel;
	public GameObject coinsplosion;
	public GameObject sparkles;
	public GameObject headerLabelSizer;
	public GameObject subheaderLabelSizer;
	public TextMeshPro subheaderLabel;
	public GameObject shareVipHeaderParent;
	public GameObject shareGiantHeaderParent;
	public GameObject shareHeaderParent;

	protected LobbyGame game = null;
	protected long credits = 0L;

	public override void init()
	{ 

	}
	
	protected override void onFadeInComplete()
	{
		base.onFadeInComplete();
		
		StartCoroutine(getThisPartyStarted());
	}
	
	protected IEnumerator getThisPartyStarted()
	{
		const float INITIAL_WAIT_TIME = 1.0f;
		const float SECONDARY_WAIT_TIME = 9.0f;
		const float HEADER_DELAY = 1.4f;
		const float SUBHEADER_DELAY = 2.0f;
		
		// Starting the Audio calls with delays
		Audio.play("UnlockGamePt2", 1.0f, 0, 0, 0);
		Audio.play("FireworksSpray", 1.0f, 0, 0.25f, 0);
		Audio.play("SpcJackpot", 1.0f, 0, 1.0f, 0);
		Audio.play("CoinShowerTerm", 1.0f, 0, 1.75f, 0);
		
		SkippableWait waiter = new SkippableWait();

		celebrationParent.SetActive(true);
		
		poolAmountSizer.transform.localScale = Vector3.one * 0.1f;
		iTween.ScaleTo(poolAmountSizer, iTween.Hash("scale", Vector3.one, "time", 1.0f, "easetype", iTween.EaseType.easeOutElastic));
		
		if (headerLabelSizer != null)
		{
			headerLabelSizer.transform.localScale = Vector3.zero;
			iTween.ScaleTo(headerLabelSizer, iTween.Hash("scale", Vector3.one, "delay", HEADER_DELAY, "time", 1.0f, "easetype", iTween.EaseType.easeOutElastic));
		}

		if (subheaderLabelSizer != null)
		{
			subheaderLabelSizer.transform.localScale = Vector3.zero;
			iTween.ScaleTo(subheaderLabelSizer, iTween.Hash("scale", Vector3.one, "delay", SUBHEADER_DELAY, "time", 1.0f, "easetype", iTween.EaseType.easeOutElastic));
		}

		yield return StartCoroutine(waiter.wait(INITIAL_WAIT_TIME));

		SafeSet.gameObjectActive(coinsplosion, true);
		SafeSet.gameObjectActive(sparkles, true);
		
		SlotsPlayer.addCredits(credits, "progressive win");
		yield return StartCoroutine(waiter.wait(SECONDARY_WAIT_TIME));
		
		Dialog.close();
	}

	protected void Update()
	{
		AndroidUtil.checkBackButton(closeClicked, "dialog", "share_big_win", StatsManager.getGameTheme(), StatsManager.getGameName(), "back", "click");

		if (messageInput.selected)
		{
			resetIdle();
		}
		
		if (shouldAutoClose)
		{
			cancelAutoClose();
			Dialog.close();
		}
	}
	
	/// NGUI button callback.
	protected void shareClicked()
	{
		postAndClose();
	}
	
	/// Post the user message and close the dialog.
	protected void postAndClose()
	{
		cancelAutoClose();

		string postMessage;
		if (messageInput.text == messageInput.defaultText)
		{
			postMessage = "";
		}
		else
		{
			postMessage = messageInput.text;
		}

		Dialog.close();
	}

	/// NGUI button callback.
	protected void closeClicked()
	{
		// TODO: Add some metrics.
		Dialog.close();
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Maybe we'll want to prompt for Rate Me here, but not sure yet.
//		RateMe.checkAndPrompt(RateMe.RateMeTrigger.BIG_WIN);
		// Do special cleanup.
	}
	
	public static void showDialog(JSON json)
	{
		// We need to get the ProgressiveJackpot and LobbyGame to know which game texture to load before showing the dialog.
		LobbyGame game = null;
		string jpKey = json.getString("jackpot_key", "");
		
		ProgressiveJackpot jp = ProgressiveJackpot.find(jpKey);

		if (jp == null)
		{
			Debug.LogError("ProgressiveJackpotDialog: jackpot_key is invalid: " + jpKey);
		}
		else
		{
			bool isVIPJackpot = (jp == ProgressiveJackpot.vipJackpot);

			if (isVIPJackpot && GameState.game != null)
			{
				// The VIP jackpot applies to several games, so use the current game for the message and icon.
				game = GameState.game;
			}
			else
			{
				game = jp.game;
			}
		}
		
		if (game == null)
		{
			Debug.LogError("ProgressiveJackpotDialog: game is null");
			return;
		}
		
		Dict args = Dict.create(
			D.CUSTOM_INPUT, json,
			D.OPTION, jp,
			D.GAME_KEY, game,
			// We must force this dialog so that it can be shown before processing normal spin outcomes:
			D.PRIORITY, SchedulerPriority.PriorityType.IMMEDIATE
		);

		string[] filenames;
		filenames = new string[]
		{
			ProgressiveJackpotDialogHIR.WHEEL_PATH
		};

		string[] gameImages = new string[]
		{
			SlotResourceMap.getLobbyImagePath(game.groupInfo.keyName, game.keyName, ""),
		};
			
		Dialog.instance.showDialogAfterDownloadingTextures("progressive_jackpot_win", filenames, args, nonMappedBundledTextures:gameImages);
	}
}
