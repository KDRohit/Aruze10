using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Com.Scheduler
{
	public class SchedulerPackage
	{
		// =============================
		// PROTECTED
		// =============================
		protected List<SchedulerTask> tasks;
		protected List<SchedulerTask> readyTasks;
		protected SmartTimer timeout;

		// =============================
		// CONST
		// =============================
		protected const int TIMEOUT_DELAY = 20;

		public SchedulerPackage()
		{
			tasks = new List<SchedulerTask>();
			readyTasks = new List<SchedulerTask>();
			timeout = new SmartTimer(TIMEOUT_DELAY, false, onTimeout, "scheduler_package_timeout");
			timeout.start();
		}

		public virtual void addTask(SchedulerTask task, SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW)
		{
			tasks.Add(task);
			task.priority.addToRating(priorityType);
		}

		public virtual void removeTask(string key)
		{
			SchedulerTask task = findTaskWith(key);
			if (task != null)
			{
				if (tasks.Contains(task))
				{
					tasks.Remove(task);
				}

				if (readyTasks.Contains(task))
				{
					readyTasks.Remove(task);
				}
				
				if (isReadyToRun)
				{
					onTaskComplete();
				}
			}
		}

		/// <summary>
		/// Called form scheduler when a task is ready to run
		/// </summary>
		/// <param name="task"></param>
		internal virtual void onTaskScheduled(SchedulerTask task)
		{
			if (contains(task))
			{
				tasks.Remove(task);
				readyTasks.Add(task);

				timeout.reset();

				if (isReadyToRun)
				{
					onTaskComplete();
				}
			}
		}

		/// <summary>
		/// Task potentially stalled, this is the callback from the smart timer. Once this happens
		/// we remove this package from the Scheduler, and call all the complete tasks
		/// </summary>
		internal virtual void onTimeout()
		{
			timeout.stop();

			PackageTask task = Scheduler.findTaskWith(this) as PackageTask;

			Scheduler.removePackage(this);

			if (task != null)
			{
				task.execute();
			}
		}

		/// <summary>
		/// Called from onTaskScheduled when all tasks have been completed
		/// </summary>
		internal virtual void onTaskComplete()
		{
			timeout.stop();
			Scheduler.run();
		}

		/// <summary>
		/// Returns true if the tasks lists contains the specified SchedulerTask
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		internal virtual bool contains(SchedulerTask t)
		{
			return tasks.Contains(t);
		}

		/// <summary>
		/// Returns the SchedulerTask if it contains the specified value
		/// </summary>
		/// <param name="dialogKey"></param>
		/// <returns></returns>
		internal virtual SchedulerTask findTaskWith<T>(T value)
		{
			for (int i = 0; i < tasks.Count; ++i)
			{
				if (tasks[i].contains(value))
				{
					return tasks[i];
				}
			}

			return null;
		}

		/// <summary>
		/// Assigns a task at the specified index
		/// </summary>
		/// <param name="index"></param>
		/// <param name="t"></param>
		internal virtual void updateTask(SchedulerTask oldTask, SchedulerTask newTask)
		{
			if (tasks.IndexOf(oldTask) >= 0)
			{
				tasks[tasks.IndexOf(oldTask)] = newTask;
			}
		}

		/*=========================================================================================
		GETTERS/SETTERS
		=========================================================================================*/
		public virtual bool isReadyToRun
		{
			get { return tasks != null && readyTasks != null && readyTasks.Count > 0 && tasks.Count == 0; }
		}

		public List<SchedulerTask> completedTasks
		{
			get { return readyTasks; }
		}

		private List<SchedulerTask> _allTasks = null;
		public List<SchedulerTask> allTasks
		{
			get
			{
				if (_allTasks == null)
				{
					_allTasks = new List<SchedulerTask>();
					_allTasks.AddRange(tasks);
					_allTasks.AddRange(completedTasks);
				}

				return _allTasks;
			}
		}
	}
}