using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SortColumnButton : TICoroutineMonoBehaviour
{
	public UISprite sprite;
	public Color normalColor;
	public Color sortingColor;
	public UISprite arrowSprite;
	
	public bool isSorting
	{
		get { return _isSorting; }
		
		set
		{
			_isSorting = value;
		
			if (isSorting)
			{
				sprite.color = sortingColor;
			}
			else
			{
				sprite.color = normalColor;
			}
		
			arrowSprite.gameObject.SetActive(isSorting);
		}
	}
	private bool _isSorting = false;
}
