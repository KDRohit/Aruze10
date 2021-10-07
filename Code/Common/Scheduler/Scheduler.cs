using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Com.Scheduler
{
	// =============================
	// PUBLIC
	// =============================
	public delegate void SchedulerDelegate(Dict args);

	public class Scheduler : IResetGame
	{
		// =============================
		// PRIVATE
		// =============================
		private static SchedulerTask currentTask;

		// =============================
		// PUBLIC
		// =============================
		public static readonly List<SchedulerTask> tasks = new List<SchedulerTask>();

		/*=========================================================================================
		ADD/REMOVAL FUNCTIONS
		=========================================================================================*/
		public static DialogTask addDialog
		(
			string dialogKey,
			Dict args = null,
			SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW,
			SchedulerPackage package = null
		)
		{
			return addItem<string, DialogTask>(dialogKey, args, priority, package);
		}

		public static DialogTask addDialog
		(
			DialogType type,
			Dict args = null,
			SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW,
			SchedulerPackage package = null
		)
		{
			return addItem<string, DialogTask>(type.keyName, args, priority, package);
		}

		public static FunctionTask addFunction
		(
			SchedulerDelegate f,
			Dict args = null,
			SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW,
			SchedulerPackage package = null
		)
		{
			return addItem<SchedulerDelegate, FunctionTask>(f, args, priority, package);
		}

		public static PackageTask addPackage
		(
			SchedulerPackage package,
			SchedulerPriority.PriorityType priority = SchedulerPriority.PriorityType.LOW,
			SchedulerPackage parentPackage = null
		)
		{
			return addItem<SchedulerPackage, PackageTask>(package, null, priority, parentPackage);
		}

		/// <summary>
		/// Creates a task based on the item type added. Examples can be:
		/// a string, which is used for a DialogTask.
		/// or a function, which is used for a FunctionTask
		/// </summary>
		/// <param name="value"></param>
		/// <param name="args"></param>
		/// <param name="rating"></param>
		/// <param name="package">optionally pass a package to add the item to when the task is created</param>
		public static U addItem<T,U>
		(
			T value,
			Dict args = null,
			SchedulerPriority.PriorityType rating = SchedulerPriority.PriorityType.LOW,
			SchedulerPackage package = null
		) where U : SchedulerTask
		{
			SchedulerTask task = null;

			try
			{
				task = (SchedulerTask)Activator.CreateInstance(typeof(U), value, args);
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("Scheduler: Attempted to create task in addItem failed {0}", e.Message);
			}

			if (task != null)
			{
				task.priority.addToRating(rating);

				if (package != null)
				{
					addToPackage(task, package);
				}
				else
				{
					addTask(task);
				}
			}

			return (U)task;
		}

		public static void removeDialog(string dialogKey)
		{
			removeTask(findTaskWith(dialogKey));
		}

		public static void removeDialog(DialogType type)
		{
			removeTask(findTaskWith(type.keyName));
		}

		public static void removePackage(SchedulerPackage package)
		{
			removeTask(findTaskWith(package));
		}

		public static void removeFunction(SchedulerDelegate f)
		{
			removeTask(findTaskWith(f));
		}

		/// <summary>
		/// Adds the task to the specified package
		/// </summary>
		/// <param name="t"></param>
		/// <param name="package"></param>
		private static void addToPackage(SchedulerTask t, SchedulerPackage package)
		{
			package.onTaskScheduled(t);
			run();
		}

		/// <summary>
		/// Adds the task the task list
		/// </summary>
		/// <param name="t"></param>
		/// <param name="rating"></param>
		public static void addTask(SchedulerTask t, SchedulerPriority.PriorityType rating = SchedulerPriority.PriorityType.LOW)
		{
			if (!tasks.Contains(t))
			{
				t.priority.addToRating(rating);
				tasks.Add(t);
			}

			run();
		}

		public static void removeTask(SchedulerTask t)
		{
			if (tasks.Contains(t))
			{
				t.removedFromScheduler = true;
				tasks.Remove(t);
			}
			run();
		}

		public static void removeTasksOfType<T>()
		{
			tasks.RemoveAll(x => x is T);
			run();
		}

		/*=========================================================================================
		TASK EXECUTION
		=========================================================================================*/
		/// <summary>
		/// Core loop method
		/// </summary>
		public static void run()
		{
			tasks.Sort(sortByPriority);
			currentTask = getNextTask();

			if (currentTask != null && canRunScheduler)
			{
				runTask(currentTask);
			}
		}

		public static SchedulerTask getNextTask()
		{
			for (int i = 0; i < tasks.Count; ++i)
			{
				if (tasks[i].canExecute)
				{
					return tasks[i];
				}
				if (tasks[i].priority.rating >= (int)SchedulerPriority.PriorityType.BLOCKING)
				{
					break;
				}
			}

			return null;
		}

		public static SchedulerTask getNextBlockingTask()
		{
			for (int i = 0; i < tasks.Count; ++i)
			{
				if (tasks[i] == null)
				{
					continue;
				}
				if (tasks[i].priority.rating >= (int)SchedulerPriority.PriorityType.BLOCKING)
				{
					return tasks[i];
				}
			}

			return null;
		}

		public static string getCurrentTaskInfo()
		{
			if (currentTask == null)
			{
				return null;
			}

			return currentTask.ToString();
		}

#if UNITY_EDITOR
		public static void removeCurrentTask()
		{
			if (currentTask != null)
			{
				tasks.Remove(currentTask);
				currentTask = null;
				run();
			}
			
		}
#endif

		private static void runTask(SchedulerTask t)
		{
			t.execute();
		}

		/*=========================================================================================
		ANCILLARY
		=========================================================================================*/
		internal static SchedulerTask findTaskWith<T>(T value)
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
		
		internal static List<SchedulerTask> findAllTasksWith<T>(T value)
		{
			List<SchedulerTask> foundTasks = new List<SchedulerTask>();
			for (int i = 0; i < tasks.Count; ++i)
			{
				if (tasks[i].contains(value))
				{
					foundTasks.Add(tasks[i]);
				}
			}
			
			return foundTasks;
		}

		internal static bool hasTaskWith<T>(T value)
		{
			return findTaskWith(value) != null;
		}

		internal static int sortByPriority(SchedulerTask a, SchedulerTask b)
		{
			int output = b.priority.rating - a.priority.rating;

			if (output == 0)
			{
				return a.priority.time - b.priority.time;
			}

			return output;
		}

		/// <summary>
		/// removeDuplicatesOf tasks a Scheduler task, and the value that is being used in its execution.
		/// it will then compare the current task list for any new tasks that are duplicates. this includes comparing
		/// their arguments. if the arguments differ, the task is considered unique, and will not be removed
		/// </summary>
		/// <param name="task">SchedulerTask that is being considered</param>
		/// <param name="value">Value used in execution. E.g. a dialog key, or SchedulerDelegate</param>
		/// <typeparam name="T">Type used in execution path, e.g. a string or schedulerdelegate</typeparam>
		internal static void removeDuplicatesOf<T>(SchedulerTask task, T value)
		{
			List<SchedulerTask> removals = new List<SchedulerTask>();

			int i;
			for (i = 0; i < tasks.Count; ++i)
			{
				if (tasks[i] == task)
				{
					continue;
				}

				if (tasks[i].contains(value))
				{
					if (task.args == tasks[i].args || task.args != null && task.args.isEqualTo(tasks[i].args))
					{
						removals.Add(tasks[i]);
					}
				}
			}

			for (i = 0; i < removals.Count; ++i)
			{
				removeTask(removals[i]);
			}
		}

		/// <summary>
		/// Returns true if the Scheduler contains the specified task type
		/// </summary>
		/// <param name="task"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static bool hasTaskOfType<T>() where T : SchedulerTask
		{
			for (int i = 0; i < tasks.Count; ++i)
			{
				if (tasks[i] is T)
				{
					return true;
				}
			}

			return false;
		}

		public static void dump()
		{
			tasks.Clear();
			currentTask = null;
		}

		/*=========================================================================================
		GETTERS/SETTERS
		=========================================================================================*/
		private static bool canRunScheduler
		{
			get { return Glb.isNothingHappening || currentTask.priority.rating >= (int)SchedulerPriority.PriorityType.IMMEDIATE; }
		}

		public static bool hasTask
		{
			get { return tasks != null && tasks.Count > 0; }
		}

		public static bool hasTaskCanExecute
		{
			get
			{
				SchedulerTask task = getNextTask();
				return task != null && task.canExecute;
			}
		}

		public static bool hasBlockingTask
		{
			get
			{
				for (int i = 0; i < tasks.Count; ++i)
				{
					if (tasks[i].priority.rating >= (int)SchedulerPriority.PriorityType.BLOCKING)
					{
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// IResetGame call
		/// </summary>
		public static void resetStaticClassData()
		{
			tasks.Clear();
			currentTask = null;
		}
	}
}