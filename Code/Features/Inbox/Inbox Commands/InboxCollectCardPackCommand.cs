using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class InboxCollectCardPackCommand : InboxCommand
{
	public const string COLLECT_CARD_PACK = "collect_card_pack";

	/// <inheritdoc/>
	public override void execute(InboxItem inboxItem)
	{
		Server.registerEventDelegate("collectible_pack_dropped", onPackDropped, true);
	}

	private void onPackDropped(JSON data)
	{
		Server.unregisterEventDelegate("collectible_pack_dropped", onPackDropped, true);
		if (Dialog.instance != null && Dialog.instance.currentDialog is InboxDialog)
		{
			Dialog.close();
		}
		Scheduler.addTask(new InboxTask());
	}

	/// <inheritdoc/>
	public override string actionName
	{
		get { return COLLECT_CARD_PACK; }
	}
}