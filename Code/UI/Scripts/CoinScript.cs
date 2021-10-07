using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/**
Attached to the coin 3D model to control behavior of it such as spinning.
*/

public class CoinScript : TICoroutineMonoBehaviour
{
	public GameObject coin;
	public ParticleSystem sparkles;
	
	private const float REST_ROTATION = 325f;
	
	private int _spinCount = 0;				///< The number of times to spin around before stopping. -1 means infinite.
	private float _spinTime = .5f;

	private const string COIN_PREFAB_PATH = "assets/data/hir/bundles/initialization/prefabs/misc/spinning coin.prefab";
	
	/// Creates a coin and returns the reference to the attached script.
	public static CoinScript create(Transform parent, Vector3 worldPosition, Vector3 localOffset)
	{
		GameObject prefab = SkuResources.getObjectFromMegaBundle<GameObject>(COIN_PREFAB_PATH);
		if (prefab != null)
		{
			GameObject go = CommonGameObject.instantiate(prefab) as GameObject;
			go.transform.parent = parent;
			go.transform.localScale = Vector3.one;
			go.transform.position = worldPosition;
			go.transform.localPosition = go.transform.localPosition + localOffset;
			return go.GetComponent<CoinScript>();
		}

		return null;
	}
	
	public void spin(int spinCount = -1, float spinTime = .5f)
	{
		_spinCount = spinCount;
		_spinTime = spinTime;

		startSpin();
	}
	
	/// Make the coin fly to the coordinates. Vector2 overload.
	public IEnumerator flyTo(Vector2 position, float time = 1f)
	{
		yield return StartCoroutine(flyTo(position.x, position.y, time));
	}

	/// Make the coin fly to the coordinates. x and y overload.
	public IEnumerator flyTo(float x, float y, float time = 1f)
	{
		spin();
		
		iTween.MoveTo(gameObject, iTween.Hash("x", x, "y", y, "time", time, "islocal", true, "easetype", iTween.EaseType.linear));
		
		// Emit sparkles while flying.
		sparkles.Play();
		
		yield return new WaitForSeconds(time);
		
		sparkles.Stop();
	}
		
	/// Decrements the spin counter and starts a spin if necessary.
	private void startSpin()
	{
		if (_spinCount > 0)
		{
			_spinCount--;
		}
				
		if (_spinCount != 0)
		{
			// A full 360-degree spin needs to be broken down into three different partial spins,
			// because Unity sees rotations of +360 the same as the original rotation and doesn't do anything.
			iTween.RotateTo(coin, iTween.Hash("y", REST_ROTATION + 120, "time", _spinTime / 3, "easetype", iTween.EaseType.linear, "oncompletetarget", gameObject, "oncomplete", "spin2"));
		}		
	}
	
	/// Part 2 of a full rotation spin.
	private void spin2()
	{
		iTween.RotateTo(coin, iTween.Hash("y", REST_ROTATION + 240, "time", _spinTime / 3, "easetype", iTween.EaseType.linear, "oncompletetarget", gameObject, "oncomplete", "spin3"));
	}

	/// Part 3 of a full rotation spin.
	private void spin3()
	{
		iTween.RotateTo(coin, iTween.Hash("y", REST_ROTATION + 360, "time", _spinTime / 3, "easetype", iTween.EaseType.linear, "oncompletetarget", gameObject, "oncomplete", "startSpin"));
	}
	
	/// Tells the coin to destroy itself when it's ready to,
	/// which typically means after the particles are gone.
	public void destroy()
	{
		StartCoroutine(destroyWhenReady());
	}
	
	private IEnumerator destroyWhenReady()
	{
		coin.SetActive(false);
		
		while (sparkles.IsAlive())
		{
			yield return null;
		}
		
		Destroy(gameObject);
	}
}
