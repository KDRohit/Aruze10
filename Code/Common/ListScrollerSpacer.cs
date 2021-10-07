using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Attach this script to parts of a list item prefab that will be used with the ListScroller,
but that you don't want to see rendered. The list scroller leaves 0 space between items,
so if you want space between them, you need to create a spacing sprite (like a "White" stretchy one),
size and position it for your spacing, and attach this script. The ListScroller will automatically
hide any sprite that has this script after it has spaced things.
*/

public class ListScrollerSpacer : TICoroutineMonoBehaviour
{
	// Set this true when you have a visible element that goes outside the normal spacing bounds of the item,
	// and you don't want it to affect the spacing.
	// It essentially does the opposite of having a hidden object that affects spacing.
	public bool doIgnoreForSpacing = false;
}
