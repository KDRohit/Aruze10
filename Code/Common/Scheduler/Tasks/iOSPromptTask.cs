using System;
using Com.HitItRich.IDFA;
using Com.Scheduler;

/// <summary>
/// Task for the facebook ios14 ad tracking prompt
/// </summary>
public class iOSPromptTask : SchedulerTask
{
	private IDFASoftPromptManager.SurfacePoint surfacePoint;
	private Action onRequestFinishCallback;

	public iOSPromptTask(Dict args) : base(args)
	{
		if (args != null)
		{
			surfacePoint =
				(IDFASoftPromptManager.SurfacePoint) args.getWithDefault(D.OPTION,
					(int) IDFASoftPromptManager.SurfacePoint.GameEntry);
			onRequestFinishCallback = (Action) args.getWithDefault(D.CALLBACK, null);
		}
	}

	public override void execute()
	{
		base.execute();

		iOSAppTracking.RequestTracking(surfacePoint, onRequestFinishCallback);
		Scheduler.removeTask(this);
	}
}