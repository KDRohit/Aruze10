using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Holds data about a hyperlink that is used with the HyperLinker script.
Also parses the data when called for, such as when a link is touched.

This can be done by doing this in the NGUI button callback function for a link:

// NGUI button callback
private void linkClicked(GameObject go)
{
	HyperLinkData data = go.GetComponent<HyperLinkData>();
	if (data != null)
	{
		DoSomething.now(data.action);
	}
}
*/

public class HyperLinkData : TICoroutineMonoBehaviour
{
	public string action;
}
