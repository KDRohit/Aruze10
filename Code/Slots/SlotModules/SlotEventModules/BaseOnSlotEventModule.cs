using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Base class for a series of OnSlotEvent SlotModules which will have data that extends
 * SlotModule.SlotModuleEventHandler which will tell what events the extended data class wants
 * to react to, and a function that is called to react when the event occurs.
 * SEE: InitAudioCollectionOnEventModule, PlayAudioListOnEventModule, PlayAnimationListOnEventModule for examples.
 *
 * Original Author: Scott Lepthien
 * Creation Date: 12/4/2019
 */
public abstract class BaseOnSlotEventModule<T> : SlotModule where T : SlotModule.SlotModuleEventHandler
{
	[SerializeField] private T[] eventHandlers;
	protected readonly Dictionary<SlotModuleEventType, List<SlotModuleEventHandler>> eventHandlerDictionary = new Dictionary<SlotModuleEventType, List<SlotModuleEventHandler>>();
	
	public override void Awake()
	{
		base.Awake();

		foreach (T eventHandler in eventHandlers)
		{
			eventHandler.setParentSlotModule(this);
			eventHandler.setOnEventDelegates();
			
			foreach (SortedSlotModuleEventType sortedSlotEvent in eventHandler.eventList)
			{
				if (!eventHandlerDictionary.ContainsKey(sortedSlotEvent.slotEvent))
				{
					eventHandlerDictionary.Add(sortedSlotEvent.slotEvent, new List<SlotModuleEventHandler>());
				}
				
				eventHandlerDictionary[sortedSlotEvent.slotEvent].Add(eventHandler);
			}
		}
	}
	
	//executeOnSlotGameStartedNoCoroutine() section
	//executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		return eventHandlerDictionary.ContainsKey(SlotModuleEventType.OnSlotGameStartedNoCoroutine);
	}
	
	public override void executeOnSlotGameStartedNoCoroutine(JSON reelSetDataJson)
	{
		List<SlotModuleEventHandler> eventHandlersForEvent = eventHandlerDictionary[SlotModuleEventType.OnSlotGameStartedNoCoroutine];
		
		callOnEventDelegates(eventHandlersForEvent);

		if (doesEventHaveCoroutineDelegates(eventHandlersForEvent))
		{
			StartCoroutine(callOnEventCoroutineDelegates(this, eventHandlersForEvent));
		}
	}
	
	// executeOnSlotGameStarted() section
	// executes right when a base game starts or when a freespin game finishes initing.
	public override bool needsToExecuteOnSlotGameStarted(JSON reelSetDataJson)
	{
		return eventHandlerDictionary.ContainsKey(SlotModuleEventType.OnSlotGameStarted);
	}
	
	public override IEnumerator executeOnSlotGameStarted(JSON reelSetDataJson)
	{
		List<SlotModuleEventHandler> eventHandlersForEvent = eventHandlerDictionary[SlotModuleEventType.OnSlotGameStarted];
		
		callOnEventDelegates(eventHandlersForEvent);

		if (doesEventHaveCoroutineDelegates(eventHandlersForEvent))
		{
			yield return StartCoroutine(callOnEventCoroutineDelegates(this, eventHandlersForEvent));
		}
	}
	
	// executeOnPreSpin() section
	// Functions here are executed during the startSpinCoroutine (either in SlotBaseGame or FreeSpinGame) before the reels spin
	public override bool needsToExecuteOnPreSpin()
	{
		return eventHandlerDictionary.ContainsKey(SlotModuleEventType.OnPrespin);
	}
	
	public override IEnumerator executeOnPreSpin()
	{
		List<SlotModuleEventHandler> eventHandlersForEvent = eventHandlerDictionary[SlotModuleEventType.OnPrespin];
		
		callOnEventDelegates(eventHandlersForEvent);

		if (doesEventHaveCoroutineDelegates(eventHandlersForEvent))
		{
			yield return StartCoroutine(callOnEventCoroutineDelegates(this, eventHandlersForEvent));
		}
	}
	
	// executeOnReevaluationPreSpin() section
	// function in a very similar way to the normal PreSpin hook, but hooks into ReelGame.startNextReevaluationSpin()
	// and triggers before the reels begin spinning
	public override bool needsToExecuteOnReevaluationPreSpin()
	{
		return eventHandlerDictionary.ContainsKey(SlotModuleEventType.OnReevaluationPreSpin);
	}
	
	public override IEnumerator executeOnReevaluationPreSpin()
	{
		List<SlotModuleEventHandler> eventHandlersForEvent = eventHandlerDictionary[SlotModuleEventType.OnReevaluationPreSpin];
		
		callOnEventDelegates(eventHandlersForEvent);

		if (doesEventHaveCoroutineDelegates(eventHandlersForEvent))
		{
			yield return StartCoroutine(callOnEventCoroutineDelegates(this, eventHandlersForEvent));
		}
	}
	
	// executeOnReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReelsStoppedCallback()
	{
		return eventHandlerDictionary.ContainsKey(SlotModuleEventType.OnReelsStoppedCallback);
	}

	public override IEnumerator executeOnReelsStoppedCallback()
	{
		List<SlotModuleEventHandler> eventHandlersForEvent = eventHandlerDictionary[SlotModuleEventType.OnReelsStoppedCallback];
		
		callOnEventDelegates(eventHandlersForEvent);

		if (doesEventHaveCoroutineDelegates(eventHandlersForEvent))
		{
			yield return StartCoroutine(callOnEventCoroutineDelegates(this, eventHandlersForEvent));
		}
	}
	
	// executeOnReevaluationReelsStoppedCallback() section
	// functions in this section are accessed by ReelGame.reelsStoppedCallback()
	public override bool needsToExecuteOnReevaluationReelsStoppedCallback()
	{
		return eventHandlerDictionary.ContainsKey(SlotModuleEventType.OnReevaluationReelsStoppedCallback);
	}

	public override IEnumerator executeOnReevaluationReelsStoppedCallback()
	{
		List<SlotModuleEventHandler> eventHandlersForEvent = eventHandlerDictionary[SlotModuleEventType.OnReevaluationReelsStoppedCallback];
		
		callOnEventDelegates(eventHandlersForEvent);

		if (doesEventHaveCoroutineDelegates(eventHandlersForEvent))
		{
			yield return StartCoroutine(callOnEventCoroutineDelegates(this, eventHandlersForEvent));
		}
	}
	
	// executeOnFreespinGameEnd() section
	// functions in this section are accessed by FreeSpinGame.gameEnded()
	public override bool needsToExecuteOnFreespinGameEnd()
	{
		return eventHandlerDictionary.ContainsKey(SlotModuleEventType.OnFreespinGameEnd);
	}
	
	public override IEnumerator executeOnFreespinGameEnd()
	{
		List<SlotModuleEventHandler> eventHandlersForEvent = eventHandlerDictionary[SlotModuleEventType.OnFreespinGameEnd];
		
		callOnEventDelegates(eventHandlersForEvent);

		if (doesEventHaveCoroutineDelegates(eventHandlersForEvent))
		{
			yield return StartCoroutine(callOnEventCoroutineDelegates(this, eventHandlersForEvent));
		}
	}

	// Call the non-coroutine delegate functions for an event
	private static void callOnEventDelegates(List<SlotModuleEventHandler> eventHandlersForEvent)
	{
		foreach (SlotModuleEventHandler eventHandler in eventHandlersForEvent)
		{
			if (eventHandler.onEventDelegate != null)
			{
				eventHandler.onEventDelegate();
			}
		}
	}

	// Used to skip spawning a Coroutine for Coroutine delegates if we know that there aren't any
	// to call
	private static bool doesEventHaveCoroutineDelegates(List<SlotModuleEventHandler> eventHandlersForEvent)
	{
		foreach (SlotModuleEventHandler eventHandler in eventHandlersForEvent)
		{
			if (eventHandler.onEventCoroutineDelegate != null)
			{
				return true;
			}
		}

		return false;
	}

	// Call the Coroutine delegate function for an event
	private static IEnumerator callOnEventCoroutineDelegates(SlotModule module, List<SlotModuleEventHandler> eventHandlersForEvent)
	{
		List<TICoroutine> blockingCoroutineList = new List<TICoroutine>();
		
		foreach (SlotModuleEventHandler eventHandler in eventHandlersForEvent)
		{
			if (eventHandler.onEventCoroutineDelegate != null)
			{
				if (eventHandler.isOnEventCoroutineDelegateBlocking)
				{
					// All blocking events will run concurrently. If you need to have something where
					// coroutines responding to the same event block each other you should use two
					// different modules which will force that to happen.
					blockingCoroutineList.Add(module.StartCoroutine(eventHandler.onEventCoroutineDelegate()));
				}
				else
				{
					module.StartCoroutine(eventHandler.onEventCoroutineDelegate());
				}
			}
		}

		if (blockingCoroutineList.Count >= 0)
		{
			yield return module.StartCoroutine(Common.waitForCoroutinesToEnd(blockingCoroutineList));
		}
	}
}
