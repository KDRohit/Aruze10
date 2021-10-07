using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InboxEliteCommand : InboxCommand
{
	/// <inheritdoc/>
	public override bool canExecute
	{
		get { return EliteManager.isActive; }
	}
	
	public override void execute(InboxItem inboxItem)
	{
		if (!collectedEvents.Contains(inboxItem.eventId))
		{
			collectedEvents.Add(inboxItem.eventId);
			long credits = 0L;
			if (long.TryParse(args, out credits))
			{
				SlotsPlayer.addNonpendingFeatureCredits(credits, "inboxEliteCmd");
			}
		}
	}

	/// <inheritdoc/>
	public override string actionName
	{
		//default to the elite_collect_credits primary action
		get { return "elite_collect_credits"; }
	}
}
