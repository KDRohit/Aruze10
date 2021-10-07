using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class InboxTask : SchedulerTask
{
	private bool hasInboxData;
	private SmartTimer timeout;

	public InboxTask(Dict args = null) : base(args)
	{
		Server.registerEventDelegate("inbox_items_event", onInboxUpdate, true);
		timeout = new SmartTimer(10, false, onTimeExpired, "inbox_task_timeout");
	}

	private void onTimeExpired()
	{
		RoutineRunner.instance.StartCoroutine(waitBeforeShowingInbox());
	}

	public void onInboxUpdate(JSON data)
	{
		hasInboxData = true;
		RoutineRunner.instance.StartCoroutine(waitBeforeShowingInbox());
		Server.unregisterEventDelegate("inbox_items_event", onInboxUpdate, true);
	}

	/// <inheritdoc/>
	public override void execute()
	{
		InboxAction.getInboxItems();
		timeout.start();
	}

	public IEnumerator waitBeforeShowingInbox()
	{
		Server.unregisterEventDelegate("inbox_items_event", onInboxUpdate, true);

		timeout.stop();

		// wait for all registered event delegates to complete
		yield return null;

		SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW;

		string defaultTab = "";
		if (args != null)
		{
			defaultTab = (string) args.getWithDefault(D.KEY, defaultTab);
			priorityType = (SchedulerPriority.PriorityType)args.getWithDefault(D.DATA, priorityType);
		}

		base.execute();

		Scheduler.removeTask(this);

		InboxDialog.showDialog(defaultTab, p:priorityType);
	}
}