using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkAvatarSelectPanel : MonoBehaviour
{
    public MeshRenderer profileRenderer;
	public GameObject selectedParent;
	public ClickHandler clickHandler;
	public UISprite facebookLogo;
	
	// Used for fading
	public List<UISprite> allSprites;
	public UISprite selectedOverlay;
	
	[HideInInspector]public NetworkAvatarSelect selector;
	[HideInInspector]public string url;
	private bool _isSelected = false;

	private const float FADE_DURATION = 1.0f;
	private const float FADE_IN_DURATION = 1.0f;
	private const float FADE_OUT_DURATION = 1.0f;
	private const float OVERLAY_ALPHA_VALUE = 0.53f;
	
	public const string LOCAL_URL_FORMAT = "avatars/{0}";

	public bool isSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			_isSelected = value;
			selectedParent.SetActive(_isSelected);
		}
	}

	public void init(string url, NetworkAvatarSelect selector, bool selected = false, bool isFacebook = false)
	{
		this.url = url;
		this.selector = selector;
		//If the URL isn't a facebook photo, attempt to load from the avatars images bundle first
		if (!url.Contains("network_profiles/avatars"))
		{
			DisplayAsset.loadStreamedTextureToRenderer(profileRenderer, url, "", true, false);
		}
		else
		{
			string localAvatarPath = string.Format(LOCAL_URL_FORMAT, DisplayAsset.textureNameFromRemoteURL(url));
			DisplayAsset.loadTextureToRenderer(profileRenderer, localAvatarPath, url, false, false, skipBundleMapping:true, pathExtension:".png");
		}
		transform.localPosition = Vector3.zero;
		isSelected = selected;
		clickHandler.registerEventDelegate(panelClicked);
		facebookLogo.gameObject.SetActive(isFacebook);

		
		onFadeUpdate(0.0f); // Make sure we turn them all off before we start.
	}

	public void fade(bool isFadingIn)
	{
		float targetAlpha = isFadingIn ? 1.0f : 0f;
		float sourceAlpha = isFadingIn ? 0f : 1.0f;
		iTween.ValueTo(gameObject,
					   iTween.Hash(
						   "from", sourceAlpha,
						   "to", targetAlpha,
						   "time", FADE_DURATION,
						   "onupdate", "onFadeUpdate"));		
	}

	private void onFadeUpdate(float alpha)
	{
	    for (int i = 0; i< allSprites.Count; i++)
		{
			allSprites[i].alpha = alpha;
		}

		// The selected overlay gets displayed at a 50% alpha, so we don't want to fade it to 100%. 
		Color targetOverlayColor = selectedOverlay.color;
		targetOverlayColor.a = alpha * OVERLAY_ALPHA_VALUE;
		selectedOverlay.color = targetOverlayColor;

		
		Color targetColor = profileRenderer.material.color;
	    targetColor.a = alpha;
		profileRenderer.material.color = targetColor;
	}
	
	private void panelClicked(Dict args = null)
	{
		if (!isSelected)
		{
			selector.selectPicture(this);
			isSelected = !isSelected;
		}
	}
}
