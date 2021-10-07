using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

//
// Animates custom paylines and plays an animation list.
// Since only on version of PaylineOutcomeDisplayModule can exist. We have to call the base class
// methods from any overrides if not using our special case.
//
// Author : Nick Saito <nsaito@zynga.com>
// Date : March 24, 2020
// Games : gen95
//
public class AnimatedPaylineOutcomeDisplayModule : PaylineOutcomeDisplayModule
{
#region public properties

	// reel dimensions to use in generating the payline animations using the editor
	[SerializeField] private int numberOfReels;
	[SerializeField] private int numberOfSymbolsPerReel;
	[SerializeField] private float acquiredDelay;

	// payline animations
	[Tooltip("Template for the acquired payline animation")]
	public AnimationListController.AnimationInformation acquiredAnimationTemplate;

	[Tooltip("Template for the looped payline animation that plays when looping through the outcomes or playing the cascade")]
	public AnimationListController.AnimationInformation loopAnimationTemplate;

	[Tooltip("Template for payline end animation when paylines are turned off")]
	public AnimationListController.AnimationInformation endAnimationTemplate;

	// final generated payline animations
	[Tooltip("Paylines generated in the editor")]
	public List<PaylineAnimationData> paylineAnimationDataList;

	// payline settings
	[Tooltip("Always draw the custom paylines for all line wins")]
	[SerializeField] private bool alwaysDrawAnimatedPaylines;

	[Tooltip("Draw the custom paylines when there is a multiplier for the line win")]
	[SerializeField] private bool drawAnimatedPaylinesForMultipliers;

	[Tooltip("Specify the animators for each reel and position and override and other options here")]
	public List<PaylineReelPositionAnimationOverride> paylineReelPositionAnimationOverrides;

	[Tooltip("Specify the animators for each connector and override and other options here")]
	public List<PaylineConnectorAnimationOverride> paylineConnectorAnimationOverrides;

	[Tooltip("Use this to return any animations to their default state before starting the next spin.")]
	public AnimationListController.AnimationInformationList clearAnimations;

	// special effect animations settings
	[Tooltip("Always play the special effect for all line wins")]
	[SerializeField] private bool alwaysPlaySpecialEffectAnimation;

	[Tooltip("Play the special effect when there is a multiplier for the line win")]
	[SerializeField] private bool playSpecialEffectAnimationForMultipliers;

	[Tooltip("Play special effect once per outcome")]
	[SerializeField] private bool playSpecialEffectAnimationOnce = true;

	[Tooltip("Play an animation list with the payline.")]
	[SerializeField] private AnimationListController.AnimationInformationList specialEffectAnimation;

	[Tooltip("Multiplier label that can be updated if the multiplier is > 1")]
	[SerializeField] private LabelWrapperComponent multiplierLabel;

	// additional options
	[Tooltip("Choose this to draw normal payline cascade when showing all the rows.")]
	[SerializeField] private bool isDrawingCascade;

	[Tooltip("Adds a delay before showing paylines after a player selects a tier from the bet selector")]
	[SerializeField] private float showPaylinesAfterBetSelectorDelay;

#endregion

#region private properties
	// Tracks if we need to play the acquiredAnimation when first animating the payline.
	private Dictionary<string, bool> paylineAquiredAnimationDidPlay = new Dictionary<string, bool>();

	// Coroutines for animated paylines so we can wait until they are done displaying
	List<TICoroutine> allCoroutines = new List<TICoroutine>();

	// Keep a list of all the outcomes for the paylines so we can initially display all the lines at once in cascade.
	private List<SlotOutcome> paylineOutcomes;
	private bool arePaylinesHidden;

	// list of animators that are currently animating.
	private List<string> animatingPaylineKeys = new List<string>();

	// map of paylines we can use to quickly lookup a payline to play. Created using a key name like "3x3_1_2_3"
	private Dictionary<string, PaylineAnimationData> paylineAnimationDataMap = new Dictionary<string, PaylineAnimationData>();

	// slot reels we can get slotSymbols to animate
	private SlotReel[] slotReels;

	// if paylines are hidden we stop waitToFinish since we will restart the outcome display
	private TICoroutine waitToFinishCoroutine;

	// check for if animations get cleared by clearOutcome method
	private bool areAnimationsCleared;

	// track if we need to reenable the special effect after basegame is hidden
	private bool needsToReenableSpecialEffect;

#endregion

#region payline override methods

	public override void init(OutcomeDisplayController controller)
	{
		base.init(controller);

		if (paylineAnimationDataMap.Count == 0)
		{
			foreach (PaylineAnimationData paylineAnimationData in paylineAnimationDataList)
			{
				string paylineKey = getPaylineKey(paylineAnimationData.positions.ToArray());
				paylineAnimationDataMap.Add(paylineKey, paylineAnimationData);
			}
		}
	}

	// need to override this to clear paylineAquiredAnimationDidPlay
	// and to initialize our paylines if they aren't yet created
	public override void setupPaylineOutcomes(List<SlotOutcome> slotOutcomes)
	{
		if (slotReels == null)
		{
			slotReels = ReelGame.activeGame.engine.getAllSlotReels();
		}

		base.setupPaylineOutcomes(slotOutcomes);
		paylineAquiredAnimationDidPlay.Clear();
		areAnimationsCleared = false;
		paylineOutcomes = slotOutcomes;
	}

	// Verify we can and should play the payline cascade when the reels initially stop to show the player all the paylines they won.
	public override bool displayPaylineCascade(GenericDelegate doneCallback, GenericDelegate failedCallback)
	{
		// if we are drawing boxes only then we will skip the cascade and trigger the done Callback right away
		if (isDrawingBoxesOnly)
		{
			if (doneCallback != null)
			{
				doneCallback();
			}

			return false;
		}

		if (!isDrawingCascade)
		{
			// let the base module handle drawing cascades
			return base.displayPaylineCascade(doneCallback, failedCallback);
		}

		if (paylineOutcomes != null && paylineOutcomes.Count > 0)
		{
			// Play the looped animations all the paylines
			StartCoroutine(playCascade(doneCallback));
			return true;
		}

		// need to escape from a stuck loop, even if the paylines are broken
		Debug.LogError("Something is wrong with the paylines, aborting payline display!");
		if (failedCallback != null)
		{
			failedCallback();
		}

		return false;
	}

	// Animate the payline found in the outcome
	public override void playOutcome(SlotOutcome outcome, bool isPlayingSound)
	{
		if (outcome == null || arePaylinesHidden)
		{
			return;
		}

		allCoroutines.Clear();
		animatingPaylineKeys.Clear();

		// setup before playing the paylines
		_outcome = outcome;
		_outcomeDisplayDone = false;

		// update the multiplier label
		updateMultiplierLabel();

		// If we are not drawing animated paylines, play normal paylines by calling the base module
		if (!shouldAnimatePaylines(_outcome))
		{
			base.playOutcome(_outcome, isPlayingSound);
			return;
		}

		// Get the payline we need to draw and initialize a complete set of payline animations
		Payline payline = Payline.find(outcome.getPayLine());

		if (payline == null)
		{
			return;
		}

		// keep track of the paylines that are being played for the first time so we can loop them later
		string paylineKeyName = getPaylineKey(payline.positions);

		if (!paylineAquiredAnimationDidPlay.ContainsKey(paylineKeyName))
		{
			animatingPaylineKeys.Add(paylineKeyName);
			playListOfAnimationInformation(paylineAnimationDataMap[paylineKeyName].acquiredAnimation);
		}
		else
		{
			animatingPaylineKeys.Add(paylineKeyName);
			playListOfAnimationInformation(paylineAnimationDataMap[paylineKeyName].loopAnimation);
		}

		if (shouldPlaySpecialEffectAnimation(outcome))
		{
			playListOfAnimationInformation(specialEffectAnimation);
		}

		// animate the symbols on this payline
		animateSymbols(payline.positions);

		waitToFinishCoroutine = StartCoroutine(waitToFinish());
	}

	private void playListOfAnimationInformation(AnimationListController.AnimationInformationList animationInformationList)
	{
		foreach (AnimationListController.AnimationInformation animationInformation in animationInformationList.animInfoList)
		{
			// Play the animation
			allCoroutines.Add(StartCoroutine(playAnimationInformation(animationInformation)));
		}
	}

	private IEnumerator playAnimationInformation(AnimationListController.AnimationInformation animationInformation)
	{
		if (animationInformation.delay > 0)
		{
			yield return new WaitForSeconds(animationInformation.delay);
		}

		if (!areAnimationsCleared && !arePaylinesHidden)
		{
			animationInformation.targetAnimator.Play(animationInformation.ANIMATION_NAME);
			allCoroutines.Add(StartCoroutine(playAudio(animationInformation)));
		}
	}

	private static IEnumerator playAudio(AnimationListController.AnimationInformation info)
	{
		if (info == null || info.soundsPlayedDuringAnimation == null || info.soundsPlayedDuringAnimation.audioInfoList.Count <= 0)
		{
			yield break;
		}

		TICoroutine audioCoroutine = RoutineRunner.instance.StartCoroutine(AudioListController.playListOfAudioInformation(info.soundsPlayedDuringAnimation));
		yield return audioCoroutine;
	}

	// Stop animating paylines when the payline display state is set to off in OutcomeDisplayController
	public override void clearOutcome()
	{
		if (_outcome == null)
		{
			return;
		}

		stopPaylinesPlaying();

		if (waitToFinishCoroutine != null)
		{
			StopCoroutine(waitToFinishCoroutine);
		}

		if (clearAnimations != null && clearAnimations.Count > 0)
		{
			playListOfAnimationInformation(clearAnimations);
		}

		arePaylinesHidden = false;
		areAnimationsCleared = true;
		needsToReenableSpecialEffect = false;

		base.clearOutcome();
	}

	// wait for the payline to animate and then clear things out
	protected override IEnumerator waitToFinish()
	{
		// Wait the minimum amount of time before finishing up, even if there are no animations.
		yield return new WaitForSeconds(MIN_DISPLAY_TIME);

		if (allCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
		}

		// Keep waiting until there are no symbol animations playing before finishing up.
		while (animPlayingCounter > 0)
		{
			yield return null;
		}

		rememberPaylineAcquired();
		stopPaylinesPlaying();

		// Ready to finish up.
		StartCoroutine(displayFinish());
	}

	// Callback used in gen95 for when the bet selector tier is selected
	public void showLinesWithDelay()
	{
		allCoroutines.Add(StartCoroutine(showLinesWithDelayCoroutine()));
	}

	private IEnumerator showLinesWithDelayCoroutine()
	{
		if (showPaylinesAfterBetSelectorDelay > 0)
		{
			yield return new WaitForSeconds(showPaylinesAfterBetSelectorDelay);
		}

		showLines();
	}

	// Hide the lines if we are disabled, usually by big win, or a feature.
	public override void hideLines()
	{
		stopPaylinesPlaying();

		if (clearAnimations != null && clearAnimations.Count > 0)
		{
			playListOfAnimationInformation(clearAnimations);
		}

		// stop the "wait to finish" so we can restart the paylines when showLines is called
		if (waitToFinishCoroutine != null)
		{
			StopCoroutine(waitToFinishCoroutine);
		}

		arePaylinesHidden = true;
		needsToReenableSpecialEffect = true;
	}

	public override void showLines()
	{
		arePaylinesHidden = false;

		// restart the payline display after being hidden.
		playOutcome(_outcome, true);
	}

#endregion

#region helper methods

	// Play the loop animation state for all the paylines all at the same time when the reels initially stop
	// to show the player all the paylines they won.
	private IEnumerator playCascade(GenericDelegate doneCallback)
	{
		allCoroutines.Clear();
		animatingPaylineKeys.Clear();

		foreach (SlotOutcome slotOutcome in paylineOutcomes)
		{
			Payline payline = Payline.find(slotOutcome.getPayLine());

			if (payline != null)
			{
				string paylineKeyName = getPaylineKey(payline.positions);

				if (paylineAnimationDataMap.ContainsKey(paylineKeyName))
				{
					animatingPaylineKeys.Add(paylineKeyName);
					allCoroutines.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(paylineAnimationDataMap[paylineKeyName].loopAnimation)));
				}
			}
		}

		if (allCoroutines.Count > 0)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(allCoroutines));
		}

		stopPaylinesPlaying();
		allCoroutines.Clear();
		doneCallback();
	}

	// Set the value of the multiplier label if it exists
	private void updateMultiplierLabel()
	{
		if (multiplierLabel != null)
		{
			long multiplier = _outcome.getMultiplier();

			if (multiplier > 1)
			{
				multiplierLabel.text = CommonText.formatNumber(multiplier);
			}
		}
	}

	// Controls if we should draw a custom animation based on the selected options
	private bool shouldAnimatePaylines(SlotOutcome outcome)
	{
		if (drawAnimatedPaylinesForMultipliers && outcome.getMultiplier() > 1)
		{
			// if multiplier > 1 use custom payline
			return true;
		}

		if (alwaysDrawAnimatedPaylines)
		{
			return true;
		}

		return false;
	}

	// Determine if we should play the special effect animation.
	private bool shouldPlaySpecialEffectAnimation(SlotOutcome outcome)
	{
		if (specialEffectAnimation == null || specialEffectAnimation.Count == 0)
		{
			return false;
		}

		if(needsToReenableSpecialEffect)
		{
			needsToReenableSpecialEffect = false;
			return true;
		}

		if (playSpecialEffectAnimationOnce && paylineAquiredAnimationDidPlay.Count > 0)
		{
			return false;
		}

		if (playSpecialEffectAnimationForMultipliers && outcome.getMultiplier() > 1)
		{
			return true;
		}

		if (alwaysPlaySpecialEffectAnimation)
		{
			return true;
		}

		return false;
	}

	private void stopPaylinesPlaying()
	{
		foreach (TICoroutine coroutine in allCoroutines)
		{
			StopCoroutine(coroutine);
		}

		allCoroutines.Clear();

		// play the ending animations
		foreach(string paylineKey in animatingPaylineKeys)
		{
			StartCoroutine(AnimationListController.playListOfAnimationInformation(paylineAnimationDataMap[paylineKey].endAnimation));
			stopAnimatingSymbols(paylineAnimationDataMap[paylineKey].positions.ToArray());
		}

		animPlayingCounter = 0;
	}

	private string getPaylineKey(int[] positions)
	{
		StringBuilder paylineKey = new StringBuilder();
		paylineKey.AppendFormat("{0}x{1}", numberOfReels, numberOfSymbolsPerReel);
		foreach (int i in positions)
		{
			paylineKey.AppendFormat("_{0}", i);
		}

		return paylineKey.ToString();
	}

	private void animateSymbols(int[] symbolPositions)
	{
		for (int reelIndex = 0; reelIndex < symbolPositions.Length; reelIndex++)
		{
			SlotSymbol slotSymbol = slotReels[reelIndex].visibleSymbolsBottomUp[symbolPositions[reelIndex]];
			if (!slotSymbol.isAnimating)
			{
				animPlayingCounter++;
				allCoroutines.Add(StartCoroutine(slotSymbol.playAndWaitForAnimateOutcome(symbolAnimateDoneCallback)));
			}
		}
	}

	private void stopAnimatingSymbols(int[] symbolPositions)
	{
		for (int reelIndex = 0; reelIndex < symbolPositions.Length; reelIndex++)
		{
			SlotSymbol slotSymbol = slotReels[reelIndex].visibleSymbolsBottomUp[symbolPositions[reelIndex]];
			if (slotSymbol.isAnimating)
			{
				slotSymbol.haltAnimation();
			}
		}
	}

	private void symbolAnimateDoneCallback(SlotSymbol sender)
	{
		animPlayingCounter--;
	}

	// Remember which payline acquired animations have played completely.
	// Big win effect can interupt by hiding the paylines, so this shouldn't
	// be called until the acquired have finished completely.
	private void rememberPaylineAcquired()
	{
		foreach(string paylineKey in animatingPaylineKeys)
		{
			if (!paylineAquiredAnimationDidPlay.ContainsKey(paylineKey))
			{
				paylineAquiredAnimationDidPlay.Add(paylineKey, true);
			}
		}
	}

#endregion

#region data classes

	[Serializable]
	public class PaylineReelPositionAnimationOverride
	{
		// Because of the large amount of setup required for these paylines,
		// We define a default animation information and use this data
		// to create AnimationInformation and set the proper overrides.
		public Animator targetAnimator;
		public int reelIndex;
		public int position;
	}

	[Serializable]
	public class PaylineConnectorAnimationOverride : PaylineReelPositionAnimationOverride
	{
		public int endPosition;
	}

	[Serializable]
	public class PaylineAnimationData
	{
		public List<int> positions;
		public string paylineName;
		public AnimationListController.AnimationInformationList acquiredAnimation;
		public AnimationListController.AnimationInformationList loopAnimation;
		public AnimationListController.AnimationInformationList endAnimation;
	}

#endregion
}
