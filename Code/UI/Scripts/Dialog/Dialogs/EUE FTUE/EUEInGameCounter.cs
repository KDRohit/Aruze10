using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Com.HitItRich.EUE
{	
	public class EUEInGameCounter : InGameFeatureDisplay
	{
		//panel animations
		private const string INTRO_ANIM = "Intro";
		private const string IDLE_ANIM = "Idle";
		private const string OUTRO_ANIM = "Outro";
		
		//pop out animation
		private const string POPOUT_COMPLETE_ANIM_NAME = "Popout Panel Complete";
		
		//object swapper states
		private const string CHALLENGE_COMPLETE_STATE = "challenge_complete";
		private const string IN_PROGRESS_STATE = "inprogress";
		private const string CHALLENGE_RESET_STATE = "challenge_reset";
		
		//audio
		private const string CHALLENGE_COMPLETE_SOUND = "FTUEChallengeComplete";
		
		private class PresentationData
		{
			public DisplayState state;
			public Objective objective;
			public DialogBase.AnswerDelegate callback;
		}
		
		[SerializeField] private Animator animator;
		[SerializeField] private Animator qualifiedBetAnimator;
		[SerializeField] private ObjectSwapper objectSwap;
		[SerializeField] private TextMeshPro challengeLabel;
		[SerializeField] private TextMeshPro percentageLabel;
		[SerializeField] private TextMeshPro pointValueLabel;
		[SerializeField] private TextMeshPro popoutLabel;
		[SerializeField] private float challengeCompleteDisplayTime = 5.0f;

		[SerializeField] private GameObject notificationBubble;
		[SerializeField] private GameObject popoutPanel;
		[SerializeField] private ObjectSwapper popoutSwapper;
		[SerializeField] private Animator popoutAnimator;
		[SerializeField] private UIStretch popoutShadowStretch;
		[SerializeField] private UIMeterNGUI progressMeter;
		[SerializeField] private UIAnchor popoutCheckmarkImageAnchor;
		[SerializeField] private UIAnchor popoutResetImageAnchor;
		[SerializeField] private UIStretchTextMeshPro popoutBackgroundStretch;
		[SerializeField] private Vector2 popOutPixelOffset;
		[SerializeField] private Vector2 popOutPixelOffsetNoIcon;

		private Objective currentObjective = null;
		private long currentSpinCount = 0;
		private long currentProgress = 0;
		private bool playingPresentation = false;
		private bool didHide = false;
		private DisplayState state = DisplayState.IN_PROGRESS;
		private static Queue<PresentationData> presentationQueue = new Queue<PresentationData>();

		private bool shouldUpdate = false;
		private bool buttonHandlerEnabled = false;
		
		private enum DisplayState
		{
			OFF,
			IN_PROGRESS,
			CHALLENGE_COMPLETE,
			PROGRESS_RESET
		}

		public override void init(Dict args = null)
		{
			//set initial state parameters based on first load
			state = DisplayState.OFF;
			//animator.Play(OUTRO_ANIM, -1, 1.0f);

			//disable notification
			SafeSet.gameObjectActive(notificationBubble, false);
			
			//initialize to default objective
			Objective objective = null;
			if (CampaignDirector.eue != null && CampaignDirector.eue.isActive && CampaignDirector.eue.currentMission != null)
			{
				objective = CampaignDirector.eue.currentMission.currentObjective;
			}
			initObjective(objective);
		}
		
		public override void setButtonsEnabled(bool enabled)
		{
			buttonHandlerEnabled = enabled;
		}
		
		public override void onStartNextAutoSpin()
		{
			//in the case that auto spin is runnin gare buttons are never enabled.  Make sure we run the presentation when the next auto spin is started
			if (shouldUpdate)
			{
				shouldUpdate = false;
				doFullRefresh();
			}
		}
		
		private void initObjective(Objective objective, bool playSound = false)
		{
			//make the first objective we track the default objective
			currentObjective = objective;

			//disable flyout
			popoutPanel.SetActive(false);


			if (objective != null && !objective.isComplete)
			{
				setState(DisplayState.IN_PROGRESS);
			}
			else if (state != DisplayState.OFF)
			{
				setState(DisplayState.OFF);
			}
			
			//update the mission counter and progress bar
			updateCounts();
			
		}

		public static void resetStaticClassData()
		{
			presentationQueue.Clear();
		}
		
		public override void onSpinComplete()
		{
			shouldUpdate = true;
		}
		
		public override void refresh(Dict args)
		{
			if (args != null)
			{
				bool didCompleteObjective = (bool)args.getWithDefault(D.KEY, false);
				DialogBase.AnswerDelegate callback = args.getWithDefault(D.CALLBACK, null) as DialogBase.AnswerDelegate;
				if (didCompleteObjective)
				{
					onChallengeComplete(callback);
				}
			}
			shouldUpdate = true;
			
		}

		private void Update()
		{
			if (shouldUpdate && !playingPresentation && buttonHandlerEnabled && !EUEManager.pauseInGameCounterUpdates)
			{
				shouldUpdate = false;
				doFullRefresh();
			}
		}
		
		public override void onHide()
		{
			didHide = true;
		}

		public override void onShow()
		{
			if (!didHide)
			{
				return;
			}

			//if we've been hidden the gameobject has been turned off then back on.  These animators will reset to default states
			didHide = false;

			if (state == DisplayState.OFF)
			{
				animator.Play(OUTRO_ANIM, -1, 1.0f);
			}
			else
			{
				animator.Play(IDLE_ANIM, -1, 1.0f);
			}
		}

		private void doFullRefresh()
		{
			if (presentationQueue.Count > 0)
			{
				PresentationData data = presentationQueue.Dequeue();
				setState(data.state, data.objective, false, data.callback);
			}
			else if (!ExperimentWrapper.EueFtue.isInExperiment || CampaignDirector.eue == null || CampaignDirector.eue.isComplete)
			{
				if (state != DisplayState.OFF)
				{
					animator.Play(OUTRO_ANIM);
					state = DisplayState.OFF;
				}
			}
			else
			{
				updateCounts();
			}
		}

		private void showIfHidden()
		{
			if (state == DisplayState.OFF && gameObject != null && gameObject.activeInHierarchy)
			{
				animator.Play(INTRO_ANIM);
			}
		}
		
		
		public void updateCounts()
		{
		    bool wasReset = false;
		    if (playingPresentation || presentationQueue.Count > 0)
		    {
		        return;
		    }

		    if (currentObjective != null)
		    {
		        XinYObjective xInY = null;
		        if (currentObjective.type == XinYObjective.X_COINS_IN_Y)
		        {
		            xInY = currentObjective as XinYObjective;
		        }

		        // If we haven't finished
		        if (currentObjective.currentAmount < currentObjective.progressBarMax)
		        {
		            if (currentProgress > currentObjective.progressBarMax ||
		                (xInY != null && xInY.constraints != null && xInY.constraints.Count > 0 &&  currentSpinCount > xInY.constraints[0].amount))
		            {
		                wasReset = true;
		            }
		            
		            if (xInY != null && xInY.constraints != null && xInY.constraints.Count > 0)
		            {
		                currentSpinCount = xInY.constraints[0].amount;
		            }

		            currentProgress = currentObjective.currentAmount;
		            if (state == DisplayState.IN_PROGRESS)
		            {
		                progressMeter.gameObject.SetActive(true);
		                progressMeter.setState(currentObjective.currentAmount,currentObjective.progressBarMax, true);    
		            }

		            if (wasReset)
		            {
		                setState(DisplayState.PROGRESS_RESET);
		            }
		        }

		        //set percent label and text
		        updateProgressLabels(currentObjective);

		        //Hide the pecentage text for certain challenge types
		        if (currentObjective.type == Objective.MAX_VOLTAGE_TOKENS_COLLECT || currentObjective.type == XinYObjective.X_COINS_IN_Y)
		        {
		            percentageLabel.text = "";
		        }

		    }
		}

		private void updateProgressLabels(Objective objective)
		{
			XinYObjective xInY = null;
			if (objective.type == XinYObjective.X_COINS_IN_Y)
			{
				xInY = objective as XinYObjective;
			}
			
			if (xInY != null)
			{
				percentageLabel.text = "";
				if (objective.usesTwoPartLocalization())
				{
					challengeLabel.text = objective.getShortChallengeTypeActionHeader() + System.Environment.NewLine + xInY.getShortDescriptionWithCurrentAmountAndLimit("robust_challenges_desc_short_", true);    
				}
				else
				{
					challengeLabel.text = objective.getTinyDynamicChallengeDescription(true);
				}
			}
			else
			{
				percentageLabel.text = objective.getProgressText();

				if (objective.usesTwoPartLocalization())
				{
					StringBuilder sb = new StringBuilder();
					string header = objective.getShortChallengeTypeActionHeader().ToLower();
					sb.Append(System.Char.ToUpper(header[0])); 
					sb.Append(header.Substring(1).ToLower());
					bool addNewLine = objectiveRequiresNewLine(objective.type);
					if (addNewLine)
					{
						sb.AppendLine();
					}
					else
					{
						sb.Append(" ");
					}
					string shortDesc = objective.getShortDescriptionLocalization("robust_challenges_desc_tiny_", true);
					sb.Append(shortDesc);
					challengeLabel.text = sb.ToString();
				}
				else
				{
					challengeLabel.text = objective.getTinyDynamicChallengeDescription(true);
				}
			}
		}

		private bool objectiveRequiresNewLine(string type)
		{
		    switch (type)
		    {
		        case Objective.BIG_WIN:
		        case Objective.BONUS_GAME: 
		        case CollectObjective.OF_A_KIND: 
		        case CollectObjective.SYMBOL_COLLECT:
		            return false;
		        
		        default:
		            return true;
		    }

		}

		private void setState(DisplayState newState, object param = null, bool playSounds = false, DialogBase.AnswerDelegate callback = null)
	    {
	        if (state == newState)
	        {
	            return;
	        }
	        
	        switch (newState)
	        {
	            case DisplayState.IN_PROGRESS:
		            showIfHidden();
	                objectSwap.setState(IN_PROGRESS_STATE);
	                if (currentObjective != null)
	                {
	                    progressMeter.gameObject.SetActive(true);
	                    progressMeter.setState(currentObjective.currentAmount,currentObjective.progressBarMax);   
	                }
	                else
	                {
	                    progressMeter.gameObject.SetActive(false);
	                }
	                state = DisplayState.IN_PROGRESS;
	                //update again if we have more queue items
	                shouldUpdate = presentationQueue.Count > 0;
	                break;
	          

	            case DisplayState.CHALLENGE_COMPLETE:
	                {
	                    Objective completedObjective = param as Objective;
	                    if (completedObjective == null)
	                    {
	                        Debug.LogError("No completed objective");
	                        return;
	                    }
	                    
	                    showIfHidden();
	                    playingPresentation = true;
	                    currentObjective = completedObjective;
	                    popoutSwapper.setState(CHALLENGE_COMPLETE_STATE);
	                    popoutBackgroundStretch.pixelOffset = popOutPixelOffset;
	                    popoutLabel.text = Localize.text(ChallengeCampaign.challengeCompleteLocalization);
	                    progressMeter.gameObject.SetActive(false);
	                    updateProgressLabels(completedObjective);
	                    pointValueLabel.text = "";
	                    state = DisplayState.CHALLENGE_COMPLETE;
	                    if (gameObject != null && gameObject.activeInHierarchy)
	                    {
	                        StartCoroutine(playCompletePresentation(callback));
	                    }
	                }
	                break;
	            
	            case DisplayState.PROGRESS_RESET:
		            showIfHidden();
	                objectSwap.setState(IN_PROGRESS_STATE);
	                popoutSwapper.setState(CHALLENGE_RESET_STATE);
	                popoutBackgroundStretch.pixelOffset = popOutPixelOffset;
	                popoutLabel.text = Localize.text(ChallengeCampaign.challengeResetLocalization);
	                if (currentObjective != null)
	                {
	                    progressMeter.gameObject.SetActive(true);
	                    progressMeter.setState(0,currentObjective.progressBarMax, false);   
	                }
	                else
	                {
	                    progressMeter.gameObject.SetActive(false);
	                }
	                state = DisplayState.PROGRESS_RESET;
	                if (gameObject != null && gameObject.activeInHierarchy)
	                {
	                    StartCoroutine(playResetPresentation());
	                }
	                playingPresentation = true;
	                break;
	            
	            case DisplayState.OFF:
		            objectSwap.setState(IN_PROGRESS_STATE);
		            progressMeter.gameObject.SetActive(false);
		            popoutPanel.SetActive(false);
		            animator.Play(OUTRO_ANIM);
		            break;

	            
	            default:
	                Debug.LogWarning("Invalid in game ui state");
	                break;
	        }
	    }
		
		private IEnumerator playCompletePresentation(DialogBase.AnswerDelegate callback)
		{
			//show popout
			popoutPanel.SetActive(true);
        
			//animate and play audio
			popoutAnimator.Play(POPOUT_COMPLETE_ANIM_NAME);
			Audio.play(CHALLENGE_COMPLETE_SOUND);

			//wait for animation to start and reposition checkmark
			yield return new WaitForSeconds(0.1f);
			if (popoutCheckmarkImageAnchor != null)
			{
				popoutCheckmarkImageAnchor.enabled = true;
			}
			if (popoutShadowStretch != null)
			{
				popoutShadowStretch.enabled = true;
			}
			
			//show EUE overlay if necessary
			if (EUEManager.shouldDisplayChallengeComplete)
			{
				EUEManager.showChallengeComplete();
			}
        
			//yield for rest of animation
			yield return new WaitForSeconds(challengeCompleteDisplayTime);

			//turn off popout
			if (popoutPanel != null)
			{
				popoutPanel.SetActive(false);
			}
			
			//reset data
			currentSpinCount = 0;
			currentProgress = 0;

			playingPresentation = false;
        
			//reinit with default objective
			if (ExperimentWrapper.EueFtue.isInExperiment && CampaignDirector.eue != null)
			{
				if (!CampaignDirector.eue.isComplete && 
				    CampaignDirector.eue.currentMission != null &&
				    CampaignDirector.eue.currentMission.currentObjective != null && 
				    !CampaignDirector.eue.currentMission.currentObjective.isComplete)
				{
					initObjective(CampaignDirector.eue.currentMission.currentObjective, true);						
				}
				else
				{
					setState(DisplayState.OFF, null);
				}
			}
			else
			{
				setState(DisplayState.OFF, null);
			}

			if (callback != null)
			{
				callback.Invoke(null);
			}
		}
		
		
		private IEnumerator playResetPresentation()
		{
			popoutPanel.SetActive(true);
			popoutAnimator.Play(POPOUT_COMPLETE_ANIM_NAME);
        
			//reposition image
			yield return new WaitForSeconds(0.1f);
			if (popoutResetImageAnchor != null)
			{
				popoutResetImageAnchor.enabled = true;
			}
			if (popoutShadowStretch != null)
			{
				popoutShadowStretch.enabled = true;
			}
        
			yield return new WaitForSeconds(challengeCompleteDisplayTime);

			if (popoutPanel != null)
			{
				popoutPanel.SetActive(false);
			}

			playingPresentation = false;
        
			//re-init with default objective
			initObjective(currentObjective);
		}
		
		private void onChallengeComplete(DialogBase.AnswerDelegate callback)
		{
			if (playingPresentation || !buttonHandlerEnabled)
			{
				PresentationData data = new PresentationData();
				data.state = DisplayState.CHALLENGE_COMPLETE;
				data.objective = currentObjective;
				data.callback = callback;
				presentationQueue.Enqueue(data);
				return;
			}
        
			setState(DisplayState.CHALLENGE_COMPLETE, currentObjective); 
		}
	}
}
