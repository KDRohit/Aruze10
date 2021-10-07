using UnityEngine;
using System.Collections;
using Com.Scheduler;

public class TransferToGameTask : SchedulerTask
{
	public TransferToGameTask(Dict args = null) : base(args){}

	public override void execute()
	{
		base.execute();

		if (args.ContainsKey(D.GAME_KEY))
		{
			LobbyGame game = (LobbyGame) args[D.GAME_KEY];

			if (game != null)
			{
				// additional spins from triggering while we wait for the bonus
				// to load.
				if (SlotBaseGame.instance != null)
				{
					SlotBaseGame.instance.stopAutoSpin();
				}

				string details = (string)args.getWithDefault(D.KEY, "");

				if (!string.IsNullOrEmpty(details))
				{
					SlotAction.setLaunchDetails(details);
				}
				game.askInitialBetOrTryLaunch();
			}
		}
	}
}