using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GenericPopup : MonoBehaviour
{
	public delegate void PopupLoadedDelegate(GenericPopup popup);
	
	public TextMeshPro descriptionLabel;
	public ClickHandler yesButton;
	public ClickHandler noButton;
	public ClickHandler okayButton;

	private ClickHandler.onClickDelegate yesCallback;
	private ClickHandler.onClickDelegate noCallback;
	private ClickHandler.onClickDelegate okayCallback;

	private const string FRIENDS_POPUP_PREFAB_PATH = "Features/Network Friends/Lobby Bundle/Friends Popup";
	
	public void init(Dict args)
	{
		string description = (string)args.getWithDefault(D.TITLE, "notset");
		if (description == "notset")
		{
			Debug.LogErrorFormat("GenericPopup.cs -- init -- initialized with no text, wtf man.");
			close();
		}
		if (args.ContainsKey(D.SECONDARY_CALLBACK))
		{
			// If they passed a secondary callback, then init with yes/no
			initYesNo(
				description,
				(ClickHandler.onClickDelegate)args.getWithDefault(D.CALLBACK, null),
				(ClickHandler.onClickDelegate)args.getWithDefault(D.SECONDARY_CALLBACK, null));
		}
		else if (args.ContainsKey(D.CALLBACK))
		{
			// Otherwise init with okay.
			initOkay(description,(ClickHandler.onClickDelegate)args.getWithDefault(D.CALLBACK, null));
		}
	}
	
	public void initYesNo(string description, ClickHandler.onClickDelegate yesClicked = null, ClickHandler.onClickDelegate noClicked = null)
	{
		Dict args = Dict.create(D.OBJECT, gameObject);
		descriptionLabel.text = description;
		okayButton.clearAllDelegates();
		yesButton.clearAllDelegates();
		noButton.clearAllDelegates();
		
		yesCallback = yesClicked;
		yesButton.registerEventDelegate(onYesClicked, args);
		noCallback = noClicked;
		noButton.registerEventDelegate(onNoClicked, args);
		
		yesButton.gameObject.SetActive(true);
		noButton.gameObject.SetActive(true);
		okayButton.gameObject.SetActive(false);
	}
	
	public void initOkay(string description, ClickHandler.onClickDelegate okayClicked = null)
	{
		Dict args = Dict.create(D.OBJECT, gameObject);
		descriptionLabel.text = description;
		okayButton.clearAllDelegates();
		yesButton.clearAllDelegates();
		noButton.clearAllDelegates();
		okayCallback = okayClicked;
		okayButton.registerEventDelegate(onOkayClicked, args);

		yesButton.gameObject.SetActive(false);
		noButton.gameObject.SetActive(false);
		okayButton.gameObject.SetActive(true);
	}

	private void onOkayClicked(Dict args = null)
	{
		if (okayCallback != null)
		{
			okayCallback(args);
		}
		close();
	}
	
	private void onNoClicked(Dict args = null)
	{
		if (noCallback != null)
		{
			noCallback(args);
		}
		close();
	}

	private void onYesClicked(Dict args = null)
	{
		if (yesCallback != null)
		{
			yesCallback(args);
		}
		close();
	}
	
	public void close()
	{
		Destroy(this.gameObject);
		Overlay.instance.hideShroud();
	}

	public static void showFriendsPopupAtAnchor(Transform anchor, string description, bool isYesNo, ClickHandler.onClickDelegate callback = null, ClickHandler.onClickDelegate secondaryCallback = null, Dict extraArgs = null)
	{
		showPopupAtAnchor(FRIENDS_POPUP_PREFAB_PATH, anchor, description, isYesNo, callback, secondaryCallback, extraArgs);
	}
	
	public static void showPopupAtAnchor(string prefabPath, Transform anchor, string description, bool isYesNo, ClickHandler.onClickDelegate callback = null, ClickHandler.onClickDelegate secondaryCallback = null, Dict extraArgs = null)
	{
		Dict args = Dict.create(D.TITLE, description, D.CALLBACK, callback);
		args.merge(extraArgs);
		
		if (isYesNo)
		{
			args.Add(D.SECONDARY_CALLBACK, secondaryCallback);
		}
		
		if (anchor == null)
		{
			Debug.LogErrorFormat("GenericPopup.cs -- showPopupAtAnchor -- anchor is not allowed to be null.");
			return;
		}
		
		args.Add(D.TRANSFORM, anchor);
		AssetBundleManager.load(prefabPath, loadSuccess, loadFailure, args);
	}

	private static void loadSuccess(string assetPath, Object obj, Dict args = null)
	{
		Transform anchor = args.getWithDefault(D.TRANSFORM, null) as Transform;
		if (anchor != null)
		{
			if (obj == null)
			{
				Debug.LogErrorFormat("GenericPopup.cs -- loadSuccess -- object was null");
				return;
			}
			GameObject popupPrefab = obj as GameObject;
			if (popupPrefab == null)
			{
				Debug.LogErrorFormat("GenericPopup.cs -- loadSuccess -- could not cast object as GameObject");
			}

			GameObject popupObject = GameObject.Instantiate(popupPrefab, anchor);
			if (popupObject == null)
			{
				Debug.LogErrorFormat("GenericPopup.cs -- loadSuccess -- failed to instantiate prefab.");
				return;
			}
			
			GenericPopup popup = popupObject.GetComponent<GenericPopup>();
			if (popup == null)
			{
				Debug.LogErrorFormat("GenericPopup.cs -- loadSuccess -- object did not have a GenericPopup component.");
				Destroy(popupObject); // Destroy this since its now a dead object.
				return;
			}
			popup.init(args);
		}
		else
		{	
			Debug.LogErrorFormat("GenericPopup.cs -- loadSuccess -- anchor was null, this isn't supposed to be allowed so something went wrong here.");
		}
	}
	
	private static void loadFailure(string assetPath, Dict data = null)
	{
		Debug.LogErrorFormat("GenericPopup.cs -- loadFailure -- sorry, failed to load the prefab :(");
	}
}
