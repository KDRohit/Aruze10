using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class VIPBonusSummaryDialogKindle : VIPBonusSummaryDialog 
{
	new public static void showCustomDialog(Dict args)
	{
		Scheduler.addDialog(
			"vip_bonus_summary_dialog_kindle", 
			args,
			SchedulerPriority.PriorityType.IMMEDIATE
		);
	}

}
