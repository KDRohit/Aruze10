using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Com.Scheduler
{
	public class DialogPackage : SchedulerPackage
	{
		/// <inheritdoc/>
		internal override void onTaskScheduled(SchedulerTask task)
		{
			DialogTask dialogTask = findTaskWith((task as DialogTask).dialogKey) as DialogTask;
			if (dialogTask != null)
			{
				updateTask(dialogTask, task);
				base.onTaskScheduled(task);
			}
		}
	}
}