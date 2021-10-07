using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Is attached at runtime for a one-time or looping color throbbing effect.
*/

public class ThrobSpriteColor : TICoroutineMonoBehaviour
{
	private Color _targetColor = Color.white;
	private Color _originalColor = Color.white;
	private UISprite _sprite = null;
	
	void Awake()
	{
		_sprite = GetComponent<UISprite>();
		if (_sprite == null)
		{
			Debug.LogError("ThrobSpriteColor is attached to object without a UISPrite.", gameObject);
			return;
		}
		
		_originalColor = _sprite.color;
		// Default color, just in case only alpha is used.
		_targetColor = _sprite.color;
	}
	
	/// Start throbbing only the alpha.
	public void throbAlpha(float alpha, float duration, int count = 1)
	{
		_targetColor.a = alpha;
		StartCoroutine(throb(duration, count));
	}
	
	/// Start throbbing the whole color.
	public void throbColor(Color color, float duration, int count = 1)
	{
		_targetColor = color;
		StartCoroutine(throb(duration, count));
	}
	
	private IEnumerator throb(float duration, int count)
	{
		duration *= .5f;
		
		int done = 0;
		
		while (done < count || count == 0)
		{
			iTween.ValueTo(_sprite.gameObject, iTween.Hash("from", _originalColor, "to", _targetColor, "time", duration, "easetype", iTween.EaseType.easeOutSine, "onupdate", "throbUpdate"));
			yield return new WaitForSeconds(duration);
			iTween.ValueTo(_sprite.gameObject, iTween.Hash("from", _targetColor, "to", _originalColor, "time", duration, "easetype", iTween.EaseType.easeInSine, "onupdate", "throbUpdate"));
			yield return new WaitForSeconds(duration);
			done++;
		}
		
		// Destroy itself when done.
		Destroy(this);
	}

	/// Callback function for iTween calls.
	private void throbUpdate(Color color)
	{
		_sprite.color = color;
	}

}
