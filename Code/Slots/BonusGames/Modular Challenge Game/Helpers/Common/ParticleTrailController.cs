using UnityEngine;
using System.Collections;

/*
Wraps code to play a particle trail going from one transform to another
*/
public class ParticleTrailController : TICoroutineMonoBehaviour
{
	// Allows the use of more than one ParticleTrailInfo tagged for the modules that will use that version,
	// if the specific version isn't defined then it you will use the basic Default version
	// If you want to use ParticleTrailController outside of the Modular Challenge game system, then just use Default
	public enum ParticleTrailControllerType
	{
		Default = 0,
		Advance = 1,
		IncreasePicks = 2,
		Multiplier = 3,
		Jackpot = 4,
		Bad = 5,
		Gameover = 6
	}

	public ParticleTrailControllerType particleTrailControllerType = ParticleTrailControllerType.Default;
	[SerializeField] private GameObject particleTrailEffectPrefab = null; 	// Prefab for the particle trail effect
	[SerializeField] private bool isUsingCachingForTrailEffectPrefab = true;// tells if cached versions should be used, or if just the original should be used
	[SerializeField] private Transform spawnLocation = null;				// The position where the particle trail will spawn, for instance, if it needs to line up with a specific object
	[SerializeField] private bool shouldTreatZCoordLikeLocalCoord = false;  // If the trail and explosion are separate prefabs in the project, you should probably treat the z-coord like a local coord. 
	[SerializeField] private bool isUsingTime = true;						// tells if we are using time or speed to determine how fast the trail moves
	[SerializeField] private float duration = 1.0f;							// duration it will take for the trail to arrive
	[SerializeField] private float speed = 1.0f;							// if using speed instead of duration then this is the speed that the trail will travel
	[SerializeField] private float delay = 0.0f;							// time the trail will wait before starting to move
	[SerializeField] private iTween.EaseType easeType = iTween.EaseType.easeInCubic; // ease type to use when doing the movement of the trail
	[SerializeField] private float TIME_AFTER_ARRIVE_BEFORE_HIDE = 0.0f;	// Sometimes we might want the particle trail to hang around for a little bit before hiding
	[SerializeField] private bool doesTrailHaveBuiltInArriveEffect = false; // The arrive effect isn't separate from the trail, it's built-in to the trail eg the hawaii02 Picking Game.
	[SerializeField] private GameObject particleTrailArriveEffectPerfab = null; // Effect that will happen when the trail arrives, not required
	[SerializeField] private bool keepAnimatingWhileParticleTrailEnds = false; //lets the animation sequence continue while the particle trail waits to be deactivated
	[SerializeField] private string TRAIL_ARRIVE_EFFECT_ANIM = "anim";			// Name of the animation on the trail arrive effect
	[SerializeField] private float arriveDurOverride = 0.0f;                    // You don't have to wait for the animation to finish.
	[SerializeField] private bool turnArriveEffectOnAndOff = false; // If the arrive effect is something like a particle effect use this flag to toggle it on and off over TIME_AFTER_ARRIVE_BEFORE_RELEASE
	[SerializeField] private float TIME_AFTER_ARRIVE_BEFORE_RELEASE = 0.0f;	// If the coroutine needs to return before hiding the explosion effect (to mask a transition)
	[SerializeField] private bool shouldInstantiateCopyOfArriveEffect = true;
	[SerializeField] private AudioListController.AudioInformationList trailStartSounds = null;
	[SerializeField] private AudioListController.AudioInformationList trailTravelSounds = null;
	[SerializeField] private AudioListController.AudioInformationList trailArriveSounds = null; 
	[SerializeField] private bool pointSparkleAtDestination = false;			// if true trail will be rotated to point at destination vector
	[SerializeField] private bool isSettingEffectScaleToVector3One = true;	// Most games want the effect to be at scale (1,1,1) epscially if instantiated and reparented, but if not and the scale is already correct, just keep the scale as is 

	private Vector3 originalRotation;

	private GameObjectCacher particleTrailEffectCacher = null;
	private GameObjectCacher particleTrailArriveEffectCacher = null;
	private Vector3 particleTrailEffectOriginalPosition = Vector3.zero; // need to save this out since the transform of the prefab object will be modified when it moves
	
	public void Awake()
	{
		if (isUsingCachingForTrailEffectPrefab)
		{
			particleTrailEffectCacher = new GameObjectCacher(this.gameObject, particleTrailEffectPrefab);
		}
		else if (!isUsingCachingForTrailEffectPrefab && particleTrailEffectPrefab != null && spawnLocation == null)
		{
			// if we are using the prefab object directly, and a spawnLocation is not defined, assume the location is where the object is starting
			particleTrailEffectOriginalPosition = particleTrailEffectPrefab.transform.position;
		}
		else if (!isUsingCachingForTrailEffectPrefab && particleTrailEffectPrefab == null)
		{
			Debug.LogError("ParticleTrailController.Awake() - Not using caching for trail effect, but particleTrailEffectPrefab is null!");
		}

		if (spawnLocation == null)
		{
			spawnLocation = transform;
		}

		if (particleTrailArriveEffectPerfab != null)
		{
			particleTrailArriveEffectCacher = new GameObjectCacher(this.gameObject, particleTrailArriveEffectPerfab);
		}
	}

	// Tells if this particle trail controller is setup to be used
	public bool isSetup()
	{
		return particleTrailEffectPrefab != null;
	}

	private void resetSparkleRotation(GameObject sparkle) 
	{
		sparkle.transform.eulerAngles = originalRotation;
	}	

	// Handle animating the particle trail using the spawnLocation as the fromPos and then the passed data for the toPos and parent
	public IEnumerator animateParticleTrail(Vector3 toPos, Transform parent, GenericIEnumeratorDelegate onArriveCallback = null)
	{
		Vector3 fromPos = Vector3.zero;

		if (!isUsingCachingForTrailEffectPrefab && spawnLocation == null)
		{
			fromPos = particleTrailEffectOriginalPosition;
		}
		else if (spawnLocation != null)
		{
			fromPos = particleTrailEffectOriginalPosition = spawnLocation.position;
		}
		else
		{
			Debug.LogError("particleTrailController.animateParticleTrail() - Can't automatically determine fromPos, bailing out!");
			yield break;
		}

		yield return StartCoroutine(animateParticleTrail(fromPos, toPos, parent, onArriveCallback));
	}

	// Handle animating the particle trail using the passed positions (will ignore spawnLocation)
	public IEnumerator animateParticleTrail(Vector3 fromPos, Vector3 toPos, Transform parent, GenericIEnumeratorDelegate onArriveCallback = null)
	{
		GameObject particleTrail;
		if (isUsingCachingForTrailEffectPrefab)
		{
			particleTrail = particleTrailEffectCacher.getInstance();
		}
		else
		{
			particleTrail = particleTrailEffectPrefab;
		}

		particleTrail.transform.parent = parent;
		float trailZPos = particleTrailEffectPrefab.transform.position.z;
		particleTrail.transform.position = new Vector3(fromPos.x, fromPos.y, trailZPos);
		
		if (shouldTreatZCoordLikeLocalCoord)
		{
			// If the trail and explosion are separate prefabs in the project,
			// then their positions are in absolute coordinates that don't make sense in the scene at all.
			// The x,y don't matter because they get set to the spawn location,
			// but you should probably treat the absolute z-coordinate like a local z-coordinate.
			
			particleTrail.transform.localPosition =
				new Vector3(
					particleTrail.transform.localPosition.x,
					particleTrail.transform.localPosition.y,
					trailZPos);

			// Now that the local position has been set to the local z-coordinate,
			// you can safely get the absolute z-coordinate from the absolute position.
			
			trailZPos = particleTrail.transform.position.z;
		}
		
		toPos = new Vector3(toPos.x, toPos.y, trailZPos);

		if (isSettingEffectScaleToVector3One)
		{
			particleTrail.transform.localScale = Vector3.one;
		}

		particleTrail.SetActive(true);

		if (pointSparkleAtDestination)
		{
			originalRotation = CommonGameObject.lookAt(particleTrail, toPos);
		}

		yield return StartCoroutine(AudioListController.playListOfAudioInformation(trailStartSounds));

		yield return StartCoroutine(AudioListController.playListOfAudioInformation(trailTravelSounds));

		if (isUsingTime)
		{
			yield return new TITweenYieldInstruction(iTween.MoveTo(particleTrail, iTween.Hash("position", toPos, "delay", delay, "time", duration, "easetype", easeType)));
		}
		else
		{
			yield return new TITweenYieldInstruction(iTween.MoveTo(particleTrail, iTween.Hash("position", toPos, "delay", delay, "speed", speed, "easetype", easeType)));
		}

		if (onArriveCallback != null)
		{
			yield return StartCoroutine(onArriveCallback());
		}

		yield return StartCoroutine(AudioListController.playListOfAudioInformation(trailArriveSounds));

		if (doesTrailHaveBuiltInArriveEffect)
		{
			StartCoroutine(releaseParticleTrailWithDelay(particleTrail, TIME_AFTER_ARRIVE_BEFORE_HIDE));
			yield break;
		}
		else if(keepAnimatingWhileParticleTrailEnds) 
		{
			StartCoroutine(releaseParticleTrailWithDelay(particleTrail, TIME_AFTER_ARRIVE_BEFORE_HIDE));
		}
		else
		{
			if (TIME_AFTER_ARRIVE_BEFORE_HIDE != 0.0f)
			{
				yield return new TIWaitForSeconds(TIME_AFTER_ARRIVE_BEFORE_HIDE);
			}
			
			releaseParticleTrailEffects(particleTrail);
		}
	
		if (particleTrailArriveEffectPerfab != null)
		{
			// we have a trail arrive effect to play
			GameObject arriveEffect = particleTrailArriveEffectPerfab;

			if (shouldInstantiateCopyOfArriveEffect && particleTrailArriveEffectCacher != null)
			{
				arriveEffect = particleTrailArriveEffectCacher.getInstance();
				arriveEffect.transform.parent = parent;
				arriveEffect.transform.position = toPos;
				arriveEffect.transform.localScale = Vector3.one;
				arriveEffect.SetActive(true);
			}

			// Handle just toggling the object on
			if (turnArriveEffectOnAndOff)
			{
				arriveEffect.transform.position = toPos;
				arriveEffect.SetActive(true);
			}

			// Handle animation
			Animator animator = arriveEffect.GetComponent<Animator>();
			if (animator != null)
			{
				if (arriveDurOverride > 0.0f)
				{
					animator.Play(TRAIL_ARRIVE_EFFECT_ANIM);
					yield return new TIWaitForSeconds(arriveDurOverride);
				}
				else
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(animator, TRAIL_ARRIVE_EFFECT_ANIM));
				}
			}

			// release the effect asset after an optional delay, allowing the function to return
			if (shouldInstantiateCopyOfArriveEffect)
			{
				if (TIME_AFTER_ARRIVE_BEFORE_RELEASE > 0.0f)
				{
					StartCoroutine(releaseTrailArriveEffectWithDelay(arriveEffect, TIME_AFTER_ARRIVE_BEFORE_RELEASE));
				}
				else
				{
					// release without waiting a frame
					if (particleTrailArriveEffectCacher != null)
					{
						particleTrailArriveEffectCacher.releaseInstance(arriveEffect);
					}
				}
			}
			else if (turnArriveEffectOnAndOff)
			{
				if (TIME_AFTER_ARRIVE_BEFORE_RELEASE > 0.0f)
				{
					yield return new TIWaitForSeconds(TIME_AFTER_ARRIVE_BEFORE_RELEASE);
				}

				arriveEffect.SetActive(false);
			}
		}
	}

	public IEnumerator releaseParticleTrailWithDelay(GameObject particleTrail, float delay = 0.0f)
	{
		if (delay > 0.0f)
		{
			yield return new TIWaitForSeconds(delay);
		}
		
		releaseParticleTrailEffects(particleTrail);
	}
	
	public void releaseParticleTrailEffects(GameObject particleTrail)
	{
		// reset all the particle systems for when this object is used again
		CommonGameObject.clearAllParticleSystemsAndTrailRenderers(particleTrail);

		if (pointSparkleAtDestination)
		{
			resetSparkleRotation(particleTrail);
		}

		if (isUsingCachingForTrailEffectPrefab)
		{
			particleTrailEffectCacher.releaseInstance(particleTrail);
		}
		else
		{
			particleTrail.SetActive(false);
			particleTrail.transform.position = particleTrailEffectOriginalPosition;
		}
	}
	
	// Release a trail arrive effect with an optional delay
	public IEnumerator releaseTrailArriveEffectWithDelay(GameObject effect, float delay = 0.0f)
	{
		yield return new TIWaitForSeconds(delay);
		particleTrailArriveEffectCacher.releaseInstance(effect);
	}

	// Search for the ParticleTrailController with the matching type, will fallback to trying to return Default if that type doesn't exist
	public static ParticleTrailController getParticleTrailControllerForType(GameObject gameObject, ParticleTrailControllerType type)
	{
		ParticleTrailController[] particleTrailControllerArray = gameObject.GetComponents<ParticleTrailController>();
		ParticleTrailController defaultParticleTrailController = null;

		foreach (ParticleTrailController particleTrailController in particleTrailControllerArray)
		{
			if (particleTrailController.particleTrailControllerType == type)
			{
				return particleTrailController;
			}
			else if (particleTrailController.particleTrailControllerType == ParticleTrailControllerType.Default)
			{
				defaultParticleTrailController = particleTrailController;
			}
		}

		return defaultParticleTrailController;
	}

	public void addArrivalSoundToList(AudioListController.AudioInformation arriveSound)
	{
		trailArriveSounds.audioInfoList.Add(arriveSound);
	}

	public void removeArrivalSound(AudioListController.AudioInformation arriveSound)
	{
		trailArriveSounds.audioInfoList.Remove(arriveSound);
	}
}
