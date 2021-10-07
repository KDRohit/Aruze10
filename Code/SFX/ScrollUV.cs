using UnityEngine;
using System.Collections;

public class ScrollUV : TICoroutineMonoBehaviour {

	public float scrollSpeedU = 0.5f;
	public float scrollSpeedV  = 0.5f;
	private Vector2 offset = Vector2.zero;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		offset.x += (Time.deltaTime * scrollSpeedU);
		offset.y += (Time.deltaTime * scrollSpeedV);
		GetComponent<Renderer>().material.SetTextureOffset("_MainTex", offset);
	
	}
}
