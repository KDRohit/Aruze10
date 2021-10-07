using UnityEngine;
using System.Collections;

/**
 * Common module class for displaying a sparkle trail on items
 */
public class ChallengeGameSparkleTrailModule : PickingGameModule
{
	public GameObject 	sparkleTrailPrefab; // the particle prefab to use for the trail
	public GameObject	targetEndGO;	// target for the trail to fly to
	public GameObject	targetBurst;	// prefab to instantiate when arriving at the target

	[SerializeField] private bool shouldActivate = true;
	[SerializeField] private bool shouldClearParticlesWhenTurnedOff = true;
	[SerializeField] private string START_AUDIO = "scatter_multiplier_travel";
	[SerializeField] private string END_AUDIO = "scatter_multiplier_arrive";
	[SerializeField] protected string[] SPARKLE_TRAIL_START_ANIMS; 
	[SerializeField] protected string[] SPARKLE_TRAIL_END_ANIMS; 
	[SerializeField] private iTween.EaseType easeType = iTween.EaseType.easeInCubic;
	[SerializeField] private float SPARKLE_DURATION = 1f;
	[SerializeField] private float WAIT_AFTER_SPARKLE_TIME = 0.2f;

	private GameObject sparkleTrailInstance;
	private int numFound = 0;

	public override bool needsToExecuteOnItemClick(ModularChallengeGameOutcomeEntry pickData)
	{
		return true;
	}

	public override IEnumerator executeOnItemClick(PickingGameBasePickItem pickItem)
	{
		yield return StartCoroutine(doSparkleTrail(pickItem.gameObject, targetEndGO));
	}

	private string getStartAnimation()
	{
		if (numFound >= SPARKLE_TRAIL_START_ANIMS.Length)
		{
			return null;
		}
		else
		{
			return SPARKLE_TRAIL_START_ANIMS[numFound];
		}

	}

	private string getEndAnimation()
	{
		if (numFound >= SPARKLE_TRAIL_END_ANIMS.Length)
		{
			return null;
		}
		else
		{
			return SPARKLE_TRAIL_END_ANIMS[numFound];
		}
	}

	// Animate a sparkle trail with full FX
	public IEnumerator doSparkleTrail(GameObject startPos = null, GameObject endPos = null, bool ignoreZValue = false, int soundPostfix = -1, bool positionBurstAtTrailEnd = false)
	{
		Audio.play(Audio.soundMap(START_AUDIO));

		if (sparkleTrailInstance == null)
		{
			sparkleTrailInstance = CommonGameObject.instantiate(sparkleTrailPrefab) as GameObject;
		}

		if (shouldActivate)
		{
			sparkleTrailInstance.SetActive(true);
		}

		// There are cases (layering issues) where you don't want the sparkle trail Z to be modified
		Vector3 finalStartPosition = new Vector3();
		finalStartPosition.x = startPos.transform.position.x;
		finalStartPosition.y = startPos.transform.position.y;

		if (ignoreZValue)
		{
			finalStartPosition.z = sparkleTrailInstance.transform.position.z;
		}
		else
		{
			finalStartPosition.z = startPos.transform.position.z;
		}

		// Assign the position
		sparkleTrailInstance.transform.position = finalStartPosition;


		if (!string.IsNullOrEmpty(getStartAnimation()))
		{
			sparkleTrailInstance.GetComponent<Animator>().Play(getStartAnimation());
		}


		// There are casses (layering issues) where you don't want the sparkle trail Z to be modified
		Vector3 finalEndPosition = new Vector3();
		finalEndPosition.x = endPos.transform.position.x;
		finalEndPosition.y = endPos.transform.position.y;

		// Only set the z value if ignoreZValue is false
		if (ignoreZValue)
		{
			finalEndPosition.z = sparkleTrailInstance.transform.position.z;
		}
		else
		{
			finalEndPosition.z = endPos.transform.position.z;
		}
		if (shouldClearParticlesWhenTurnedOff)
		{
			foreach (ParticleSystem particles in sparkleTrailInstance.GetComponentsInChildren<ParticleSystem>())
			{
				particles.Clear();
			}
		}
		yield return new TITweenYieldInstruction(iTween.MoveTo(
			sparkleTrailInstance, iTween.Hash(
			"position", finalEndPosition,
			"islocal", false,
			"time", SPARKLE_DURATION,
			"easetype", easeType
		)
		));

		if (!string.IsNullOrEmpty(getEndAnimation()))
		{
			sparkleTrailInstance.GetComponent<Animator>().Play(getEndAnimation());
		}

		Audio.play(Audio.soundMap(END_AUDIO));

		// if a final burst is defined, play it
		if (targetBurst != null)
		{
			GameObject burstInstance = GameObject.Instantiate<GameObject>(targetBurst);
			burstInstance.SetActive(true);

			// parent this to the bonus game and restore the original offsets
			Vector3 originalPosition = burstInstance.transform.localPosition;
			Vector3 originalScale = burstInstance.transform.localScale;
			burstInstance.transform.parent = roundVariantParent.gameParent.transform;
			// for burst animations that can vary position, optionally place them at the trail end
			if (positionBurstAtTrailEnd)
			{
				burstInstance.transform.position = finalEndPosition;
			}
			else
			{
				burstInstance.transform.localPosition = originalPosition;
			}
			burstInstance.transform.localScale = originalScale;
		}

		yield return StartCoroutine(waitAfterSparkleEffects());

		if (shouldActivate)
		{
			sparkleTrailInstance.SetActive(false);
		}
	}

	// Allow sparkle effects to finish
	public IEnumerator waitAfterSparkleEffects()
	{
		yield return new TIWaitForSeconds(WAIT_AFTER_SPARKLE_TIME);
	}
}
