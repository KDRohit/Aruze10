using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
Module for playing AudioInformationLists on various WheelGameModule events as determined
by the AudioEventType setting for each AudioListEvent created

Creation Date: August 27, 2019
Original Author: Shaun Peoples
*/

public class WheelGamePlaySoundOnEventModule : WheelGameModule 
{
	protected enum AudioEventType
	{
		OnRoundInit = 1,
		OnRoundStart = 2,
		OnRoundEnd = 3,
		OnGameEnd = 4,
		OnSpin = 5,
		OnSpinComplete = 6,
		OnNumberOfWheelSlicesChanged = 7,
	}
	
	[System.Serializable]
	protected class AudioListEvent
	{
		public string name;
		public AudioListController.AudioInformationList audioInformationList;
		public AudioEventType eventType;
	}
	
	[SerializeField] protected List<AudioListEvent> audioListEvents;
	protected readonly Dictionary<AudioEventType, AudioListEvent> audioListEventsDictionary = new Dictionary<AudioEventType, AudioListEvent>();

	public void Awake()
	{
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

	public override bool needsToExecuteOnSpin()
	{
		return audioListEventsDictionary.ContainsKey(AudioEventType.OnSpin);
	}

	public override bool needsToExecuteOnSpinComplete()
	{
		return audioListEventsDictionary.ContainsKey(AudioEventType.OnSpinComplete);
	}

	public override bool needsToExecuteOnNumberOfWheelSlicesChanged(int newSize)
	{
		return audioListEventsDictionary.ContainsKey(AudioEventType.OnNumberOfWheelSlicesChanged);
	}

	public override void executeOnRoundInit(ModularWheelGameVariant round, ModularWheel wheel)
	{
		base.executeOnRoundInit(round, wheel);
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
	
	public override IEnumerator executeOnSpin()
	{
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(audioListEventsDictionary[AudioEventType.OnSpin].audioInformationList));
	}
	
	public override IEnumerator executeOnSpinComplete()
	{
		yield return StartCoroutine(AudioListController.playListOfAudioInformation(audioListEventsDictionary[AudioEventType.OnSpinComplete].audioInformationList));
	}

	public override void executeOnNumberOfWheelSlicesChanged(int newSize)
	{
		RoutineRunner.instance.StartCoroutine(AudioListController.playListOfAudioInformation(audioListEventsDictionary[AudioEventType.OnNumberOfWheelSlicesChanged].audioInformationList));
	}
}
