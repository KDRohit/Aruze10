using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeGamePlayAnimationListOnEventModule : ChallengeGameModule 
{
	protected enum AnimationEventType
	{
		OnRoundInit = 1,
		OnRoundStart = 2,
		OnRoundEnd = 3,
		OnGameEnd = 4,
		OnBonusGamePresenterFinalCleanup = 5,
	}
	
	[System.Serializable]
	protected class AnimationEvent
	{
		public string name;
		public AnimationListController.AnimationInformationList animationInformation;
		public AnimationEventType eventType;
	}
	
	[SerializeField] protected List<AnimationEvent> animationEvents;
	protected readonly Dictionary<AnimationEventType, AnimationEvent> animationEventDictionary = new Dictionary<AnimationEventType, AnimationEvent>();

	public override void Awake()
	{
		base.Awake();
		
		foreach (AnimationEvent animationEvent in animationEvents)
		{
			if (animationEventDictionary.ContainsKey(animationEvent.eventType))
			{
				Debug.LogError("Duplicate AnimationEvent already exists, name: !" + animationEvent.name + " type: " + animationEvent.eventType);
			}
			else
			{
				animationEventDictionary.Add(animationEvent.eventType, animationEvent);
			}
		}
	}

	public override bool needsToExecuteOnRoundInit()
	{
		return animationEventDictionary.ContainsKey(AnimationEventType.OnRoundInit);
	}

	public override bool needsToExecuteOnRoundStart()
	{
		return animationEventDictionary.ContainsKey(AnimationEventType.OnRoundStart);
	}

	public override bool needsToExecuteOnRoundEnd(bool isEndOfGame)
	{
		return isEndOfGame ? animationEventDictionary.ContainsKey(AnimationEventType.OnGameEnd) : animationEventDictionary.ContainsKey(AnimationEventType.OnRoundEnd);
	}

	public override bool needsToExecuteOnBonusGamePresenterFinalCleanup()
	{
		return animationEventDictionary.ContainsKey(AnimationEventType.OnBonusGamePresenterFinalCleanup);
	}
	
	public override void executeOnRoundInit(ModularChallengeGameVariant round)
	{
		base.executeOnRoundInit(round);
		RoutineRunner.instance.StartCoroutine(AnimationListController.playListOfAnimationInformation(animationEventDictionary[AnimationEventType.OnRoundInit].animationInformation));
	}

	public override IEnumerator executeOnRoundStart()
	{ 
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationEventDictionary[AnimationEventType.OnRoundStart].animationInformation));
	}

	public override IEnumerator executeOnRoundEnd(bool isEndOfGame)
	{
		AnimationEventType eventType = isEndOfGame ? AnimationEventType.OnGameEnd : AnimationEventType.OnRoundEnd;
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationEventDictionary[eventType].animationInformation));
	}
	
	public override IEnumerator executeOnBonusGamePresenterFinalCleanup()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationEventDictionary[AnimationEventType.OnBonusGamePresenterFinalCleanup].animationInformation));
	}
}
