using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class AutoDestroyParticleSystem : TICoroutineMonoBehaviour {

	// Use this for initialization
	void Start ()
	{
		GameObject.Destroy(this.gameObject, this.GetComponent<ParticleSystem>().main.duration);
	}
}
