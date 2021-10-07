using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls the visual FX for a passive level up.
Self-destructs when done.
*/

public class PassiveLevelUpFX : TICoroutineMonoBehaviour
{
	public GameObject[] stars;
	public ParticleSystem sparkleBurst;
	
	private bool _didBurst = false;
	
	void Update()
	{
		if (_didBurst && !sparkleBurst.IsAlive())
		{
			// Self-destruct after doing the sparkle burst, after the sparkles are gone.
			// Do this here instead of as part of the doEffect() coroutine so that the coroutine
			// can finish as soon as the burst starts, for syncing with the calling function.
			Destroy(gameObject);
		}
	}
	
	/// Do the visual effect sequence.
	public IEnumerator doEffect()
	{
		Audio.play("LevelUpHighlight1");
		
		updateStarAlpha(0f);
		
		float duration = 1f;
		for (int i = 0; i < stars.Length; i++)
		{
			GameObject star = stars[i];
			
			// Make the stars fly into the XP meter star.
			iTween.ScaleTo(star, iTween.Hash("scale", Vector3.one, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear));
			iTween.RotateTo(star, iTween.Hash("z", 0, "time", duration, "islocal", true, "easetype", iTween.EaseType.linear));
			iTween.MoveTo(star, iTween.Hash("position", Vector3.zero, "time", duration, "islocal", true, "easetype", iTween.EaseType.easeInBack));
		}
		
		iTween.ValueTo(gameObject, iTween.Hash("from", 0f, "to", 1f, "time", duration, "islocal", true, "easetype", iTween.EaseType.easeOutCubic, "onupdate", "updateStarAlpha"));
		
		yield return new WaitForSeconds(duration);
		yield return null;	// Wait one more frame to guarantee that the iTweens are done.
		
		for (int i = 0; i < stars.Length; i++)
		{
			stars[i].SetActive(false);
		}
		
		sparkleBurst.Play();
		_didBurst = true;
	}
	
	/// iTween ValueTo() callback.
	private void updateStarAlpha(float alpha)
	{
		for (int i = 0; i < stars.Length; i++)
		{

			CommonRenderer.alphaRenderer(stars[i].GetComponent<Renderer>(), alpha);
		}
	}
}
