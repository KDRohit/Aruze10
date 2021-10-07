using UnityEngine;
using System.Collections;

public class FarmVilleSymbolAnimator : SymbolAnimator3d
{
	public Animation symbolAnimation;

	public enum State { IDLE, ANIM_DONE, HAPPY };
	public State animationState = State.ANIM_DONE;

	private TICoroutine symbolCoroutine;
	public int lastPlayedFidget = -2;

	private const string ANIM_NAME_HAPPY = "happy";
	private const string ANIM_NAME_IDLE = "idle";
	private const string ANIM_NAME_FIDGET = "fidget_A";
	private const string ANIM_NAME_PECKING = "pecking";
	private const string ANIM_NAME_POSE = "icon_pose";

	[SerializeField] private GameObject neck;
	[SerializeField] private GameObject eye;
	[SerializeField] private GameObject animalGeo;

	public bool hasPlopped = false;
	public bool hasRotatedAfterPlop = false;

	// Not sure why this update method exists, perhaps someone forgot to remove it.
	void Update()
	{
		if (Input.GetKeyDown("m"))
		{
			updateRotation();
		}
	}

	public void updateRotation(bool onlyOnce = false)
	{
		if (eye != null && neck != null && !hasRotatedAfterPlop)
		{
			StartCoroutine(doRotation(onlyOnce));
		}
	}

	public void markAsPlopped()
	{
		hasPlopped = true;
	}

	public override void activate(bool isFlattened = false)
	{
		symbolAnimation.Play(ANIM_NAME_POSE);
		animationState = State.ANIM_DONE;
		hasPlopped = false;
		hasRotatedAfterPlop = false;
		base.activate(isFlattened);
	}

	/// Turns off a symbol (deactivates it), which may include some special cleanup/reset code.
	/// Override this for special symbol prefab types.
	public override void deactivate()
	{
		symbolAnimation.Play(ANIM_NAME_POSE);
		animationState = State.ANIM_DONE;
		lastPlayedFidget = 0;
		hasPlopped = false;
		hasRotatedAfterPlop = false;
		// Make sure the symbol isn't animating from a prior life
		if (isAnimating)
		{
			stopAnimation();
		}
		transform.localScale = info.scaling;
		gameObject.SetActive(false);
		setIsSymbolActive(false);
	}

	// For some crazy reason that makes me really angry, we have to do this calculation a few times in succession
	// in order for the animals to end up in the right spot. Each rotation brings them closer, but if we only do it once,
	// they look really weird. 4 seems like a good number of times for now.
	public IEnumerator doRotation(bool onlyOnce = false)
	{
		int numberOfIterations = 4;
		if (onlyOnce)
		{
			numberOfIterations = 1;
		}
		for (int i = 0; i < numberOfIterations; i++)
		{
			Vector3 forward = eye.transform.position - neck.transform.position;
			Vector3 toCamera = GameObject.Find("Camera 3 (Reels)").transform.position - neck.transform.position;

			Vector3 forwardXZ = new Vector3(forward.x, 0f, forward.z);
			Vector3 toCameraXZ = new Vector3(toCamera.x, 0f, toCamera.z);

			float angleXZ = Vector3.Angle(forwardXZ, toCameraXZ);
			Vector3 crossY = Vector3.Cross(forwardXZ, toCameraXZ);

			animalGeo.transform.Rotate(crossY, angleXZ);
			yield return null;
		}

		if (hasPlopped)
		{
			hasRotatedAfterPlop = true;
		}
	}

	public void updateState(State state, float delay = 0.0f)
	{
		updateRotation();

		if (delay > 0)
		{
			StartCoroutine(delayThenUpdateState(state, delay));
		}
		else
		{
			if (state == State.ANIM_DONE)
			{
				if (symbolCoroutine != null)
				{
					StopCoroutine(symbolCoroutine);
				}
				symbolAnimation.Play(ANIM_NAME_POSE);
			}
			else if (state == State.IDLE && animationState == State.ANIM_DONE)
			{
				if (symbolCoroutine != null)
				{
					StopCoroutine(symbolCoroutine);
				}
				symbolCoroutine = StartCoroutine(doIdleSymbolAnimations());
			}
			else if (state == State.HAPPY && (animationState != State.HAPPY))
			{
				if (symbolCoroutine != null)
				{
					StopCoroutine(symbolCoroutine);
				}
				symbolCoroutine = StartCoroutine(doHappySymbolAnimation());
			}
			animationState = state;
		}
	}

	private IEnumerator delayThenUpdateState(State state, float delay)
	{
		yield return new TIWaitForSeconds(delay);
		updateState(state);
	}

	private IEnumerator doHappySymbolAnimation()
	{
		float timeForHappyAnim = 0.0f;
		yield return null;
		foreach (AnimationState animState in symbolAnimation)
		{
			if (animState.name.Contains(ANIM_NAME_HAPPY))
			{
				timeForHappyAnim = animState.length;
				symbolAnimation.CrossFade(animState.name, .2f);
			}
		}

		yield return new TIWaitForSeconds(timeForHappyAnim);
		animationState = State.ANIM_DONE;
		updateState(State.IDLE);
	}

	private IEnumerator doIdleSymbolAnimations()
	{
		yield return null; // wait a frame so that the previous idle animation is actually finished.
		int idleAnim = Random.Range(0, 10);
		float waitLength = 1.0f;

		if (symbolAnimation == null)
		{
			Debug.LogWarning("Cannot doIdleSymbolAnimations for null symbolAnimation");
			yield break;
		}

		if (idleAnim < 8 || lastPlayedFidget < 3)
		{
			foreach (AnimationState animState in symbolAnimation)
			{
				if (animState.name.Contains(ANIM_NAME_IDLE))
				{
					symbolAnimation.Play(animState.name);
					waitLength = animState.length;
				}
			}
		}
		else
		{
			foreach (AnimationState animState in symbolAnimation)
			{
				if (animState.name.Contains(ANIM_NAME_FIDGET) || animState.name.Contains(ANIM_NAME_PECKING))
				{
					symbolAnimation.Play(animState.name);
					waitLength = animState.length;
					lastPlayedFidget = 0;
				}
			}
		}

		yield return new WaitForSeconds(waitLength);
		lastPlayedFidget++;

		if (animationState == State.IDLE)
		{
			animationState = State.ANIM_DONE;
			updateState(State.IDLE);
		}
	}
}
