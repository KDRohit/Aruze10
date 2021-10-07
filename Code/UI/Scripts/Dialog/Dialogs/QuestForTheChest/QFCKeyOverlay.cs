using System.Collections;
using System.Collections.Generic;
using QuestForTheChest;
using UnityEngine;
using TMPro;

public class QFCKeyOverlay : MonoBehaviour
{

	private const float STATE_FINISH_TIME_DELTA = 1.5f;
	private const string FIND_KEY_INTRO = "Find Key In Game Intro";
	private const string FIND_KEY_OUTRO = "Find Key In Game Outro";
	private const string FIND_KEY_RED = "Find Key Blue";
	private const string FIND_KEY_BLUE = "Find Key Red";
	private const string FOUND_IDLE = "Reward Loop";
	private const string REWARD_COLLECT = "Reward Outro";
	private const string FIND_KEY_SMALL = "Find Key Small";

	[SerializeField] private QFCKeyObject keyItem;
	[SerializeField] private Animator animator;
	[SerializeField] private MultiLabelWrapperComponent messageLabels;
	[SerializeField] private ClickHandler skipButton;

	private DialogBase.AnswerDelegate callback = null;
	private DialogBase.AnswerDelegate skipClickedCallback = null;
	private string eventId = "";

	public void init(string id, int numKeys, DialogBase.AnswerDelegate closeCallback, DialogBase.AnswerDelegate skipCallback)
	{
		callback = closeCallback;
		skipClickedCallback = skipCallback;
		eventId = id;

		if (numKeys == 0)
		{
			messageLabels.text = "?";
		}
		else
		{
			messageLabels.text = (numKeys == 1) ? Localize.text("qfc_found_{0}_key", numKeys) : Localize.text("qfc_found_{0}_keys", numKeys);
		}

		keyItem.setupKeyObjects(numKeys);

		StartCoroutine(playKeyFind());
		skipButton.registerEventDelegate(skipClicked);
	}

	private void skipClicked(Dict args = null)
	{
		if (skipClickedCallback != null)
		{
			skipClickedCallback.Invoke(Dict.create(D.EVENT_ID, eventId, D.OPTION, true));
		}
		Destroy(gameObject);
	}

	public void initRaceComplete(bool homeTeamWin, int numKeys, DialogBase.AnswerDelegate closeCallback, string id)
	{
		callback = closeCallback;
		keyItem.setupKeyObjects(numKeys);

		//animator named the animations wrong :(
		StartCoroutine(playAndCallCallback(homeTeamWin ? FIND_KEY_RED : FIND_KEY_BLUE));
	}

	private IEnumerator playKeyFind()
	{
		animator.Play(FIND_KEY_INTRO);
		yield return new WaitForSeconds(3);
		StartCoroutine(playAndCallCallback(FIND_KEY_OUTRO, STATE_FINISH_TIME_DELTA));
	}
	
	public void playKeyFindSmall(int numKeys)
	{
		skipButton.gameObject.SetActive(false);
		messageLabels.gameObject.SetActive(false);
		keyItem.setupKeyObjects(numKeys);
		animator.Play(FIND_KEY_SMALL);
	}

	public void initStaticRewardItem(int numKeys)
	{
		keyItem.setupKeyObjects(numKeys);
		animator.Play(FOUND_IDLE);
		skipButton.gameObject.SetActive(false);
	}

	public void playWheelCollectAnimation()
	{
		animator.Play(REWARD_COLLECT);
	}

	private IEnumerator playAndCallCallback(string animationState, float offsetFromEnd = 0)
	{
		// Null check just in case the animator is destroyed while we wait for the start delay.
		if (animator != null)
		{
			if (offsetFromEnd <= 0)
			{
				yield return StartCoroutine(CommonAnimation.playAnimAndWait(animator, animationState));
			}
			else
			{
				animator.Play(animationState);

				// It has to wait one frame before it can get the duration of the animation.
				yield return null;
				yield return null;  // Sometimes it takes more than one frame for some reason.

				// Additional null check just in case the animator is destroyed while we wait the two frames above.
				// MCC -- Checking if the name that we tried to play matches the name of current animation
				// If they dont match we can assume that the animation has finished already during the two frames.
				if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName(animationState))
				{
					float dur = animator.GetCurrentAnimatorStateInfo(0).length;
					float waitTime = dur - offsetFromEnd;
					if (waitTime > 0)
					{
						yield return new WaitForSeconds(waitTime);
					}
				}
			}
		}

		if (callback != null)
		{
			skipButton.gameObject.SetActive(false);
			callback.Invoke(Dict.create(D.EVENT_ID, eventId));
		}

		if (offsetFromEnd > 0)
		{
			yield return new WaitForSeconds(offsetFromEnd);
		}
		Destroy(this.gameObject);
	}
}
