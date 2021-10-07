using UnityEngine;
using System.Collections;

public class SpecialFadeBehavior : TICoroutineMonoBehaviour 
{

	public Transform symbol;

	public void updateFade(float value)
	{
			Material mat = symbol.GetComponent<Renderer>().material;
			mat.SetFloat("_Fade", value);
	}

	public void startFade(float from = 1f, float to = 0f, float time = .5f)
	{
		iTween.ValueTo(this.gameObject, iTween.Hash("from", from, "to", to, "time", time, "onupdate", "updateFade"));
	}
}
