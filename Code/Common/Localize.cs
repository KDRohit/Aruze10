using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;


/**
Holds a dictionary of Text translations for multiple languages
*/
public static class Localize
{
	/// A dictionary that stores the text translations
	private static Dictionary<string,string> translations = new Dictionary<string,string>();

    public const string DELIMITER = "_";
	
	public static bool isPopulated = false;
	public static string language
	{
		get { return _language; }
		
		set
		{
			// Validate that the language is one that we support.
			// If not, fall back to english.
			switch (value)
			{
				case "dutch":
				case "english":
				case "french":
				case "german":
				case "portuguese":
				case "spanish":
				case "turkish":
					_language = value;
					break;
				default:
					_language = "english";
					break;
				
			}

			AnalyticsManager.Instance.LogLocale(Localize.locale);
			CustomLog.Log.log("Set language to: " + _language);
		}
	}
	private static string _language = "english";
	
	// Return default locale string for currently set language.
	public static string locale
	{
		get
		{
			switch (_language)
			{
				case "dutch":
					return "nl_NL";
				case "english":
					return "en_US";
				case "french":
					return "fr_FR";
				case "german":
					return "de_DE";
				case "portuguese":
					return "pt_BR";
				case "spanish":
					return "es_ES";
				case "turkish":
					return "tr_TR";
				default:
					return "en_US";
			}
		}

		private set {;}
	}

	public static void populateAll(JSON[] data)
	{
		foreach (JSON text in data)
		{
			addText(text.getString("key", ""), text.getString("contents", ""));
		}
		
		isPopulated = true;
	}

	/// Adds some text to the dictionary
	public static void addText(string key, string value)
	{
		// Replace literal \n with a newline character.
		value = value.Replace("\\n", "\n");

		translations[key] = value;
	}
	
	public static bool keyExists(string textKey)
	{
		return textKey != null && translations.ContainsKey(textKey);
	}
	
	/// Returns translated text as upper case.
	public static string textUpper(string textKey, params object[] args)
	{
		return text(textKey, true, false, false, args);
	}

	/// Returns translated text as lower case.
	public static string textLower(string textKey, params object[] args)
	{
		return text(textKey, false, true, false, args);
	}

	/// Returns tranlsated text as title case.
	public static string textTitle(string textKey, params object[] args)
	{
		return text(textKey, false, false, true, args);
	}

	// Returns the localized key if that exists, or return the plaintext version that we passed in.
	public static string textOr(string textKey, string alternative)
	{
		if (keyExists(textKey))
		{
			return text(textKey);
		}
		return alternative;
	}

	// Returns the localized key if that exists, or return the plaintext version that we passed in.
	public static string textOrUpper(string textKey, string alternative)
	{
		if (keyExists(textKey))
		{
			return textUpper(textKey);
		}
		return toUpper(alternative);
	}

	/// Returns translated text as-is.
	public static string text(string textKey, params object[] args)
	{
		return text(textKey, false, false, false, args);
	}
	
	// Returns the name of the game.
	public static string getGameName(bool longForm = true)
	{
#if ZYNGA_SKU_HIR
		if (longForm)
		{
			return Localize.textOr("game_name", "Hit It Rich");
		}
		else
		{
			return Localize.textOr("game_name_simple_hir", "Hit It Rich");
		}
#else
		return "UNKNOWN";
#endif
	}
	
	private static string text(string textKey, bool doUpperCase, bool doLowerCase, bool doTitleCase, object[] args)
	{
		if (!isPopulated)
		{
			// Don't show any text if the localizations haven't been populated from global data yet.
			// Exception for "ok", mainly for the error dialog button if it happens due to connectivity issues.
			if (textKey == "ok")
			{
				return "OK";
			}
			return "";
		}
		
		string localizedFormatString = string.Empty;
		
		if (textKey == null)
		{
			Debug.LogWarning("Localization key was null.");
			return "";
		}
		else if (textKey.Trim() == "")
		{
			// Pass through whitespace-only items without warnings
			return textKey;
		}
		else if (translations.TryGetValue(textKey, out localizedFormatString))
		{
			// nothing to do, localizedFormatString is assigned in TryGetValue
		}
		else
		{
			// no longer a warning; don't spam the same 50+ localization warnings to every crittercism report
			Debug.Log("Localization key not found: " + textKey);
			localizedFormatString = textKey.Replace("_", "_ ");	// Add spaces so word wrapping will at least work on the key.
		}
		
		// If filterSpecialWords becomes a big method we may want to make it a separate call
		// for special cases.
		localizedFormatString = filterSpecialWords(localizedFormatString);
		
		if (args != null && args.Length > 0)
		{
			try
			{
				localizedFormatString = System.String.Format(localizedFormatString, args);
			}
			catch
			{
				localizedFormatString = "Error Formatting: " + localizedFormatString;
			}
		}
				
		if (doUpperCase)
		{
			localizedFormatString = toUpper(localizedFormatString);
		}
		else if (doLowerCase)
		{
			localizedFormatString = toLower(localizedFormatString);
		}
		else if (doTitleCase)
		{
			localizedFormatString = toTitle(localizedFormatString);
		}

		return localizedFormatString;
	}
	
	/// Turns a string into upper-case, with certain language exceptions taken into consideration.
	public static string toUpper(string text)
	{
		switch (language)
		{
			case "dutch":
			case "english":
			case "french":
			case "german":
			case "portuguese":
			case "spanish":
				// No special handling for these languages at this time.
				break;
			
			case "turkish":
				// Turkish has two different letter i's, and the default ToUpper() behavior capitalizes
				// the upper case i incorrectly, so we replace it manually before calling ToUpper().
				
				// For some reason using the actual characters "ı" and "İ" aren't recognized, but the unicode values are.
				text = text.Replace("i", "\u0130");	// Unicode for İ - dotted upper-case I.
				text = text.Replace("\u0131", "I");	// Unicode for ı - non-dotted lower-case i.
				break;
		}
		
		return text.ToUpper();
	}

	/// Turns a string into lower-case, with certain language exceptions taken into consideration.
	public static string toLower(string text)
	{
		switch (language)
		{
			case "dutch":
			case "english":
			case "french":
			case "german":
			case "portuguese":
			case "spanish":
				// No special handling for these languages at this time.
				break;
			
			case "turkish":
				// Turkish has two different letter i's, and the default ToLower() behavior doesn't
				// handle it correctly, so we replace it manually before calling ToLower().

				// For some reason using the actual characters "ı" and "İ" aren't recognized, but the unicode values are.
				text = text.Replace("\u0130", "i");	// Unicode for İ - dotted upper-case I.
				text = text.Replace("I", "\u0131");	// Unicode for ı - non-dotted lower-case i.
				break;
		}
		
		return text.ToLower();
	}
	
	/// Turns a string into title-case, with certain language exceptions taken into consideration.
	public static string toTitle(string text)
	{		
		// Since we had build problems while using CultureInfo objects, we must do this the brute-force way.
		// Just make sure this function isn't being called in every game loop, since it is especially intensive.
		string[] lines = text.Split('\n');
		StringBuilder sb = new StringBuilder();
	
		bool appendNewLine = false;

		string line = "";
		string firstChar = "";
		string otherChars = "";
		string word = "";
					
		// Capitalize the first letter of each word while retaining line breaks of the original text.
		for (int i = 0; i < lines.Length; i++)
		{
		    line = lines[i];
			if (appendNewLine)
			{
				sb.Append("\n");
			}
			
			if (line != "")
			{
				string[] words = line.Split(' ');
				for (int j = 0; j < words.Length; j++)
				{
				    word = words[j];
				    firstChar = "";
				    otherChars = "";

					if (word != "")
					{
						firstChar = word.Substring(0, 1);
						if (word.Length > 1)
						{
							otherChars = word.Substring(1);
						}
					}
					
					// Now add the word with capitalization added in.
					sb.Append(toUpper(firstChar) + toLower(otherChars));
					
					if (j < words.Length - 1)
					{
						// If this isn't the last word, add a space.
						sb.Append(" ");
					}
					appendNewLine = !string.IsNullOrEmpty(word);
				}
			}
		}
		return sb.ToString();
	}
	
	private static string filterSpecialWords(string input)
	{
		if (input == null)
		{
			return null;
		}
		
		if (input.Contains ("{player}"))
		{
			if (SlotsPlayer.instance != null &&
				SlotsPlayer.instance.socialMember != null &&
				string.IsNullOrEmpty(SlotsPlayer.instance.socialMember.firstName) == false && 
				SlotsPlayer.instance.socialMember.firstName != SocialMember.BLANK_USER_NAME
			)
			{
				input = input.Replace("{player}", SlotsPlayer.instance.socialMember.firstName);
			}
			else
			{
				input = input.Replace("{player}", Localize.text("high_roller"));
			}
		}

		return input;
	}

	public static void resetStaticClassData()
	{
		translations = new Dictionary<string,string>();
		isPopulated = false;
		language = "english";
	}
}

public class LocalizeIResetGame : IResetGame
{
	// Implements IResetGame here since Localize is a static class and can't implement it.
	// Localize class was made static so it could be used in editor scripts.
	public static void resetStaticClassData()
	{
		Localize.resetStaticClassData();
	}
}
