using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InboxListItemOOC : InboxListItem
{
	[SerializeField] protected GameObject freeCoinsTag;
	
	public void enableFreeCoinsTag(bool enabled)
    {
    	SafeSet.gameObjectActive(freeCoinsTag, enabled);
    }
}
