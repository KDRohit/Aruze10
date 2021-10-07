using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Module for playing AudioInformationLists on various ChallengeGameModule events as determined
by the AudioEventType setting for each AudioListEvent created

Creation Date: August 27, 2019
Original Author: Shaun Peoples
*/

public class ChallengeGamePlaySoundsOnEventModule : ChallengeGameModule 
{
	enum AudioEventType
	{
		OnRoundInit = 1,
		OnRoundStart = 2,
		OnRoundEnd = 3,
		OnGameEnd = 4,
		OnBonusGamePresenterFinalCleanup = 5,
	}
	
	[System.Serializable]
	class AudioListEvent
	{
		public string name;
		public AudioListController.AudioInformationList audioInformationList;
		public AudioEventType eventType;
	}
	
	[SerializeField] private List<AudioListEvent> audioListEvents;
	private readonly Dictionary<AudioEventType, AudioListEvent> audioListEventsDictionary = new Dictionary<AudioEventType, AudioListEvent>();

	public override void Awake()
	{
		base.Awake();

		foreach (AudioListEvent audioListEvent in audioListEvents)
		{
			if (audioListEventsDictionary.ContainsKey(audioListEvent.eventType))
			{
				Debug.LogError("Duplicate AudioEventType already exists, name: !" + audioListEvent.name + " type: " + audioListEvent.eventType);
			}
			else
			{
				audioListEventsDictionary.Add(audioListEvent.eventType, audioListEvent);
			}
		}
	}

	public override bool needsToExecuteOnRoundInit()
	{
		return audioListEventsDictionary.ContainsKey(AudioEventType.OnRoundInit);
	}

	public override bool needsToExecuteOnRoundStart()
	{
		return audioListEventsDictionary.ContainsKey(AudioEventType.OnRoundStart);
	}

	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		return isEndOfGame ? audioListEventsDictionary.ContainsKey(AudioEventType.OnGameEnd) : audioListEventsDictionary.ContainsKey(AudioEventType.OnRoundEnd);
	}

	public override bool needsToExecuteOnBonusGamePresenterFinalCleanup()
	{
		return audioListEventsDictionary.ContainsKey(AudioEventType.OnBonusGamePresenterFinalCleanup);
	}
	
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		RoutineRunner.instance.StartCoroutine(AudioListController.playListOfAudioInformation(audioListEventsDictionary[AudioEventType.OnRoundInit].audioInformationList));
	}

	public override IEnumerator executeOnRoundStart()
	{ 
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(audioListEventsDictionary[AudioEventType.OnRoundStart].audioInformationList));
	}

	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		AudioEventType eventType = isEndOfGame ? AudioEventType.OnGameEnd : AudioEventType.OnRoundEnd;
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(audioListEventsDictionary[eventType].audioInformationList));
	}
	
	public override IEnumerator executeOnBonusGamePresenterFinalCleanup()
	{
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(audioListEventsDictionary[AudioEventType.OnBonusGamePresenterFinalCleanup].audioInformationList));
	}
}
