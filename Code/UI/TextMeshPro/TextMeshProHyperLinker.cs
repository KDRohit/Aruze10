using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Helper for parsing and acting upon touches on hyperlinks in TextMeshPro objects.
To use it, simply attach it to a TextMeshPro game object and link the TextMeshPro inspector property.
*/

public class TextMeshProHyperLinker : TICoroutineMonoBehaviour
{
	private const string LINK_START = "<link";		// Text that starts a hyperlink.
	private const string LINK_END = "</link>";		// Text that ends a hyperlink.
	private const string ACTION_START = "link=\"";	// Text that starts the action within a hyperlink.
	private const string ACTION_END = "\"";			// Text that ends the action within a hyperlink.

	public TextMeshPro label;
	
	private List<string> linkActions = new List<string>();
	private string lastText = "";
	private Camera linkCamera = null;	// Needed for determining which link was touched.

#if UNITY_WEBGL && !UNITY_EDITOR
	private bool isTouching = false;
#endif
	
	void Start()
	{
		linkCamera = NGUIExt.getObjectCamera(label.gameObject);
	}
	
	void Update()
	{
		if (lastText != label.text)
		{
			parseLinks();
		}
		
#if UNITY_WEBGL && !UNITY_EDITOR
		if (TouchInput.isTouchDown && !isTouching)
		{
			isTouching = true;
#else
		// See if a link is touched.
		if (TouchInput.didTap)
		{
#endif
	        int linkIndex = TMP_TextUtilities.FindIntersectingLink(label, Input.mousePosition, linkCamera);
			
			if (null == linkActions || linkIndex >= linkActions.Count)
			{
				Debug.LogError("No actions found for link index " + linkIndex + " in text:  " + label.text);
			}
			else if (linkIndex > -1)
			{
				DoSomething.now(linkActions[linkIndex]);
			}
		}
#if UNITY_WEBGL && !UNITY_EDITOR
		else if (!TouchInput.isTouchDown)
		{
			isTouching = false;
		}
#endif
	}
	
	// Parses the text for hyperlink info and creates linkData from it.
	// This is useful for labels that use data-driven text instead of
	// a static localization key, since we don't know what hyperlinks
	// will be in the text until it's parsed.
	// The value can be any actions that are supported by DoSomething.cs.
	// Format is "This is a <link="game:oz01">hyperlink</link>."
	public void parseLinks()
	{
		// Update the links whenever the text changes.
		// Fix the link tags with the format that TextMeshPro understands,
		// which is slightly different than our own format was.
		string text = label.text;
		
		text = text.Replace("<link action=", "<link=");
		
		// TextMeshPro doesn't automatically make links underlined, so manually do that here.
		text = text.Replace("<link", "<u><link");
		text = text.Replace("</link>", "</link></u>");
		
		label.text = text;

		lastText = text;

		linkActions.Clear();	// Just in case.

		while (text.Contains(LINK_START) && text.Contains(LINK_END))
		{
			int start = text.IndexOf(LINK_START);
			int startClose = text.IndexOf(">", start);
			int end = text.IndexOf(LINK_END, startClose);
			
			string link = text.Substring(start, startClose - start + 1);
			string linkText = text.Substring(startClose + 1, end - startClose - 1);
			
			int startAction = link.IndexOf(ACTION_START) + ACTION_START.Length;
			int endAction = link.IndexOf(ACTION_END, startAction);

			string action = link.Substring(startAction, endAction - startAction);
			
			linkActions.Add(action);
			
			string preText = text.Substring(0, start);
			string postText = text.Substring(end + LINK_END.Length);
			
			text = preText + linkText + postText;
		}
	}
}
