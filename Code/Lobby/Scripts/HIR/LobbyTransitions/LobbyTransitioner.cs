using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.States;

namespace Com.LobbyTransitions
{
	/// <summary>
	/// LobbyTransitioner handles any arbitrary amount of functionality involved when traversing in/out of lobbies
	/// </summary>
	public class LobbyTransitioner : IResetGame
	{
		// =============================
		// PRIVATE
		// =============================
		/// <summary>
		/// List of pending transitinos
		/// </summary>
		private static List<LobbyTransitionData> transitionQueue;

		/// <summary>
		/// Basic state machine
		/// </summary>
		private static StateMachine stateMachine;

		// =============================
		// CONST
		// =============================
		private const string PLAYING = "playing";
		private const string PLAYING_ALL = "playing_all";

		public static void init()
		{
			transitionQueue = new List<LobbyTransitionData>();
			stateMachine = new StateMachine("sm_lobby_transitioner");
			stateMachine.addState(StateMachine.READY);
			stateMachine.addState(PLAYING);
			stateMachine.addState(PLAYING_ALL);
			stateMachine.updateState(StateMachine.READY);
		}

		/// <summary>
		/// Adds a LobbyTransition to the queue
		/// </summary>
		/// <param name="transition"></param>
		public static void addTransition(LobbyTransition transition, Dict args = null)
		{
			LobbyTransitionData data = findDataWith(transition);
			if (data == null)
			{
				transitionQueue.Add(new LobbyTransitionData(transition, args));
			}
			else
			{
				data.args = args;
			}
		}

		/// <summary>
		/// Removes a LobbyTransition from the queue
		/// </summary>
		/// <param name="transition"></param>
		public static void removeTransition(LobbyTransition transition)
		{
			LobbyTransitionData data = findDataWith(transition);
			removeTransition(data);
		}

		/// <summary>
		/// Adds a LobbyTransitionData to the queue
		/// </summary>
		/// <param name="transition"></param>
		private static void addTransition(LobbyTransitionData data)
		{
			if (!transitionQueue.Contains(data))
			{
				transitionQueue.Add(data);
			}
		}

		/// <summary>
		/// Removes a LobbyTransitionData from the queue
		/// </summary>
		/// <param name="transition"></param>
		private static void removeTransition(LobbyTransitionData data)
		{
			if (transitionQueue.Contains(data))
			{
				transitionQueue.Remove(data);
			}
		}

		/// <summary>
		/// Will sequentially play all the lobby transitions that were added to the queue. This will wait
		/// for a transition to complete if one is already in progress
		/// </summary>
		public static void playAll()
		{
			if (stateMachine.can(StateMachine.READY))
			{
				playNextTransition();
			}
			stateMachine.updateState(PLAYING_ALL);
		}

		/// <summary>
		/// Plays the specified transition, if the transition was not added using addTransition()
		/// it will automatically be added to the transition list. When calling playTransition
		/// the assumption is this transition should play immediately, regardless of whatever was queued.
		/// If the LobbyTransitioner is playAll() was called, playTransition() will not run until that process has completed
		/// </summary>
		/// <param name="transition"></param>
		public static void playTransition(LobbyTransition transition)
		{
			LobbyTransitionData data = findDataWith(transition);
			removeTransition(transition);

			transitionQueue.Insert(0, data);

			if (stateMachine.can(StateMachine.READY))
			{
				transition.play(data.args);
			}
		}

		/// <summary>
		/// Calls playTransition(LobbyTransition) using the data provided
		/// </summary>
		/// <param name="data"></param>
		private static void playTransition(LobbyTransitionData data)
		{
			playTransition(data.transition);
		}

		/// <summary>
		/// When a LobbyTransition has finished, removes the transition
		/// </summary>
		/// <param name="transition"></param>
		internal static void onTransitionComplete(LobbyTransition transition)
		{
			removeTransition(transition);

			if (stateMachine.can(PLAYING_ALL))
			{
				playNextTransition();
			}
			else
			{
				stateMachine.updateState(StateMachine.READY);
			}
		}

		/// <summary>
		/// Plays the next transition in the queue
		/// </summary>
		private static void playNextTransition()
		{
			if (transitionQueue.Count > 0)
			{
				playTransition(transitionQueue[0]);
			}
		}

		/*=========================================================================================
		ANCILLARY
		=========================================================================================*/
		/// <summary>
		/// Returns the lobbytransitiondata that has the parameter transition
		/// </summary>
		/// <param name="transition"></param>
		/// <returns></returns>
		private static LobbyTransitionData findDataWith(LobbyTransition transition)
		{
			for (int i = 0; i < transitionQueue.Count; ++i)
			{
				if (transitionQueue[i].transition == transition)
				{
					return transitionQueue[i];
				}
			}

			return null;
		}

		/*=========================================================================================
		LOBBY TRANSITION DATA CLASS
		=========================================================================================*/
		/// <summary>
		/// Holds a reference to the lobby transition, and arguments to passed when LobbyTransition.play() is called
		/// </summary>
		private class LobbyTransitionData
		{
			public LobbyTransition transition;
			public Dict args;

			public LobbyTransitionData(LobbyTransition transition, Dict args = null)
			{
				this.transition = transition;
				this.args = args;
			}
		}

		public new static void resetStaticClassData()
		{
			transitionQueue.Clear();
			stateMachine.updateState(StateMachine.READY);
		}
	}
}