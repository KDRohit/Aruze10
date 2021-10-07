using UnityEngine;
using System.Collections;
using TMPro;

public class FreeSpinEffect : TICoroutineMonoBehaviour
{
	// Use this for initialization
	public TextMeshPro text;
	public UIWidget shroud;
	private const int _maxCount = 3;
	private float MAX_WAIT_TIME = 5.0f;	
	private const float _fadeStartTime = 0.5f;
	private const float _fadeEndTime = 0.5f;
	private const float _fadeDelayTime = 0.2f;

	private const float _numberSlideTime = 0.15f;
	private const float _numberRestTime = 0.25f;
	private const float _bufferTime = 0.25f;
	
	private AnimationType animType = AnimationType.Intro;
	
	public enum AnimationType
	{
		Intro = 0,
		SpinIncrease = 1
	}

	void Awake ()
	{
		// sumation of time required to fade initial text + loop through all counts + fade out shroud time + additional buffer time
		//				------- InitialText --------    ------------------- Numbers -------------------------------     ------ Shroud-------------
		MAX_WAIT_TIME = (_fadeStartTime + _fadeEndTime) + ((_maxCount) * ((_numberSlideTime * 2) + _numberRestTime)) +  _fadeStartTime + _bufferTime;
		//StartCoroutine(startTextAnimation());
	}

	public IEnumerator startTextAnimation(AnimationType animationType = AnimationType.Intro, int autoSpinIncrease = 0)
	{
		//Debug.Log("max wait time " + MAX_WAIT_TIME);
		
		animType = animationType;
		if (animType == AnimationType.SpinIncrease)
		{
			MAX_WAIT_TIME = (_fadeStartTime + _fadeEndTime);

			if (text != null)
			{
				text.text = Localize.text("you_won_free_spins_{0}", autoSpinIncrease);
			}
		}

		if (text != null)
		{
			text.alpha = 0.0f;
		}

		iTween.ValueTo(this.gameObject, iTween.Hash("from", 0f,
													"to", 1f,
													"time", _fadeStartTime,
													"onupdate", "updateFade",
													"onupdatetarget", this.gameObject,
													"oncomplete", "fadeoutAnimation",
													"oncompletetarget", this.gameObject));
		yield return new WaitForSeconds(MAX_WAIT_TIME);
	}
	
	public void updateFade(float value)
	{
		if (text != null)
		{
			text.alpha = value;
		}
	}

	public void updateShroudFade(float value)
	{
		if (shroud != null)
		{
			shroud.alpha = value;
		}
	}
	
	public void fadeoutAnimation()
	{
		if (text != null)
		{
			iTween.ValueTo(text.gameObject, iTween.Hash("from", 1.0f,
														"to", 0f,
														"time", _fadeEndTime,
														"delay", _fadeDelayTime,
														"onupdate", "updateFade",
														"onupdatetarget", this.gameObject,
			                                            "oncomplete", "startSlideIn",
														"oncompletetarget", this.gameObject));
		}
	}

	public void startSlideIn()
	{
		if (animType == AnimationType.SpinIncrease)
		{
			destroySelf();
		}
		else
		{
			StartCoroutine(slideIn());
		}
	}

	// Debug function
	public void updateab()
	{
		if (text != null)
		{
			Debug.Log(text.text + " " + text.transform.localPosition);
		}
	}

	//starts the slide in animation
	public void startAnimation()
	{
		if (text != null)
		{
			iTween.MoveTo(text.gameObject, iTween.Hash("position", new Vector3(0.0f, 0.0f, 0.0f), 
				                                       		"time", _numberSlideTime * 1.5f,
			                                           		"islocal", true,
				                                            "oncomplete", "endAnimation",
				                                            "oncompletetarget", this.gameObject));
		}
	}

	//ends the slide in animation
	public void endAnimation()
	{
		if (text != null)
		{
			iTween.MoveTo(text.gameObject, iTween.Hash("position", new Vector3(-1600.0f, 0.0f, 0.0f),
															"time", _numberSlideTime * 1.5f,
			                                            	"easetype", iTween.EaseType.linear,
			                                            	"delay", _numberRestTime,
			                                           		"islocal", true));
		}
	}

	public IEnumerator slideIn()
	{
		Vector3 savedScale = text.transform.localScale;

		//loop through all the maxCounts and display the numbers using the slide in/out effect
		for (int i = _maxCount; i > 0; i--)
		{
			if (text != null)
			{
				text.text = i.ToString();
				text.transform.localPosition = new Vector3(1600.0f, text.transform.position.y);
				text.transform.localScale = savedScale;
				text.alpha = 1.0f;
			}

			startAnimation();
			Audio.play(Audio.soundMap("freespinintro"));
			yield return new WaitForSeconds(_numberSlideTime * 2 + (_numberRestTime + _numberSlideTime));
		}

		if (text != null)
		{
			// for the go use the scale in/scale out effect
			text.text = Localize.text("go_freespin");
			text.transform.localPosition = new Vector3(0,0,-1);
			text.transform.localScale = savedScale;
		}
		
		Audio.play(Audio.soundMap("freespinintro"));
		
		if (text != null)
		{
			//scale in
			iTween.ScaleBy(text.gameObject, new Vector3(1.5f, 1.5f), 0.25f);
			yield return new WaitForSeconds(_bufferTime);

			//scale out and fadeout
			iTween.ValueTo(text.gameObject, iTween.Hash("from", 1.0f,
			                                             "to", 0.0f,
			                                             "time", 0.25f,
			                                             "onupdate", "updateFade",
			                                             "onupdatetarget", this.gameObject));
			iTween.ScaleBy(text.gameObject, new Vector3(1/1.5f, 1/1.5f), 0.25f);
		}

		if (shroud != null)
		{
			//fadeout the shroud as well
			iTween.ValueTo(shroud.gameObject, iTween.Hash("from", shroud.alpha,
			                                            "to", 0.0f,
			                                            "time",_fadeStartTime,
			                                            "onupdate", "updateShroudFade",
			                                            "onupdatetarget", this.gameObject,
			                                            "oncomplete", "destroySelf",
			                                            "oncompletetarget", this.gameObject));
		}                                           
	}

	public void destroySelf()
	{
		Destroy(this.gameObject);
	}
}
