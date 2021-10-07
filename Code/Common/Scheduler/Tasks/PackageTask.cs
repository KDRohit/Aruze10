using UnityEngine;
using System.Collections;
using System.Text;

namespace Com.Scheduler
{
	public class PackageTask : SchedulerTask
	{
		// =============================
		// INTERNAL
		// =============================
		internal SchedulerPackage package;

		public PackageTask(SchedulerPackage p, Dict args = null) : base(args)
		{
			package = p;
		}

		/// <inheritdoc/>
		public override void execute()
		{
			base.execute();

			package.completedTasks.Sort(Scheduler.sortByPriority);

			for (int i = 0; i < package.completedTasks.Count; ++i)
			{
				Scheduler.addTask(package.completedTasks[i]);
			}

			Scheduler.removeTask(this);
		}

		/// <inheritdoc/>
		public override bool contains<T>(T value)
		{
			SchedulerPackage p = value as SchedulerPackage;
			if (Equals(p, package))
			{
				return true;
			}

			var task = value as SchedulerTask;
			return task != null && package.contains(task);
		}

		/*=========================================================================================
		ANCILLARY
		=========================================================================================*/
		public override string ToString()
		{
			int rating = priority != null ? priority.rating : (int)SchedulerPriority.PriorityType.LOW;
			StringBuilder stringBuilder = new StringBuilder();

			stringBuilder.Append(string.Format("PackageTask: priority {0} | ", rating.ToString()));

			for (int i = 0; i < package.allTasks.Count; ++i)
			{
				stringBuilder.AppendLine(package.allTasks[i].ToString());
			}

			return stringBuilder.ToString();
		}

		/*=========================================================================================
		GETTERS/SETTERS
		=========================================================================================*/
		/// <inheritdoc/>
		public override bool canExecute
		{
			get
			{
				return package.isReadyToRun && !isDone && !Dialog.isTransitioning;
			}
		}
	}
}