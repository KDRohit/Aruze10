using Com.Rewardables;
using UnityEngine;

public class PrizePopDialogOverlay : TICoroutineMonoBehaviour
{
   [SerializeField] protected ClickHandler closeButton;
   [SerializeField] protected ClickHandler ctaButton;
   [SerializeField] private AnimationListController.AnimationInformationList introAnimationList;

   protected DialogBase parentDialog;
   protected string overlayType = "";

   private const string CARD_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - Award Card Pack";
   private const string COIN_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - Award Coins";
   private const string JACKPOT_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - Award Jackpot";
   private const string EXTRA_PICKS_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - Award Extra Chances";
   private const string FINAL_JACKPOT_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - Event Complete";
   private const string BUY_EXTRA_CHANCES_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - Buy Extra Chances";
   private const string HOW_TO_PLAY_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - How to Play";
   private const string KEEP_SPINNING_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - Keep Spinning";
   private const string OUT_OF_PICKS_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - Out of Picks";
   private const string EVENT_ENDED_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - Event Ended";
   private const string NEW_STAGE_OVERLAY_PREFAB_PATH = "Features/Prize Pop/Prefabs/Overlay Dialogs/Prize Pop Overlay Dialog - New Stage";

   protected const string COLLECT_CLICKED_AUDIO_KEY = "CollectPrizePopCommon";
   public virtual void init(Rewardable reward, DialogBase parent, Dict overlayArgs)
   {
      parentDialog = parent;
      closeButton.registerEventDelegate(closeClicked, overlayArgs);
      ctaButton.registerEventDelegate(ctaClicked, overlayArgs);
      overlayType = (string)overlayArgs.getWithDefault(D.KEY, "");
      StartCoroutine(AnimationListController.playListOfAnimationInformation(introAnimationList));
      StatsPrizePop.logOverlayView(overlayType); 
   }

   protected virtual void closeClicked(Dict args = null)
   {
      StatsPrizePop.logOverlayClose(overlayType);
      Dialog.close();
   }
   
   protected virtual void ctaClicked(Dict args = null)
   {
      StatsPrizePop.logOverlayClose(overlayType);
      Destroy(gameObject);
   }

#region Static Functions
   public static void loadCoinRewardOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args)
   {
      args.Add(D.KEY, "coin_reward");
      loadOverlay(caller, COIN_OVERLAY_PREFAB_PATH, successCallback, failCallback, args);
   }
   
   public static void loadCardRewardOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args)
   {
      args.Add(D.KEY, "card_pack");
      loadOverlay(caller, CARD_OVERLAY_PREFAB_PATH, successCallback, failCallback, args);
   }
   
   public static void loadJackpotRewardOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args)
   {
      args.Add(D.KEY, "jackpot");
      loadOverlay(caller, JACKPOT_OVERLAY_PREFAB_PATH, successCallback, failCallback, args);
   }
   
   public static void loadFinalJackpotRewardOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args)
   {
      args.Add(D.KEY, "all_jackpots");
      loadOverlay(caller, FINAL_JACKPOT_OVERLAY_PREFAB_PATH, successCallback, failCallback, args);
   }
   
   public static void loadExtraPicksRewardOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args)
   {
      args.Add(D.KEY, "extra_chance");
      loadOverlay(caller, EXTRA_PICKS_OVERLAY_PREFAB_PATH, successCallback, failCallback, args);
   }
   
   public static void loadBuyExtraPicksRewardOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args)
   {
      loadOverlay(caller, BUY_EXTRA_CHANCES_OVERLAY_PREFAB_PATH, successCallback, failCallback, args);
   }
   
   public static void loadHowToPlayOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
   {
      loadOverlay(caller, HOW_TO_PLAY_OVERLAY_PREFAB_PATH, successCallback, failCallback, Dict.create(D.KEY, "ftue_how_to_play"));
   }
   
   public static void loadKeepSpinningOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
   {
      loadOverlay(caller, KEEP_SPINNING_OVERLAY_PREFAB_PATH, successCallback, failCallback, Dict.create(D.KEY, "ftue_keep_spinning"));
   }
   
   public static void loadOutOfPicksOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
   {
      loadOverlay(caller, OUT_OF_PICKS_OVERLAY_PREFAB_PATH, successCallback, failCallback, Dict.create(D.KEY, "out_of_pops"));
   }
   
   public static void loadEventEndedOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
   {
      loadOverlay(caller, EVENT_ENDED_OVERLAY_PREFAB_PATH, successCallback, failCallback, Dict.create(D.KEY, "event_ended"));
   }
   
   public static void loadNewStageOverlay(object caller, AssetLoadDelegate successCallback, AssetFailDelegate failCallback)
   {
      loadOverlay(caller, NEW_STAGE_OVERLAY_PREFAB_PATH, successCallback, failCallback, Dict.create(D.KEY, "new_stage"));
   }

   private static void loadOverlay(object caller, string path, AssetLoadDelegate successCallback, AssetFailDelegate failCallback, Dict args = null)
   {
      AssetBundleManager.load(caller, path, successCallback, failCallback, args, false, true, ".prefab");
   }
   
   #endregion
}
