using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// This is a new class that handles animating particle effects. 
// This improves on our old particleTrailController in several ways. 
// The most important is its awareness of the bonus spin panel for positioning 
// effects on to of the win label or spins remaining. It is able to do so by using 
// multiple camera to convert screen space to world space allowing particle effects 
// to be positioned properly.
//
// You can also add AnimationInformationListComponent to your particle effect and those
// will be played at the start.
//
// It can layer particle effects easily by added the layeredZOffset and can handle multiple 
// particle effects simultaneously
//
// Author : Nick Saito <nsaito@zynga.com>
// Sept 16, 2018
//

public class AnimatedParticleEffect : TICoroutineMonoBehaviour
{
	#region public properties
	// Particle Settings
	public GameObject particleEffectPrefab;
	public float particleEffectStartDelay;
	public float particleEffectDestroyDelay;
	public float layeredZOffset;
	public bool loopParticleEffect;
	public bool waitForAnimationListComplete;
	public bool isBlocking;
	public bool overrideStartingScale;
	public Vector3 newDefaultScale;

	// Particle Translate Settings
	public bool enableTranslation;
	public iTween.EaseType translateEaseType;
	public float translateTime;
	public float translateDelay;
	public float translateZOffset;
	public bool waitForTranslationComplete = true;
	
	public Transform[] inbetweenTransforms;
	public bool useStartTransformCameraForInbetween = true;

	// Particle Positions
	public Transform translateStartTransform;
	public bool useUIObjectAsStartPosition;
	public UIObjectPosition uiStartPosition;
	public Transform translateEndTransform;
	public bool useUIObjectAsEndPosition;
	public UIObjectPosition uiEndPosition;
	
	// Particle Cameras
	public Camera particleEffectCamera;
	public bool useUICameraForParticleEffect;
	public Camera startPositionCamera;
	public bool useUICameraForStartPosition;
	public Camera endPositionCamera;
	public bool useUICameraForEndPosition;

	// Particle Audio
	public AudioListController.AudioInformationList particleEffectSounds;

	// Chained Particle Effect
	public List<AnimatedParticleEffect> chainedParticleEffects;
	public bool playChainedEffectsLast;
	public bool waitForChainedEffectsToComplete;
	public bool useEndPositionForChainedEffectStartPosition;

	// Events
	// For some reason, with the Editor script, these need to be explicitly SerializeField
	[SerializeField] public UnityEvent particleEffectStartedEvent;
	[SerializeField] public UnityEvent particleEffectCompleteEvent;

	// This is called immediately after the particle effect prefab is instantiated and provides a reference to the
	// particle effect that can be used to manipulate any of the components, labels, or otherwise.
	[System.Serializable] public class ParticleEffectStartedPrefabEvent : UnityEvent <GameObject> {}
	public ParticleEffectStartedPrefabEvent particleEffectStartedPrefabEvent;

	public float particleEffectCompleteEventDelay;
	
	public enum UIObjectPosition
	{
		SPIN_PANEL_WIN_BOX,
		SPIN_PANEL_COUNT_BOX,
		XP_BAR_STAR,
		BET_AMOUNT
	}
	#endregion

	#region private properties
	// Privates
	private Vector3 translateStartPosition;
	private Vector3 translateEndPosition;
	private GameObjectCacher particleEffectCache = null;
	private float totalLayeredZOffset = 0f;
	private List<GameObject> animatingParticleEffects = new List<GameObject>();
	#endregion

	#region methods

	void Start()
	{
		particleEffectCache = new GameObjectCacher(this.gameObject, particleEffectPrefab);
	}

	public IEnumerator animateParticleEffect(Transform startTransform = null, Transform endTransform = null)
	{
		// Check if this object is actually turned on, and if not abort trying to play this
		// effect so that the we don't get stuck waiting on particle effects that aren't going
		// to finish
		if (!gameObject.activeInHierarchy)
		{
			Debug.LogWarning("AnimatedParticleEffect.animateParticleEffect() - gameObject: " + gameObject.name + " wasn't active, so coroutine wouldn't finish.  Skipping this particle effect so the game doesn't freeze!");
			yield break;
		}
		
		initCameras();
		initStartEndTransforms(startTransform, endTransform);

		if (isBlocking)
		{
			yield return StartCoroutine(animateParticleEffectInternal());
		}
		else
		{
			StartCoroutine(animateParticleEffectInternal());
		}
	}

	private IEnumerator animateParticleEffectInternal()
	{
		if (particleEffectStartDelay > 0.0f)
		{
			yield return new TIWaitForSeconds(particleEffectStartDelay);
		}

		// create our particle effect
		GameObject particleEffect = createParticleEffect();

		particleEffectStartedEvent.Invoke();
		particleEffectStartedPrefabEvent.Invoke(particleEffect);

		List<TICoroutine> coroutineList = new List<TICoroutine>();

		// start playing sounds
		coroutineList.Add(StartCoroutine(AudioListController.playListOfAudioInformation(particleEffectSounds)));

		// This will fire off the chained particle effects right at the beginning
		if (!playChainedEffectsLast)
		{
			coroutineList.Add(StartCoroutine(playChainedParticleEffects()));
		}

		// start any animations that are on the particle effect
		AnimationInformationListComponent animationComponent = particleEffect.GetComponent<AnimationInformationListComponent>();
		if (animationComponent != null)
		{
			coroutineList.Add(StartCoroutine(AnimationListController.playListOfAnimationInformation(animationComponent.animationInformationList)));
		}

		// move the particle effect across the screen and wait for it to finish
		if (enableTranslation)
		{
			coroutineList.Add(StartCoroutine(animateTranslation(particleEffect)));
		}

		yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));

		// releasing the particleEffect back to the pool (looped particle effects must be stopped manually)
		if (!loopParticleEffect)
		{
			releaseParticleEffect(particleEffect);
		}

		// This will fire off the chained particle effects after the current particleEffect is complete
		if (playChainedEffectsLast)
		{
			yield return StartCoroutine(playChainedParticleEffects());
		}

		if (particleEffectCompleteEventDelay > 0.0f)
		{
			yield return new TIWaitForSeconds(particleEffectCompleteEventDelay);
		}

		particleEffectCompleteEvent.Invoke();
	}

	private IEnumerator animateTranslation(GameObject particleEffect)
	{
		if (translateDelay > 0.0f)
		{
			yield return new TIWaitForSeconds(translateDelay);
		}

		List<TICoroutine> coroutineList = new List<TICoroutine>();
		coroutineList.Add(StartCoroutine(animateParticleTrailToEndPosition(particleEffect)));
		if (waitForTranslationComplete)
		{
			yield return StartCoroutine(Common.waitForCoroutinesToEnd(coroutineList));
		}
	}

	public void stopAllParticleEffects()
	{
		if (animatingParticleEffects != null)
		{
			List<GameObject> tempAnimatingParticleEffects = new List<GameObject>(animatingParticleEffects);

			foreach (GameObject particleEffect in tempAnimatingParticleEffects)
			{
				releaseParticleEffect(particleEffect);
			}

			tempAnimatingParticleEffects.Clear();
		}
	}

	private void initStartEndTransforms(Transform startTransform, Transform endTransform)
	{
		if (startTransform != null)
		{
			translateStartTransform = startTransform;
		}
		else if (useUIObjectAsStartPosition)
		{
			translateStartTransform = getTransformFromUIObject(uiStartPosition);
		}

		if (endTransform != null)
		{
			translateEndTransform = endTransform;
		}
		else if (useUIObjectAsEndPosition)
		{
			translateEndTransform = getTransformFromUIObject(uiEndPosition);
		}

		if (translateStartTransform == null)
		{
			Debug.LogError("need to specify start transform on " + gameObject.name);
		}

		if (translateEndTransform == null && enableTranslation)
		{
			Debug.LogError("need to specify end transform on " + gameObject.name);
		}
	}

	private void initCameras()
	{
		bool isBonusGame = BonusGameManager.instance != null;
		Camera uiCamera = null;

		if (isBonusGame)
		{
			uiCamera = BonusGameManager.instance.GetComponentInParent<Camera>();
		}
		else
		{
			uiCamera = SpinPanel.instance.GetComponentInParent<Camera>();
		}

		if (uiCamera == null)
		{
			Debug.LogError("uiCamera is null on " + gameObject.name);
		}

		if (particleEffectCamera == null || useUICameraForParticleEffect)
		{
			particleEffectCamera = uiCamera;
		}

		if (startPositionCamera == null || useUICameraForStartPosition)
		{
			startPositionCamera = uiCamera;
		}

		if (endPositionCamera == null || useUICameraForEndPosition)
		{
			endPositionCamera = uiCamera;
		}
	}

	private Transform getTransformFromUIObject(UIObjectPosition objectPositionToUse)
	{
		bool isBonusGame = BonusGameManager.instance != null;
		switch (objectPositionToUse)
		{
			case UIObjectPosition.SPIN_PANEL_WIN_BOX:
				if (isBonusGame)
				{
					return BonusSpinPanel.instance.winningsBackgroundTransform;
				}
				else
				{
					return SpinPanel.instance.winningsAmountLabel.transform;
				}
			
			case UIObjectPosition.SPIN_PANEL_COUNT_BOX:
				return BonusSpinPanel.instance.spinCountLabel.transform;
			
			case UIObjectPosition.XP_BAR_STAR:
				return Overlay.instance.top.xpUI.starObject.transform;
			
			case UIObjectPosition.BET_AMOUNT:
				if (isBonusGame)
				{
					return BonusSpinPanel.instance.betAmountLabel.transform;
				}
				else
				{
					return SpinPanel.instance.totalBetAmountLabel.transform;				
				}
		}

		return null;
	}

	private IEnumerator playChainedParticleEffects()
	{
		if (chainedParticleEffects != null)
		{
			List<TICoroutine> chainedEffectCoroutineList = new List<TICoroutine>();

			foreach (AnimatedParticleEffect chainedEffect in chainedParticleEffects)
			{
				if (useEndPositionForChainedEffectStartPosition)
				{
					chainedEffectCoroutineList.Add(StartCoroutine(chainedEffect.animateParticleEffect(translateEndTransform)));
				}
				else
				{
					chainedEffectCoroutineList.Add(StartCoroutine(chainedEffect.animateParticleEffect()));
				}
			}

			if (waitForChainedEffectsToComplete)
			{
				yield return StartCoroutine(Common.waitForCoroutinesToEnd(chainedEffectCoroutineList));
			}
		}
	}

	//calculate start and end positions for the particle relative to the the start and end cameras
	private Vector3 getPositionFromTransform(Camera targetCamera, Transform targetTransform)
	{
		if (targetCamera == null)
		{
			Debug.LogError("targetCamera is null on " + gameObject.name + " " + useUIObjectAsStartPosition + " " + useUICameraForStartPosition);
			return Vector3.zero;
		}

		if (targetTransform == null)
		{
			Debug.LogError("targetTransform is null on " + gameObject.name);
			return Vector3.zero;
		}

		if (particleEffectCamera == null)
		{
			Debug.LogError("particleEffectCamera is null on " + gameObject.name);
			return Vector3.zero;
		}

		Vector3 screenPosition = targetCamera.WorldToScreenPoint(targetTransform.position);
		Vector3 particlePosition = particleEffectCamera.ScreenToWorldPoint(screenPosition);
		totalLayeredZOffset += layeredZOffset;
		particlePosition.z = translateZOffset + totalLayeredZOffset;
		return particlePosition;
	}

	private GameObject createParticleEffect()
	{
		translateStartPosition = getPositionFromTransform(startPositionCamera, translateStartTransform);
		if (particleEffectCache == null)
		{
			particleEffectCache = new GameObjectCacher(this.gameObject, particleEffectPrefab);
		}
		GameObject particleEffect = particleEffectCache.getInstance();
		animatingParticleEffects.Add(particleEffect);
		particleEffect.transform.parent = gameObject.transform;
		if (overrideStartingScale)
		{
			particleEffect.transform.localScale = newDefaultScale;
		}

		particleEffect.transform.SetPositionAndRotation(translateStartPosition, Quaternion.identity);
		particleEffect.SetActive(true);
		return particleEffect;
	}

	// Function to handle animating the particle trail to the end position
	private IEnumerator animateParticleTrailToEndPosition(GameObject particleEffect)
	{
		translateEndPosition = getPositionFromTransform(endPositionCamera, translateEndTransform);
		List<ITIYieldInstruction> particleEffectInstructions = new List<ITIYieldInstruction>();
		if (inbetweenTransforms != null && inbetweenTransforms.Length >= 1)
		{			
			particleEffectInstructions.Add(new TITweenYieldInstruction(iTween.MoveTo(particleEffect,
				iTween.Hash("path", buildFullTweenPath(),
					"delay", translateDelay,
					"time", translateTime,
					"easetype", translateEaseType))));
		}
		else
		{
			particleEffectInstructions.Add(new TITweenYieldInstruction(iTween.MoveTo(particleEffect,
				iTween.Hash("position", translateEndPosition,
					"delay", translateDelay,
					"time", translateTime,
					"easetype", translateEaseType))));
		}

		yield return StartCoroutine(Common.waitForITIYieldInstructionsToEnd(particleEffectInstructions));
	}

	// handle releasing of the particle effect back into the pool
	private void releaseParticleEffect(GameObject particleEffect)
	{
		if (particleEffectDestroyDelay > 0.0f)
		{
			StartCoroutine(releaseParticleTrailWithDelay(particleEffect, particleEffectDestroyDelay));
		}
		else
		{
			releaseParticleTrailImmediate(particleEffect);
		}
	}

	private IEnumerator releaseParticleTrailWithDelay(GameObject particleTrail, float delay)
	{
		yield return new TIWaitForSeconds(delay);
		releaseParticleTrailImmediate(particleTrail);
	}

	private void releaseParticleTrailImmediate(GameObject particleEffect)
	{
		// reset all the particle systems for when this object is used again
		if (enableTranslation)
		{
			iTween.Stop(particleEffect);
		}
		
		CommonGameObject.clearAllParticleSystemsAndTrailRenderers(particleEffect);
		particleEffectCache.releaseInstance(particleEffect);

		if (animatingParticleEffects.Contains(particleEffect))
		{
			animatingParticleEffects.Remove(particleEffect);
		}
	}

	private Vector3[] buildFullTweenPath()
	{
		List<Vector3> fullTweenPath = new List<Vector3>();
		fullTweenPath.Add(translateStartPosition);
		Camera inBetweenCamera = useStartTransformCameraForInbetween ? startPositionCamera : endPositionCamera;
		for (int i = 0; i < inbetweenTransforms.Length; i++)
		{
			// add check here to prevent error log spam "targetTransform is null on"
			if (inbetweenTransforms[i] != null)
			{
				fullTweenPath.Add(getPositionFromTransform(inBetweenCamera, inbetweenTransforms[i]));
			}
		}
		fullTweenPath.Add(translateEndPosition);
		return fullTweenPath.ToArray();
	}

	void OnDrawGizmos() 
	{
		if (Application.isPlaying)
		{
			if (inbetweenTransforms.Length > 0)
			{
				iTween.DrawPath(buildFullTweenPath(), Color.white);
			}
		}
	}
	
	#endregion
}
