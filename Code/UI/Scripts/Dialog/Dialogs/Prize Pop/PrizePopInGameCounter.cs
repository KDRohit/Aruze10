using System.Collections;
using TMPro;
using UnityEngine;

namespace PrizePop
{
	public class PrizePopInGameCounter : InGameFeatureDisplay
	{
		private enum MeterState
		{
			DEFAULT,
			DEFAULT_FULL,
			DEFAULT_DISABLED,
			EXTRA,
			EXTRA_FULL,
			EXTRA_DISABLED,
		}

		private const float TOOLTIP_DISPLAY_TIME = 2.0f;
		private const float EXTRA_STATE_TRANSITION_TIME = 0.8f;

		private const string METER_EXTRA_INTRO_ANIM = "Extras Intro";
		private const string METER_EXTRA_IDLE_ANIM = "Extras Idle";
		private const string METER_EXTRA_ADDED_ANIM = "Extras Added";
		private const string METER_IDLE_ANIM = "Idle Tall";
		private const string METER_INCREASE_SHORT_ANIM = "Increase Short";
		private const string METER_INCREASE_TALL_ANIM = "Increase Tall";
		private const string METER_FULL_SHORT_ANIM = "Filled Short";
		private const string METER_FULL_TALL_ANIM = "Filled Tall";
		private const string METER_BOUNCE_TALL = "Bounce Tall";
		private const string METER_BOUNCE_SHORT = "Bounce Short";
		private const string METER_DISABLED_SHORT_ANIM = "Disabled Short";
		private const string METER_DISABLED_TALL_ANIM = "Disabled Tall";
		private const string METER_MAXIMUM_SHORT_ANIM = "Maxed Out Short";
		private const string METER_MAXIMUM_TALL_ANIM = "Maxed Out Tall";
		private const string BUTTON_INTRO_ANIM = "Intro";
		private const string BUTTON_OFF_ANIM = "Off";
		private const string TOOLTIP_INTRO_ANIM = "Intro";
		private const string TOOLTIP_OUTRO_ANIM = "Outro";
		private const string TOOLTIP_OFF_ANIM = "Off";
		private const string NOTIF_INTRO_ANIM = "Intro";
		private const string NOTIF_IDLE_ANIM = "Idle";
		private const string NOTIF_OFF_ANIM = "Off";
		private const string NOTIF_EXTRA_PICK_ADD = "Extra Pick Added";
		private const string LOGO_IDLE_ANIM = "Idle";
		private const string LOGO_CELEBRATION_ANIM = "Celebration";
		private const string BOTTOM_BUTTON_ANIM = "Buy Extra";
		private const string BOTTOM_BUTTON_END_ANIM = "Event Ended";
	
		//Sounds
		private const string BUY_PICKS_CLICKED_AUDIO_KEY = "BuyExtraClickPrizePopCommon";
		private const string METER_FILLS_AUDIO_KEY = "MeterFillsPrizePopCommon";
		private const string PLAY_NOW_BUTTON_ACTIVATE_AUDIO_KEY = "PlayNowAnimatePrizePopCommon";
		private const string PLAY_NOW_CLICKED_AUDIO_KEY = "PlayNowClickPrizePop{0}";
		private const string PICK_ADDED_AUDIO_KEY = "PickAddedPrizePopCommon";
		
		[SerializeField] private TextMeshPro titleLabel;
		[SerializeField] private TextMeshPro pointsLabel;
		[SerializeField] private TextMeshPro notificationLabel;
		[SerializeField] private UIMeterNGUI progressMeter;
		[SerializeField] private UIMeterNGUI progressMeterShort;
		[SerializeField] private ButtonHandler playButton;
		[SerializeField] private ButtonHandler buyButton;
		[SerializeField] private Animator meterAnimator;
		[SerializeField] private Animator tooltipAnimatorExtra;
		[SerializeField] private Animator tooltipAnimatorBet;
		[SerializeField] private Animator tooltipAnimatorQualify;
		[SerializeField] private Animator playNowAnimator;
		[SerializeField] private Animator notificationAnimator;
		[SerializeField] private Animator logoAnimator;
		[SerializeField] private Animator bottomButtonAnimator;
		[SerializeField] private ButtonHandler infoButton;
		
		private bool buttonHandlerEnabled = false;
		private bool eventSetup = false;
		private long currentValue = 0;
		private long currentPicks = 0;
		private long currentBet = 0;
		private MeterState meterState = MeterState.DEFAULT;
		private bool meterEnabled = false;
		private bool meterFull = false;
		private bool buyButtonDisplayed = false;
		private bool playButtonDisplayed = false;
		private bool notificationBubbleDisplayed = false;
		private int notificationCount = 0;
		private Coroutine playingTooltip = null;
		private Animator activeTooltip = null;
		private bool shouldUpdate = false;
		private bool shouldBounce = false;
		private bool didHide = false;

		private void Awake()
		{
			init();
		}
		
		public override void init(Dict args = null)
		{
			if (PrizePopFeature.instance == null)
			{
				if (this.gameObject != null)
				{
					Destroy(this.gameObject);	
				}
				return;
			}
			
			//get the current bet
			if (SlotBaseGame.instance != null)
			{
				currentBet = SlotBaseGame.instance.currentWager;
			}
			
			//set current points/picks
			currentValue = PrizePopFeature.instance.currentPoints;
			currentPicks = PrizePopFeature.instance.numPicksAvailable;
			
			//register event handlers
			registerEvents();

			//set initial values
			updateProgressMeter((int)currentValue, PrizePopFeature.instance.maximumPoints, true);
			meterEnabled = PrizePopFeature.instance.isQualifyingBet(currentBet);
			meterFull = PrizePopFeature.instance.meterFillCount == PrizePopFeature.instance.maximumMeterFills;
			buyButtonDisplayed = PrizePopFeature.instance.isEnabled && !PrizePopFeature.instance.isEndingSoon();
			playButtonDisplayed = PrizePopFeature.instance.isEnabled && PrizePopFeature.instance.meterFillCount > 0;
			notificationCount = Mathf.Max(0, PrizePopFeature.instance.meterFillCount - 1) + PrizePopFeature.instance.extraPicks;;
			notificationBubbleDisplayed = false;
			playingTooltip = null;
			activeTooltip = null;
			detectMeterState();
			
			//tell the meter we've hidden and need to reset our display
			didHide = true;
			onShow();
		}

		private void detectMeterState()
		{
			if (PrizePopFeature.instance.meterFillCount > 0 && PrizePopFeature.instance.maximumMeterFills > 0)
			{
				//extra state
				if (meterFull)
				{
					meterState = MeterState.EXTRA_FULL;
				}
				else if (!meterEnabled)
				{
					meterState = MeterState.EXTRA_DISABLED;
				}
				else
				{
					meterState = MeterState.EXTRA;
				}
			}
			else
			{
				//extra state
				if (meterFull)
				{
					meterState = MeterState.DEFAULT_FULL;
				}
				else if (!meterEnabled)
				{
					meterState = MeterState.DEFAULT_DISABLED;
				}
				else
				{
					meterState = MeterState.DEFAULT;
				}
			}
		}

		private void showToolTip(Animator toolTipAnimator)
		{
			if (playingTooltip != null)
			{
				StopCoroutine(playingTooltip);
			}

			if (activeTooltip != null)
			{
				activeTooltip.Play(TOOLTIP_OFF_ANIM);
			}

			playingTooltip = StartCoroutine(playToolTip(toolTipAnimator));
		}

		private IEnumerator playToolTip(Animator tooltipAnimator)
		{
			activeTooltip = tooltipAnimator;
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(tooltipAnimator, TOOLTIP_INTRO_ANIM));
			if (this == null || this.gameObject == null)
			{
				yield break;
			}
			yield return new WaitForSeconds(TOOLTIP_DISPLAY_TIME);
			if (this == null || this.gameObject == null)
			{
				yield break;
			}

			yield return CommonAnimation.playAnimAndWait(tooltipAnimator, (TOOLTIP_OUTRO_ANIM));
			playingTooltip = null;
			activeTooltip = null;
		}

		private void activatePlayButton(bool animateTooltip = true)
		{
			playButtonDisplayed = true;
			playButton.enabled = true;
			if (PrizePopFeature.instance.maximumMeterFills > 1 && animateTooltip)
			{
				showToolTip(tooltipAnimatorExtra);	
			}
			if (playNowAnimator != null && playNowAnimator.gameObject != null)
			{
				playNowAnimator.Play(BUTTON_INTRO_ANIM);
			}

			Audio.play(PLAY_NOW_BUTTON_ACTIVATE_AUDIO_KEY);
		}

		private void disablePlayButton()
		{
			playButtonDisplayed = false;
			playButton.enabled = false;
			if (playNowAnimator != null && playNowAnimator.gameObject != null)
			{
				playNowAnimator.Play(BUTTON_OFF_ANIM);	
			}
		}

		public override void onBetChanged(long newWager)
		{
			currentBet = newWager;
			StartCoroutine(setMeterState(0, null, null));
		}

		private IEnumerator setMeterState(float meterIncrementTime, string meterIncrementAnimation, string logoAnimationToPlay)
		{
			if (PrizePopFeature.instance == null)
			{
				yield break;
			}

			shouldBounce = shouldBounce && string.IsNullOrEmpty(meterIncrementAnimation);
			bool isDisplayExtraPicks = isInExtraState();

			bool shouldDisplayBuyButton = PrizePopFeature.instance.isEnabled &&
			                              !PrizePopFeature.instance.isEndingSoon();
			
			if (!buyButtonDisplayed && shouldDisplayBuyButton)
			{
				buyButtonDisplayed = true;
				bottomButtonAnimator.Play(BOTTOM_BUTTON_ANIM);
			}
			else if (buyButtonDisplayed && !shouldDisplayBuyButton)
			{
				buyButtonDisplayed = false;
				bottomButtonAnimator.Play(BOTTOM_BUTTON_END_ANIM);
			}

			if (!string.IsNullOrEmpty(meterIncrementAnimation))
			{
				StartCoroutine(playMeterAnimation(meterIncrementAnimation));	
			}

			//add delay
			if (meterIncrementTime > 0)
			{
				yield return new WaitForSeconds(meterIncrementTime);
			}

			bool isQualifiedBet = PrizePopFeature.instance.isQualifyingBet(currentBet);
			bool shouldShowExtraPicks = PrizePopFeature.instance.meterFillCount > 0 && PrizePopFeature.instance.maximumMeterFills > 1;
			meterFull = PrizePopFeature.instance.meterFillCount == PrizePopFeature.instance.maximumMeterFills;
			if (!playButtonDisplayed && PrizePopFeature.instance.isEnabled && PrizePopFeature.instance.meterFillCount > 0)
			{
				activatePlayButton();
			}
			
			if (!isQualifiedBet && meterEnabled)
			{
				meterEnabled = false;
				showToolTip(tooltipAnimatorQualify);
			}
			else if (isQualifiedBet && !meterEnabled)
			{
				showToolTip(tooltipAnimatorBet);
				meterEnabled = true;
			}
			
			if (isDisplayExtraPicks && !string.IsNullOrEmpty(logoAnimationToPlay) && PrizePopFeature.instance.meterFillCount > 1)
			{
				//show extra added animation
				if (logoAnimator != null && logoAnimator.gameObject != null)
				{
					logoAnimator.Play(logoAnimationToPlay);
				}
				yield return StartCoroutine(playMeterAnimation(METER_EXTRA_ADDED_ANIM));
			}
			
			if (shouldShowExtraPicks)
			{
				yield return StartCoroutine(showExtraIcon());
			}
			else
			{
				yield return showDefault();
			}

			//reset bounce
			shouldBounce = false;
			updateNotificationBubble();
		}

		private IEnumerator showDefault()
		{
			switch (meterState)
			{
				case MeterState.EXTRA:
				case MeterState.EXTRA_FULL:
				case MeterState.EXTRA_DISABLED:
					meterState = MeterState.DEFAULT;
					yield return StartCoroutine(playMeterAnimation(METER_IDLE_ANIM));
					if (meterFull)
					{
						meterState = MeterState.DEFAULT_FULL;
						StartCoroutine(playMeterAnimation(METER_MAXIMUM_TALL_ANIM));
					}
					else if (!meterEnabled)
					{
						meterState = MeterState.DEFAULT_DISABLED;
						
						StartCoroutine(playMeterAnimation(METER_DISABLED_TALL_ANIM));
					}
					break;
				
				case MeterState.DEFAULT:
					if (meterFull)
					{
						meterState = MeterState.DEFAULT_FULL;
						yield return StartCoroutine(playMeterAnimation(METER_MAXIMUM_TALL_ANIM));
					}
					else if (!meterEnabled)
					{
						meterState = MeterState.DEFAULT_DISABLED;
						yield return StartCoroutine(playMeterAnimation(METER_DISABLED_TALL_ANIM));
					}
					else if (shouldBounce)
					{
						shouldBounce = false;
						yield return StartCoroutine(playMeterAnimation(METER_BOUNCE_TALL));
					}
					break;

				case MeterState.DEFAULT_FULL:
					if (!meterFull)
					{
						if (!meterEnabled)
						{
							meterState = MeterState.DEFAULT_DISABLED;
							yield return StartCoroutine(playMeterAnimation(METER_DISABLED_TALL_ANIM));
						}
						else
						{
							meterState = MeterState.DEFAULT;
							yield return StartCoroutine(playMeterAnimation(METER_IDLE_ANIM));
						}
					}
					break;

				case MeterState.DEFAULT_DISABLED:
					if (meterFull)
					{
						meterState = MeterState.DEFAULT_FULL;
						yield return StartCoroutine(playMeterAnimation(METER_MAXIMUM_TALL_ANIM));
					}
					else if (meterEnabled)
					{
						meterState = MeterState.DEFAULT;
						yield return StartCoroutine(playMeterAnimation(METER_IDLE_ANIM));
					}
					break;
			}

		}

		private IEnumerator showExtraIcon()
		{
			switch (meterState)
			{ 
				case MeterState.DEFAULT:
				case MeterState.DEFAULT_FULL:
				case MeterState.DEFAULT_DISABLED:
					meterState = MeterState.EXTRA;
					yield return StartCoroutine(playMeterAnimation(METER_EXTRA_INTRO_ANIM));
					if (meterFull)
					{
						meterState = MeterState.EXTRA_FULL;
						yield return StartCoroutine(playMeterAnimation(METER_MAXIMUM_SHORT_ANIM));
					}
					else if (!meterEnabled)
					{
						meterState = MeterState.EXTRA_DISABLED;
						yield return StartCoroutine(playMeterAnimation(METER_DISABLED_SHORT_ANIM));
					}
					break;
				
				case MeterState.EXTRA:
					if (meterFull)
					{
						meterState = MeterState.EXTRA_FULL;
						yield return StartCoroutine(playMeterAnimation(METER_MAXIMUM_SHORT_ANIM));
					}
					else if (!meterEnabled)
					{
						meterState = MeterState.EXTRA_DISABLED;
						yield return StartCoroutine(playMeterAnimation(METER_DISABLED_SHORT_ANIM));
					}
					else if (shouldBounce)
					{
						shouldBounce = false;
						yield return StartCoroutine(playMeterAnimation(METER_BOUNCE_SHORT));
					}
					break;
				case MeterState.EXTRA_FULL:
					if (!meterFull)
					{
						if (!meterEnabled)
						{
							meterState = MeterState.EXTRA_DISABLED;
							yield return StartCoroutine(playMeterAnimation(METER_DISABLED_SHORT_ANIM));
						}
						else
						{
							meterState = MeterState.EXTRA;
							yield return StartCoroutine(playMeterAnimation(METER_EXTRA_IDLE_ANIM));
						}
					}
					break;
				case MeterState.EXTRA_DISABLED:
					if (meterFull)
					{
						meterState = MeterState.EXTRA_FULL;
						yield return StartCoroutine(playMeterAnimation(METER_MAXIMUM_SHORT_ANIM));
					}
					else if (meterEnabled)
					{
						meterState = MeterState.EXTRA;
						yield return StartCoroutine(playMeterAnimation(METER_EXTRA_IDLE_ANIM));
					}
					break;
				
			}
		}
		
		private IEnumerator playMeterAnimation(string name)
		{
			if (meterAnimator != null && meterAnimator.gameObject != null)
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(meterAnimator, name));
			}
		}

		public override void onSpinComplete()
		{
			shouldBounce = true;
			shouldUpdate = true;
		}
		
		public override void setButtonsEnabled(bool enabled)
		{
			buttonHandlerEnabled = enabled;
		}
		
		private void registerEvents()
		{
			if (!eventSetup)
			{
				if (PrizePopFeature.instance != null)
				{
					PrizePopFeature.instance.onDisabledEvent += eventEnded;	
				}

				if (playButton != null)
				{
					playButton.registerEventDelegate(onPlayClick);
				}

				if (buyButton != null)
				{
					buyButton.registerEventDelegate(onBuyClick);
				}

				if (infoButton != null)
				{
					infoButton.registerEventDelegate(onInfoClick);
				}

				eventSetup = true;
			}
		}
		
		private void onPlayClick(Dict args = null)
		{
			if (buttonHandlerEnabled && PrizePopFeature.instance != null)
			{
				if(PrizePopFeature.instance.meterFillCount > 0)
				{
					PrizePopFeature.instance.startBonusGame(false, true);
					Audio.play(string.Format(PLAY_NOW_CLICKED_AUDIO_KEY, ExperimentWrapper.PrizePop.theme));
					//immediately hide play button while we're waiting for dialog to load
					disablePlayButton();
				}
				else
				{
					//just show the dialog
					PrizePopDialog.showDialog(true, startingOverlay:PrizePopFeature.PrizePopOverlayType.KEEP_SPINNING);
				}
			}
		}

		private void onInfoClick(Dict args = null)
		{
			if (buttonHandlerEnabled && PrizePopFeature.instance != null && PrizePopFeature.instance.isEnabled)
			{
				//just show the dialog
				PrizePopDialog.showDialog(true, startingOverlay:PrizePopFeature.PrizePopOverlayType.KEEP_SPINNING);
			}
		}

		private void onBuyClick(Dict args = null)
		{
			Audio.play(BUY_PICKS_CLICKED_AUDIO_KEY);
			if (buttonHandlerEnabled && 
			    PrizePopFeature.instance != null)
			{
				PrizePopOverlayStandaloneDialog.showDialog(PrizePopFeature.PrizePopOverlayType.BUY_EXTRA_PICKS);
			}
		}

		private void Update()
		{
			if (shouldUpdate && buttonHandlerEnabled)
			{
				shouldUpdate = false;
				doFullRefresh();
			}
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


		public override void refresh(Dict args)
		{
			shouldUpdate = true;
		}

		private bool isInExtraState()
		{
			switch (meterState)
			{
				case MeterState.EXTRA:
				case MeterState.EXTRA_FULL:
				case MeterState.EXTRA_DISABLED:
					return true;
				
				default:
					return false;
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
			 
			if (!buyButtonDisplayed)
			{
				bottomButtonAnimator.Play(BOTTOM_BUTTON_END_ANIM);	
			}
			else
			{
				bottomButtonAnimator.Play(BOTTOM_BUTTON_ANIM);
			}

			bool extra = isInExtraState();
			if (extra)
			{
				StartCoroutine(setupExtraMeter());
			}
			else
			{
				StartCoroutine(setupDefaultMeter());	
			}

			if (playButtonDisplayed)
			{
				activatePlayButton(false);
			}
			else
			{
				disablePlayButton();
			}

			if (notificationCount > 0 && extra)
			{
				notificationLabel.text = CommonText.formatNumber(notificationCount);
				notificationAnimator.Play(NOTIF_IDLE_ANIM);
			}
			else
			{
				notificationAnimator.Play(NOTIF_OFF_ANIM);
			}
			
			//play logo idle anim
			if (logoAnimator != null)
			{
				logoAnimator.Play(LOGO_IDLE_ANIM);
			}
		}

		private IEnumerator setupDefaultMeter()
		{
			yield return StartCoroutine(playMeterAnimation(METER_IDLE_ANIM));
			if (meterFull)
			{
				yield return StartCoroutine(playMeterAnimation(METER_MAXIMUM_TALL_ANIM));
			}
			else if (!meterEnabled)
			{
				yield return StartCoroutine(playMeterAnimation(METER_DISABLED_TALL_ANIM));
			}
		}

		private IEnumerator setupExtraMeter()
		{
			yield return StartCoroutine(playMeterAnimation(METER_EXTRA_IDLE_ANIM));
			if (meterFull)
			{
				yield return StartCoroutine(playMeterAnimation(METER_MAXIMUM_SHORT_ANIM));
			}
			else if (!meterEnabled)
			{
				yield return StartCoroutine(playMeterAnimation(METER_DISABLED_SHORT_ANIM));
			}
		}
		
		private void doFullRefresh()
		{
			if (PrizePopFeature.instance == null)
			{
				return;
			}
			
			if (!PrizePopFeature.instance.isEnabled)
			{
				if (buyButtonDisplayed)
				{
					buyButtonDisplayed = false;
					bottomButtonAnimator.Play(BOTTOM_BUTTON_END_ANIM);	
				}
				if (playButtonDisplayed)
				{
					disablePlayButton();
				}
				return;
			}
			
			float updateTime = 0;
			string meterIncrementAnimation = null;
			string logoAnimation = null;
			
			bool hasMorePicks = currentPicks != PrizePopFeature.instance.numPicksAvailable;
			bool hasNewMeterValue = currentValue != PrizePopFeature.instance.currentPoints || hasMorePicks;
			if (PrizePopFeature.instance.meterFillCount == PrizePopFeature.instance.maximumMeterFills)
			{
				//show the meter full because they can't fill it again
				updateProgressText(PrizePopFeature.instance.maximumPoints, PrizePopFeature.instance.maximumPoints);
				updateTime = updateProgressMeter(PrizePopFeature.instance.maximumPoints, PrizePopFeature.instance.maximumPoints);
			}
			else
			{
				//show progress
				updateProgressText(PrizePopFeature.instance.currentPoints, PrizePopFeature.instance.maximumPoints);
				
				if (hasMorePicks)
				{
					
					//if changing state from default to zero, and tweend default to full.  Will do tween from 0 to current value in followup coroutine
					bool isExtra = isInExtraState();
					if (!isExtra && PrizePopFeature.instance.maximumMeterFills > 1)
					{
						//we're going to do a transition, handle meter cases separately
						updateTime = updateProgressMeter(progressMeter, PrizePopFeature.instance.maximumPoints, PrizePopFeature.instance.maximumPoints, true);
						updateProgressMeter(progressMeterShort, 0, PrizePopFeature.instance.maximumPoints, false);
					}
					else
					{
						updateTime = updateProgressMeter(PrizePopFeature.instance.maximumPoints, PrizePopFeature.instance.maximumPoints);	
					}
					
					//show meter filling up to max 
					
					float delayTime = updateTime;
					if (meterState == MeterState.DEFAULT)
					{
						updateTime += 0.1f; //add a tenth of a second so that we don't play animation during tween 
						delayTime += EXTRA_STATE_TRANSITION_TIME;
					}

					//fill meter up more after (or set to 0)
					StartCoroutine(resetProgressBarInSeconds(delayTime + 0.1f)); //add a tenth of a second so we don't update on same frame and tween keeps going
				}
				else
				{
					updateTime = updateProgressMeter(PrizePopFeature.instance.currentPoints, PrizePopFeature.instance.maximumPoints);	
				}
			}
 
			if (hasNewMeterValue)
			{
				meterIncrementAnimation = meterState == MeterState.EXTRA ? METER_INCREASE_SHORT_ANIM : METER_INCREASE_TALL_ANIM;
				currentValue = PrizePopFeature.instance.currentPoints;
			}
			

			if (hasMorePicks)
			{
				logoAnimation = LOGO_CELEBRATION_ANIM;
				currentPicks = PrizePopFeature.instance.numPicksAvailable;
			}
			
			//let meter finish then update state/notificaition bubble
			StartCoroutine(setMeterState(updateTime, meterIncrementAnimation, logoAnimation));	
		}

		private IEnumerator resetProgressBarInSeconds(float seconds)
		{
			yield return new WaitForSeconds(seconds);
			float delay = updateProgressMeter(0, PrizePopFeature.instance.maximumPoints);
			if (delay > 0)
			{
				yield return new WaitForSeconds(delay);	
			}

			currentValue = PrizePopFeature.instance.currentPoints;
			if (PrizePopFeature.instance.maximumMeterFills == PrizePopFeature.instance.meterFillCount)
			{
				updateProgressMeter(PrizePopFeature.instance.maximumPoints, PrizePopFeature.instance.maximumPoints);
			}
			else if (currentValue != 0)
			{
				updateProgressMeter((int)currentValue, PrizePopFeature.instance.maximumPoints);
			}	
		}

		private void updateProgressText(int currentPoints, int requiredPoints)
		{
			if (titleLabel != null)
			{
				titleLabel.text = "Points:";	
			}

			if (pointsLabel != null)
			{
				pointsLabel.text = string.Format("{0}/{1}", currentPoints, requiredPoints);	
			}
		}

		private float updateProgressMeter(int currentPoints, int requiredPoints, bool instantUpdate = false)
		{
			float updateTime = 0;
			bool isExtra = isInExtraState();
			bool doTween = currentPoints > progressMeter.currentValue && !isExtra && !instantUpdate;
			float time = updateProgressMeter(progressMeter, currentPoints, requiredPoints, doTween);
			if (doTween)
			{
				updateTime = time;
			}

			doTween = currentPoints > progressMeterShort.currentValue && isExtra && !instantUpdate;
			time = updateProgressMeter(progressMeterShort, currentPoints, requiredPoints, doTween);
			if (doTween)
			{
				updateTime = Mathf.Max(updateTime, time);
			}
			
			return updateTime;
		}
		
		private static float updateProgressMeter(UIMeterNGUI meter, int currentPoints, int requiredPoints, bool doTween)
		{
			float updateTime = 0;
			if (meter != null && meter.gameObject != null)
			{
				if (doTween)
				{
					Audio.play(METER_FILLS_AUDIO_KEY);
				}
				meter.setState(currentPoints, requiredPoints, doTween);
				updateTime = doTween ? meter.tweenDuration : 0;
			}
			return updateTime;
		}

		private void updateNotificationBubble()
		{
			if (this == null || this.gameObject == null || PrizePopFeature.instance == null)
			{
				return;
			}

			int numExtraPicks = Mathf.Max(0, PrizePopFeature.instance.meterFillCount - 1) + PrizePopFeature.instance.extraPicks;
			if (numExtraPicks > 0 && PrizePopFeature.instance.meterFillCount > 0 && !notificationBubbleDisplayed)
			{
				notificationBubbleDisplayed = true;
				notificationCount = numExtraPicks;
				Audio.play(PICK_ADDED_AUDIO_KEY);
				showNotificationBubble();
			}
			else if (notificationBubbleDisplayed && PrizePopFeature.instance.meterFillCount <= 0)
			{
				notificationBubbleDisplayed = false;
				notificationAnimator.Play(NOTIF_OFF_ANIM);
			}
			else if (notificationBubbleDisplayed && notificationCount != numExtraPicks)
			{
				if (numExtraPicks <= 0)
				{
					notificationBubbleDisplayed = false;
					notificationAnimator.Play(NOTIF_OFF_ANIM);
				}
				else
				{
					Audio.play(PICK_ADDED_AUDIO_KEY);
					notificationAnimator.Play(NOTIF_EXTRA_PICK_ADD);
				}
				notificationCount = numExtraPicks;
			}
			if (notificationLabel != null)
			{
				notificationLabel.text = CommonText.formatNumber(numExtraPicks);
			}
		}

		private void showNotificationBubble()
		{
			if (notificationAnimator != null && notificationAnimator.gameObject != null)
			{
				notificationAnimator.Play(NOTIF_INTRO_ANIM);
			}
		}

		private void eventEnded()
		{
			refresh(null);
		}

		private void OnDestroy()
		{
			if (PrizePopFeature.instance != null)
			{
				PrizePopFeature.instance.onDisabledEvent -= eventEnded;	
			}

			if (playButton != null)
			{
				playButton.unregisterEventDelegate(onPlayClick);
			}

			if (buyButton != null)
			{
				buyButton.unregisterEventDelegate(onBuyClick);
			}
		}
	}    
}

