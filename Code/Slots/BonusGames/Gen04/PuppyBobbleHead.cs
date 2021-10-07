using UnityEngine;
using System.Collections;

/// <summary>
/// Puppy bobble head, it bobbles the heads.
/// </summary>
public class PuppyBobbleHead : TICoroutineMonoBehaviour {

	public GameObject head;
	public UISprite headSprite;
	public UISprite eyesSprite;
	public UISprite bodySprite;

	public float bobbleSpeed = 2.25f;
	public float bobbleMaxAngle = 15.0f;

	private float timeDelta = 0f;
	private float randomBobVariance;

	private void Start()
	{
		// setup a random offset for the bob so the puppies dont all move in unison and be creepy like.
		timeDelta = Random.Range(0f, 1f);
		// setup small variance in the tilt speed
		randomBobVariance = Random.Range(0.8f, 1.1f);
		StartCoroutine(blink ());
	}

	/// <summary>
	/// makes the puppy blink. Sets a random time for puppy to blink again
	/// </summary>
	private IEnumerator blink()
	{
		yield return new WaitForSeconds(Random.Range(0.0f, 2.0f));
		eyesSprite.gameObject.SetActive(true);
		yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
		eyesSprite.gameObject.SetActive(false);
		yield return new WaitForSeconds(Random.Range(3.5f, 7f));
		StartCoroutine(blink());
	}

	private void Update()
	{
		timeDelta += Time.deltaTime;

		head.transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(timeDelta * (bobbleSpeed * randomBobVariance)) * bobbleMaxAngle);
	}
}
