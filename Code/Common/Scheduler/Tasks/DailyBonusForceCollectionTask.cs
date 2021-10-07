using UnityEngine;
using System.Collections;

namespace Com.Scheduler
{
	public class DailyBonusForceCollectionTask : SchedulerTask
	{
		// =============================
		// PRIVATE
		// =============================
		private bool hasTimerOutcome = false;
		private SmartTimer timeout;

		// =============================
		// CONST
		// =============================
		private const int TIMEOUT_DELAY = 10;

		public DailyBonusForceCollectionTask()
		{
			Decs.registerEvent("timer_outcome", onTimerOutcome);
			timeout = SmartTimer.create(TIMEOUT_DELAY, false, onTimeout, "daily_bonus_force_collection_task_timeout");
			timeout.start();
		}

		/// <summary>
		/// Callback from the smart timer expiration
		/// </summary>
		private void onTimeout()
		{
			Scheduler.removeTask(this);
		}

		/// <inheritdoc/>
		public override void execute()
		{
			base.execute();
			timeout.destroy();
		}

		/// <summary>
		/// Callback when timer_outcome event happens, see Server.processTimerOutcome()
		/// </summary>
		private void onTimerOutcome(Dict args = null)
		{
			Scheduler.removeTask(this);
			timeout.destroy();
			hasTimerOutcome = true;
		}

		/// <inheritdoc/>
		public override bool canExecute
		{
			get { return base.canExecute && hasTimerOutcome; }
		}
	}
}