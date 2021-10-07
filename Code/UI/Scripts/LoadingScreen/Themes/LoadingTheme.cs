using UnityEngine;


public abstract class LoadingTheme : MonoBehaviour, ILoadingTheme
{
	public GameObject prefab { get; protected set; }

	void Awake()
	{
		DontDestroyOnLoad(this);
	}
	
	public virtual void show()
	{
		Transform t = gameObject.transform;
		t.SetParent(Loading.instance.displayParent.transform, false);
		t.localPosition = Vector3.zero;
		t.localRotation = Quaternion.identity;
		t.localScale = Vector3.one;

		gameObject.layer = Loading.instance.displayParent.layer;
		
		this.gameObject.SetActive(true);
	}

	public virtual void hide()
	{
		this.gameObject.SetActive(false);
	}
}