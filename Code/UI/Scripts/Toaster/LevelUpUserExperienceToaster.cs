using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Com.Scheduler;
using TMPro;
using UnityEngine.SocialPlatforms;

public class LevelUpUserExperienceToaster : Toaster
{
	
	public enum PRESENTATION_TYPE
	{
		LEVEL_UP,
		INFO,
		GAME_UNLOCK
	}
	private const string ANIMATION_STATE_LEVEL_UP = "Intro - level up";
	private const string ANIMATION_INFO_STATE = "Intro - next level";
	private const string GAME_UNLOCK_STATE = "Intro - game unlock";
	
	// Level Up Content 
	public TextMeshPro coinsGranted;
	public TextMeshPro vipPointsGranted;
	public TextMeshPro newMaxBet;
	public VIPNewIconRevamp vipLevelSpriteParent;
	public UILabelStaticText staticUnlockText;
	public GameObject lockIcon;
	
	// Next Level Content
	public TextMeshPro percentToLevel;
	public TextMeshPro coinBonus;
	public TextMeshPro maxBet;
	
	//Game Unlocked Content
	public UITexture currentGameUnlock;
	public UITexture nextGameUnlock;
	public TextMeshPro currentGameInfo;
	public TextMeshPro nextGameInfo;
	public TextMeshPro levelLabel;
	public ButtonHandler playButton;
	
	
	// relevant info for the toaster
	private int levelUnlockLevel = 0;
	private ExperienceLevelData experienceLevel;
	private PRESENTATION_TYPE type;
	private GameObject overlayShroud;
	private SmartTimer timer;

	private const float DELAY_BEFORE_CLOSING = 3.0f;

    public override void init(ProtoToaster proto)
    {
	    toaster = proto;

		attatchToOverlay();

		if (proto.args.ContainsKey(D.DATA))
		{
			experienceLevel = proto.args[D.DATA] as ExperienceLevelData;
	    }

		float time = 4f;
		if (proto.args.ContainsKey(D.KEY))
		{
			type = (PRESENTATION_TYPE)proto.args[D.KEY];

			switch (type)
			{
				case PRESENTATION_TYPE.INFO:
					time = DELAY_BEFORE_CLOSING;
					setupInfoState();
					animator.Play(ANIMATION_INFO_STATE);
					break;
				case PRESENTATION_TYPE.LEVEL_UP:
					time = DELAY_BEFORE_CLOSING;
					setupLevelUpState();
					animator.Play(ANIMATION_STATE_LEVEL_UP);
					break;
				case PRESENTATION_TYPE.GAME_UNLOCK:
					if (proto.args.ContainsKey(D.OPTION1) && proto.args.ContainsKey(D.OPTION2))
					{
						LobbyGame currentUnlock = (LobbyGame)proto.args[D.OPTION1];
						LobbyGame nextUnlock = (LobbyGame)proto.args[D.OPTION2];
					     
						setupGameUnlockState(currentUnlock, nextUnlock);
						animator.Play(GAME_UNLOCK_STATE);
					}
					else
					{
						Debug.LogError("Missing unlock options");
					}
					break;
			}
		}

		timer = new SmartTimer(time, false, onTimeOut, "level_up_toaster_timer");
		timer.start();
     }

     private void setupLevelUpState()
     {
	     if (experienceLevel != null)
	     {
		     long creditsAwarded = experienceLevel.bonusAmt + experienceLevel.levelUpBonusAmount;
		     long vipPointsAdded = experienceLevel.bonusVIPPoints * experienceLevel.vipMultiplier;

		     coinsGranted.text = CreditsEconomy.convertCredits(creditsAwarded);
		     vipPointsGranted.text = string.Format("+{0}", CommonText.formatNumber(vipPointsAdded));
		     newMaxBet.text = CreditsEconomy.convertCredits(experienceLevel.maxBetIncrease);
		     
		     levelUnlockLevel = experienceLevel.level;
		     
		     StatsManager.Instance.LogCount("toaster", "level_up", experienceLevel.level.ToString(), "", "", "view", creditsAwarded);
		     
		     if (Overlay.instance != null)
		     {
			     Overlay.instance.top.xpUI.updateXP();
		     }
		
		     // If there is a "next unlock" carousel slide, see if it is still valid to show.
		     CarouselData slide = CarouselData.findActiveByAction("next_unlock");
		     if (slide != null && !slide.getIsValid())
		     {
			     slide.deactivate();
		     }
		
		     // We need to update the max bet. That is what updateMaxBet is doing.
		     // There's only one wager set in progressive games and mysterygift games in the new system, so it should be updated on level up for all games.
		     if (SpinPanel.instance != null)
		     {
			     SpinPanel.instance.updateMaxBet();
		     }
	     }

	     GameObject loadedVIPGemObject = VIPLevel.loadVIPGem(vipLevelSpriteParent.gameObject);
	     UISprite loadedVIPIcon = loadedVIPGemObject.GetComponent<UISprite>();

	     if (loadedVIPIcon != null)
	     {
		     loadedVIPIcon.spriteName = string.Format("VIP Icon {0}", SlotsPlayer.instance.vipNewLevel);
		     loadedVIPIcon.gameObject.SetActive(false);
		     loadedVIPIcon.gameObject.SetActive(true);
	     }

	     Audio.play("LevelUp2019UICoins");
     }

     private void setupInfoState()
     {
	     Audio.play("LevelUp2019UIDialogIn");
	     Audio.playWithDelay("LevelUp2019UIDialogIn", 2f);
	     ExperienceLevelData currentLevelData = ExperienceLevelData.find(SlotsPlayer.instance.socialMember.experienceLevel + 1);

		 if (currentLevelData != null && SlotsPlayer.instance != null)
		 {
			 percentToLevel.text = string.Format("{0}% to Level {1}", Mathf.RoundToInt(Common.getLevelProgress() * 100), SlotsPlayer.instance.socialMember.experienceLevel + 1);
			 coinBonus.text = CreditsEconomy.convertCredits(currentLevelData.bonusAmt);
			 maxBet.text = CommonText.formatNumber(currentLevelData.maxBetIncrease * CreditsEconomy.economyMultiplier);
		 }
     }
     
     private void setupGameUnlockState(LobbyGame currentGame, LobbyGame nextGame)
     {
	    Audio.play("LevelUp2019UnlockGame");
	    Dict args = Dict.create(D.GAME_KEY, currentGame);
	    playButton.registerEventDelegate(onCLickPlay, args);

		string imagePathCurrentGame = SlotResourceMap.getLobbyImagePath(currentGame.groupInfo.keyName, currentGame.keyName);
	
		StatsManager.Instance.LogCount("toaster", "game_unlock", currentGame.keyName, "", "", "view");
		
		RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(imagePathCurrentGame, onLoadCurrentGame, skipBundleMapping:true, pathExtension:".png"));
		
		// this can happen when we've unlocked the next game
		if (nextGame != null)
		{
			levelLabel.text = CommonText.formatNumber(nextGame.unlockLevel);
			string imagePathNextGame = SlotResourceMap.getLobbyImagePath(nextGame.groupInfo.keyName, nextGame.keyName);
			// Hide lock and number
			StatsManager.Instance.LogCount("toaster", "next_game_unlock", nextGame.keyName, "", "", "view");
			RoutineRunner.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(imagePathNextGame, onLoadNextGame, skipBundleMapping:true, pathExtension:".png"));
		}
		else
		{
			staticUnlockText.enabled = false;
			lockIcon.SetActive(false);
			levelLabel.gameObject.SetActive(false);
			StatsManager.Instance.LogCount("toaster", "next_game_unlock", "none", "", "", "view");
			loadComingSoonIcon();
			nextGameInfo.text = Localize.text("coming_soon");
		}
     }

     private void loadComingSoonIcon()
     {
     
	  Texture2D comingSoonTexture = SkuResources.loadSkuSpecificResourcePNG("textures/ComingSoonCard_baked");
	  nextGameUnlock.mainTexture = comingSoonTexture;
     }
     
     private void onCLickPlay(Dict args = null)
     {
	     // additional spins from triggering while we wait for the bonus
	     // to load.
	     if (SlotBaseGame.instance != null)
	     {
		     SlotBaseGame.instance.stopAutoSpin();
	     }

	     if (args.ContainsKey(D.GAME_KEY))
	     {
		     Scheduler.addTask(new TransferToGameTask(Dict.create(D.GAME_KEY, args[D.GAME_KEY], D.KEY, "level_up_toaster")));
		     
		     LobbyGame game = (LobbyGame) args[D.GAME_KEY];
		     if (game != null)
		     {
			     if (!game.isMaxVoltageGame && !game.isVIPGame)
			     {
				     Overlay.instance.removeJackpotOverlay();
			     }
			     StatsManager.Instance.LogCount("toaster", "game_unlock", game.keyName, SlotsPlayer.instance.socialMember.experienceLevel.ToString(), "play_now", "click");
		     }
	     }
     }
     
     private void onLoadCurrentGame(Texture2D tex, Dict data = null)
     {
	     currentGameUnlock.mainTexture = tex;
     }
     
     private void onLoadNextGame(Texture2D tex, Dict data = null)
     {
	     nextGameUnlock.mainTexture = tex;
     }
     
     private void attatchToOverlay()
     {
	     // We're going to base this attach location on the actual XP Bar
	     if (Overlay.instance.topV2.xpUI.levelUpFxContainer != null)
	     {
		     Overlay.instance.topV2.xpUI.levelUpFxContainer.gameObject.SetActive(true);
		     gameObject.transform.parent = Overlay.instance.topV2.xpUI.levelUpFxContainer.gameObject.transform;
		     CommonGameObject.setLayerRecursively(gameObject, Layers.ID_NGUI_OVERLAY);
		     Vector3 adjustedYPosition = Vector3.zero;
		     //adjustedYPosition.y += 100;
		     transform.localPosition = adjustedYPosition;
	     }
	     
	     // Now that we're attached, play whatever animations we need and hook up whatever we need
     }

     private void onTimeOut()
     {
	     if (type == PRESENTATION_TYPE.LEVEL_UP && Overlay.instance != null && Overlay.instance.topV2 != null && Overlay.instance.topV2.xpUI != null)
	     {
		     Overlay.instance.topV2.xpUI.checkEventStates();
		     checkUnlockDataAndShowToaster();
	     }
	     
	     ToasterManager.toasterClosed(this);
	     Destroy(gameObject);
	     Overlay.instance.topV2.xpUI.levelUpFxContainer.gameObject.SetActive(false);

	     if (toaster != null && toaster.onCompleteCallback != null)
	     {
		     toaster.onCompleteCallback();
	     }
     }

     private void checkUnlockDataAndShowToaster()
     {
	     List<string> unlocked = GameUnlockData.findUnlockedGamesForLevel(levelUnlockLevel);
		
	     if (unlocked == null)
	     {
		     // No games unlocked at this level.
		     return;
	     }

	     if (LevelUpUserExperienceFeature.instance.isEnabled && unlocked.Count > 0)
	     {
		     LobbyGame gameToUse = null;
		     for (int i = 0; i < unlocked.Count; i++)
		     {
			     gameToUse = LobbyGame.find(unlocked[i]);
			     if (gameToUse != null && gameToUse.isEnabledForLobby)
			     {
				     gameToUse.setIsUnlocked();
			     }
		     }

		     if (gameToUse != null)
		     {
			     Dict args = Dict.create(D.KEY, LevelUpUserExperienceToaster.PRESENTATION_TYPE.GAME_UNLOCK, D.OPTION1, gameToUse, D.OPTION2, LobbyGame.getNextUnlocked(levelUnlockLevel));
			     ToasterManager.addToaster(ToasterType.LEVEL_UP, args, null, 4f);
		     }
	     }
     }

     private void Update()
     {
	     if (type == PRESENTATION_TYPE.INFO)
	     {
		     if (TouchInput.isTouchDown && timer != null)
		     {
			     // Nuke the timer, delete this
			     timer.stop();
			     timer.destroy();
			     onTimeOut();
		     }
	     }
     }
}