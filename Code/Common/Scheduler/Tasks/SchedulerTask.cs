using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Com.Scheduler
{
	public class SchedulerTask
	{
		// =============================
		// INTERNAL
		// =============================
		internal Dict args = null;
		internal bool removedFromScheduler = false;

		// =============================
		// PROTECTED
		// =============================
		protected bool isDone = false;

		// =============================
		// PUBLIC
		// =============================
		public SchedulerPriority priority;

		public string description;

		public SchedulerTask(Dict args = null)
		{
			this.args = args;
			if (args != null)
			{
				description = args.getWithDefault(D.TITLE, "") as string;	
			}
			
			priority = new SchedulerPriority();
		}

		/// <summary>
		/// Function ran from Scheduler when a task is completed, or after a SchedulerPackage calls Scheduler.runPackage()
		/// </summary>
		public virtual void execute()
		{
			isDone = true;
		}

		/// <summary>
		/// The contains method should be overwritten by subclasses. Each task can check to see if it contains
		/// a value passed in. E.g. for a DialogTask contains will check to see if a dialog key exists
		/// </summary>
		/// <returns></returns>
		public virtual bool contains<T>(T value)
		{
			Debug.LogWarning("SchedulerTask.contains() should be overwritten by subclass");
			return false;
		}

		/*=========================================================================================
		ANCILLARY
		=========================================================================================*/
		public override string ToString()
		{
			int rating = priority != null ? priority.rating : (int)SchedulerPriority.PriorityType.LOW;
			return string.Format("SchedulerTask: {0} | priority {1} | {2}", "Base", rating.ToString(), description ?? "N/A");
		}

		/*=========================================================================================
		GETTERS/SETTERS
		=========================================================================================*/
		/// <summary>
		/// Base implementation does not let any scheduler tasks run if the task is already completed,
		/// or a dialog is in the process of opening or closing
		/// </summary>
		public virtual bool canExecute
		{
			get
			{
				return !isDone && !Dialog.isTransitioning;
			}
		}
	}
}