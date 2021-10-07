using UnityEngine;
using TMPro;

/**
Controls UI behavior of an active (not "coming soon") menu option button in the lobby.
*/

public abstract class LobbyOptionButtonActive : LobbyOptionButton
{
	public GameObject image;				// Only used for 2D images.
	public Renderer cabinetScreenRenderer;	// Only used for 3D models with image material as part of it.
	public Renderer cabinetGlowRenderer;	// Only used for 3D models
	public LabelWrapper gameNameLabel;
	public GameObject frameSparklePrefab;
#if RWR
	public GameObject rwrSweepstakesMeterAnchor = null;
	public RWRSweepstakesMeter rwrSweepstakesMeterPrefab = null;
#endif
	public GameObject newGameEffectPrefab;
	public GameObject dynamicTextParent;
	public TextMeshPro dynamicLabel;
	
	protected GameObject frameSparkle;
	protected Texture screenTexture;
	protected GameObject sneakPreviewIcon = null;

	private static Vector3 SNEAK_PREVIEW_LOC = new Vector3(245, -797, -250);
	
	protected Renderer imageRenderer = null;
	protected UITexture imageRendererUI = null;

	private Material clonedMaterial = null;
	[SerializeField] private GameObject newGameEffect = null;
#if RWR
	// KrisG: exposing rwrSweepstakesMeter in Inspector so I can de-link it for SIR prefabs, so SIR stops pulling in RWRSweepstakesMeter.prefab  (used to have [HideInInspector])
	 public RWRSweepstakesMeter rwrSweepstakesMeter = null;
#endif
	protected Color imageTint = Color.white;	// VIP options are tinted gray if the player hasn't reached the VIP level for the game.
	
	protected Material screenMaterial
	{
		get
		{
			if (cabinetScreenRenderer != null)
			{
				if (_screenMaterial == null && cabinetScreenRenderer != null)
				{
					_screenMaterial = CommonMaterial.findMaterial(cabinetScreenRenderer.materials, "Icon");
				}
			}
			return _screenMaterial;
		}
	}
	private Material _screenMaterial = null;

	// Each subclass must override this to do its own visual setup.
	// Each override must call base.setup() first.
	public override void setup(LobbyOption option, int page, float width, float height)
	{
		base.setup(option, page, width, height);

		// this object may have been cached and is being reused, make sure the frame sparkle gets recreated to the correct size 1x1 or 1x2
		frameSparkle = null;
		
		// Create the new game effect if necessary.
		createNewGameEffect();
		
		if (gameNameLabel != null)
		{
			SafeSet.gameObjectActive(gameNameLabel.gameObject, false);
		}

		if (image != null)
		{
			//we're recylcing this after it has already cloned a material.  Lets kill the old material
			if (clonedMaterial != null)
			{
				Destroy(clonedMaterial);
			}
			
			// try to get it as a renderer. If we don't have one, try as a UI texture.
			imageRendererUI = image.GetComponent<UITexture>();
			imageRenderer = image.GetComponent<Renderer>();

			if (imageRenderer != null)
			{
				Shader shader = getOptionShader();
				if (shader != null)
				{
					clonedMaterial = new Material(getOptionShader());
					clonedMaterial.color = Color.black;
				}
				else
				{
					imageRenderer.enabled = false;
					Debug.LogError("Could not load lobby option shader");
				}

				if (clonedMaterial != null)
				{
					imageRenderer.enabled = true;
					imageRenderer.material = clonedMaterial;
				}
			}
			else if (imageRendererUI != null)
			{
				//if prefab has a shader, use it, otherwise instantiate one
				if (imageRendererUI.material != null && imageRendererUI.material.shader != null)
				{
					Shader shader = ShaderCache.find(imageRendererUI.material.shader.name);
					if (shader != null)
					{
						clonedMaterial = new Material(shader);
						clonedMaterial.color = Color.black;	
					}
					else
					{
						imageRendererUI.enabled = false;
						Debug.LogError("Could not load shader: " + imageRendererUI.material.shader.name);
					}	
				}
				else
				{
					Shader shader = getOptionShader(this.option.action == "personalized_content" || this.option.game != null && 
						(
							LoLaLobby.main.findGame(option.game.keyName) != null || // either it's in the main lobby
							(option.game.eosControlledLobby != null && !option.game.isEOSControlled) // or it's about to be in the main lobby
						));
					if (shader != null)
					{
						clonedMaterial = new Material(shader);
						clonedMaterial.color = Color.black;	
					}
					else
					{
						imageRendererUI.enabled = false;
						Debug.LogError("Could not find lobby option shader");
					}
				}

				if (clonedMaterial != null)
				{
					imageRendererUI.material = clonedMaterial;
					imageRendererUI.enabled = true;
				}	
			}
		}
		
		if (screenMaterial != null)
		{
			screenMaterial.color = Color.black;
		}

		if (cabinetGlowRenderer != null && option != null && option.game != null)
		{
			// Set the glow to a color that matches the game's color scheme.
			cabinetGlowRenderer.material.color = option.game.lobbyColor;
		}
		
		// Disable all mousedown tinting until the image is finished being loaded.
		foreach (UIButtonColor bc in gameObject.GetComponentsInChildren<UIButtonColor>())
		{
			bc.enabled = false;
		}
	}

#if RWR
	// Create the real world rewards UI element if necessary.
	protected void createRWR(bool isVIPOption = false)
	{
		if (option.game != null && SlotsPlayer.instance.getIsRWRSweepstakesActive() && option.game.isRWRSweepstakes)
		{
			rwrSweepstakesMeter = RWRSweepstakesMeter.create(
				option.game,
				rwrSweepstakesMeterAnchor,
				rwrSweepstakesMeterPrefab
			);
			
			if (isVIPOption)
			{
				CommonGameObject.setLayerRecursively(rwrSweepstakesMeter.gameObject, gameObject.layer);
				
				if (SlotsPlayer.instance.vipNewLevel < option.game.vipLevel.levelNumber)
				{
					CommonGameObject.colorUIGameObject(rwrSweepstakesMeter.gameObject, Color.gray, true);
				}
			}
		}
	}
#endif

	protected virtual bool isNewGame()
	{
		return MOTDDialogData.newGameMotdData != null &&
		       option != null &&
		       option.game == MOTDDialogData.newGameMotdData.action1Game;
	}
	
	protected void createNewGameEffect()
	{
		if (isNewGame())
		{
			if (newGameEffect != null)
			{
				// The effect was already created from a previously used instance of this lobby option, so just enable it.
				newGameEffect.SetActive(true);
			}
			else if (newGameEffectPrefab != null)
			{
				// Put it at the same hierarchy level as the image object.
				GameObject imageObject = image;
				if (image == null && cabinetScreenRenderer != null)
				{
					imageObject = cabinetScreenRenderer.gameObject;
				}
				if (imageObject != null)
				{
					newGameEffect = NGUITools.AddChild(imageObject.transform.parent.gameObject, newGameEffectPrefab);
				}
			}
		}
		else if (newGameEffect != null)
		{
			// If this lobby option isn't the new game, but it was recycled from the new game in a previous usage, make sure the effect is hidden.
			newGameEffect.SetActive(false);
		}
	}

	
	public static Shader getOptionShader(bool isMainLobbyOption = false)
	{
		//if (_optionShader == null)
		//{
			if (isMainLobbyOption)
			{
				return ShaderCache.find("Unlit/Texture (AlphaClip)");
			}
			else
			{
				return ShaderCache.find("Unlit/GUI Texture");
			}

		//}
		//return _optionShader;
	}
	private static Shader _optionShader = null;
	
	public void enableSparkles(bool doEnable)
	{
		if (!doEnable)
		{
			if (frameSparkle != null)
			{
				frameSparkle.SetActive(false);
			}
			return;
		}

		if (frameSparklePrefab != null)
		{
			if (frameSparkle == null)
			{
				frameSparkle = CommonGameObject.instantiate(frameSparklePrefab) as GameObject;
				if (image != null)
				{
					frameSparkle.transform.parent = this.transform;
					frameSparkle.transform.position = image.transform.position;
					frameSparkle.transform.localPosition += Vector3.back * 10;
					// Set the scale b/c these buttons can be bigger than 2x1
					// Often, the image has a parent sizer, so we need to account for that too.
					Vector3 parentScale = image.transform.parent.localScale;
					
					// If the image is on a RoundedRectangle thing, we also need to multiply by the pixel size on that.
					Vector3 rrScale = Vector3.one;
					RoundedRectangle rr = image.GetComponent<RoundedRectangle>();
					if (rr != null)
					{
						rrScale.x = rr.size.x;
						rrScale.y = rr.size.y;
					}
					
					Vector3 locScale = new Vector3(
						frameSparklePrefab.transform.localScale.x * image.transform.localScale.x * parentScale.x * rrScale.x,
						frameSparklePrefab.transform.localScale.y * image.transform.localScale.y * parentScale.y * rrScale.y,
						frameSparklePrefab.transform.localScale.z
					);
					frameSparkle.transform.localScale = locScale;
				}
				else if (cabinetScreenRenderer != null)
				{
					// For 3D cabinet options, the sparkles are designed to fit the cabinet exactly,
					// so we don't need to do any special scaling.
					frameSparkle.transform.parent = cabinetScreenRenderer.transform;
					frameSparkle.transform.localPosition = Vector3.zero;
					frameSparkle.transform.localScale = Vector3.one;
					frameSparkle.transform.localEulerAngles = Vector3.zero;
				}
			}
			else
			{
				frameSparkle.SetActive(true);
			}
		}
	}

	protected virtual bool canShowSparkleAnimation()
	{
		return true; // by default set to to always show.
	}

	protected virtual void OnPress()
	{
		if (canShowSparkleAnimation())
		{
			enableSparkles(true);
		}
	}

	protected virtual void OnRelease()
	{
		enableSparkles(false);
	}
	
	protected override void Update()
	{
		// If dragging the options, stop the frame sparkle.
		if (frameSparkle != null && TouchInput.isDragging && frameSparkle.activeSelf)
		{
			frameSparkle.SetActive(false);
		}
#if RWR
		if ((rwrSweepstakesMeter != null) && !SlotsPlayer.instance.getIsRWRSweepstakesActive())
		{
			Destroy(rwrSweepstakesMeter.gameObject);
			rwrSweepstakesMeter = null;
		}
#endif

		base.Update();
	}

//	public void setSelectable(bool isSelectable) 
//	{
//	    Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
//
//		foreach (Collider col in colliders) 
//		{
//			col.enabled = isSelectable;
//		}
//	}
			
	// Sets an option's image after it has loaded.
	public void setImage()
	{
		Texture tex = null;

		if (this == null || option == null || gameObject == null)
		{
			return;
		}

		if (option.pinned != null && option.pinnedBitmap != null)
		{
			tex = option.pinnedBitmap;
		}
		else if (option.bitmap != null)
		{
			tex = option.bitmap;
		}

		if (tex != null)
		{
			screenTexture = tex;

			if (imageRenderer != null)
			{
				Material mat = imageRenderer.material;
				if (mat != null)
				{
					mat.color = imageTint;
					mat.mainTexture = tex;
				}
				imageRenderer.gameObject.SetActive(true);
			}
			else if (imageRendererUI != null && imageRendererUI.gameObject != null)
			{
				Material mat = imageRendererUI.material;
				if (mat != null)
				{
					mat.color = imageTint;
					mat.mainTexture = tex;
				}

				// Refresh
				imageRendererUI.gameObject.SetActive(false);
				imageRendererUI.gameObject.SetActive(true);
			}

			if (screenMaterial != null)
			{
				screenMaterial.color = imageTint;
				screenMaterial.mainTexture = tex;
			}

			Collider col = gameObject.GetComponentInChildren<Collider>();
			if (col != null)
			{
				if (col.enabled)
				{
					UIButtonColor[] colors = gameObject.GetComponentsInChildren<UIButtonColor>();
					if (null != colors)
					{
						foreach (UIButtonColor bc in colors)
						{
							if (bc == null)
							{
								continue;
							}
							bc.enabled = true;
							bc.defaultColor = Color.white;
						}
					}
				}
			}
		
			// Turn off the backup label just in case it was turned on at some point before the image loaded.
			if (gameNameLabel != null)
			{
				SafeSet.gameObjectActive(gameNameLabel.gameObject, false);
			}
			
			image.SetActive(true);
		}
		else
		{
			// If no image is available, make sure the button's image is blank.
			if (imageRenderer != null && imageRenderer.sharedMaterial != null) //check shared material so we don't duplicate material
			{
				imageRenderer.material.mainTexture = null;
			}
			else if (imageRendererUI != null && imageRendererUI.material != null)
			{
				imageRendererUI.material.mainTexture = null;
			}
			if (option.game != null)
			{
				SafeSet.labelText(gameNameLabel, option.game.name);
				if (gameNameLabel != null)
				{
					SafeSet.gameObjectActive(gameNameLabel.gameObject, true);
				}
			}
		}
	}

	protected virtual void OnDestroy()
	{
		if (clonedMaterial != null)
		{
			Destroy(clonedMaterial);
		}
	}

	/// Force a refresh of some visible element, initially going to be used to control 
	/// lock icons on options that need to be displayed or hidden based on using the old or new wager system
	public override void refresh()
	{
		if (option == null || gameObject == null)
		{
			Debug.LogError("INVESTIGATE - Refreshing the LobbyOptionButtonActive class without an option or gameobject");
			return;
		}

		base.refresh();
		
		if (option.game != null && option.game.isComingSoon)
		{
			setSelectable(false);

			// Get ready to show COMING SOON as the dynamic text on the lobby option.
			if (option != null)
			{
				option.localizedText = Localize.textUpper("coming_soon");
			}
		}

		if (option.localizedText != "")
		{
			SafeSet.gameObjectActive(dynamicTextParent, true);
			SafeSet.labelText(dynamicLabel, option.localizedText);
		}
		else
		{
			SafeSet.gameObjectActive(dynamicTextParent, false);
		}
	}
}
