using UnityEngine;
using System.Collections;

namespace Com.Scheduler
{
	public class WeeklyRaceAlertTask : FunctionTask
	{
		private bool hasTimerCompleted = false;
		private SmartTimer delayTaskTimer;

		public WeeklyRaceAlertTask(SchedulerDelegate callback, Dict args = null, int delay = 0) : base(callback, args)
		{
			if (delay > 0)
			{
				delayTaskTimer = new SmartTimer(delay, false, onTimerComplete, "weekly_race_alert_task_delay");
				delayTaskTimer.start();
			}
			else
			{
				hasTimerCompleted = true;
			}
		}

		private void onTimerComplete()
		{
			hasTimerCompleted = true;
			Scheduler.run();
		}

		/// <inheritdoc/>
		public override bool canExecute
		{
			get { return base.canExecute && hasTimerCompleted; }
		}
	}
}