using UnityEngine;

public abstract class LoadingWidget : MonoBehaviour, ILoadingWidget
{
	public GameObject prefab { get; protected set; }
	new public abstract string name { get; }

	void Awake()
	{
		DontDestroyOnLoad(this);
	}
	
	public virtual void show()
	{
		Transform t = gameObject.transform;
		t.SetParent(Loading.instance.displayParent.transform, false);
		t.localPosition = new Vector3(0, 0, -10);
		t.localRotation = Quaternion.identity;
		t.localScale = Vector3.one;

		gameObject.layer = Loading.instance.displayParent.layer;
		
		this.gameObject.SetActive(true);
	}

	public virtual void hide()
	{
		this.gameObject.SetActive(false);
	}

	protected JSON getWidgetData()
	{
		if (LoadingScreenData.currentData != null && LoadingScreenData.currentData.widgetLookup != null)
		{			
			return LoadingScreenData.currentData.widgetLookup[name];		
		}

		return null;
	}
}