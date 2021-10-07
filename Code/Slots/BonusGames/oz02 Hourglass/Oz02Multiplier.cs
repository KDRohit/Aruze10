using UnityEngine;
using System.Collections;

public class Oz02Multiplier : TICoroutineMonoBehaviour
{
	[SerializeField()] private UILabelStyler styler;
	[SerializeField()] private UILabelStyle onStyle;
	[SerializeField()] private UILabelStyle offStyle;
	[SerializeField()] private GameObject trailPrefab;
	[SerializeField()] private GameObject flamePrefab;
	[SerializeField()] private GameObject explosionPrefab;
	[SerializeField()] private GameObject fizzlePrefab;

	private const string FIRE_IGNITE = "HG_ignite_multiplier";					// Name of Sound played when the fire lights.
	private const string FIRE_MOVE = "HG_fire_multiplier";						// Name of Sound when the fire is trail is turned on.
	private const string EXPLODE = "HG_explode_result";							// Name of sound played when there is an explositon

	public void init(int multiplier)
	{
	    styler.labelWrapper.text = Localize.text("{0}X", multiplier);
	    styler.style = offStyle;
	    // Make sure everything is inactive.
	    flamePrefab.SetActive(false);
	    explosionPrefab.SetActive(false);
	    fizzlePrefab.SetActive(false);
	}

	public void setActive()
	{        
		Audio.play(FIRE_IGNITE);
		this.StartCoroutine(explode(false));
		styler.gameObject.SetActive(true);
		flamePrefab.SetActive(true);
		styler.style = onStyle;
	}

	public void hide()
	{
		styler.gameObject.SetActive(false);
		flamePrefab.SetActive(false);
	}

	public void turnOnTrail()
	{
		Audio.play(FIRE_MOVE);
		flamePrefab.SetActive(false);
		trailPrefab.SetActive(true);
	}

	public IEnumerator fizzle()
	{
		styler.style = onStyle;
		flamePrefab.SetActive(false);
		fizzlePrefab.SetActive(true);
		yield return new TIWaitForSeconds(1.0f);
		fizzlePrefab.SetActive(false);
		styler.style = offStyle;
	}

	// Set active does the same animation but shouldn't play the explosion sound.
	public IEnumerator explode(bool withSound = true)
	{
		if (withSound)
		{
			Audio.play(EXPLODE);
		}
		styler.gameObject.SetActive(false);
		trailPrefab.SetActive(false);
		explosionPrefab.SetActive(true);
		yield return new TIWaitForSeconds(1.0f);
		if (explosionPrefab != null)
		{
			explosionPrefab.SetActive(false);
		}
	}
}
