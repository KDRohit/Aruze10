using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Creates hyperlinks within UILabels.
*/

public class HyperLinker : TICoroutineMonoBehaviour
{
	private const string LINK_START = "<link ";			// Text that starts a hyperlink.
	private const string LINK_END = "</link>";			// Text that ends a hyperlink.
	private const string ACTION_START = "action=\"";	// Text that starts the action within a hyperlink.
	private const string ACTION_END = "\"";				// Text that ends the action within a hyperlink.

	public UILabel label;
	public GameObject functionTarget;
	public string functionName;
	public UIAtlas underlineAtlas;	// Uses the "White" sprite from this atlas to create the underlines.
	
	private Dictionary<string, LinkInfo> linkInfos = new Dictionary<string, LinkInfo>();

	private GameObject underlinesParent = null;
	private string lastText = "";
	
	private string[] lines = null;
	private Vector2 labelSize;
	private float ySpacing = 0;
	
	[System.Serializable]
	public class LinkInfo
	{
		public string text;
		public string action;
	}
	
	void Update()
	{
		if (lastText != label.processedText)
		{
			// Update the links whenever the text changes.
			createLinks();
			lastText = label.processedText;
		}
	}
	
	// Parses the text for hyperlink info and creates linkData from it.
	// This is useful for labels that use data-driven text instead of
	// a static localization key, since we don't know what hyperlinks
	// will be in the text until it's parsed.
	// "action" can use any actions that are supported by DoSomething.cs.
	// Format is "This is a <link action="game:oz01">hyperlink</link>."
	public void setLabelText(string text)
	{
		linkInfos.Clear();	// Just in case.
		
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
			
			if (!linkInfos.ContainsKey(linkText))
			{
				// Only create one instance of LinkInfo for each unique link text.
				// This limitation means all text that matches this link text must
				// behave exactly the same as each other. Shouldn't be a big deal,
				// and actually allows us to define one link and have it automatically
				// apply to all copies of the link text.
				LinkInfo info = new LinkInfo();
				info.text = linkText;
				info.action = action;
				linkInfos.Add(linkText, info);
			}
			
			string preText = text.Substring(0, start);
			string postText = text.Substring(end + LINK_END.Length);
			
			text = preText + linkText + postText;
		}
		
		label.text = text;	// Automatically calls createLinks() when the label text is set.
	}
	
	// Creates links for the label. Cleans up previously created links, if any.
	private void createLinks()
	{
		if (underlinesParent != null)
		{
			Destroy(underlinesParent);
		}
		
		// Create a common parent to put all the created objects under.
		underlinesParent = new GameObject();
		underlinesParent.layer = gameObject.layer;
		underlinesParent.name = gameObject.name + " Links";
		underlinesParent.transform.parent = transform.parent;
		underlinesParent.transform.localPosition = transform.localPosition;
		underlinesParent.transform.localScale = Vector3.one;
		underlinesParent.AddComponent<UIPanel>();	// Make sure the underlines render at the same z depth as the label.
	
		lines = label.processedText.Split('\n');
		labelSize = NGUIExt.getLabelPixelSize(label);
		ySpacing = labelSize.y / lines.Length;

		for (int i = 0; i < lines.Length; i++)
		{
			// Check each link data to see if this line has a link to make.
			foreach (LinkInfo linkInfo in linkInfos.Values)
			{
				// We pass in the link text separately then data, so we can
				// make multiple links for data that has text that wraps onto multiple lines.
				createLink(linkInfo.text, linkInfo.action, i);
			}
		}
	}
	
	// Creates a single link for the given text on the given line, if the line contains the text.
	// lines, labelSize and ySpacing are already defined before calling this.
	private bool createLink(string linkText, string action, int i)
	{
		if (i >= lines.Length)
		{
			return false;
		}
		
		string line = lines[i];

		if (!line.Contains(linkText))
		{
			// If the link text isn't found on the line, it's possible that the link text wraps to the next line.
			tryWrapping(linkText, action, i);
			return false;
		}
		
		// Figure out how the position and width of the text on this line.
		float baseX = 0;

		if (label.pivot != UIWidget.Pivot.TopLeft &&
			label.pivot != UIWidget.Pivot.Left &&
			label.pivot != UIWidget.Pivot.BottomLeft
			)
		{
			// If not left-aligned, then adjust the base X.
			float lineWidth = getTextWidth(line);
		
			// Adjust the base x position for the horizontal centering/pivot position. 
			switch (label.pivot)
			{
				case UIWidget.Pivot.Top:
				case UIWidget.Pivot.Bottom:
				case UIWidget.Pivot.Center:
					baseX = lineWidth * -.5f;
					break;
			
				case UIWidget.Pivot.TopRight:
				case UIWidget.Pivot.Right:
				case UIWidget.Pivot.BottomRight:
					baseX = -lineWidth;
					break;

				// No need to make adjustments for left pivots.
			}
		}
					
		float underlineX = baseX;
		float underlineY = (i + 1) * -ySpacing;
		float underlineWidth = getTextWidth(linkText);

		// The x position is the width of the text before the link text on this line.
		string textBefore = "";
		int startPos = line.IndexOf(linkText);
		if (startPos > 0)
		{
			textBefore = line.Substring(0, startPos);
			underlineX += getTextWidth(textBefore);
		}

		// Adjust the y position for the label's line spacing.
		// This is more complicated than you'd think, mainly because
		// the height of the label doesn't include the line spacing
		// for after the last line.
		underlineY += (float)label.lineSpacing * (1f - (float)i / lines.Length);
	
		// Adjust the y position for the vertical centering/pivot position. 
		switch (label.pivot)
		{
			case UIWidget.Pivot.Left:
			case UIWidget.Pivot.Right:
			case UIWidget.Pivot.Center:
				underlineY += labelSize.y * .5f;
				break;
			
			case UIWidget.Pivot.Bottom:
			case UIWidget.Pivot.BottomLeft:
			case UIWidget.Pivot.BottomRight:
				underlineY += labelSize.y;
				break;

			// No need to make adjustments for top pivots.
		}
						
		// Create the underline sprite.
		GameObject underlineObj = new GameObject();
		underlineObj.name = "Underline: " + linkText;
		underlineObj.layer = gameObject.layer;
		UISprite underline = underlineObj.AddComponent<UISprite>();
		underline.type = UISprite.Type.Sliced;
		underline.atlas = underlineAtlas;
		underline.spriteName = "White";
		underline.color = label.color;
		underline.pivot = UISprite.Pivot.Left;

		// Adjust thickness of the underline based on the font size.
		float underlineThickness = 4f * (transform.localScale.y / 66f);

		// Position and scale the underline.
		underlineObj.transform.parent = underlinesParent.transform;
		underlineObj.transform.localScale = new Vector3(underlineWidth, underlineThickness, 1);
		underlineObj.transform.localPosition = new Vector3(underlineX, underlineY, 0);
	
		// Create the collider for touching.
		GameObject colliderObj = new GameObject();
		colliderObj.name = "Collider: " + linkText;
		colliderObj.layer = gameObject.layer;
		BoxCollider collider = colliderObj.AddComponent<BoxCollider>();
		collider.center = new Vector3(.5f, .5f, 0f);

		// Position and scale the collider.
		colliderObj.transform.parent = underlinesParent.transform;
		colliderObj.transform.localScale = new Vector3(underlineWidth, ySpacing, 1);
		colliderObj.transform.localPosition = underlineObj.transform.localPosition;
	
		// Wire up the collider to respond to touches.
		UIButtonMessage msg = colliderObj.AddComponent<UIButtonMessage>();
		msg.target = functionTarget;
		msg.functionName = functionName;
		
		HyperLinkData dataScript = colliderObj.AddComponent<HyperLinkData>();
		dataScript.action = action.ToLower();
		
		return true;
	}
	
	// Try wrapping the link to a second line.
	private void tryWrapping(string linkText, string action, int i)
	{
		string line = lines[i];
		string nextLine = "";
		string firstPart = linkText;
		string lastPart = "";
		string previousBreakingChar = "";
		
		if (i < lines.Length - 1)
		{
			nextLine = lines[i + 1];
		}
		
		// Keep parsing off the link text's last words into a second part until we might find a match.
		while (containsBreakingChar(firstPart))
		{
			// Since text can wrap on various characters, check for all of them.
			bool didAddBreakingChar = false;
			string breakingChar = "";
			int lastBreak = -1;
			foreach (string ch in UIFont.breakingChars)
			{
				int index = firstPart.LastIndexOf(ch);
				if (index > lastBreak)
				{
					lastBreak = index;
					breakingChar = ch;
				}
			}
			
			lastPart = firstPart.Substring(lastBreak + 1) + previousBreakingChar + lastPart;
			firstPart = firstPart.Substring(0, lastBreak);
			
			if (breakingChar != " ")
			{
				// If the previous breaking character wasn't a space,
				// then we can expect it to still be present at the end of the first part.
				// So, we need to ignore that when checking for a breaking character.
				// When wrapping on anything except a space, we re-add the character at the end of the line.
				firstPart += breakingChar;
				didAddBreakingChar = true;
			}
			
			// Remember what character was used for link breaking, so it can be re-added in the next iteration if necessary.
			// This must be stored after adding it to the lastPart above, so it can be used only on the next loop.
			previousBreakingChar = breakingChar;

			// Alright, see if this line ends with with the first part of the link text,
			// and the next line starts with the last part of the link text.
			if (line.EndsWith(firstPart) && nextLine.StartsWith(lastPart))
			{
				// Seems to be a match, so try creating the link for the first part.
				if (createLink(firstPart, action, i))
				{
					// If successfully created a link with the first part of the text,
					// then try also creating a link with the last part of the text on the next line.
					createLink(lastPart, action, i + 1);
				}
			}
			
			if (didAddBreakingChar)
			{
				// Remove that breaking character from the previous first part,
				// so we don't get stuck in an infinite loop when checking for
				// the next part's breaking character.
				firstPart = firstPart.Substring(0, firstPart.Length - 1);
			}
		}
	}
	
	// Does the string contain a breaking character?
	private bool containsBreakingChar(string text)
	{
		foreach (string breaking in UIFont.breakingChars)
		{
			// Only return true if the text doesn't end with a breaking character,
			// since we keep non-space breaking characters at the end of broken text.
			if (text.Contains(breaking) && !text.EndsWith(breaking))
			{
				return true;
			}
		}
		return false;
	}

	// Get the pixel width of the block of text when using the same font as the main label.
	private float getTextWidth(string text)
	{
		GameObject go = new GameObject();
		UILabel labelTemp = go.AddComponent<UILabel>();
		go.layer = gameObject.layer;
		labelTemp.font = label.font;
		labelTemp.text = text;
		labelTemp.transform.parent = transform.parent;
		labelTemp.transform.localScale = transform.localScale;
		labelTemp.transform.localPosition = Vector3.zero;
		
		float width = NGUIExt.getLabelPixelSize(labelTemp).x;
		
		Destroy(go);
		
		return width;
	}
}
