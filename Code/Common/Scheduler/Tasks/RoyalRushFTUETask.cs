using UnityEngine;
using System.Collections;

namespace Com.Scheduler
{
	public class RoyalRushFTUETask : FunctionTask
	{
		public RoyalRushFTUETask(SchedulerDelegate callback, Dict args = null) : base(callback, args){}

		/// <inheritdoc/>
		public override bool canExecute
		{
			get
			{
				return base.canExecute && MainLobby.hirV3 != null && !MainLobby.isTransitioning;
			}
		}
	}
}