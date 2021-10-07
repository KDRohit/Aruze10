using UnityEngine;
using System.Collections;
using Com.LobbyTransitions;
using Com.Scheduler;
using Com.States;

public class LobbyTransitionTask : SchedulerTask
{
	// =============================
	// PROTECTED
	// =============================
	protected LobbyTransition transition;
	protected StateMachine stateMachine;

	// =============================
	// CONST
	// =============================
	private const string PLAYING = "playing";
	private const string READY = "ready";

	public LobbyTransitionTask(Dict args = null) : base(args)
	{
		stateMachine = new StateMachine();
		stateMachine.addState(PLAYING);
		stateMachine.addState(READY);
		stateMachine.updateState(READY);

		if (args != null)
		{
			transition = args.getWithDefault(D.OBJECT, null) as LobbyTransition;
		}
	}

	public override void execute()
	{
		Loading.hirV3.toggleCamera(true);

		if (transition != null)
		{
			stateMachine.updateState(PLAYING);
			transition.addCompleteCallback(onTransitionComplete);
			LobbyTransitioner.addTransition(transition);
			LobbyTransitioner.playTransition(transition);
		}
		else
		{
			Loading.hirV3.toggleCamera(false);
			Scheduler.removeTask(this);
			base.execute();
		}
	}

	public void onTransitionComplete(Dict args = null)
	{
		Loading.hirV3.toggleCamera(false);
		Scheduler.removeTask(this);
		base.execute();
	}

	public override bool canExecute
	{
		get
		{
			return base.canExecute &&
			       MainLobby.hirV3 != null &&
			       MainLobbyBottomOverlay.instance != null &&
			       Overlay.instance != null &&
			       stateMachine.can(READY);
		}
	}
}