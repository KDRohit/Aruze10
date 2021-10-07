using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// The serializable class has to be separate from the monobehaviour for backwards compatibility.
[System.Serializable]
public class PickingRound
{
	public BonusOutcomeTypeEnum BONUS_OUTCOME_TYPE = BonusOutcomeTypeEnum.Undefined; // Fill this out if different rounds have different outcome types.
	public string BONUS_OUTCOME_NAME = ""; // Name of the outcome JSON in case the server doesn't send it.
	
	public PickemStageType PICKEM_STAGE_TYPE;
	public NextStageRule nextStageRule;
	
	public GameObject stageObject;
	
	public UILabel multiplierLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent multiplierLabelWrapperComponent;

	public LabelWrapper multiplierLabelWrapper
	{
		get
		{
			if (_multiplierLabelWrapper == null)
			{
				if (multiplierLabelWrapperComponent != null)
				{
					_multiplierLabelWrapper = multiplierLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_multiplierLabelWrapper = new LabelWrapper(multiplierLabel);
				}
			}
			return _multiplierLabelWrapper;
		}
	}
	private LabelWrapper _multiplierLabelWrapper = null;
	
	public LabelWrapperComponent currentWinAmountTextWrapper;
	public UILabel currentWinAmountText;	// To be removed when prefabs are updated.

	public LabelWrapper currentWinAmountTextWrapperNew	// Needed to use "New" due to existing wrapper component using same name.
	{
		get
		{
			if (_currentWinAmountTextWrapperNew == null)
			{
				if (currentWinAmountTextWrapper != null)
				{
					_currentWinAmountTextWrapperNew = currentWinAmountTextWrapper.labelWrapper;
				}
				else
				{
					_currentWinAmountTextWrapperNew = new LabelWrapper(currentWinAmountText);
				}
			}
			return _currentWinAmountTextWrapperNew;
		}
	}
	private LabelWrapper _currentWinAmountTextWrapperNew = null;
	
	public LabelWrapperComponent jackpotLabelWrapper;
	public UILabel jackpotLabel;	// To be removed when prefabs are updated.

	public LabelWrapper jackpotLabelWrapperNew	// Needed to use "New" due to existing wrapper component using same name.
	{
		get
		{
			if (_jackpotLabelWrapperNew == null)
			{
				if (jackpotLabelWrapper != null)
				{
					_jackpotLabelWrapperNew = jackpotLabelWrapper.labelWrapper;
				}
				else
				{
					_jackpotLabelWrapperNew = new LabelWrapper(jackpotLabel);
				}
			}
			return _jackpotLabelWrapperNew;
		}
	}
	private LabelWrapper _jackpotLabelWrapperNew = null;
	
	public UILabel messagingLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent messagingLabelWrapperComponent;

	public LabelWrapper messagingLabelWrapper
	{
		get
		{
			if (_messagingLabelWrapper == null)
			{
				if (messagingLabelWrapperComponent != null)
				{
					_messagingLabelWrapper = messagingLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_messagingLabelWrapper = new LabelWrapper(messagingLabel);
				}
			}
			return _messagingLabelWrapper;
		}
	}
	private LabelWrapper _messagingLabelWrapper = null;
	
	
	public bool hasWings; // This round has it's own set of wings so change the wings!
	public RoundGameObjects objectsToActivateOrDeactivateByRound;
	
	public AnimationDefinition[] animationDefinitions;
	public SoundDefinition[] soundDefinitions;
	
	public GameObject[] pickemGos; // This is either a PickGameButton or a game object that contains a PickGameButton.
	public RoundAnimationNames animationNames;
	public int numPickMeAnims = 1;
	
	public GameObject jackpotWinEffect;
	public SparkleTrailDefinition sparkleTrailDefinition;
	public RevealDefinition revealDefinition;	
	
	public PickSoundOverrides soundOverrides;
	
	public SoundDelaysDefinition soundDelays;
	public RevealDelaysDefintion revealDelays;
	
	public FindDefinition findDefinition;
	public PickNumPicksDefinition PickNumPicksDefinition;

	public bool keepShowingCurrentStage;
}

// This is the picking round game object / monobehaviour.
public class PickingRoundGO : TICoroutineMonoBehaviour
{
	// Tunables
	public PickingRound pickingRound;
	
	// Variables
	[HideInInspector] public GenericPickemGameGO genericPickemGame;
	
	public virtual void initRound()
	{
	}
	
	public virtual IEnumerator pickemButtonPressedCoroutine(GameObject pickButton)
	{
		yield return null;
	}
	
	public virtual void revealRemainingPick(PickGameButtonData pick)
	{
	}
}

