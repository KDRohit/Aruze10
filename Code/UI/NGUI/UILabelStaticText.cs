using UnityEngine;
using System.Collections;
using TMPro;

/**
A simple script to localize static UI labels once upon startup.
*/
public class UILabelStaticText : TICoroutineMonoBehaviour
{
	public UILabel label;					// The label that needs to be localized.
	public TextMeshPro labelTMPro;			// Another possible label to set the text to.
	public string localizationKey = "";		// The key used for localizing the text on the label upon start.
	public bool titleCase = false;			// Should the text be in title case? 
	public bool allCaps = false;			// Should the text be set to all upper case?
	public bool addColon = false;			// Should a colon be added to the end of the text?
	public bool replaceSpacesWithNewLines = false; // Should new lines be inserted between each word?
	public bool allLowerCAse = false;   	// Should the text be all lower case
	public int spacesBetweenCharacters = 0;	// The number of spaces to put between each character of the localized string.

	void Awake()
	{
		doLocalization();
	}
	
	public string text
	{
		set
		{
			if (label != null)
			{
				label.text = value;
			}
			if (labelTMPro != null)
			{
				labelTMPro.text = value;
			}
		}
		get
		{
			if (label != null)
			{
				return label.text;
			}
			if (labelTMPro != null)
			{
				return labelTMPro.text;
			}
			return "";
		}
	}
	
	public void doLocalization(string newLocalizationKey = "")
	{
		if (newLocalizationKey != "")
		{
			localizationKey = newLocalizationKey;
		}
		
		// If there is no label assigned, check for it on this object
		if (label == null)
		{
			label = gameObject.GetComponent<UILabel>();
		}

		// If there is no TextMeshPro assigned, check for it on this object
		if (labelTMPro == null)
		{
			labelTMPro = gameObject.GetComponent<TextMeshPro>();
		}

		
		if (localizationKey != "")
		{
			// If a static text localization key is provided, then localize it when the game starts and set the text value.
			string localText = "";
		
			if (allCaps)
			{
				localText = Localize.textUpper(localizationKey);
			}
			else if (allLowerCAse)
			{
				localText = Localize.textLower(localizationKey);
			}
			else if (titleCase)
			{
				localText = Localize.textTitle(localizationKey);
			}
			else
			{
				localText = Localize.text(localizationKey);
			}
		
			if (addColon)
			{
				localText += ":";
			}

			if (replaceSpacesWithNewLines)
			{
				localText = CommonText.replaceSpacesWithNewLines(localText);
			}
		
			localText = CommonText.spaceOut(localText, spacesBetweenCharacters);
		
			if (label != null)
			{
				label.text = localText;
			}
			
			if (labelTMPro != null)
			{
				labelTMPro.text = localText;
			}

			// No longer need this script attached after it is run once.
			// Only destroy this if localization was done, so we can manally
			// call this after programmatically setting the localization key.
			Destroy(this);
		}
	}
}
