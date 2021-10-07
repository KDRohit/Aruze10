using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class NetworkProfileReportSentDialog : DialogBase
{
	public ImageButtonHandler okayButton;
	public Animator animator;
	
	public override void init()
	{
		okayButton.registerEventDelegate(okayClicked);
	}

	public override void close()
	{
		// Do cleanup
	}

	private void okayClicked(Dict args = null)
	{
		Dialog.close();
	}
	
	public static void showDialog(SchedulerPriority.PriorityType priorityType = SchedulerPriority.PriorityType.LOW)
	{
		Scheduler.addDialog("network_profile_report_send", Dict.create(D.PRIORITY, priorityType), priorityType);
	}
}