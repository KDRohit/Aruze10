using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Com.Scheduler;
using TMPro;

public class InboxListItem : TICoroutineMonoBehaviour, IResetGame
{
	// =============================
	// PROTECTED
	// =============================
	protected InboxTab.onRemoveItem onItemRemoved;
	protected InboxTab.onAcceptItem onItemAccepted;
	protected InboxTab.onDestroyItem onItemDestroyed;
	protected InboxListItemWrapper itemWrapper;
	protected SmartTimer dismissTimer;
	protected SmartTimer destroyTimer;
	protected bool isDoingRootAnimation = false;

	[SerializeField] protected ButtonHandler buttonHandler;
	[SerializeField] protected ButtonHandler closeButton;
	[SerializeField] protected ButtonHandler viewProfileButton;
	[SerializeField] protected UITexture profileImage;
	[SerializeField] protected UITexture gameImage;
	[SerializeField] protected UITexture backgroundImage;
	[SerializeField] protected TextMeshPro messageLabel;
	[SerializeField] protected TextMeshPro buttonLabel;
	[SerializeField] protected TextMeshPro timerLabel;
	[SerializeField] protected GameObject checkmark;
	[SerializeField] protected GameObject giftRibbon;
	[SerializeField] protected GameObject limitReached;
	[SerializeField] protected GameObject exclusiveOfferHeader;
	[SerializeField] protected Animator coinBurst;
	[SerializeField] protected Animator helpFriendCoinBurst;
	[SerializeField] protected PowerupSpawner powerupSpawner;

	// item animations
	[SerializeField] protected AnimationListController.AnimationInformation offAnimation;
	[SerializeField] protected AnimationListController.AnimationInformation introAnimation;
	[SerializeField] protected AnimationListController.AnimationInformation idleAnimation;
	[SerializeField] protected AnimationListController.AnimationInformation outroAnimation;

	// item button animations
	[SerializeField] protected AnimationListController.AnimationInformation buttonOffAnimation;
	[SerializeField] protected AnimationListController.AnimationInformation buttonIntroAnimation;
	[SerializeField] protected AnimationListController.AnimationInformation buttonIdleAnimation;
	[SerializeField] protected AnimationListController.AnimationInformation buttonOutroAnimation;

	// item checkmark animations
	[SerializeField] protected AnimationListController.AnimationInformation checkIntroAnimation;
	[SerializeField] protected AnimationListController.AnimationInformation checkOutroAnimation;

	// Default profile images
	[SerializeField] protected Texture2D defaultMale;
	[SerializeField] protected Texture2D defaultFemale;
	
	private const int PICTURE_CACHE_LIMIT = 50; //limit texture amount so we don't run out of memory
	protected static Dictionary<string, Material> profilePicCache = new Dictionary<string, Material>();
	
	// =============================
	// PUBLIC
	// =============================
	// state swapper
	public ObjectSwapper swapper;
	public InboxItem inboxItem { get; private set; }

	// =============================
	// CONST
	// =============================
	protected const float DISMISS_DELAY = 1.5f;
	protected const float DESTROY_DELAY = 0.8f;
	protected const string BACKGROUND_PATH = "inbox_backgrounds/";
	protected const string ICON_PATH = "inbox_icons/";
	
	private const string SEND_COINS_SOUND = "InboxHelp";
	private const string CLAIM_COINS_SOUND = "InboxClaim";

	private Material clonedMaterial = null;

	public enum ItemAnimations
	{
		Idle,
		Intro,
		Outro,
		Off
	}

	void OnDestroy()
	{
		cleanup();
	}

	void OnDisable()
	{
		cleanup();
	}

	public void reset()
	{
		removeWrapper();
		cleanup();
	}
	
	public static void clearProfilePictures()
	{
		//memory clean up
		foreach (Material mat in profilePicCache.Values)
		{
			Destroy(mat);
		}

		profilePicCache.Clear();
	}

	public void cleanup()
	{
		if (dismissTimer != null)
		{
			if (!dismissTimer.isExpired)
			{
				dismissTimer.destroy();
				removeItem();
			}
			else
			{
				dismissTimer.destroy();
			}
			
		}

		if (destroyTimer != null)
		{
			if (!destroyTimer.isExpired)
			{
				destroyTimer.destroy();
				onDestroyTimerExpired();
			}
			else
			{
				destroyTimer.destroy();
			}
			
		}

		if (inboxItem != null && inboxItem.expirationTimer != null)
		{
			inboxItem.expirationTimer.removeFunction(onTimerExpired);
		}

		if (clonedMaterial != null)
		{
			Destroy(clonedMaterial);
			clonedMaterial = null;
		}

		unregisterHandlers();
	}

	/*=========================================================================================
	BUTTON/EVENT HANDLING
	=========================================================================================*/
	protected virtual void registerHandlers()
	{
		if (buttonHandler != null)
		{
			buttonHandler.registerEventDelegate(onSelect);
		}

		if (closeButton != null)
		{
			closeButton.registerEventDelegate(onCloseClick);
		}

		if (viewProfileButton != null)
		{
			viewProfileButton.registerEventDelegate(onViewProfile);
		}
	}

	protected virtual void unregisterHandlers()
	{
		if (buttonHandler != null)
		{
			buttonHandler.unregisterEventDelegate(onSelect);
		}

		if (closeButton != null)
		{
			closeButton.unregisterEventDelegate(onCloseClick);
		}

		if (viewProfileButton != null)
		{
			viewProfileButton.unregisterEventDelegate(onViewProfile);
		}
	}

	private void onViewProfile(Dict args = null)
	{
		if (inboxItem != null)
		{
			SocialMember member = SocialMember.findByZId(inboxItem.senderZid);

			if (member != null)
			{
				Dialog.close();
				NetworkProfileDialog.showDialog(member, SchedulerPriority.PriorityType.IMMEDIATE);
			}
		}
	}

	

	private void onCloseClick(Dict args = null)
	{
		dismiss();
	}

	public virtual void onSelect(Dict args = null)
	{
		Bugsnag.LeaveBreadcrumb("Inbox item on click");
		action();
		playSelect();
	}

	/// <summary>
	/// Plays animation when user collects inbox item
	/// </summary>
	public virtual void playSelect()
	{
		if (buttonHandler != null)
		{
			SafeSet.gameObjectActive(buttonHandler.gameObject, false);
		}

		SafeSet.gameObjectActive(checkmark, true);
		
		//Since we use the same prefab for all the inbox message types, sound has to be dynamically set
		if (inboxItem.itemType == InboxItem.InboxType.FREE_CREDITS)
		{
			checkIntroAnimation.soundsPlayedDuringAnimation.audioInfoList[0].SOUND_NAME = CLAIM_COINS_SOUND;
		}
		else if (inboxItem.itemType == InboxItem.InboxType.SEND_CREDITS)
		{
			checkIntroAnimation.soundsPlayedDuringAnimation.audioInfoList[0].SOUND_NAME = SEND_COINS_SOUND;
		}
		
		StartCoroutine(AnimationListController.playAnimationInformation(checkIntroAnimation));

		if (buttonHandler != null)
		{
			buttonHandler.unregisterEventDelegate(onSelect);
		}

		dismissTimer = SmartTimer.create(DISMISS_DELAY, false, removeItem, "inbox_list_item_dismiss");
		dismissTimer.start();
		closeButton.gameObject.SetActive(false);
	}

	/// <summary>
	/// Launches the wrapper action, and runs the item accepted callbacked
	///
	/// This function is used for the regular inbox item with just a single primary action.
	/// </summary>
	public void action()
	{
		if (itemWrapper != null)
		{
			itemWrapper.action();
		}
		else
		{
			Debug.LogError("No item wrapper for inbox");
		}

		if (onItemAccepted != null)
		{
			onItemAccepted(this);
		}
	}

	/// <summary>
	/// Launches the wrapper action, and runs the item accepted callbacked
	///
	/// This function is used for the inbox item with more than one primary actions. e.g. inbox slot machine rating has
	/// primary options "love", "like" and "dislike".
	/// so the caller needs to specify the actionKey to get the correct action involved.
	/// </summary>
	public void action(string actionKey)
	{
		if (itemWrapper != null)
		{
			itemWrapper.action(actionKey);
		}
		else
		{
			Debug.LogError("No item wrapper for inbox");
		}

		if (onItemAccepted != null)
		{
			onItemAccepted(this);
		}
	}
	
	/// <summary>
	/// Dismisses the item from the user's inbox
	/// </summary>
	public void dismiss()
	{
		if (itemWrapper != null)
		{
			itemWrapper.dismiss();
		}

		removeItem();
	}

	public virtual void removeItem()
	{
		StartCoroutine(setItemAnimation(ItemAnimations.Outro));

		if (buttonHandler != null)
		{
			buttonHandler.gameObject.SetActive(false);
		}

		if (onItemRemoved != null)
		{
			onItemRemoved(this);
		}
		
		destroyTimer = new SmartTimer(DESTROY_DELAY, false, onDestroyTimerExpired, "inbox_list_item_destroy");
		destroyTimer.start();
	}

	public void onDestroyTimerExpired()
	{
		StopAllCoroutines();
		isDoingRootAnimation = false;
	
		if (onItemDestroyed != null)
		{
			onItemDestroyed(this);
		}
	}

	/*=========================================================================================
	SETUP METHODS
	=========================================================================================*/
	public virtual void init
	(
		InboxItem item,
		InboxTab.onRemoveItem onRemoveCallback,
		InboxTab.onAcceptItem onAcceptedCallback,
		InboxTab.onDestroyItem onDestroyCallback
	)
	{
		//For WebGL use the OnMouseDown to fix issue with opening external links in the browser
#if UNITY_WEBGL
		if (buttonHandler != null)
		{
			buttonHandler.registeredEvent = ClickHandler.MouseEvent.OnMouseDown;
		}
#endif
		inboxItem = item;
		onItemRemoved = onRemoveCallback;
		onItemAccepted = onAcceptedCallback;
		onItemDestroyed = onDestroyCallback;

		registerHandlers();
		toggleCoinBurst(false);
		toggleHelpCoinBurst(false);
		SafeSet.gameObjectActive(checkmark, false);

		StartCoroutine(setItemAnimation(ItemAnimations.Idle));

		setProfileImage();

		if (powerupSpawner != null)
		{
			powerupSpawner.useTextMasks = true;
		}
	}

	private void onNewMaterialCreated(Material mat)
	{
		SocialMember senderSocialMember = inboxItem.senderSocialMember;
		string fullURL = senderSocialMember.getImageURL;

		if (profilePicCache.Count < PICTURE_CACHE_LIMIT)
		{
			
#if UNITY_EDITOR
			//sanity check this in dev builds.  Overhead is not worth it in production builds
			//the leaked material will get cleaned up on scene change
			if (profilePicCache.TryGetValue(fullURL, out Material toDestroy))
			{
				Debug.LogError("Invalid profile image load");
				if (toDestroy != mat)
				{
					Destroy(mat);
				}
			}
#endif
			profilePicCache[fullURL] = mat;	
		}
		else
		{
			clonedMaterial = mat;
		}
	}

	private void setProfileImage()
	{
		if (inboxItem == null || inboxItem.senderSocialMember == null || profileImage == null || profileImage.gameObject == null)
		{
			return;
		}

		SocialMember senderSocialMember = inboxItem.senderSocialMember;

		string fullURL = senderSocialMember.getImageURL;

		if (profilePicCache.TryGetValue(fullURL, out Material mat))
		{
			profileImage.gameObject.SetActive(false);
			profileImage.material = mat;
			profileImage.gameObject.SetActive(true);
			return;
		}
		
		// Set the default image before trying to get the real one from Facebook.
		Texture2D newTexture = senderSocialMember.isFemale ? defaultFemale : defaultMale;
		if ((newTexture != null) && (profileImage != null))
		{
			clonedMaterial = new Material(profileImage.material);
			profileImage.material = clonedMaterial;
			profileImage.mainTexture = newTexture;
		}
		
		if (!fullURL.Contains("network_profiles/avatars"))
		{
			DisplayAsset.loadTextureToUITexture(profileImage, fullURL, PhotoSource.BACKUP_URL, true, showLoadingCircle:false, newMaterialCallback: onNewMaterialCreated);
		}
		else
		{
			//Attempt to load from the avatars bundle first and fallback to the fullURL
			string localAvatarPath = string.Format(NetworkAvatarSelectPanel.LOCAL_URL_FORMAT, DisplayAsset.textureNameFromRemoteURL(fullURL));
			DisplayAsset.loadTextureToUITexture(profileImage, localAvatarPath, fullURL, false, false, skipBundleMapping:true, pathExtension:".png", newMaterialCallback: onNewMaterialCreated);
		}
	}

	public virtual void setState(string state)
	{
		if (swapper != null)
		{
			swapper.setState(state);
		}

		state = CommonText.snakeCaseToPascalCase(state);

		try
		{
			if (itemWrapper == null)
			{
				string wrapperClass = string.Format("Inbox{0}ListItemWrapper", state);
				itemWrapper = Activator.CreateInstance(Type.GetType(wrapperClass)) as InboxListItemWrapper;

				if (itemWrapper != null)
				{
					itemWrapper.setup(this, inboxItem);
				}
				else
				{
					Debug.LogWarning("Couldn't find wrapper class: " + wrapperClass);
				}
			}
			else
			{
				Debug.LogWarning("No item wrapper found");
			}
		}
		catch (Exception e)
		{
			Debug.LogError("Unable to add inbox list item wrapper: " + state +  System.Environment.NewLine + e.ToString());
		}
	}

	public void refresh()
	{
		if (itemWrapper != null)
		{
			itemWrapper.refresh();
		}
	}

	public void reload()
	{
		if (itemWrapper != null)
		{
			itemWrapper.reload();
		}
	}

	public void removeWrapper()
	{
		if (itemWrapper != null)
		{
			itemWrapper = null;
		}
	}

	public void hide()
	{
		if (this == null)
		{
			return;
		}
		if (gameObject != null)
		{
			gameObject.SetActive(false);
		}
	}

	public void show()
	{
		if (gameObject != null)
		{
			gameObject.SetActive(true);
		}
	}

	/*=========================================================================================
	PUBLIC METHODS
	=========================================================================================*/
	public void setMessageLabel(string text)
	{
		if (messageLabel != null)
		{
			messageLabel.text = text;
		}
	}

	public virtual void setButtonText(string unlockedText, string lockedText = null)
	{
		if (buttonLabel != null)
		{
			buttonLabel.text = unlockedText;
		}
	}

	public void setTimer(bool active, string text = "")
	{
		if (timerLabel == null)
		{
			return;
		}
		
		if (active && inboxItem.showTimer)
		{
			timerLabel.transform.parent.gameObject.SetActive(true);

			if (inboxItem.expirationTimer != null && !inboxItem.isExpired)
			{
				if (string.IsNullOrEmpty(text))
				{
					inboxItem.expirationTimer.registerLabel(timerLabel, GameTimerRange.TimeFormat.REMAINING_HMS_FORMAT);
					inboxItem.expirationTimer.registerFunction(onTimerExpired);
				}
				else
				{
					timerLabel.text = text;
				}
			}
			// no expiration means, it doesn't expire
			else
			{
				timerLabel.transform.parent.gameObject.SetActive(false);
			}
		}
		else
		{
			timerLabel.transform.parent.gameObject.SetActive(false);
		}
	}

	public void onTimerExpired(Dict args = null, GameTimerRange originalTimer = null)
	{
		timerLabel.text = Localize.textUpper("expired");
		toggleButton(false);
	}

	public void setGameImage(string textureFile)
	{
		DisplayAsset.loadTextureToUITexture(gameImage, textureFile, "", shouldShowBrokenImage:true, skipBundleMapping:true, pathExtension:".png");
	}

	public void setButtonAnimation(ItemAnimations anim)
	{
		StartCoroutine(waitForListItemAnimThenSetButtonAnimation(anim));
	}

	protected virtual IEnumerator waitForListItemAnimThenSetButtonAnimation(ItemAnimations anim)
	{
		while (isDoingRootAnimation)
		{
			// Wait until the root item animation is done before setting the button
			// since the button object is controlled by that root animation.
			yield return null;
		}
		
		if (buttonHandler != null)
		{
			switch (anim)
			{
				case ItemAnimations.Idle:
					StartCoroutine(AnimationListController.playAnimationInformation(buttonIdleAnimation));
					break;

				case ItemAnimations.Intro:
					StartCoroutine(AnimationListController.playAnimationInformation(buttonIntroAnimation));
					break;

				case ItemAnimations.Outro:
					StartCoroutine(AnimationListController.playAnimationInformation(buttonOutroAnimation));
					break;

				case ItemAnimations.Off:
					StartCoroutine(AnimationListController.playAnimationInformation(buttonOffAnimation));
					break;
			}
		}
	}

	public IEnumerator setItemAnimation(ItemAnimations anim)
	{
		if (this == null || gameObject == null)
		{
			yield break;
		}
		
		isDoingRootAnimation = true;
		
		if (gameObject.activeSelf)
		{
			switch (anim)
			{
				case ItemAnimations.Idle:
					yield return StartCoroutine(AnimationListController.playAnimationInformation(idleAnimation));
					break;

				case ItemAnimations.Intro:
					yield return StartCoroutine(AnimationListController.playAnimationInformation(introAnimation));
					break;

				case ItemAnimations.Outro:
					yield return StartCoroutine(AnimationListController.playAnimationInformation(outroAnimation));

					if (checkmark != null && checkmark.activeSelf)
					{
						yield return StartCoroutine(AnimationListController.playAnimationInformation(checkOutroAnimation));
					}
					break;

				case ItemAnimations.Off:
					yield return StartCoroutine(AnimationListController.playAnimationInformation(offAnimation));
					break;
			}
		}

		isDoingRootAnimation = false;
	}

	public void toggleCloseButton(bool isActive)
	{
		if (closeButton != null)
		{
			closeButton.gameObject.SetActive(isActive);
		}
	}

	public void toggleGiftRibbon(bool isActive)
	{
		SafeSet.gameObjectActive(giftRibbon, isActive);
	}

	public void toggleCoinBurst(bool isActive)
	{
		if (coinBurst != null)
		{
			coinBurst.gameObject.SetActive(isActive);

			if (isActive)
			{
				coinBurst.Play("Burst");
			}
		}
	}

	public void toggleHelpCoinBurst(bool isActive)
	{
		if (helpFriendCoinBurst != null)
		{
			helpFriendCoinBurst.gameObject.SetActive(isActive);

			if (isActive)
			{
				helpFriendCoinBurst.Play("Burst");
			}
		}
	}

	public void toggleExclusiveOffer(bool isActive)
	{
		SafeSet.gameObjectActive(exclusiveOfferHeader, isActive);
	}

	public void loadTextureToBackground(string filepath)
	{
		if (backgroundImage != null)
		{
			DisplayAsset.loadTextureToUITexture(backgroundImage, BACKGROUND_PATH + filepath);
		}
	}

	/// <summary>
	/// Toggles the button on or off, and calls setButtonState()
	/// </summary>
	/// <param name="enabled"></param>
	public virtual void toggleButton(bool enabled)
	{
		if (buttonHandler != null)
		{
			SafeSet.gameObjectActive(buttonHandler.gameObject, enabled);
		}
		setButtonState(enabled);
	}

	/// <summary>
	/// Sets the button to the enabled/disabled state, and registers handlers as needed
	/// </summary>
	/// <param name="enabled"></param>
	public virtual void setButtonState(bool enabled)
	{
		if (buttonHandler != null)
		{
			buttonHandler.enabled = enabled;

			if (enabled)
			{
				buttonHandler.registerEventDelegate(onSelect);
			}
			else
			{
				buttonHandler.unregisterEventDelegate(onSelect);
			}
		}
	}

	public void setLimitReached(bool enabled)
	{
		SafeSet.gameObjectActive(limitReached, enabled);
	}
	
	public static void resetStaticClassData()
	{
		profilePicCache.Clear();
	}
}
