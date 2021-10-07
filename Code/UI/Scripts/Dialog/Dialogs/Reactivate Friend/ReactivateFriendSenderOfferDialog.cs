using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class ReactivateFriendSenderOfferDialog : DialogBase 
{
	public const string HIGHLIGHT_COLOR = "<#df2dff>";
	public const string HIGHLIGHT_COLOR_IN_DARK = "<#f1a4ff>";

	public const float LOGO_ANIM_DELAY = 0.5f;
	public const float BG_MUSIC_DELAY = 3f;

	private const float DOG_SOUND_DELAY = 3.8f;
	private const float BUTTON_SOUND_DELAY = 6f;
	private const float TIME_TO_RETRACT_INTRO = 1f;
	private const float TIME_TO_SHOW_PICK_GAME = 2.5f;
	private const float BANNER_ANIM_DELAY = 1f;
	private const float WAITING_TO_REVEAL_GREY_PICKS = 1.5f;
	private const float INTERVAL_TO_REVEAL_GREY_PICKS = 1f;
	private const float REMINDER_ANIM_DELAY = 1f;

	// Introduction Part
	public TextMeshPro bodyMessageLabel;
	public TextMeshPro rewardAmountLabel;
	public FacebookFriendInfo receiverInfo;
	public Animator parentAnimator;
	public Animator dogAnimator;
	public Animator paperAnimator;
	public Animator introBubbleAnimator;
	public Animator introButtonAnimator;
	public GameObject coinParticles;
	public Transform background;
	public GameObject closeButton;

	// Pick Game Part
	public Animator facebookAnimator;
	public TextMeshPro facebookBodyMessage;
	public FacebookFriendInfo facebookIcon;
	public Animator pickItemAnimator;
	public Animator pickBannerAnimator;
	public TextMeshPro[] coinLabels;
	public ButtonHandler[] pickItems;
	public Animator[] itemAnimators;

	// Reward Summary Part
	public TextMeshPro congratTextLabel;
	public Animator speechBubbleAnimator;
	public Animator rewardButtonAnimator;

	// Reward Reminder Dialog
	public Animator rewardReminderAnimator;
	public TextMeshPro reminderTextLabel;

	private string eventID = string.Empty;
	private string receiverZID = string.Empty;
	private string receiverFBID = string.Empty;
	private long rewardAmount = 0;
	private string favoriteGameKey = string.Empty;
	private string favoriteGameName = string.Empty;
	private string pickGameEventID = string.Empty;
	private long pickedCoinAmount = 0;
	private long[] revealedCoinAmount;
	private bool isPickable = false;

	private bool caseIsActive = false;

	public override void init()
	{
		// Intro
		Audio.playWithDelay("LogoInRAF", LOGO_ANIM_DELAY);
		StartCoroutine(playBackgroundMusicWithDelay());
		Audio.playWithDelay("CharacterInRAF", DOG_SOUND_DELAY);
		Audio.playWithDelay("AcceptButtonInRAF", BUTTON_SOUND_DELAY);

		JSON data = dialogArgs.getWithDefault(D.DATA, null) as JSON;
		eventID = data.getString("event", "");
		long finalReward = ReactivateFriend.rewardAmount;
		rewardAmountLabel.text = CreditsEconomy.convertCredits(finalReward);

		receiverZID = data.getString("friend_id", "");
		receiverFBID = data.getString("fb_id", "");
		SocialMember receiver = SocialMember.findByZId(receiverZID);
		string receiverNameText = string.Empty;
		receiverNameText = string.Format("{0}{1}</color>", HIGHLIGHT_COLOR, receiver.fullName);
		receiverInfo.member = receiver;
		facebookIcon.member = receiver;

		favoriteGameKey = data.getString("game", "");
		LobbyGame game = LobbyGame.find(favoriteGameKey);
		if (game != null)
		{
			favoriteGameName = game.name;
		}
		else
		{
			favoriteGameName = favoriteGameKey;
			Debug.LogError("Game " + favoriteGameKey + " is invalid.");
		}

		string favoriteGameNameText = string.Format("{0}{1}</color>", HIGHLIGHT_COLOR, favoriteGameName);
		bodyMessageLabel.text = Localize.text("reactivate_friend_sender_offer_desc_{0}", receiverNameText);

		// Pick game
		string receiverNameTextInDark = string.Format("{0}{1}</color>", HIGHLIGHT_COLOR_IN_DARK, receiver.fullName);
		string favoriteGameNameTextInDark = string.Format("{0}{1}</color>", HIGHLIGHT_COLOR_IN_DARK, favoriteGameName);

		facebookBodyMessage.text = Localize.text
			(
				"reactivate_friend_sender_offer_fb_desc_{0}_{1}", 
				receiverNameTextInDark,
				favoriteGameNameTextInDark
			);

		pickedCoinAmount = data.getLongArray("bonus_game_outcome.picks")[0];
		revealedCoinAmount = data.getLongArray("bonus_game_outcome.reveals");
				
		for (int i = 0; i < pickItems.Length; ++i)
		{
			pickItems[i].registerEventDelegate(itemClicked, Dict.create(D.OPTION, i));
		}

		// Congratulation Sceen & Reminder Dialog
		congratTextLabel.text = Localize.text
			(
				"reactivate_friend_sender_offer_pick_congrat"
			);
				
		string finalRewardTextInDark = string.Format("{0}{1}</color>", HIGHLIGHT_COLOR_IN_DARK, CreditsEconomy.convertCredits(finalReward));
		reminderTextLabel.text = Localize.text
			(
				"reactivate_friend_sender_reminder_desc_{0}_{1}", 
				receiverNameTextInDark,
				finalRewardTextInDark
			);

		StatsManager.Instance.LogCount("dialog", "reactivate_friend", "offer", "", "view");
	}

	public IEnumerator playBackgroundMusicWithDelay()
	{
		yield return new WaitForSeconds(BG_MUSIC_DELAY);
		Audio.switchMusicKeyImmediate("BGMusicRAF");
	}

	protected virtual void Update()
	{
		AndroidUtil.checkBackButton(clickClose);
	}

	public virtual void clickClose()
	{
		Audio.play("CollectSubmitRAF");
		Dialog.close();
	}

	public void introAcceptClicked()
	{
		if (caseIsActive)
		{
			return;   // since showPickGameAndFB destroys objects prevent spam clickiing which will cause null exceptions
		}
		caseIsActive = true;
		
		Audio.play("AcceptButtonSubmitRAF");
		RoutineRunner.instance.StartCoroutine(showPickGameAndFB());

		// Hide the close button after user accepts the case.
		Destroy(closeButton);
		StatsManager.Instance.LogCount("dialog", "reactivate_friend", "offer", "", "accept", "click");
	}

	public IEnumerator showPickGameAndFB()
	{
		paperAnimator.Play("Poster Retract");
		introBubbleAnimator.Play("Speech Bubble Retract");
		introButtonAnimator.Play("Button Retract");
		Destroy(coinParticles);

		for (int i = 0; i < pickItems.Length; ++i)
		{
			pickItems[i].SetActive(false);
		}

		yield return new WaitForSeconds(TIME_TO_RETRACT_INTRO);

		Destroy(paperAnimator.gameObject);
		Destroy(introBubbleAnimator.gameObject);
		Destroy(introButtonAnimator.gameObject);

		Audio.play("PickEntersScreenRAF");
		dogAnimator.Play("Dog Sit To Sniff");
		pickItemAnimator.gameObject.SetActive(true);

		yield return new WaitForSeconds(TIME_TO_SHOW_PICK_GAME);
		Audio.play("PostWallScreenInRAF");
		facebookAnimator.gameObject.SetActive(true);
		StatsManager.Instance.LogCount("dialog", "reactivate_friend", "facebook_post", "", "view");
	}

	// This is always done before showing the player the items to pick from.
	public void facebookAcceptClicked()
	{
		Audio.play("CollectRAF");
		isPickable = true;

		for (int i = 0; i < pickItems.Length; ++i)
		{
			pickItems[i].SetActive(true);
		}
		facebookAnimator.Play("FB Wall Post Retract");
		StartCoroutine(showBannerAnimationWithDelay());
		StatsManager.Instance.LogCount("dialog", "reactivate_friend", "facebook_post", "", "accept", "click");
	}

	private IEnumerator showBannerAnimationWithDelay()
	{
		yield return new WaitForSeconds(BANNER_ANIM_DELAY);
		Destroy(facebookAnimator.gameObject);
		pickBannerAnimator.gameObject.SetActive(true);
	}

	public void itemClicked(Dict args = null)
	{
		// Player can't reveal gift before accepting facebook posting.
		if (!isPickable)
		{
			return;
		}

		// Set it false so player can reveal only one gift.
		isPickable = false;

		if (args != null)
		{
			int tag = (int)args.getWithDefault(D.OPTION, 0);
			RoutineRunner.instance.StartCoroutine(itemRevealAnim(tag));
		}
		Audio.play("PickemPickRAF");
		StatsManager.Instance.LogCount("dialog", "reactivate_friend", "pick_game", "", "", "click");
	}

	public IEnumerator itemRevealAnim(int tag)
	{
		itemAnimators[tag].Play("Bag Reveal");
		coinLabels[tag].text = CreditsEconomy.convertCredits(pickedCoinAmount);
		dogAnimator.Play("Dog Sniff To Happy");
		pickBannerAnimator.Play("Pick Banner Retract");

		yield return new WaitForSeconds(WAITING_TO_REVEAL_GREY_PICKS);
		Destroy(pickBannerAnimator.gameObject);

		SkippableWait revealWait = new SkippableWait();
		for (int i = 0; i < itemAnimators.Length; ++i)
		{
			if (i == tag)
			{
				continue;
			}
			Audio.play("PickemRevealOthersRAF");
			itemAnimators[i].Play("Bag Reveal Grey");
			coinLabels[i].text = CreditsEconomy.convertCredits(revealedCoinAmount[i < tag ? i : i - 1]);
			yield return StartCoroutine(revealWait.wait(INTERVAL_TO_REVEAL_GREY_PICKS));
		}

		speechBubbleAnimator.gameObject.SetActive(true);
		rewardButtonAnimator.gameObject.SetActive(true);
	}

	// This is always called after the player has picked one of the items.
	// Finally do everything once the last collect button is clicked.
	// Don't do anything before this, just in case the game crashes or is closed during the flow.
	public void collectClicked()
	{
		Audio.play("CollectRAF");
		StatsManager.Instance.LogCount("dialog", "reactivate_friend", "pick_game", "", "collect", "click");

		// Tell the server that the player claimed the reward and accepted the challenge.
		ReactivateFriendAction.confirmSend(eventID, receiverZID);
		ReactivateFriend.offerData = null;

		SlotsPlayer.addCredits(pickedCoinAmount, "Reactivate Friend: sender's reward of sending invite", playCreditsRollupSound:false, reportToGameCenterManager:false);
		// Need to adjust the amount of credits the client thinks the server has,
		// since there is no additional event that grants these credits after claiming them.
		Server.adjustKnownCredits(pickedCoinAmount);
				
		StartCoroutine(showReminderDialog());
	}
	
	private IEnumerator showReminderDialog()
	{
		if (pickItemAnimator != null && speechBubbleAnimator != null && rewardButtonAnimator != null)
		{
			pickItemAnimator.speed = 2.0f;
			pickItemAnimator.Play("Pick Items Retract");
			speechBubbleAnimator.Play("Speech Bubble Retract");
			rewardButtonAnimator.Play("Button Retract");
			yield return new WaitForSeconds(REMINDER_ANIM_DELAY);
		}
		else
		{
			if (pickItemAnimator == null)
			{
				Bugsnag.LeaveBreadcrumb("ReactivateFriendSendOfferDialog: pickItemAnimator is null");
			}
			if (speechBubbleAnimator == null)
			{
				Bugsnag.LeaveBreadcrumb("ReactivateFriendSendOfferDialog: speechBubbleAnimator is null");
			}
			if (rewardButtonAnimator == null)
			{
				Bugsnag.LeaveBreadcrumb("ReactivateFriendSendOfferDialog: rewardButtonAnimator is null");
			}
			clickClose();
			yield break;
		}

		if (dogAnimator != null)
		{
			Destroy(dogAnimator.gameObject);
		}
		pickItemAnimator.gameObject.SetActive(false);
		Audio.play("SummaryFanfareRAF");
		rewardReminderAnimator.gameObject.SetActive(true);
	}

	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		if (SlotBaseGame.instance != null)
		{
			SlotBaseGame.instance.playBgMusic();
		}
		else
		{
			MainLobby.playLobbyMusic();
		}

		StatsManager.Instance.LogCount("dialog", "reactivate_friend", "offer", "", "", "close");
	}

	public static void showDialog(JSON response)
	{
		if (response == null)
		{
			Debug.LogError("ReactivateFriendSenderOfferDialog: reponse is null.");
			return;
		}

		Dict args = Dict.create(D.DATA, response);

		Scheduler.addDialog("reactivate_friend_sender_offer", args);
	}
}