using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
Base class for generic lobby option option overlays. 
*/

public class LobbyOptionDecorator : TICoroutineMonoBehaviour
{
	public GameObject loadedPrefab;
	public GameObject anchor;
	public LobbyOptionButtonGeneric parentOption;
	public bool hideFeatured;
	public bool hideLock;
	public Vector3 featuredOffset;
	public LobbyOptionFeatureLabel featuredParent;

	private const float UI_PANEL_Z = -65.0f;

	protected static void prepPrefabForLoading(GameObject prefabToLoad, GameObject parentObject, LobbyOptionButtonGeneric option,
								string assetPath, System.Type componentType, bool hideFrames = true, string fileExtension = "")
	{
		// check if we need to load the asset which will only happen once per session per type
		if (prefabToLoad == null)
		{
			GameObject loaderGameObject = new GameObject("lobby option asset asset loader");
			loaderGameObject.transform.parent = option != null ? option.gameObject.transform : parentObject.transform;

			LobbyOptionDecorator loader = (LobbyOptionDecorator)loaderGameObject.AddComponent(componentType);
			if (loader != null)
			{
				loader.anchor = parentObject;
				loader.parentOption = option;
				AssetBundleManager.load(assetPath, loader.prefabLoadSuccess, loader.prefabLoadFailure, 	Dict.create(D.OPTION, hideFrames), isSkippingMapping:fileExtension != "", fileExtension:fileExtension);
			}
		}
		else
		{
			attachPrefab(prefabToLoad, parentObject, option, hideFrames);
		}
	}	

	public static void attachPrefab(GameObject prefabToLoad, GameObject parentAnchor, LobbyOptionButtonGeneric option, bool hideFrames = true)
	{
		if (prefabToLoad != null)
		{
			// create new instance of prefab and attach to the parent anchor
			GameObject attachedInstance = NGUITools.AddChild(parentAnchor, prefabToLoad);
			if (option != null)
			{
				if (attachedInstance != null)
				{
					CommonTransform.addZ(attachedInstance.transform, UI_PANEL_Z);
					LobbyOptionDecorator instance = attachedInstance.GetComponent<LobbyOptionDecorator>();
					if (instance != null)
					{
						instance.parentOption = option;
						option.buttonOverlay = instance;
						instance.setup();
						if (hideFrames && option != null)
						{
							option.hideFrames(instance.hideFeatured, instance.hideLock);
						}

						// offset the feature label as needed
						if (instance.featuredParent != null)
						{
							Vector3 vScale = instance.featuredParent.transform.localScale;
							Vector3 vOffset = new Vector3(instance.featuredOffset.x * vScale.x, instance.featuredOffset.y * vScale.y, instance.featuredOffset.z * vScale.y);
							instance.featuredParent.transform.localPosition += vOffset;
						}
					}
				}
			}
		}
	}

	protected virtual void setup()
	{

	}

	public void registerProgressiveJackpotLabel(TextMeshPro label)
	{
		if (label != null && parentOption != null)
		{
			// null check, this crashed in critterism
			if (parentOption.option != null && parentOption.option.game != null 
                && parentOption.option.game.progressiveJackpots != null && parentOption.option.game.progressiveJackpots.Count > 0 && parentOption.option.game.progressiveJackpots[parentOption.option.game.progressiveJackpots.Count - 1] != null)
			{
                parentOption.option.game.progressiveJackpots[parentOption.option.game.progressiveJackpots.Count - 1].registerLabel(label);		
			}
			parentOption.refresh();	
		}
	}

	public void prefabLoadSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this == null)
		{
			// we may have been killed off before the asset finished loading but the callback still gets invoked
			// fix for crittercism crash where this gets called 5 seconds after a slot game is already loading and the lobby is gone
			// looks like the only place the crash could happen is when we check anchor and parentOption for null if this == null
			/*
			0	NullReferenceException: NullReferenceException
			1	at .LobbyOptionDecorator.prefabLoadSuccess (System.String assetPath, UnityEngine.Object obj, .Dict data)()
			2	at .AssetLoadDelegate.Invoke (System.String assetPath, UnityEngine.Object obj, .Dict data)()
			3	at .AssetBundleDownloader.notifyCallersThatResourceIsReady ()()
			4	at .AssetBundleManager.Update ()()
			*/
			return;
		}

		loadedPrefab = obj as GameObject;
		setOverlayPrefab(loadedPrefab, assetPath);
		featuredParent = (obj as GameObject).GetComponent<LobbyOptionFeatureLabel>();

		TextMeshPro[] textMeshes = GetComponentsInChildren<TextMeshPro>();
		if (textMeshes != null && MainLobby.hirV3 != null)
		{
			MainLobby.hirV3.masker.addObjectArrayToList(textMeshes);
		}

		if (parentOption != null && parentOption.frameVisible != null)
		{
			// if frameVisible is set, it needs to be turned off
			parentOption.frameVisible.gameObject.SetActive(false); 
		}

		if (anchor != null)
		{
			// only attach if the parent and lobby option still exist.
			bool hideFrames = true;
			if (data != null)
			{
				hideFrames = (bool)data.getWithDefault(D.OPTION, hideFrames);
			}
			attachPrefab(loadedPrefab, anchor, parentOption, hideFrames);
		}

		// Destroy our loading selves
		DestroyObject(gameObject);
	}

	public void prefabLoadFailure(string assetPath, Dict data = null)
	{
		if (this == null)
		{
			return;
		}
		Debug.LogError("LobbyOptionDecorator::prefab load failure - Failed to load asset at: " + assetPath);

		// Destroy our loading selves
		DestroyObject(gameObject);
	}

	protected virtual void setOverlayPrefab(GameObject prefabFromAssetBundle, string assetPath)
	{
		if (this == null)
		{
			return;
		}
		Debug.LogError("You have failed to overide setOverlayPrefab for " + this.name);
	}

}
