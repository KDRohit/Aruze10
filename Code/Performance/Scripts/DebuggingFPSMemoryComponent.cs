using System;
using UnityEngine;
using System.Text;
using TMPro;

public class DebuggingFPSMemoryComponent : TICoroutineMonoBehaviour
{
	private static DebuggingFPSMemoryComponent singleton;
	public static DebuggingFPSMemoryComponent Singleton { get { return singleton; } }
	
	public TextMeshPro label;
	public FramesPerSecond framesPerSecond;

	public void Start()
	{
		this.gameObject.SetActive(false);
	}

	public void Update()
	{
		label.text = framesPerSecond.FPS;	
		label.color = framesPerSecond.color;
	}

	protected virtual void Awake()
	{
		if(singleton != null) Debug.LogError("More than one " + this.GetType().Name);
		singleton = this;
	}

	protected virtual void OnDestroy()
	{
		if(singleton == this)
		{
			singleton = null;
		}
	}
	
	public static void ClearSingleton()
	{
		singleton = null;
	}
}
