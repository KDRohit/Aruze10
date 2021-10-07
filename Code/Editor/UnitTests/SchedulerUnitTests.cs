using Com.Scheduler;
using UnityEngine;
using NUnit.Framework;

public class SchedulerUnitTests
{
	private static string output = "";

	[Test]
	public static void basicTask()
	{
		output = "";
		Scheduler.addFunction(delegate(Dict args) { output = "success"; } );
		Assert.AreEqual(output, "success");
	}

	[Test]
	public static void priorityFunctionTask()
	{
		output = "";

		TestFunctionTask task = new TestFunctionTask("|test_func_low", null);
		task.priority.addToRating(SchedulerPriority.PriorityType.LOW);

		TestFunctionTask highPriority = new TestFunctionTask("test_func_immediate", null);
		highPriority.priority.addToRating(SchedulerPriority.PriorityType.IMMEDIATE);

		Scheduler.addTask(task);
		Scheduler.addTask(highPriority);

		task.overrideExecution = true;
		highPriority.overrideExecution = true;

		Scheduler.run();

		Assert.AreEqual(output, "test_func_immediate|test_func_low");
	}

	[Test]
	public static void conflictPriorityTask()
	{
		output = "";

		TestFunctionTask task = new TestFunctionTask("test_func_blocking", null);
		task.priority.addToRating(SchedulerPriority.PriorityType.BLOCKING);

		TestFunctionTask highPriority = new TestFunctionTask("|test_func_immediate", null);
		highPriority.priority.addToRating(SchedulerPriority.PriorityType.IMMEDIATE);

		Scheduler.addTask(task);
		Scheduler.addTask(highPriority);

		// turn the immediate priority on
		highPriority.overrideExecution = true;

		// force the scheduler to run
		Scheduler.run();

		// nothing should happen, we have a blocking task that hasn't executed
		Assert.AreEqual(output, "");

		// let the blocking task execute
		task.overrideExecution = true;

		// run the scheduler
		Scheduler.run();

		// the blocking task, and immediate task should run one after the other
		Assert.AreEqual(output, "test_func_blocking|test_func_immediate");
	}

	[Test]
	public static void packageTimeout()
	{
		output = "";

		SchedulerPackage package = new SchedulerPackage();
		TestFunctionTask t1 = new TestFunctionTask("package_task_1");
		TestFunctionTask t2 = new TestFunctionTask("package_task_2");
		TestFunctionTask t3 = new TestFunctionTask("package_task_3");
		package.addTask(t1);
		package.addTask(t2);
		package.addTask(t3);

		t1.overrideExecution = true;

		Scheduler.addPackage(package);
		Scheduler.run();

		package.onTaskScheduled(t1);
		package.onTimeout();

		Assert.AreEqual(output, "package_task_1");
	}

	[Test]
	public static void packageNestedPriority()
	{
		output = "";

		SchedulerPackage package = new SchedulerPackage();
		TestFunctionTask t1 = new TestFunctionTask("package_task_1");
		TestFunctionTask t2 = new TestFunctionTask("package_task_2");
		TestFunctionTask t3 = new TestFunctionTask("package_task_3");
		package.addTask(t1);
		package.addTask(t2);
		package.addTask(t3);

		t2.priority.addToRating(SchedulerPriority.PriorityType.IMMEDIATE);
		t1.priority.addToRating(SchedulerPriority.PriorityType.HIGH);

		t1.overrideExecution = true;
		t2.overrideExecution = true;
		t3.overrideExecution = true;

		Scheduler.addPackage(package);

		package.onTaskScheduled(t1);
		package.onTaskScheduled(t2);
		package.onTaskScheduled(t3);
		Scheduler.run();

		Assert.AreEqual(output, "package_task_2package_task_1package_task_3");
	}

	public class TestFunctionTask : FunctionTask
	{
		public bool overrideExecution = false;

		public TestFunctionTask(string s, Dict args = null) : base(delegate(Dict a) { output += s; }, args)
		{
		}

		/// <inheritdoc/>
		public override bool canExecute
		{
			get { return base.canExecute && overrideExecution; }
		}
	}
}