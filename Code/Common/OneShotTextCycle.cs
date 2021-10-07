using UnityEngine;
using System.Collections.Generic;
using TMPro;

/** TextCycler will take a textmeshpro object, and start doing a fadeout/fadein transition to allow
cycling through different text values */

public class OneShotTextCycle : MonoBehaviour
{
	// =============================
	// PUBLIC
	// =============================
	public TextMeshPro textMesh; // text mesh pro target to change text on
	public float fadeDuration = 0.5f;
	public List<string> texts = new List<string>();

	private bool _isPlaying;
	public bool isPlaying
	{
		get
		{
			return _isPlaying;
		}
		protected set
		{
			_isPlaying = value;
		}
	}
	
	// =============================
	// PRIVATE
	// =============================
	private int currentIndex = 0;
	private int previousIndex;
	
	void OnDestroy()
	{
		isPlaying = false;
		iTween.Stop(gameObject);
	}

	public void addText(string text)
	{
		texts.Add(text);
		translate();
	}

	public void updateDefaultText(string defaultText)
	{
		if (texts.Count > 0)
		{
			texts[0] = defaultText;
		}
	}

	public void stopAnimation()
	{
		isPlaying = false;
		iTween.Stop(gameObject);
		if (texts.Count > 0)
		{
			textMesh.text = texts[0];
		}
		Color color = CommonColor.adjustAlpha(textMesh.color, 1f);
		textMesh.color = color;
	}
	
	public void cycleOnce(string newText, string oldText = "")
	{
		// Validate input. 
		if (_isPlaying)
		{
			return;
		}
		
		isPlaying = true;
		currentIndex = 0;
		iTween.Stop(gameObject);
		Color color = CommonColor.adjustAlpha( textMesh.color, 1f );
		textMesh.color = color;
		if (string.IsNullOrEmpty(oldText) && textMesh != null)
		{
			oldText = textMesh.text;
		}
		
		texts = new List<string>();
		texts.Add(oldText);
		texts.Add(newText);

		fadeOutOnce();
	}
	
	public void fadeOutOnce()
	{
		iTween.ValueTo(gameObject, iTween.Hash
		(
			"from"
			, 1f
			, "to"
			, 0f
			, "time"
			, fadeDuration
			, "onUpdate"
			, "onTextUpdate"
			, "onComplete"
			, "onFadeOut"
		));
	}
	
	public void fadeInOnce()
	{
		iTween.ValueTo(gameObject, iTween.Hash
		(
			"from"
			, 0f
			, "to"
			, 1f
			, "time"
			, fadeDuration
			, "onUpdate"
			, "onTextUpdate"
			, "onComplete"
			, "onFadeIn"
		));
	}

	private void onFadeOut()
	{
		currentIndex++;

		if (currentIndex >= texts.Count)
		{
			currentIndex = 0;
		}

		textMesh.text = texts[currentIndex];
		fadeInOnce();
	}

	private void onFadeIn()
	{
		if (currentIndex != 0)
		{
			fadeOutOnce();
		}
		else
		{
			isPlaying = false;
		}
	}

	public void onTextUpdate(float value)
	{
		Color color = CommonColor.adjustAlpha( textMesh.color, value );
		textMesh.color = color;
	}

	public void nextText(bool skipFade)
	{
		currentIndex = ++currentIndex % texts.Count;
		textMesh.text = texts[currentIndex];
	}

	/*=========================================================================================
	ANCILLARY
	=========================================================================================*/
	// translate any loc keys in the list
	private void translate()
	{
		for (int i = 0; i < texts.Count; ++i)
		{
			if (texts[i].Contains(Localize.DELIMITER))
			{
				texts[i] = Localize.text(texts[i]);
			}
		}
	}
}