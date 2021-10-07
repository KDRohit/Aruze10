using UnityEngine;
using System.Collections;

public class InboxListItemWrapper
{
	protected InboxListItem listItem;
	protected InboxItem inboxItem;

	protected const string MISSING_FRIEND_NAME = "inbox_missing_friend";

	public virtual void setup(InboxListItem listItem, InboxItem inboxItem)
	{
		this.listItem = listItem;
		this.inboxItem = inboxItem;
		
		if (inboxItem.expirationTimer != null)
		{
			string text = inboxItem.expirationTimer.timeRemaining < Common.SECONDS_PER_DAY ? Localize.text("expires_soon") : "";
			listItem.setTimer(true, text);
		}
		else
		{
			listItem.setTimer(false);
		}
	}

	/// <summary>
	/// Refresh any elements of the list item as needed
	/// </summary>
	public virtual void refresh()
	{

	}

	/// <summary>
	/// Reruns setup() again
	/// </summary>
	public void reload()
	{
		setup(listItem, inboxItem);
	}

	/// <summary>
	/// Run any specific actions this wrapper needs to take
	///
	/// This function is used for the regular inbox item with just a single primary action.
	/// </summary>
	public virtual void action()
	{
		if (inboxItem != null)
		{
			InboxInventory.action(inboxItem);
		}
		else
		{
			Debug.LogError("Invalid inbox item");
		}
	}
	
	/// <summary>
	/// Run any specific actions this wrapper needs to take
	///
	/// This function is used for the inbox item with more than one primary actions. e.g. inbox slot machine rating has
	/// primary options "love", "like" and "dislike".
	/// so the caller needs to specify the actionKey to get the correct action involved.
	/// </summary>
	public virtual void action(string actionKey)
	{
		if (inboxItem != null)
		{
			InboxInventory.action(inboxItem, actionKey);
		}
		else
		{
			Debug.LogError("Invalid inbox item");
		}
	}

	/// <summary>
	/// Run any specific tasks to be executed when the list item is closed/removed/dismissed
	/// </summary>
	public virtual void dismiss()
	{
		if (inboxItem != null)
		{
			InboxInventory.dismiss(inboxItem);
		}
	}

	/// <summary>
	/// Sets the CTA button for the list item
	/// </summary>
	protected virtual void setButton()
	{

	}
}