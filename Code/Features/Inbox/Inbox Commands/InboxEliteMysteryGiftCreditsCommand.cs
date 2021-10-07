using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InboxEliteMysteryGiftCreditsCommand : InboxEliteCommand
{
	public const string ELITE_COLLECT_CREDITS = "elite_mystery_gift_collect_credits";
	/// <inheritdoc/>
	public override void execute(InboxItem inboxItem)
	{
		long credits = 0L;
		if (long.TryParse(args, out credits))
		{
			SlotsPlayer.addFeatureCredits(credits, "inboxEliteGiftCmd");
		}
	}

	/// <inheritdoc/>
	public override string actionName
	{
		get { return ELITE_COLLECT_CREDITS; }
	}
}
