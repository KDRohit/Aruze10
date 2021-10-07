using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickemGameStagesUsingWheelPicks : PickingGame<WheelOutcome>
{
	[SerializeField] protected float pickmeAnimationDelay = 2.0f;
	[SerializeField] protected string pickmeAnimationName = "pickme";
	[SerializeField] protected int picksRemaining = 3;
	[SerializeField] protected string pickmeAudioKey = "";
	[SerializeField] protected GameObject winBox;
	[SerializeField] protected string winboxAnimationName = "anim";

	public UILabel pickCountLabel;		// To be removed when prefabs are updated.
	public UILabel[] instructionText;	// To be removed when prefabs are updated.
	public UILabel winAmountLabel;		// To be removed when prefabs are updated.
	public LabelWrapperComponent pickCountLabelWrapperComponent;
	public LabelWrapperComponent[] instructionTextWrapperComponent;
	public LabelWrapperComponent winAmountLabelWrapperComponent;

	public LabelWrapper pickCountLabelWrapper
	{
		get
		{
			if (_pickCountLabelWrapper == null)
			{
				if (pickCountLabelWrapperComponent != null)
				{
					_pickCountLabelWrapper = pickCountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_pickCountLabelWrapper = new LabelWrapper(pickCountLabel);
				}
			}
			return _pickCountLabelWrapper;
		}
	}
	private LabelWrapper _pickCountLabelWrapper = null;	

	public List<LabelWrapper> instructionTextWrapper
	{
		get
		{
			if (_instructionTextWrapper == null)
			{
				_instructionTextWrapper = new List<LabelWrapper>();

				if (instructionTextWrapperComponent != null && instructionTextWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in instructionTextWrapperComponent)
					{
						_instructionTextWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in instructionText)
					{
						_instructionTextWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _instructionTextWrapper;
		}
	}
	private List<LabelWrapper> _instructionTextWrapper = null;	

	public LabelWrapper winAmountLabelWrapper
	{
		get
		{
			if (_winAmountLabelWrapper == null)
			{
				if (winAmountLabelWrapperComponent != null)
				{
					_winAmountLabelWrapper = winAmountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winAmountLabelWrapper = new LabelWrapper(winAmountLabel);
				}
			}
			return _winAmountLabelWrapper;
		}
	}
	private LabelWrapper _winAmountLabelWrapper = null;	

	protected long amountWonThisRound = 0;
	protected WheelPick wheelPick;
	protected int pickButtonIndex = 0;

	public override void init() 
	{
		pickCountLabelWrapper.text = picksRemaining.ToString();
		base.init();
		inputEnabled = false;
		beginPrePickSequence();
	}

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject buttonObj)
	{
		inputEnabled = false;
		pickButtonIndex = getButtonIndex(buttonObj);
		picksRemaining--;
		pickCountLabelWrapper.text = picksRemaining.ToString();
		amountWonThisRound = wheelPick.credits;

		prePickAudioCall();

		yield return new TIWaitForSeconds(0.5f);

		StartCoroutine(beginPostPickSequence());
	}

	protected virtual void prePickAudioCall()
	{
		// Override this to do an audio call!
	}

	protected override IEnumerator pickMeAnimCallback()
	{
		if (inputEnabled)
		{
			PickGameButtonDataList pickGameButtonList = roundButtonList[currentStage];
			
			if(!string.IsNullOrEmpty(pickmeAudioKey))
			{
				Audio.play(Audio.soundMap(pickmeAudioKey));
			}
			
			if(pickGameButtonList.animationList.Length > 0)
			{
				int pickMeIndex = Random.Range(0, pickGameButtonList.animationList.Length);
				pickGameButtonList.animationList[pickMeIndex].Play(pickmeAnimationName);
				yield return new TIWaitForSeconds(pickmeAnimationDelay);
			}
			else if(pickGameButtonList.animatorList.Length > 0)
			{
				int pickMeIndex = Random.Range(0, pickGameButtonList.animatorList.Length);
				pickGameButtonList.animatorList[pickMeIndex].Play(pickmeAnimationName);
				yield return new TIWaitForSeconds(pickmeAnimationDelay);
			}
		}
	}

	protected virtual void beginPrePickSequence()
	{
		wheelPick = outcome.getNextEntry();
		
		if (wheelPick == null)
		{
			endGame();
		}
		else
		{
			StartCoroutine(beginPrePickAnimationSequence());
		}
	}

	protected virtual IEnumerator beginPrePickAnimationSequence()
	{
		yield return new TIWaitForSeconds(0.5f);
		inputEnabled = true;
	}

	// Override this function if anything extra needs to happen before we end the game
	protected virtual void endGame()
	{
		BonusGamePresenter.instance.gameEnded();
	}


	protected virtual IEnumerator beginPostPickSequence()
	{
		if(winBox != null)
		{
			winBox.GetComponent<Animator>().Play(winboxAnimationName);
		}
		
		yield return StartCoroutine(SlotUtils.rollup(BonusGamePresenter.instance.currentPayout, BonusGamePresenter.instance.currentPayout + amountWonThisRound, winAmountLabelWrapper));
		
		BonusGamePresenter.instance.currentPayout += amountWonThisRound;
		beginPrePickSequence();
	}

	protected virtual IEnumerator revealSinglePickemObject()
	{
		yield return new TIWaitForSeconds(1.0f);
	}

}
