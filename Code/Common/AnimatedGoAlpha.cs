
using UnityEngine;
/**
Aniamted game object alpha.
**/

/// <summary>
/// Makes it possible to animate alpha of the widget or a panel.
/// </summary>

public class AnimatedGoAlpha : TICoroutineMonoBehaviour
{
	public float alpha = 1f;

	void Awake ()
	{
		Update();
	}

	void Update ()
	{
		CommonGameObject.alphaUIGameObject(gameObject,alpha);
	}
}
