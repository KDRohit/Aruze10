using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultiplierPayBoxDisplayModule : SlotModule 
{
	[SerializeField] private GameObjectPayBoxScript featureSymbolPayBox = null;
	[SerializeField] private Animator[] featureSymbolAnimators = null;		// Need the list of animators so I can animate the multiplier ones when the pay box is shown
	[SerializeField] private Animator singleSymbolAnimator = null;			// Use this instead of featureSymbolAnimators if there is one animator which uses animation names
	[SerializeField] private string[] featureAnimationNames = null;			// In some games we might have a list of animation names within a single animator instead of an animator list
	[SerializeField] private string[] featureWinAnimationNames = null;		// Play this animation if you won the feature symbol.
	[SerializeField] private string[] featureFadeOutNames = null;			// Used to fade out the feature display
	[SerializeField] private string featureFadeOutSound = "";				// Sound for the fade out
	[SerializeField] private string featureShowSound = "";					// Sound played when a feature is shown, controlled by boolean param to play anim funciton to control when it plays
	[SerializeField] private string featureIdleAnimName = "";				// Way to specifiy an idle animation name
	[SerializeField] private bool shouldGoToIdleOnPaylineHide = true;
	[SerializeField] private string[] multiplierFlourishSounds = new string[4]; // Sound played in the outcome display module
	[SerializeField] private string[] multiplierFlourishEchoSounds = null; // Echo played after the flourish sound.
	[SerializeField] private string[] multiplierVOSounds = new string[4];		// VO played in the outcome display module

	[SerializeField] private float[] FEATURE_SYMBOL_ANIMATION_LENGTHS;
	[SerializeField] private float[] FEATURE_SYMBOL_ANIMATION_DELAYS;

	[SerializeField] private float NORMAL_SYMBOL_ANIMATION_LENGTH;
	[SerializeField] private float MULTIPLIER_FLOURISH_ECHO_DELAY;
	[SerializeField] private float MULTIPLIER_OUTCOME_VO_DELAY;

	[SerializeField] private string FEATURE_SYMBOL_IDLE_ANIM_NAME = "idle";
	[SerializeField] private string FEATURE_SYMBOL_OUTCOME_ANIM_NAME = "anim";

	[SerializeField] private bool alwaysPlayMultiplierFlourishSounds = false;

	public bool shouldPlaySymbolPaylineVOs = false;

	private PayTable.LineWin storedLineWin;
	private bool wasDisplayingCustomPaybox = false;

	public enum MultiplierPayBoxFeatureEnum
	{
		None = -1,
		W2 = 0,
		W3 = 1,
		W4 = 2,
		W10 = 3,
		BN = 4,
		TR = 5
	}

	private static Dictionary<string, MultiplierPayBoxFeatureEnum> featureNameDict = new Dictionary<string, MultiplierPayBoxFeatureEnum>()
	{
		{ "W2", MultiplierPayBoxFeatureEnum.W2 },
		{ "W3", MultiplierPayBoxFeatureEnum.W3 },
		{ "W4", MultiplierPayBoxFeatureEnum.W4 },
		{ "W10", MultiplierPayBoxFeatureEnum.W10 },
		{ "BN", MultiplierPayBoxFeatureEnum.BN },
		{ "TR", MultiplierPayBoxFeatureEnum.TR }
	};

// executeOnPreSpin() section
	public override bool needsToExecuteOnPreSpin()
	{
		return true;
	}

	public override IEnumerator executeOnPreSpin()
	{
		if (featureSymbolPayBox.isShowing)
		{
			StartCoroutine(featureSymbolPayBox.hide());
		}

		MultiplierPayBoxFeatureEnum featureEnum = getCurrentFeatureEnum();
		if (featureFadeOutNames != null && featureEnum != MultiplierPayBoxFeatureEnum.None && (int)featureEnum < featureFadeOutNames.Length)
		{
			string animName = featureFadeOutNames[(int)featureEnum];
			if (animName != null && animName != "")
			{
				// right now only support doing the fades for the single animator version
				if (singleSymbolAnimator != null)
				{
					singleSymbolAnimator.Play(animName, -1, 0.0f);

					if (featureFadeOutSound != "")
					{
						Audio.play(Audio.soundMap(featureFadeOutSound));
					}
				}
			}
		}

		yield break;
	}

// executeOnPaylineDisplay() section
// functions in this section are accessed by ReelGame.onPaylineDisplayed()
	public override bool needsToExecuteOnPaylineDisplay()
	{
		return true;
	}

	public override IEnumerator executeOnPaylineDisplay(SlotOutcome outcome, PayTable.LineWin lineWin, Color paylineColor)
	{
		// need to make sure we only perform this when the feature on the reel is one of the multipliers
		List<string> featureSymbolsList = reelGame.outcome.getReevaluationFeatureSymbols();

		storedLineWin = lineWin;

		// need to double check that all reels are hit
		if ((featureSymbolsList[0].Contains('W') || featureSymbolsList[0].Contains('T'))
		   && lineWin.symbolMatchCount == 4)
		{
			featureSymbolPayBox.color = paylineColor;
			StartCoroutine(featureSymbolPayBox.show(0));

			MultiplierPayBoxFeatureEnum feature = getFeatureForSymbol(featureSymbolsList[0]);

			if (featureSymbolAnimators != null && featureSymbolAnimators.Length > 0)
			{
				featureSymbolAnimators[(int)feature].Play(FEATURE_SYMBOL_OUTCOME_ANIM_NAME);
			}
			else if (singleSymbolAnimator != null)
			{
				if (featureWinAnimationNames != null && (int)feature < featureWinAnimationNames.Length)
				{
					string featureWinAnimationName = featureWinAnimationNames[(int)feature];

					if (!singleSymbolAnimator.GetCurrentAnimatorStateInfo(0).IsName(featureWinAnimationName) ||
						singleSymbolAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
					{
						singleSymbolAnimator.Play(featureWinAnimationName, -1, 0.0f);
					}
				}
				else
				{
					singleSymbolAnimator.Play(featureAnimationNames[(int)feature], -1, 0.0f);
				}
			}
			else
			{
				Debug.LogError("Something isn't right, animations aren't configured for MultiplierPayBoxDisplayModule!");
			}

			wasDisplayingCustomPaybox = true;
		}

		yield break;
	}

	/// Display the feature
	public IEnumerator playBoxDisplayAnimation(bool isPlayingShowSound = false)
	{
		MultiplierPayBoxFeatureEnum feature = getCurrentFeatureEnum();

		if (FEATURE_SYMBOL_ANIMATION_DELAYS != null 
			&& FEATURE_SYMBOL_ANIMATION_DELAYS.Length > (int)feature 
			&& FEATURE_SYMBOL_ANIMATION_DELAYS[(int)feature] > 0)
		{
			yield return new TIWaitForSeconds(FEATURE_SYMBOL_ANIMATION_DELAYS[(int)feature]);
		}

		if (isPlayingShowSound && featureShowSound != "")
		{
			Audio.play(Audio.soundMap(featureShowSound));
		}

		if (alwaysPlayMultiplierFlourishSounds)
		{
			playMultiplierFlourishSound();
		}
		if (featureSymbolAnimators != null && featureSymbolAnimators.Length > 0)
		{
			featureSymbolAnimators[(int)feature].Play(FEATURE_SYMBOL_OUTCOME_ANIM_NAME);
		}
		else if (singleSymbolAnimator != null)
		{
			singleSymbolAnimator.Play(featureAnimationNames[(int)feature], -1, 0.0f);
		}
		else
		{
			Debug.LogError("Something isn't right, animations aren't configured for MultiplierPayBoxDisplayModule!");
		}
	}

	/// Play, but skip to the end of the display for this feature, used in instances were the animation state needs to be restored
	public void resetBoxDisplayAnimationToEnd()
	{
		MultiplierPayBoxFeatureEnum feature = getCurrentFeatureEnum();

		if (featureSymbolAnimators != null && featureSymbolAnimators.Length > 0)
		{
			featureSymbolAnimators[(int)feature].Play(FEATURE_SYMBOL_OUTCOME_ANIM_NAME, -1, 1.0f);
		}
		else if (singleSymbolAnimator != null)
		{
			singleSymbolAnimator.Play(featureAnimationNames[(int)feature], -1, 1.0f);
		}
		else
		{
			Debug.LogError("Something isn't right, animations aren't configured for MultiplierPayBoxDisplayModule!");
		}
	}

// executeOnPaylineHide() section
// function in this section are accesed by ReelGame.onPaylineHidden()
	public override bool needsToExecuteOnPaylineHide(List<SlotSymbol> symbolsAnimatedDuringCurrentWin)
	{
		return true;
	}

	public override IEnumerator executeOnPaylineHide(List<SlotSymbol> symbolsAnimatedDuringCurrentWin)
	{
		if (wasDisplayingCustomPaybox)
		{
			MultiplierPayBoxFeatureEnum feature = getCurrentFeatureEnum();

			if(storedLineWin.symbol.Contains('F'))
			{
				// minor symbols don't animate so play full duration
				yield return new TIWaitForSeconds(FEATURE_SYMBOL_ANIMATION_LENGTHS[(int)feature] - OutcomeDisplayBaseModule.MIN_DISPLAY_TIME);
			}
			else
			{
				// other symbols will animate and have a set duration of 1.333 so need to wait just a bit more for the feature symbol to finish
				yield return new TIWaitForSeconds(FEATURE_SYMBOL_ANIMATION_LENGTHS[(int)feature] - NORMAL_SYMBOL_ANIMATION_LENGTH);
			}

			if (featureSymbolPayBox.isShowing)
			{
				StartCoroutine(featureSymbolPayBox.hide());
			}

			if (shouldGoToIdleOnPaylineHide)
			{
				List<string> featureSymbolsList = reelGame.outcome.getReevaluationFeatureSymbols();
				if (featureSymbolsList[0].Contains('W'))
				{
					if (featureSymbolAnimators != null && featureSymbolAnimators.Length > 0)
					{
						featureSymbolAnimators[(int)feature].Play(FEATURE_SYMBOL_IDLE_ANIM_NAME);
					}
					else if (singleSymbolAnimator != null)
					{
						singleSymbolAnimator.Play(FEATURE_SYMBOL_IDLE_ANIM_NAME);
					}
					else
					{
						Debug.LogError("Something isn't right, animations aren't configured for MultiplierPayBoxDisplayModule!");
					}
				}
			}

			wasDisplayingCustomPaybox = false;
		}

		yield break;
	}

	/// Grab the serialized data about how long the animation for the current feature will take
	public float getCurrentFeatureAnimationLength()
	{
		MultiplierPayBoxFeatureEnum currentFeature = getCurrentFeatureEnum();
		return FEATURE_SYMBOL_ANIMATION_LENGTHS[(int)currentFeature];
	}

	/// Get the enum for the currently triggered feature
	public MultiplierPayBoxFeatureEnum getCurrentFeatureEnum()
	{
		if (reelGame.outcome != null)
		{
			List<string> featureSymbolsList = reelGame.outcome.getReevaluationFeatureSymbols();
			MultiplierPayBoxFeatureEnum feature = getFeatureForSymbol(featureSymbolsList[0]);

			return feature;
		}
		else
		{
			return MultiplierPayBoxFeatureEnum.None;
		}
	}

	/// For debug purposes
	public string getFeatureEnumName()
	{
		MultiplierPayBoxFeatureEnum feature = getCurrentFeatureEnum();
		return feature.ToString();
	}

	/// Used by the outcome display module to play sounds for this type of feature
	public void playMultiplierFlourishSound()
	{
		MultiplierPayBoxFeatureEnum featureEnum = getCurrentFeatureEnum();

		if (featureEnum != MultiplierPayBoxFeatureEnum.None 
			&& featureEnum != MultiplierPayBoxFeatureEnum.BN 
			&& featureEnum != MultiplierPayBoxFeatureEnum.TR)
		{
			string soundName = multiplierFlourishSounds[(int)featureEnum];

			if (soundName != null && soundName != "")
			{
				Audio.play(Audio.soundMap(soundName));
			}

			if (multiplierFlourishEchoSounds != null && (int)featureEnum < multiplierFlourishEchoSounds.Length)
			{
				string echoSoundName = multiplierFlourishEchoSounds[(int)featureEnum];

				if (!string.IsNullOrEmpty(echoSoundName))
				{
					Audio.play(Audio.soundMap(echoSoundName), 1, 0, MULTIPLIER_FLOURISH_ECHO_DELAY);
				}
			}
		}
	}

	/// Used by the outcome display module to play a VO for this type of feature
	public void playMultiplierVOSound()
	{
		MultiplierPayBoxFeatureEnum featureEnum = getCurrentFeatureEnum();

		if (featureEnum != MultiplierPayBoxFeatureEnum.None 
			&& featureEnum != MultiplierPayBoxFeatureEnum.BN 
			&& featureEnum != MultiplierPayBoxFeatureEnum.TR)
		{
			string soundName = multiplierVOSounds[(int)featureEnum];

			if (soundName != null && soundName != "")
			{
				Audio.play(Audio.soundMap(soundName), 1, 0, MULTIPLIER_OUTCOME_VO_DELAY);
			}
		}
	}

	/// Force the feature animation to the idle state
	public void playFeatureIdleAnim()
	{
		if (singleSymbolAnimator != null && featureIdleAnimName != "")
		{
			singleSymbolAnimator.Play(featureIdleAnimName);
		}
	}

	/// Determine what the feature is that is triggered
	public static MultiplierPayBoxFeatureEnum getFeatureForSymbol(string symbolName)
	{
		if (featureNameDict.ContainsKey(symbolName))
		{
			return featureNameDict[symbolName];
		}
		else
		{
			return MultiplierPayBoxFeatureEnum.None;
		}
	}
}
