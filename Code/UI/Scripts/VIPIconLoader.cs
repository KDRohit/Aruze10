using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VIPIconLoader : MonoBehaviour 
{
	public enum Prefabs
	{
		VIP_CARD,
		VIP_GEM,
		OLD_VIP_CARD
	};

	public Prefabs prefab = Prefabs.VIP_GEM;
	public VIPNewIcon handlerToLinkTo;
	public bool setupOnAwake = false;
	// This assumes that whatever you put this on has all the necesary extra scaling for the items
	// when they get loaded. You can do it yourself after if you want with getcompnent or whatever.
	void Awake()
	{
		if (setupOnAwake)
		{
			setup();
		}
	}

	void Start()
	{
		if (!setupOnAwake)
		{
			setup();
		}
	}

	private void setup()
	{
		GameObject loadedObject;
		switch (prefab)
		{

		case Prefabs.VIP_GEM:
			loadedObject = VIPLevel.loadVIPGem(this.gameObject);

			if (handlerToLinkTo != null && loadedObject != null) 
			{
				handlerToLinkTo.levelIcon = loadedObject.GetComponent<UISprite>();
			}

			if (loadedObject == null)
			{
				Debug.LogErrorFormat("VIP Gem for {0} in VIPIconLoader::setup is null", gameObject.name);
			}

			break;

		case Prefabs.VIP_CARD:
			loadedObject = VIPLevel.loadVIPCard(this.gameObject);

			if (handlerToLinkTo != null)
			{
				linkToHandler(loadedObject);
			}

			break;

		case Prefabs.OLD_VIP_CARD:
			loadedObject = VIPLevel.loadOldVIPCard(this.gameObject);

			if (handlerToLinkTo != null) 
			{
				linkToHandler(loadedObject);
			}

			break;

		}
	}

	private void linkToHandler(GameObject loadedObject)
	{
		handlerToLinkTo = loadedObject.GetComponent<VIPNewIcon>();
	}

}
