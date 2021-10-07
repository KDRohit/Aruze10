using UnityEngine;
using System.Collections;

public class InboxCollectCreditsCommand : InboxCommand
{
	public const string COLLECT_CREDITS = "collect_credits";

	/// <inheritdoc/>
	public override void execute(InboxItem inboxItem)
	{
		if (!collectedEvents.Contains(inboxItem.eventId))
		{
			collectedEvents.Add(inboxItem.eventId);
			long credits = 0L;
			if (long.TryParse(args, out credits))
			{
				SlotsPlayer.addNonpendingFeatureCredits(credits, "inboxCollectCmd");
			}
		}
	}

	/// <inheritdoc/>
	public override string actionName
	{
		get { return COLLECT_CREDITS; }
	}
}