using UnityEngine;
using System.Collections;
using Com.LobbyTransitions;

public class LobbyTransitionBlackFade : LobbyTransition
{
	private GenericDelegate createLobbyMethod;
	private string audioToPlay;
	private string userFlowKey;

	public LobbyTransitionBlackFade(OnTransitionStart onStart = null, OnTransitionComplete onComplete = null) : base(onStart, onComplete)
	{

	}

	internal override void play(Dict args = null, bool disableInupt = true)
	{
		if (args != null)
		{
			createLobbyMethod = args.getWithDefault(D.CALLBACK, null) as GenericDelegate;
			audioToPlay = (string)args.getWithDefault(D.AUDIO_KEY, "");
			userFlowKey = (string)args.getWithDefault(D.KEY, "");
		}

		base.play(args);

		RoutineRunner.instance.StartCoroutine(fadeToBlackRoutine());
	}

	private IEnumerator fadeToBlackRoutine()
	{
		if (!string.IsNullOrEmpty(audioToPlay))
		{
			Audio.play(audioToPlay);
		}

		if (!string.IsNullOrEmpty(userFlowKey))
		{
			MainLobby.startSpecialLobbyUserflow(userFlowKey);
		}

		yield return RoutineRunner.instance.StartCoroutine(BlackFaderScript.instance.fadeTo(1.0f));
		yield return new WaitForSeconds(0.5f);

		if (createLobbyMethod != null)
		{
			createLobbyMethod();
		}

		finished();
	}
}