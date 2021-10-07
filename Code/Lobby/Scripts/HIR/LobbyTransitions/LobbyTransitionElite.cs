using UnityEngine;
using System.Collections;
using Com.LobbyTransitions;
using Com.States;

public class LobbyTransitionElite : LobbyTransition
{
	[SerializeField] private AnimationListController.AnimationInformation transitionToElite;
	[SerializeField] private AnimationListController.AnimationInformation transitionToLobby;
	[SerializeField] private AnimationListController.AnimationInformation eliteIdle;
	[SerializeField] private AnimationListController.AnimationInformation lobbyIdle;

	public delegate void OnDoorsClosed();
	private event OnDoorsClosed onDoorsClosed;

	private StateMachine stateMachine;

	// =============================
	// CONST
	// =============================
	public const string TRANSITION_TO_ELITE = "transition_to_elite";
	public const string TRANSITION_TO_LOBBY = "transition_to_lobby";
	private const float TRANSITION_TIME = 2.0f;
	private const float END_TRANSITION_TIME = 16.0f;

	void Awake()
	{
		stateMachine = new StateMachine();
		stateMachine.addState(TRANSITION_TO_ELITE);
		stateMachine.addState(TRANSITION_TO_LOBBY);
	}

	public void addDoorsClosedCallback(OnDoorsClosed onClose)
	{
		onDoorsClosed -= onClose;
		onDoorsClosed += onClose;
	}

	public void removeDoorsClosedCallback(OnDoorsClosed onClose)
	{
		onDoorsClosed -= onClose;
	}

	/// <summary>
	/// Fired from the animation
	/// </summary>
	private void closeDoors()
	{
		if (onDoorsClosed != null)
		{
			onDoorsClosed();
		}
	}

	/// <summary>
	/// Fired from the animation
	/// </summary>
	private void onCompleteTransition()
	{
		MainLobbyV3.hirV3.transitionIn(TRANSITION_TIME);
		MainLobbyBottomOverlayV4.instance.transitionIn(TRANSITION_TIME);
		Overlay.instance.transitionIn(TRANSITION_TIME);
		if (EliteManager.hasActivePass)
		{
			Audio.switchMusicKeyImmediate(EliteManager.ELITE_LOBBY_MUSIC);
		}
		else
		{
			Audio.switchMusicKeyImmediate("spookylandingloop");
		}
	
		StartCoroutine(delayBeforeComplete());
	}

	private IEnumerator delayBeforeComplete()
	{
		yield return new WaitForSeconds(TRANSITION_TIME);

		finished();
	}

	public void updateState(string state)
	{
		stateMachine.updateState(state);
	}

	internal override void play(Dict args = null, bool disableInupt = true)
	{
		LoadingHIRV3.hirV3.toggleCamera(true);
		switch (stateMachine.currentState)
		{
			case TRANSITION_TO_LOBBY:
				RoutineRunner.instance.StartCoroutine(playToLobby());
				break;

			default:
				RoutineRunner.instance.StartCoroutine(playToElite());
				break;
		}

		base.play(args);
	}

	private IEnumerator playToLobby()
	{
		MainLobbyV3.hirV3.transitionOut(TRANSITION_TIME);
		Overlay.instance.transitionOut(TRANSITION_TIME);
		MainLobbyBottomOverlayV4.instance.transitionOut(TRANSITION_TIME);

		yield return new WaitForSeconds(TRANSITION_TIME);

		yield return StartCoroutine(AnimationListController.playAnimationInformation(transitionToLobby));
	}

	private IEnumerator playToElite()
	{
		MainLobbyV3.hirV3.transitionOut(TRANSITION_TIME);
		Overlay.instance.transitionOut(TRANSITION_TIME);
		MainLobbyBottomOverlayV4.instance.transitionOut(TRANSITION_TIME);

		yield return new WaitForSeconds(TRANSITION_TIME);

		yield return StartCoroutine(AnimationListController.playAnimationInformation(transitionToElite));
	}
}