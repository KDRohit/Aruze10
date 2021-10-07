using UnityEngine;
using System.Collections;

public class Zynga01BannerScript : BannerScript {

	public Animation[] goatAnimators;
	public Animation[] grassAnimations;
	public GameObject clickMeSign;
	public GameObject bonusSign;

	private PlayingAudio goatAudio;									// The audio that plays while the goats are doing their thing.
	private int goatsFinished = 0;									// The number of goats that have finished chomping away at the grass.

	private const float GOAT_BOUND_IN_TIME = 2.0f;
	private const float GOAT_MUNCH_TIME = 1.35f;
	private const float GOAT_BOUND_OUT_TIME = 2.0f;

	private const string GOAT_BOUND_IN_SOUND = "GoatBound1";				// Sound played when the goats start to head in.
	private const string GOAT_MUNCH_SOUND = "GoatChomp";					// Sound played when the goats start eating the grass.
	private const string GOAT_BOUND_OUT_SOUND = "GoatBound1";				// Sound played when the goats finish eating the grass and leave.

	void Awake()
	{
		for (int i=3; i < grassAnimations.Length; i += 4)
		{
			grassAnimations[i].gameObject.transform.position += new Vector3(0f, Random.Range(-.4f,.4f), 0f);
		}
	}

	public IEnumerator doSignSpin()
	{
		if (clickMeSign != null) // do these nested ifs in case sign was destroyed
		{
			iTween.RotateBy(clickMeSign, iTween.Hash("z", .04f, "easyType", iTween.EaseType.easeInCubic, "time", .25f));
			yield return new TIWaitForSeconds(.25f);
			if (clickMeSign != null)
			{
				iTween.RotateBy(clickMeSign, iTween.Hash("z", -.08, "easyType", iTween.EaseType.easeInCubic, "time", .5f));
				yield return new TIWaitForSeconds(.5f);
				if (clickMeSign != null)
				{
					iTween.RotateBy(clickMeSign, iTween.Hash("z", .04f, "easyType", iTween.EaseType.easeInCubic, "time", .25f));
				}
			}
		}
		yield return new TIWaitForSeconds(1f);
	}

	public void destroySigns()
	{
		Destroy (clickMeSign);
		Destroy (bonusSign);
	}

	public IEnumerator doAnimationAndReveal()
	{
		for (int i=0; i < goatAnimators.Length; i++)
		{
			StartCoroutine(doOneGoatAnimation(goatAnimators[i], i));
		}

		yield return new TIWaitForSeconds(GOAT_BOUND_IN_TIME + GOAT_MUNCH_TIME + GOAT_BOUND_OUT_TIME);
	}

	private IEnumerator doOneGoatAnimation(Animation goatAnimation, int goatIndex)
	{
		yield return new TIWaitForSeconds(Random.Range(0f,.5f));
		if (goatAudio == null)
		{
			goatAudio = Audio.play(GOAT_BOUND_IN_SOUND, 1f, 0f, 0f, float.PositiveInfinity);
		}
		goatAnimation.Play("bound");
		iTween.MoveTo(goatAnimation.gameObject, iTween.Hash("x", 1f, "time", GOAT_BOUND_IN_TIME, "islocal", true, "easetype", iTween.EaseType.linear));
		yield return new TIWaitForSeconds(GOAT_BOUND_IN_TIME-.15f);
		if (goatAudio != null && goatAudio.audioInfo.keyName == GOAT_BOUND_IN_SOUND)
		{
			Audio.stopSound(goatAudio);
			goatAudio = Audio.play(GOAT_MUNCH_SOUND, 1f, 0f, 0f, float.PositiveInfinity);
		}

		for (int i = 0; i < 4; i++)
		{
			grassAnimations[(goatIndex*4) + i].Play();
		}

		goatAnimation.Play("munch");
		iTween.MoveTo(goatAnimation.gameObject, iTween.Hash("x", -1f, "time", GOAT_MUNCH_TIME, "islocal", true, "easetype", iTween.EaseType.linear));

		yield return new TIWaitForSeconds(GOAT_MUNCH_TIME);
		goatsFinished++;
		if (goatAudio != null && goatsFinished == goatAnimators.Length)
		{
			Audio.stopSound(goatAudio);
			goatAudio = Audio.play(GOAT_BOUND_OUT_SOUND, 1f, 0f, 0f, float.PositiveInfinity);
		}

		goatAnimation.Play("bound");
		iTween.MoveTo(goatAnimation.gameObject, iTween.Hash("x", -14f, "time", GOAT_BOUND_OUT_TIME, "islocal", true, "easetype", iTween.EaseType.linear));

		
		yield return new TIWaitForSeconds(GOAT_BOUND_OUT_TIME);

		Audio.stopSound(goatAudio);
		Destroy (goatAnimation.gameObject);
	}
}
