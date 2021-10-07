using UnityEngine;
using System.Collections;
using Com.Scheduler;
using TMPro;

public class InboxListItemRichPass : InboxListItem
{
	private bool isGoldPassRequired;

	[SerializeField] private ButtonHandler richPassButtonHandler;
	[SerializeField] private TextMeshPro richPassButtonLabel;
	[SerializeField] private TextMeshPro richPassLockedButtonLabel;

	public ObjectSwapper spinStateSwapper;
	public ObjectSwapper richPassButtonSwapper;

	public override void init
	(
		InboxItem item,
		InboxTab.onRemoveItem onRemoveCallback,
		InboxTab.onAcceptItem onAcceptedCallback,
		InboxTab.onDestroyItem onDestroyCallback
	)
	{
		base.init(item, onRemoveCallback, onAcceptedCallback, onDestroyCallback);

		initButton();
		StartCoroutine(setItemAnimation(ItemAnimations.Idle));
		
		if (CampaignDirector.richPass != null)
		{
			CampaignDirector.richPass.onPassTypeChanged -= onRichPassTypeChanged;
			CampaignDirector.richPass.onPassTypeChanged += onRichPassTypeChanged;
		}

		if (spinStateSwapper != null)
		{
			spinStateSwapper.setState("default");
		}
	}

	private void onRichPassTypeChanged(string newPassType)
	{
		switch (newPassType)
		{
			case "gold":
				richPassButtonSwapper.setState("unlocked");
				break;
			
			case "silver":
				richPassButtonSwapper.setState("locked");
				break;
		}
	}

	private void initButton()
	{
		if (buttonHandler != null)
		{
			buttonHandler.gameObject.SetActive(false);
		}

		if (richPassButtonHandler != null)
		{
			richPassButtonHandler.gameObject.SetActive(false);
		}

		ButtonHandler handler = getActiveButtonHandler();
		if (handler != null)
		{
			Animator anim = handler.gameObject.GetComponent<Animator>();
			setButtonAnimationTarget(anim);
			handler.gameObject.SetActive(true);
		}
	}

	protected void setButtonAnimationTarget(Animator anim)
	{
		if (anim == null)
		{
			Debug.LogWarning("Invalid button animator");
			return;
		}

		buttonIdleAnimation.targetAnimator = anim;
		buttonIntroAnimation.targetAnimator = anim;
		buttonOffAnimation.targetAnimator = anim;
		buttonOutroAnimation.targetAnimator = anim;
	}

	/// <inheritdoc/>
	protected override void registerHandlers()
	{
		base.registerHandlers();

		if (richPassButtonHandler != null)
		{
			richPassButtonHandler.registerEventDelegate(onRichPassSelect);
		}
	}

	/// <inheritdoc/>
	protected override void unregisterHandlers()
	{
		base.unregisterHandlers();

		if (richPassButtonHandler != null)
		{
			richPassButtonHandler.unregisterEventDelegate(onRichPassSelect);
		}
	}

	/// <inheritdoc/>
	public override void removeItem()
	{
		StartCoroutine(setItemAnimation(ItemAnimations.Outro));

		ButtonHandler handler = getActiveButtonHandler();
		if (handler != null)
		{
			handler.gameObject.SetActive(false);
		}

		if (onItemRemoved != null)
		{
			onItemRemoved(this);
		}

		destroyTimer = new SmartTimer(DESTROY_DELAY, false, onDestroyTimerExpired, "inbox_list_item_rich_pass_destroy");
		destroyTimer.start();
	}

	public void setGoldPassRequirement(bool required)
	{
		//update flag
		isGoldPassRequired = required;

		//initialize button
		if (required)
		{
			bool unlocked = CampaignDirector.richPass != null &&
			                CampaignDirector.richPass.isActive &&
			                CampaignDirector.richPass.passType == "gold";
			richPassButtonSwapper.setState( unlocked ? "unlocked" : "locked");
		}
		initButton();

		//set spin panel state
		spinStateSwapper.setState(required ? "rich_pass" : "default");
	}

	private ButtonHandler getActiveButtonHandler()
	{
		if (isGoldPassRequired)
		{
			return richPassButtonHandler;
		}
		else
		{
			return buttonHandler;
		}
	}

	public void onRichPassSelect(Dict args = null)
	{
		Bugsnag.LeaveBreadcrumb("Rich Pass Inbox List Item:  Gold Free Spins - On Click");
		if (CampaignDirector.richPass != null && CampaignDirector.richPass.isActive)
		{
			if (CampaignDirector.richPass.isPurchased())
			{
				onSelect();
			}
			else
			{
				RichPassUpgradeToGoldDialog.showDialog("gold_game", SchedulerPriority.PriorityType.IMMEDIATE);
			}
		}
		else
		{
			Debug.LogWarning("rich pass unavailable");
			//turn off button handler so they don't spam it
			richPassButtonHandler.enabled = false;
		}
	}

	/// <inheritdoc/>
	public override void playSelect()
	{
		Bugsnag.LeaveBreadcrumb("Rich Pass Inbox List Item:  Free Spins - On Click");
		ButtonHandler handler = getActiveButtonHandler();
		SafeSet.gameObjectActive(handler.gameObject, false);
		SafeSet.gameObjectActive(checkmark, true);

		StartCoroutine(AnimationListController.playAnimationInformation(checkIntroAnimation));

		buttonHandler.unregisterEventDelegate(onSelect);

		dismissTimer = SmartTimer.create(DISMISS_DELAY, false, removeItem, "inbox_list_item_dismiss");
		dismissTimer.start();
		closeButton.gameObject.SetActive(false);
		richPassButtonHandler.unregisterEventDelegate(onRichPassSelect);
	}

	/// <inheritdoc/>
	public override void setButtonText(string unlockedText, string lockedText = null)
	{
		if (string.IsNullOrEmpty(lockedText))
		{
			lockedText = unlockedText;
		}

		base.setButtonText(unlockedText, lockedText);

		if (richPassButtonLabel != null)
		{
			richPassButtonLabel.text = unlockedText;
		}

		if (richPassLockedButtonLabel.text != null)
		{
			richPassLockedButtonLabel.text = lockedText;
		}
	}

	/// <inheritdoc/>
	public override void setButtonState(bool enabled)
	{
		base.setButtonState(enabled);

		if (richPassButtonHandler != null)
		{
			richPassButtonHandler.enabled = enabled;
			if (enabled)
			{
				richPassButtonHandler.registerEventDelegate(onRichPassSelect);
			}
			else
			{
				richPassButtonHandler.unregisterEventDelegate(onRichPassSelect);
			}
		}
	}

	/// <inheritdoc/>
	protected override IEnumerator waitForListItemAnimThenSetButtonAnimation(ItemAnimations anim)
	{
		while (isDoingRootAnimation)
		{
			// Wait until the root item animation is done before setting the button
			// since the button object is controlled by that root animation.
			yield return null;
		}
	
		ButtonHandler handler = getActiveButtonHandler();
		if (handler != null && handler.gameObject.activeSelf)
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

	/// <inheritdoc/>
	public override void toggleButton(bool enabled)
	{
		//exit early if we're already being destroyed
		if (this == null || gameObject == null)
		{
			return;
		}
			
		if (buttonHandler != null)
		{
			SafeSet.gameObjectActive(buttonHandler.gameObject, false);	
		}
		
		if (richPassButtonHandler != null)
		{
			SafeSet.gameObjectActive(richPassButtonHandler.gameObject, false);	
		}
		
		ButtonHandler handler = getActiveButtonHandler();
		if (handler != null)
		{
			SafeSet.gameObjectActive(handler.gameObject, enabled);
		}
		
		setButtonState(enabled);
		setButtonAnimation(ItemAnimations.Idle);
	}

	private void OnDestroy()
	{
		if (CampaignDirector.richPass != null)
		{
			CampaignDirector.richPass.onPassTypeChanged -= onRichPassTypeChanged;	
		}
	}
}
