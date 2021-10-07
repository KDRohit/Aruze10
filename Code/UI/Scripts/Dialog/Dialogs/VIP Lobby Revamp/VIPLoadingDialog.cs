using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class VIPLoadingDialog : DialogBase
{
	[SerializeField] private Animator animationController;
	private const string INTRO_ANIMATION = "intro";
	[SerializeField] private SkinnedMeshRenderer loadingBar;

	public static void showDialog(Dict args = null)
	{
		Scheduler.addDialog("vip_loading_dialog", args, SchedulerPriority.PriorityType.IMMEDIATE);
	}

	public override void init()
	{
		StatsManager.Instance.LogCount("dialog", "vip_room_jackpot_game", GameState.game.keyName, "", "", "view");
		Loading.showingCustomLoading = true;
		StartCoroutine(animateIntro());
	}

	public static void hideDialog()
	{
		Dialog.close();
	}

	public override void close()
	{

	}

	private IEnumerator animateIntro()
	{
		int loadPercent = 0;
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(animationController, INTRO_ANIMATION)); //Make sure we play the whole intro animation
		Overlay.instance.jackpotMystery.tokenBar.startBonusGame();
		while (BonusGameManager.instance == null) //If the intro finished but the game isn't loaded yet then keep waiting
		{
			loadPercent++;
			loadingBar.SetBlendShapeWeight(0, loadPercent);
			yield return null;
		}

		while (!BonusGameManager.instance.isBonusGameLoaded())
		{
			loadPercent++;
			loadingBar.SetBlendShapeWeight(0, loadPercent);
			yield return null;
		}
		loadingBar.SetBlendShapeWeight(0, 100);
		Loading.showingCustomLoading = false;
		hideDialog();
	}
}

