using UnityEngine;
using System.Collections;


/**
 * Module to provide a clickable spin button for a wheel game
 */
public class WheelGameSpinButtonModule : WheelGameModule
{
	public GameObject spinButton;
	public Animator	spinButtonAnimator;

	public bool doIdleAttract = true;
	public float IDLE_INTERVAL = 3.5f;
	public bool isIdle = true;

	[SerializeField] private string INTRO_ANIM_NAME = "intro";
	[SerializeField] private string PICKME_ANIM_NAME = "spin_pickme";
	[SerializeField] private string PRESSED_ANIM_NAME = "spin_pressed";
	[Tooltip("List of sounds played with the pickme animation")]
	[SerializeField] private AudioListController.AudioInformationList pickmeAudioList;

	[SerializeField] private AnimationListController.AnimationInformationList spinButtonPressedAnimationInfo;

	private TICoroutine	idleLoop;
	[System.NonSerialized] public bool isExecuteOnSpinComplete = false;	

	// Enable the module on round init
	public override bool needsToExecuteOnRoundInit()
	{
		return true;
	}
	
	// Set up button presses on the spin button
	public override void executeOnRoundInit(ModularWheelGameVariant roundParent, ModularWheel wheel)
	{
		base.executeOnRoundInit(roundParent, wheel);

		if (spinButton.GetComponent<BoxCollider>() == null)
		{
			spinButton.AddComponent<BoxCollider>();
		}

		if (spinButton.GetComponent<UIButtonMessage>() == null)
		{
			UIButtonMessage btnMessage = spinButton.AddComponent<UIButtonMessage>();
			btnMessage.target = wheelParent.gameObject;
			btnMessage.functionName = "spinButtonPressed";	
		}
	}

	public override bool needsToExecuteOnSpin()
	{
		return true;
	}

	// stop the idle & play the button animation on spin started
	public override IEnumerator executeOnSpin()
	{
		if (isIdle)
		{
			isIdle = false;
			StopCoroutine(idleLoop); // cancel the idle animation routine
		}

		if (!string.IsNullOrEmpty(PRESSED_ANIM_NAME))
		{
			spinButtonAnimator.Play(PRESSED_ANIM_NAME);
		}

		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(spinButtonPressedAnimationInfo));
		yield return StartCoroutine(base.executeOnSpin());

		isExecuteOnSpinComplete = true;
	}

	private IEnumerator idleAtInterval()
	{
		while (isIdle && !wheelParent.isSpinning)
		{
			yield return new TIWaitForSeconds(IDLE_INTERVAL);

			// Since a spin could have happened while waiting we need to check for it again
			if (!wheelParent.isSpinning)
			{
				if (pickmeAudioList != null && pickmeAudioList.Count > 0)
				{
					yield return StartCoroutine(AudioListController.playListOfAudioInformation(pickmeAudioList));
				}
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(spinButtonAnimator, PICKME_ANIM_NAME));
			}
		}

		yield return null;
	}



	// Needs to the execute on round start?
	public override bool needsToExecuteOnRoundStart()
	{
		return true;
	}

	
	// Executes on round start for intro animation
	public override IEnumerator executeOnRoundStart()
	{
		if (!string.IsNullOrEmpty(INTRO_ANIM_NAME))
		{
			spinButtonAnimator.Play(INTRO_ANIM_NAME);
		}

		if (doIdleAttract)
		{
			isIdle = true;
			idleLoop = StartCoroutine(idleAtInterval());
		}

		yield break;
	}
}
