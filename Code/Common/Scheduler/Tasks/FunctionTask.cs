using UnityEngine;
using System.Collections;

namespace Com.Scheduler
{
	public class FunctionTask : SchedulerTask
	{
		// =============================
		// INTERNAL
		// =============================
		internal SchedulerDelegate callback;

		public FunctionTask(SchedulerDelegate callback, Dict args = null) : base(args)
		{
			this.callback = callback;
		}

		/// <inheritdoc/>
		public override void execute()
		{
			base.execute();

			if (callback != null)
			{
				callback(args);
			}

			Scheduler.removeTask(this);
		}

		/// <inheritdoc/>
		public override bool contains<T>(T value)
		{
			SchedulerDelegate f = value as SchedulerDelegate;
			return Equals(f, callback);
		}

		/*=========================================================================================
		ANCILLARY
		=========================================================================================*/
		public override string ToString()
		{
			int rating = priority != null ? priority.rating : (int)SchedulerPriority.PriorityType.LOW;
			return string.Format("FunctionTask: {0} | priority {1}", callback.Method.Name, rating.ToString());
		}
	}
}