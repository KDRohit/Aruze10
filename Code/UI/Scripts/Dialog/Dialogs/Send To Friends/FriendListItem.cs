using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;

/**
Basic script to define labels for each tab.
*/

public class FriendListItem : MonoBehaviour
{
	public FacebookFriendInfo friendInfo;
	public TextMeshPro flavorTextLabel;
	public GameObject checkbox;
	public int nameLabelNoFlavorTextHeight = 136;
	public int nameLabelWithFlavorTextHeight = 80;
	public int nameLabelNoFlavorTextY = -80;
	public int nameLabelWithFlavorTextY = -46;

	//This is used to fix the pink texture issue on the mfs dialog
	public bool useShader = false;
	public ButtonHandler button;
	public UISprite border;
	private MFSDialog dialog;
	public bool isChecked { get; private set; }

	public SocialMember member;

	private static string FRIEND_PANEL_BACKGROUND_NORMAL = "Button Rectangle04";
	private static string FRIEND_PANEL_BACKGROUND_DISABLED = "Button Rectangle04 Inactive";

	// Getter/Setter for the label that handles null checking.
	public string flavorText
	{
		get
		{
			if (flavorTextLabel != null)
			{
				return flavorTextLabel.text;
			}
			else
			{
				return "";
			}

		}
		set
		{
			if (flavorTextLabel != null)
			{
				flavorTextLabel.text = value;
				
				if (value == "")
				{
					// TODO:UNITY2018:obsoleteTextContainer:confirm
					friendInfo.nameTMPro.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, nameLabelNoFlavorTextHeight);
					CommonTransform.setY(friendInfo.nameTMPro.transform, nameLabelNoFlavorTextY);
				}
				else
				{
					// TODO:UNITY2018:obsoleteTextContainer:confirm
					friendInfo.nameTMPro.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, nameLabelWithFlavorTextHeight);
					CommonTransform.setY(friendInfo.nameTMPro.transform, nameLabelWithFlavorTextY);
				}
			}
		}
	}

	public bool isEnabled
	{
		set
		{
			button.isEnabled = value;
			border.enabled = value;
		}
	}

	public void init(SocialMember member, MFSDialog.Mode mode, MFSDialog dialog, bool isSelected = false)
	{
		// Setup the list item.
		this.dialog = dialog;
		this.member = member;

		friendInfo.member = member;

		if (member == null || !MFSDialog.isMemberValid(member, dialog.currentMode))
		{
			flavorText = getCooldownText(mode);
			// Sanity check the index here.
			isEnabled = false;
		}
		else
		{
			if (member == null || string.IsNullOrEmpty(member.playedGame))
			{
				// In the case that they are not a previous zynga name player, 
				// we need to set it to the empty string if they don't have a game name.
				flavorText = "";
			}
			else
			{
				string gameName = Localize.text(string.Format("zynga_game_{0}", member.playedGame));
				flavorText = Localize.text("plays_{0}", gameName);
			}
			isEnabled = true;
		}
		setSelected(isSelected);
		button.clearAllDelegates();
		button.registerEventDelegate(onButtonClick);
	}

	public void Awake()
	{
		useShader = true;
	}

	public void OnDestroy()
	{
		useShader = false;
	}

	public TextMeshPro[] getLabels()
	{
		return new TextMeshPro[] {flavorTextLabel, friendInfo.nameTMPro};
	}

	public void setSelected(bool isSelected)
	{
		// Determine if we can select it, and if we can, then set it to the selected state
		// otherwise set it to deselected.
		isChecked = isSelected && MFSDialog.isMemberValid(member, dialog.currentMode);
		border.color = getBorderColor(isChecked);
		checkbox.SetActive(isChecked);
	}

	private Color getBorderColor(bool isChecked)
	{
		return CommonColor.colorFromHex(isChecked ? "00FF00" : "6f328b");
	}

	private string getSpriteName(bool isChecked)
	{
		return isChecked ? FRIEND_PANEL_BACKGROUND_NORMAL : FRIEND_PANEL_BACKGROUND_DISABLED;
	}

	private string getCooldownText(MFSDialog.Mode mode)
	{
		switch (mode)
		{
			case MFSDialog.Mode.ASK:
				return Localize.text("mfs_ask_credits_cooldown");
			case MFSDialog.Mode.CREDITS:
				return Localize.text("mfs_credits_sent_cooldown");
			case MFSDialog.Mode.SPINS:
				return Localize.text("mfs_spins_sent_cooldown");
		}
		Bugsnag.LeaveBreadcrumb("FriendListItem.cs -- getCooldownText() -- no valid mode found so using empty string.");
		return "";
	}

	/// NGUI button callback.
	private void onButtonClick(Dict args = null)
	{
		setSelected(!isChecked);
		dialog.itemClicked(this);
	}
}
