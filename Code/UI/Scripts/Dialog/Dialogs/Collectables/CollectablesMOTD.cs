using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;
using TMPro;
public class CollectablesMOTD : DialogBase
{
	public TextMeshPro title;
	public ButtonHandler collectButton;
	public ClickHandler packButton;

	// Pack presentation needs to be added and shown. 
	public CollectAPack packPresentation;
	private CollectableAlbum albumData;
	private string packInfoKlassStat = "";
	private int cardsInPack = 0;

	public GameObject cardToolTip;

	private const string PACK_INTRO_ANIM_NAME = "Collection FTUE intro";
	private const string BOTTOM_CONTENT_ANIM_INTRO_NAME = "animation start";

	private const float PACK_INTRO_DELAY = 0.15f;

	private const string VIEW_CARDS_BUTTON_LOC = "view_my_cards";

	public override void init()
	{
		if (string.IsNullOrEmpty(Collectables.currentAlbum))
		{
			Bugsnag.LeaveBreadcrumb("Trying to show collectables motd with no active collection");
			Dialog.close(this);
			return;
		}
		collectButton.gameObject.SetActive(false);
		cardToolTip.SetActive(false);
		CollectableAlbum currentAlbum = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);
		packPresentation.starMeter.init(currentAlbum);
		packPresentation.onCardsReady += cardsReady;
		if (MainLobby.hirV3 != null && LobbyCarouselV3.instance != null)
		{
			MainLobby.hirV3.pageController.setScrollerActive(false);
			LobbyCarouselV3.instance.setCarousalScrollActive(false);
		}
		packPresentation.gameObject.SetActive(true);

		if (Collectables.cachedPackJSON != null)
		{
			onGetPackData(Collectables.cachedPackJSON);
			string eventId = Collectables.cachedPackJSON.getString("event", "");
			packInfoKlassStat = Collectables.cachedPackJSON.getString("pack", "");

			string[] cardIds = Collectables.cachedPackJSON.getStringArray("card_ids");
			if (cardIds != null)
			{
				cardsInPack = cardIds.Length;
			}
			if (!eventId.IsNullOrWhiteSpace())
			{
				CollectablesAction.cardPackSeen(eventId);
			}
		}
		else
		{
#if UNITY_EDITOR
			JSON cachedString = new JSON(@"{
	""type"": ""collectible_pack_dropped"",
	""event"": ""Qu9Dv9QbdMqmLpBtUIpOTBSwfvw0XUP0e2Mr1pzktFjWy"",
	""creation_time"": ""1526916048"",
	""album_id"": ""1"",
	""pack_id"": ""1"",
	""card_ids"": [""1"", ""3"", ""4"", ""5""]
	}");
			onGetPackData(cachedString);
#else
			Dialog.close(this); //Dialog will not function if there is no cached json, better just to abort
#endif
			Debug.LogError("Cached json was missing");
		}
	}

	private void cardsReady(Dict data = null)
	{
		collectButton.gameObject.SetActive(true);
		collectButton.registerEventDelegate(onClickCollect);
		packButton.registerEventDelegate(onClickCollect);
	}

	protected override void onFadeInComplete()
	{
		packPresentation.setupForMOTD();
		base.onFadeInComplete();
	}

	// Callback for when we need to get our "starter pack" of cards.
	public void onGetPackData(JSON data)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "ftue_welcome_step_1",
			genus: "view");
		packPresentation.preparePackSequence(data);
		StartCoroutine(playIntroAnimations());
	}

	private IEnumerator playIntroAnimations()
	{
		Audio.play("CardsPresentedCollections");
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(packPresentation.packAnimator, PACK_INTRO_ANIM_NAME, PACK_INTRO_DELAY));
		packPresentation.bottomContentAnimator.gameObject.SetActive(true);
		packPresentation.bottomContentAnimator.Play(BOTTOM_CONTENT_ANIM_INTRO_NAME);
	}

	private void onClickCollect(Dict args = null)
	{
		// So we don't start insntiating things left and right by accident
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "ftue_welcome_step_1",
			family:"continue",
			genus: "click");
		Audio.play("LobbyButtonCollectCollections");
		Audio.play("ButtonViewCardsCollections");
		collectButton.clearAllDelegates();
		packButton.clearAllDelegates();
		packPresentation.onCardAnimationsFinished += onFinishAnimation;
		packPresentation.openAndRevealPack(CollectAPack.NORMAL_PACK_COLLECT_ANIM);
		packPresentation.bottomContentAnimator.gameObject.SetActive(false);
	}

	private void onFinishAnimation(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "ftue_welcome_step_2",
			klass: packInfoKlassStat,
			genus: "view",
			val: cardsInPack);
		
		Audio.play("Alert1FTUE");
		cardToolTip.SetActive(true);
		collectButton.registerEventDelegate(closeAndStartFtue);
		collectButton.text = Localize.textOr(VIEW_CARDS_BUTTON_LOC, "View my cards");
		packPresentation.bottomContentAnimator.gameObject.SetActive(true);
		packPresentation.bottomContentAnimator.Play(BOTTOM_CONTENT_ANIM_INTRO_NAME);
	}

	private void closeAndStartFtue(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "ftue_welcome_step_2",
			klass: packInfoKlassStat,
			family: "continue",
			genus: "click");
		if (MainLobby.hirV3 != null && LobbyCarouselV3.instance != null)
		{
			MainLobby.hirV3.pageController.setScrollerActive(true);
			LobbyCarouselV3.instance.setCarousalScrollActive(true);
		}
		Audio.play("ButtonViewCardsCollections");
		Dialog.close();
		// even if we were doing more albums at once, we'd probabaly just show the first here.
	}

	// Do NOT call -- called by Dialog.close()
	public override void close()
	{
		string source = (string)dialogArgs.getWithDefault(D.KEY, "pack");
		bool hasSeenFtue = (bool) dialogArgs.getWithDefault(D.OPTION, false);
		CollectableAlbumDialog.showDialog(Collectables.currentAlbum, source, !hasSeenFtue, isTopOfList:true);
	}

	public static bool showDialog(string source, JSON data = null)
	{
		Dict args = Dict.create(D.DATA, data, D.KEY, source, D.OPTION, Collectables.hasSeenFtue());
		Scheduler.addDialog("collectables_motd", args);
		Collectables.markFtueSeen();
		return true;
	}
}
