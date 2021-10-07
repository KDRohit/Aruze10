using UnityEngine;
using System.Collections;

namespace Com.Scheduler
{
	public class PrizePopStartBonusTask : SchedulerTask
	{
		// =============================
		// PRIVATE
		// =============================
		private bool hasBundle = false;
		private bool hasServerResponse = false;
		private bool timedOut = false;
		private SmartTimer timeout;

		// =============================
		// CONST
		// =============================
		private const int TIMEOUT_DELAY_DEFAULT = 10;

		public PrizePopStartBonusTask()
		{
			Decs.registerEvent("prize_pop_bonus_game_in_progress", onBonusStart);
			int timeoutLength = Data.liveData != null ? Data.liveData.getInt("PRIZE_POP_BONUS_WAIT_TIMEOUT", TIMEOUT_DELAY_DEFAULT) : TIMEOUT_DELAY_DEFAULT;
			timeout = SmartTimer.create(timeoutLength, false, onTimeout, "prize_pop_start_bonus_task_timeout");
			timeout.start();
			RoutineRunner.instance.StartCoroutine(waitForDialogToBeQueued());
		}

		/// <summary>
		/// Callback from the smart timer expiration
		/// </summary>
		private void onTimeout()
		{
			timedOut = true;
		}

		/// <inheritdoc/>
		public override void execute()
		{
			base.execute();
			string backgroundTexturePath = string.Format(PrizePopDialog.BG_TEXTURE_PATH, ExperimentWrapper.PrizePop.theme);
			AssetBundleManager.load(backgroundTexturePath, successCallback:onBundleLoadSuccess, failCallback:onBundleLoadFailed);
		}

		private void onBundleLoadSuccess(string path, Object obj, Dict args)
		{
			hasBundle = true;
			if (hasServerResponse)
			{
				timeout.destroy();
			}
		}
		
		private void onBundleLoadFailed(string path, Dict args)
		{
		}
		
		private void onBonusStart(Dict args = null)
		{
			hasServerResponse = true;
			if (hasBundle)
			{
				timeout.destroy();
			}
		}

		private IEnumerator waitForDialogToBeQueued()
		{
			while (!Scheduler.hasTaskWith("prize_pop") && !timedOut)
			{
				yield return null;
			}
			
			Scheduler.removeTask(this);
		}

		public override bool contains<T>(T value)
		{
			return value is PrizePopStartBonusTask;
		}
	}
}