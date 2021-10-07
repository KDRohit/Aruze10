using UnityEngine;
using System.Collections;

public class PartyTimeKnifeGame : MonoBehaviour 
{
	public Animation tedAnimation;
	public Animation tableStab1; 
	public Animation tableStab2; 
	public Animation tableStab3;
	public Animation indexFinger;
	public Animation stabArea;
	public UILabel winLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winLabelWrapperComponent;

	public LabelWrapper winLabelWrapper
	{
		get
		{
			if (_winLabelWrapper == null)
			{
				if (winLabelWrapperComponent != null)
				{
					_winLabelWrapper = winLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelWrapper = new LabelWrapper(winLabel);
				}
			}
			return _winLabelWrapper;
		}
	}
	private LabelWrapper _winLabelWrapper = null;
	
	public UILabel resultSurvivedLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent resultSurvivedLabelWrapperComponent;

	public LabelWrapper resultSurvivedLabelWrapper
	{
		get
		{
			if (_resultSurvivedLabelWrapper == null)
			{
				if (resultSurvivedLabelWrapperComponent != null)
				{
					_resultSurvivedLabelWrapper = resultSurvivedLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_resultSurvivedLabelWrapper = new LabelWrapper(resultSurvivedLabel);
				}
			}
			return _resultSurvivedLabelWrapper;
		}
	}
	private LabelWrapper _resultSurvivedLabelWrapper = null;
	
	public UILabel resultStabbedLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent resultStabbedLabelWrapperComponent;

	public LabelWrapper resultStabbedLabelWrapper
	{
		get
		{
			if (_resultStabbedLabelWrapper == null)
			{
				if (resultStabbedLabelWrapperComponent != null)
				{
					_resultStabbedLabelWrapper = resultStabbedLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_resultStabbedLabelWrapper = new LabelWrapper(resultStabbedLabel);
				}
			}
			return _resultStabbedLabelWrapper;
		}
	}
	private LabelWrapper _resultStabbedLabelWrapper = null;
	
	private System.Action<bool> postGameCallback;
	// Use this for initialization
	void Start () 
	{
		reset ();

		AnimationState stab1State = tableStab1["table_stab_mark1"];
		stab1State.enabled = true;
		stab1State.weight = 1;
		stab1State.normalizedTime = 0;
		tableStab1.Sample();
		
		AnimationState stab2State = tableStab2["table_stab_mark2"];
		stab2State.enabled = true;
		stab2State.weight = 1;
		stab2State.normalizedTime = 0;
		tableStab2.Sample();
		
		AnimationState stab3State = tableStab3["table_stab_mark3"];
		stab3State.enabled = true;
		stab3State.weight = 1;
		stab3State.normalizedTime = 0;
		tableStab3.Sample();

		AnimationState stabAreaState = stabArea["stab_area"];
		stabAreaState.enabled = true;
		stabAreaState.weight = 1;
		stabAreaState.normalizedTime = 0;
		stabArea.Sample();

		AnimationState indexFingerState = indexFinger["index_finger"];
		indexFingerState.enabled = true;
		indexFingerState.weight = 1;
		indexFingerState.normalizedTime = 0;
		indexFinger.Sample();
	}

	public void reset()
	{
		tableStab1["table_stab_mark1"].speed = 0.0f;
		tableStab1.Play();
		tableStab2["table_stab_mark2"].speed = 0.0f;
		tableStab2.Play();
		tableStab3["table_stab_mark3"].speed = 0.0f;
		tableStab3.Play();
		indexFinger["index_finger"].speed = 0.0f;
		indexFinger.Play();
		stabArea["stab_area"].speed = 0.0f;
		stabArea.Play();

		winLabelWrapper.text = CreditsEconomy.convertCredits(BonusGamePresenter.portalPayout);
	}

	public void doMiss()
	{
		doMissResult(null);
	}

	public void doMissResult(System.Action<bool> callback)
	{
		postGameCallback = callback;
		StartCoroutine (loopThenMiss ());
	}

	public void doStabResult(System.Action<bool> callback)
	{
		postGameCallback = callback;
		StartCoroutine (loopThenStab ());
	}

	IEnumerator loopThenMiss()
	{
		loop ();
		yield return new WaitForSeconds(tedAnimation["loop"].length);
		miss ();
		yield return new WaitForSeconds(tedAnimation["miss"].length);
		stopAll();
		resultSurvivedLabelWrapper.gameObject.SetActive(true);
		yield return new WaitForSeconds(1.0f);
		if(postGameCallback != null)
		{
			postGameCallback(false);
		}
		resultSurvivedLabelWrapper.gameObject.SetActive(false);
	}

	IEnumerator loopThenStab()
	{
		loop ();
		stabArea["stab_area"].speed = 1.0f;
		stabArea.Play("stab_area");
		yield return new WaitForSeconds(tedAnimation["loop"].length);
		stab ();
		yield return new WaitForSeconds(tedAnimation["stab"].length);
		stopAll();
		resultStabbedLabelWrapper.gameObject.SetActive(true);
		yield return new WaitForSeconds(1.0f);
		postGameCallback(true);
		resultStabbedLabelWrapper.gameObject.SetActive(false);
	}

	private void stopAll()
	{
		tableStab1["table_stab_mark1"].speed = 0.0f;
		tableStab2["table_stab_mark2"].speed = 0.0f;
		tableStab3["table_stab_mark3"].speed = 0.0f;
		indexFinger["index_finger"].speed = 0.0f;
		stabArea["stab_area"].speed = 0.0f;
	}

	private void loop()
	{
		tableStab1["table_stab_mark1"].speed = 1.0f;
		tableStab1.Play("table_stab_mark1");
		tableStab2["table_stab_mark2"].speed = 1.0f;
		tableStab2.Play("table_stab_mark2");
		tableStab3["table_stab_mark3"].speed = 1.0f;
		tableStab3.Play("table_stab_mark3");
		indexFinger["index_finger"].speed = 1.0f;
		indexFinger.Play("index_finger");
		tedAnimation["loop"].speed = 1.0f;
		tedAnimation.Play("loop");
	}

	private void miss()
	{
		tedAnimation.Play("miss");
	}

	private void stab()
	{
		tedAnimation.Play ("stab");
	}
}

