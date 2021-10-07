using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/** TextCycler will take a textmeshpro object, and start doing a fadeout/fadein transition to allow
cycling through different text values */

public class TextCycler : MonoBehaviour
{
	// =============================
	// PUBLIC
	// =============================
	public TextMeshPro textMesh; // text mesh pro target to change text on
	public float delay; // time before transitioning to next text in the list
	public float fadeDuration = 0.5f;
	public List<string> texts;
	public bool textsRandom;

	// =============================
	// PRIVATE
	// =============================
	private int currentIndex = 0;
	private int previousIndex;
	
	/// <summary>
	///   When creating a new text cycler, pass the TMP object you want to be cycling text through
	/// </summary>
    void Awake()
	{
		translate();
		start();
	}

	void OnDestroy()
	{
		iTween.Stop(gameObject);
	}

	public void addText( string text )
	{
		texts.Add(text);
		translate();
	}

	/*=========================================================================================
	CYCLING
	=========================================================================================*/
	public void start()
	{
		if (textMesh != null && texts != null && texts.Count > 0)
		{
			fadeOut();
		}
		else
		{
			Debug.LogError("do TextCycler.setup() before calling start()");
		}
	}

	private void fadeOut()
	{
		iTween.ValueTo(gameObject, iTween.Hash
		(
			"from"
			, 1.0f
			, "to"
			, 0f
			, "time"
			, fadeDuration
			, "delay"
			, delay
			, "onComplete"
			, "nextText"
			, "onupdate"
			, "onTextUpdate"
		));

	}

	private void fadeIn()
	{
		iTween.ValueTo(gameObject, iTween.Hash
		(
			"from"
			, 0f
			, "to"
			, 1f
			, "time"
			, fadeDuration
			, "onComplete"
			, "fadeOut"
			, "onUpdate"
			, "onTextUpdate"
		));
	}

	public void onTextUpdate(float value)
	{
		Color color = CommonColor.adjustAlpha( textMesh.color, value );
		textMesh.color = color;
	}

	public void nextText()
	{
		if (texts == null || texts.Count < 1)
			return;

		if (textsRandom)
		{
			do
			{
				currentIndex = Random.Range(0, texts.Count);
			}
			while (texts.Count > 1 && previousIndex == currentIndex);

			previousIndex = currentIndex;
		}
		else
		{
			currentIndex = ++currentIndex % texts.Count;
		}

		textMesh.text = texts[currentIndex];
		
		if (fadeDuration > 0)
		{
			fadeIn();
		}
		else
		{
			fadeOut();
		}
	}

	/*=========================================================================================
	ANCILLARy
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
			
			//https://forum.unity.com/threads/escape-characters-arent-being-automatically-parsed-from-settext.516145/#post-3382727
			//TLDR: strings with a \n in public fields in Unity get converted to \\n to literally have "\n" as part of the string instead of parsing it as a newline
			texts[i] = texts[i].Replace("\\n", "\n"); 
		}
	}
}