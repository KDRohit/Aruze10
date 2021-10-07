using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Controls the common black fader that can be used for fading to black over everything except the loading screen.
Good for very basic transitions.
*/

public class BlackFaderScript : MonoBehaviour
{
	public UISprite sprite;
	
	public static BlackFaderScript instance = null;
	
	void Awake()
	{
		instance = this;
	}
	
	// Fade the panel to the given alpha.
	public IEnumerator fadeTo(float alpha, float duration = 0.5f)
	{
		Loading.hirV3.toggleCamera(true);

		iTween.ValueTo(gameObject, iTween.Hash(
			"from", sprite.alpha,
			"to", alpha,
			"time", duration,
			"easetype", iTween.EaseType.linear,
			"onupdate", "setAlpha",
			"oncomplete", "onFadeComplete"
		));

		yield return new WaitForSeconds(duration);
	}

	// iTween callback.
	public void setAlpha(float alpha)
	{
		sprite.gameObject.SetActive(alpha > 0.0f);
		sprite.alpha = alpha;
	}

	void OnDisable()
	{
		iTween.Stop(gameObject);
	}

	void OnDestroy()
	{
		iTween.Stop(gameObject);
	}
}
