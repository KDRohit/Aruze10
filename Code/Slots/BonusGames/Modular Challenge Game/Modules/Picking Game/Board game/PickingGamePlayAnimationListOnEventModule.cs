using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Module to play animations at pick events.
 */
public class PickingGamePlayAnimationListOnEventModule : PickingGameModule
{
	protected enum AnimationEventType 
	{
		// Add other events if needed
		OnItemClick = 1,
		OnAdvancePick = 2,
	}
	
	[System.Serializable]
	protected class AnimationEvent
	{
		public AnimationListController.AnimationInformationList animationInformations;
		public AnimationEventType eventType;
	}
	
	[SerializeField] protected List<AnimationEvent> animationEvents;
	protected readonly Dictionary<AnimationEventType, AnimationEvent> animationEventDictionary = new Dictionary<AnimationEventType, AnimationEvent>();

	public override void Awake()
	{
		base.Awake();
		foreach (AnimationEvent animationEvent in animationEvents)
		{
			animationEventDictionary.Add(animationEvent.eventType, animationEvent);
		}
	}

	public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		return animationEventDictionary.ContainsKey(AnimationEventType.OnItemClick);
	}

	// What happens when roll is clicked.
	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationEventDictionary[AnimationEventType.OnItemClick].animationInformations));
	}
	
	public override bool needsToExecuteOnAdvancePick()
	{
		return animationEventDictionary.ContainsKey(AnimationEventType.OnAdvancePick);
	}

	// Animations to play on advance pick event
	public override IEnumerator executeOnAdvancePick()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(animationEventDictionary[AnimationEventType.OnAdvancePick].animationInformations));
	}
}