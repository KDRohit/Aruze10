using UnityEngine;
using System.Collections;

namespace Com.LobbyTransitions
{
	public class LobbyTransition : TICoroutineMonoBehaviour
	{
		// =============================
		// PROTECTED
		// =============================
		protected event OnTransitionStart onStartEvent;
		protected event OnTransitionComplete onCompleteEvent;

		// =============================
		// PUBLIC
		// =============================
		public delegate void OnTransitionStart(Dict args = null);
		public delegate void OnTransitionComplete(Dict args = null);

		public LobbyTransition(OnTransitionStart onStart = null, OnTransitionComplete onComplete = null)
		{
			addStartCallback(onStart);
			addCompleteCallback(onComplete);
		}

		/*=========================================================================================
		EVENT HANDLING
		=========================================================================================*/
		public void addStartCallback(OnTransitionStart onStart)
		{
			if (onStart != null)
			{
				onStartEvent -= onStart;
				onStartEvent += onStart;
			}
		}

		public void addCompleteCallback(OnTransitionComplete onComplete)
		{
			if (onComplete != null)
			{
				onCompleteEvent -= onComplete;
				onCompleteEvent += onComplete;
			}
		}

		public void removeStartCallback(OnTransitionStart onStart)
		{
			onStartEvent -= onStart;
		}

		public void removeCompleteCallback(OnTransitionComplete onComplete)
		{
			onCompleteEvent -= onComplete;
		}

		public void dispatchStart()
		{
			if (onStartEvent != null)
			{
				onStartEvent();
			}
		}

		public void dispatchComplete()
		{
			if (onCompleteEvent != null)
			{
				onCompleteEvent();
			}
		}

		/// <summary>
		/// Runs the associated functionality for how the lobby transition should operate
		/// </summary>
		/// <param name="args"></param>
		/// <param name="disableInupt">calls NGUIExt.disableAllMouseInput() if set to true</param>
		internal virtual void play(Dict args = null, bool disableInupt = true)
		{
			if (disableInupt)
			{
				NGUIExt.disableAllMouseInput();
			}

			dispatchStart();
		}

		public virtual void finished(Dict args = null)
		{
			LobbyTransitioner.onTransitionComplete(this);

			dispatchComplete();
		}
	}
}