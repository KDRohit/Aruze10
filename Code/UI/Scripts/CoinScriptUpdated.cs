using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
/**
Attached to the coin 3D model to control behavior of it such as spinning.
*/

public class CoinScriptUpdated : TICoroutineMonoBehaviour
{
	public ParticleSystem coinTrail;
	public ParticleSystem sparkles;
	public ParticleSystem core;

	private const string NEW_COIN_PREFAB_PATH = "assets/data/hir/bundles/initialization/prefabs/misc/spinning coin new.prefab";
	
	/// Creates a coin and returns the reference to the attached script.
	public static CoinScriptUpdated create(Transform parent, Vector3 worldPosition, Vector3 localOffset)
	{
		GameObject prefab = SkuResources.getObjectFromMegaBundle<GameObject>(NEW_COIN_PREFAB_PATH);

		if (prefab != null)
		{
			GameObject go = CommonGameObject.instantiate(prefab) as GameObject;
			go.transform.parent = parent;
			go.transform.localScale = Vector3.one;
			go.transform.position = worldPosition;
			go.transform.localPosition = go.transform.localPosition + localOffset;
			return go.GetComponent<CoinScriptUpdated>();
		}

		return null;
	}

	/// Make the coin fly to the coordinates. Vector2 overload.
	public IEnumerator flyTo(Vector2 position, float time = 1f)
	{
		yield return StartCoroutine(flyTo(position.x, position.y, time));
	}

	/// Make the coin fly to the coordinates. x and y overload.
	public IEnumerator flyTo(float x, float y, float time = 1f)
	{
		
		iTween.MoveTo(gameObject, iTween.Hash("x", x, "y", y, "time", time, "islocal", true, "easetype", iTween.EaseType.linear));
		
		// Emit sparkles while flying.
		sparkles.Play();
		coinTrail.Play();
		core.Play();
		yield return new WaitForSeconds(time);
		
		sparkles.Stop();
		coinTrail.Stop();
		core.Stop();
	}
	
	/// Tells the coin to destroy itself when it's ready to,
	/// which typically means after the particles are gone.
	public void destroy()
	{
		StartCoroutine(destroyWhenReady());
	}
	
	private IEnumerator destroyWhenReady()
	{		
		while (sparkles.IsAlive() || coinTrail.IsAlive() || core.IsAlive())
		{
			yield return null;
		}
		
		Destroy(gameObject);
	}
}
