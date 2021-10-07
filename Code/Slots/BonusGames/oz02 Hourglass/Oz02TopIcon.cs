using UnityEngine;
using System.Collections;

public class Oz02TopIcon : TICoroutineMonoBehaviour 
{
	public UISprite icon;
	public UISprite wildOverlay;
	public GameObject borderEffect;

	protected void Awake()
	{
		icon.color = Color.grey;
		wildOverlay.enabled = false;
	}

	public void turnOn()
	{
		icon.color = Color.white;
		wildOverlay.enabled = true;
		icon.enabled = false;
		icon.enabled = true;
		this.StartCoroutine(this.doEffect());
	}

	private IEnumerator doEffect()
	{
		GameObject effect = CommonGameObject.instantiate(borderEffect) as GameObject;
		effect.transform.parent = this.gameObject.transform;
		effect.transform.localScale = new Vector3(150, 150, 1);
		effect.transform.localPosition = new Vector3(0, 0, -1);
		yield return new WaitForSeconds(1.2f);
		Destroy(effect);
	}
}
