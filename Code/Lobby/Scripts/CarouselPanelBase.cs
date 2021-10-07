using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Abstract base class for all carousel panel classes to implement an init function.
*/

public abstract class CarouselPanelBase: TICoroutineMonoBehaviour
{
	public CarouselData.Type panelType;
	
	[HideInInspector] public CarouselData data = null;
	
	// This should be overridden by subclasses to set up the slide's UI.
	public abstract void init();

	protected const int PANEL_HEIGHT = 234; // The height of the carousel panels (used for verification of any downloaded images).
	protected const int PANEL_WIDTH = 914; // The width of the carousel panels (used for verification of any downloaded images).
	
	protected static Shader imageShader
	{
		get
		{
			if (_imageShader == null)
			{
				_imageShader = ShaderCache.find("Unlit/GUI Texture");
			}
			return _imageShader;
		}
	}
	private static Shader _imageShader = null;
	
	// Loads a texture and applies it to the given renderer.
	protected void loadTexture(Renderer imageRenderer, string url, string fallbackUrl = "")
	{
		imageRenderer.gameObject.SetActive(false);
		imageRenderer.material = new Material(imageShader);
		imageRenderer.material.color = Color.black;	// Default to black until texture is loaded.
		
		LobbyCarousel.instance.StartCoroutine(DisplayAsset.loadTextureFromBundle(url, textureCallback, Dict.create(D.OPTION, imageRenderer), fallbackUrl, skipBundleMapping:true, pathExtension:".png"));
	}

	protected void setTexture(Renderer imageRenderer, Texture2D texture)
	{
		imageRenderer.material = new Material(imageShader);
		imageRenderer.material.color = Color.black;	// Default to black until texture is loaded.
		textureCallback(texture, Dict.create(D.OPTION, imageRenderer));
	}
	
	// Reusable function for loading textures.
	protected void textureCallback(Texture2D tex, Dict texData)
	{
		if (tex != null)
		{
			Renderer imageRenderer = texData.getWithDefault(D.OPTION, null) as Renderer;

			if (imageRenderer == null)
			{
				// This could happen if the slide was destroyed before the image finished downloading.
				return;
			}
			imageRenderer.gameObject.SetActive(true);
			imageRenderer.material.mainTexture = tex;
			imageRenderer.material.color = Color.white;
		}
		else if (!data.isDefault)
		{
			// If the texture failed to download, remove this slide from the carousel to prevent
			// repeated attempts to download the image, which may be causing high network traffic issues.
			// We never deactivate the default slide, even if the image failed,
			// because the carousel will automatically re-create it over and over and over and over...
			data.deactivate();
		}
	}
}
