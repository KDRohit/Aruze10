using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.HitItRich.EUE
{
	public class EUEInGameDisplay : InGameFeatureDisplay
	{

		private const string INTRO_ANIM = "Intro";
		private const string IDLE_ANIM = "Idle";
		private const string OUTRO_ANIM = "Outro";
		private const string OFF_ANIM = "Off";

		private bool didHide = false;
		private bool visible = true;
		private bool shouldUpdate = false;
		private bool shouldShowCoins = false;
			
		[SerializeField] private Animator animator;
		[SerializeField] private Animator coinAnimator;


		public override void init(Dict args = null)
		{
			//default to a hidden state
			didHide = true;
			visible = false;
			onShow();
			
			//Tell object to update when it's awake and running an update loop
			shouldUpdate = true;
			shouldShowCoins = false;

		}

		public override void onShow()
		{
			if (!didHide)
			{
				return;
			}

			didHide = false;
			if (visible)
			{
				animator.Play(IDLE_ANIM);
			}
			else
			{
				animator.Play(OFF_ANIM);
			}

		}

		public override void onHide()
		{
			didHide = true;
		}

		public override void refresh(Dict args)
		{
			bool didCompleteObjective = args == null ? false : (bool) args.getWithDefault(D.KEY, false);
			if (didCompleteObjective)
			{
				shouldShowCoins = true;
			}
			shouldUpdate = true;
		}

		private void Update()
		{
			if (shouldUpdate && !EUEManager.pauseInGameCounterUpdates)
			{
				shouldUpdate = false;
				doFullRefresh();
			}
		}

		private void doFullRefresh()
		{
			bool shouldDisplay = ExperimentWrapper.EueFtue.isInExperiment && 
			                     CampaignDirector.eue != null &&
			                     !CampaignDirector.eue.isComplete &&
			                     (SpinPanel.hir == null || !SpinPanel.hir.isSlotventuresUIEnabled());
			
			if (!visible && shouldDisplay)
			{
				visible = true;
				animator.Play(INTRO_ANIM);	
			}
			else if (visible && !shouldDisplay)
			{
				visible = false;
				animator.Play(OUTRO_ANIM);
			}
			
			if (shouldShowCoins)
			{
				shouldShowCoins = false;
				coinAnimator.Play("On");
			}
		}
	}	
}

