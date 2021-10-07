using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
iTween can only run against GameObjects, so I had to create a script to be able to tween the shroud's alpha.
*/

public class ShroudScript : TICoroutineMonoBehaviour
{
	public UISprite sprite;
	
	/// Called by iTween when fading the shroud's alpha.
	public void updateFade(float alpha)
	{
//		Debug.Log("set shroud alpha to " + alpha);
		sprite.alpha = alpha;
	}
}
