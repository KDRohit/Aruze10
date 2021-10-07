using UnityEngine;

namespace Com.Scheduler
{
	public class FunctionPackage : SchedulerPackage
	{
		/// <inheritdoc/>
		internal override void onTaskScheduled(SchedulerTask task)
		{
			FunctionTask functionTask = findTaskWith((task as FunctionTask).callback) as FunctionTask;
			if (functionTask != null)
			{
				updateTask(functionTask, task);
				base.onTaskScheduled(task);
			}
		}
	}
}