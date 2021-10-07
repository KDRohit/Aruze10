using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InboxEliteCashBackCommand : InboxEliteCommand
{
    public const string ELITE_COLLECT_CREDITS = "elite_cashback_collect_credits";
    /// <inheritdoc/>
    public override void execute(InboxItem inboxItem)
    {
        if (!collectedEvents.Contains(inboxItem.eventId))
        {
            collectedEvents.Add(inboxItem.eventId);
            long credits = 0L;
            if (long.TryParse(args, out credits))
            {
                SlotsPlayer.addNonpendingFeatureCredits(credits, "inboxEliteCashbackCmd");
            }
        }
    }

    /// <inheritdoc/>
    public override string actionName
    {
        get { return ELITE_COLLECT_CREDITS; }
    }
}
