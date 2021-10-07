using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

/**
This is a purely static class of generic useful functions that relate to text.
*/
public static class CommonText
{
	public const int DO_GROUPING_AT = 1000;

	public const long THOUSAND = 1000;
	public const long MILLION = 1000000;
	public const long BILLION = 1000000000;
	public const long TRILLION = 1000000000000;
	public const long QUADRILLION = 1000000000000000;
	
	/// Takes a path/filename and returns the same string with the file extension removed.
	public static string baseFilename(string filename)
	{
		if (filename == null)
		{
			return "";
		}
		int dot = filename.LastIndexOf(".");
		if (dot == -1)
		{
			return filename;
		}
		return filename.Substring(0, dot);
	}

	public static int parseVersionString(string versionString)
	{
		int version;

		versionString = versionString.Replace(".", string.Empty);

		if (int.TryParse(versionString, out version))
		{
			return version;
		}

		return 0;
	}
	/// Formats a number with thousands separators, automatically using the correct separator based on locale.
	/// int values will never need to have decimal points.
	public static string formatNumber(int number)
	{
		bool doGrouping = Mathf.Abs(number) >= DO_GROUPING_AT;
		// Default to English, then localize.
		string numberString = doGrouping? number.ToString("#,0", CultureInfo.InvariantCulture) : number.ToString();
		return localizeNumber(numberString, doGrouping);
	}
	
	/// Formats a number with thousands separators, automatically using the correct separator based on locale.
	public static string formatNumber(double number)
	{
		bool doGrouping = System.Math.Abs(number) >= DO_GROUPING_AT;
		// Calculate the position of the decimal place so we don't have to go through the whole string.
		double myNumber = number;
		int decimalIndexFromEnd = 0;
		while (myNumber - System.Math.Floor(myNumber) > 0)
		{
			decimalIndexFromEnd++;
			myNumber *= 10; // Move the decimal place over one.
		}
		// Default to English, then localize.
		string numberString = doGrouping? number.ToString("#,0", CultureInfo.InvariantCulture) : number.ToString();
		int decimalIndex = decimalIndexFromEnd != 0? numberString.Length - decimalIndexFromEnd - 1 : 0;
		return localizeNumber(numberString, doGrouping, decimalIndex);
	}
	
	/// Formats a number with thousands separators, automatically using the correct separator based on locale.
	public static string formatNumber(long number)
	{
		bool doGrouping = Mathf.Abs(number) >= DO_GROUPING_AT;
		// Calculate the position of the decimal place so we don't have to go through the whole string.
		float myNumber = number;
		int decimalIndexFromEnd = 0;
		while (myNumber - Mathf.Floor(myNumber) > 0)
		{
			decimalIndexFromEnd++;
			myNumber *= 10; // Move the decimal place over one.
		}
		// Default to English, then localize.
		string numberString = doGrouping? number.ToString("#,0", CultureInfo.InvariantCulture) : number.ToString();
		int decimalIndex = decimalIndexFromEnd != 0? numberString.Length - decimalIndexFromEnd - 1 : 0;
		return localizeNumber(numberString, doGrouping, decimalIndex);
	}
	
	// Get an abbreviated version of a number for instance 10,500 would become
	// 10.5K.  The variable shouldRoundUp allows for rounding values up or if false,
	// rounding them down to ensure they aren't greater than what the player is actually
	// going to be awarded
	public static string formatNumberAbbreviated(long value, int decimalPoints = 1, bool shouldRoundUp = true, int maximumDigits = -1, bool shouldRemoveTrailingDecimalZeros = true, long minimumToAbbreviate = 0)
	{
		string result = "";

		if (value <= minimumToAbbreviate)
		{
			result = CommonText.formatNumber(value);
		}
		else if (value >= QUADRILLION)
		{
			result = generateAbbreviatedNumberString(value, QUADRILLION, decimalPoints, maximumDigits, shouldRoundUp, shouldRemoveTrailingDecimalZeros);
		}
		else if (value >= TRILLION)
		{
			result = generateAbbreviatedNumberString(value, TRILLION, decimalPoints, maximumDigits, shouldRoundUp, shouldRemoveTrailingDecimalZeros);
		}
		else if (value >= BILLION)
		{
			result = generateAbbreviatedNumberString(value, BILLION, decimalPoints, maximumDigits, shouldRoundUp, shouldRemoveTrailingDecimalZeros);
		}
		else if (value >= MILLION)
		{
			result = generateAbbreviatedNumberString(value, MILLION, decimalPoints, maximumDigits, shouldRoundUp, shouldRemoveTrailingDecimalZeros);
		}
		else if (value >= THOUSAND)
		{
			result = generateAbbreviatedNumberString(value, THOUSAND, decimalPoints, maximumDigits, shouldRoundUp, shouldRemoveTrailingDecimalZeros);
		}
		else
		{
			// It is too small to be shortened
			result = CommonText.formatNumber(value);
		}
		return result;
	}
	
	// Get a truncated version of a number, either using an abbreviated letter, or a full text string to represent the magnitude
	// of the number
	private static string generateAbbreviatedNumberString(long value, long orderOfMagnitude, int decimalPoints = 1, int maxDigits = -1, bool shouldRoundUp = true, bool shouldRemoveTrailingDecimalZeros = true)
	{
		string result = "";
		string suffix = getOrderOfMagnitudeLetterAbbrev(orderOfMagnitude);

		double valueDouble = System.Convert.ToDouble(value);
		double shrunkValue = (valueDouble / orderOfMagnitude);
		string shrunkValueStr = formatDecimalNumberToString(shrunkValue, decimalPoints, shouldRoundUp, shouldRemoveTrailingDecimalZeros);
		if (maxDigits > 0 && shrunkValueStr.Contains(".") && shrunkValueStr.Length > (maxDigits + 1))
		{
			result = shrunkValueStr.Substring(0, maxDigits+1) + suffix;
		}
		else
		{
			result = shrunkValueStr + suffix;	
		}
		return result;
	}

	// Returns a letter abbreviation for the order of magnitude passed
	// for instance 1000 is represented as K
	private static string getOrderOfMagnitudeLetterAbbrev(long orderOfMagnitude)
	{
		switch (orderOfMagnitude)
		{
			case THOUSAND:
				return "K";

			case MILLION:
				return "M";

			case BILLION:
				return "B";

			case TRILLION:
				return "T";

			case QUADRILLION:
				return "Q";

			default:
				Debug.LogError("CommonText.getValueLetterAbbrev() - Unhandled orderOfMagnitude: " + orderOfMagnitude + "; return empty string!");
				return "";
		}
	}

	// Get a version of a number with text for the order of magnitude for instance 10,500 would become
	// 10.5 thousand.  The variable shouldRoundUp allows for rounding values up or if false,
	// rounding them down to ensure they aren't greater than what the player is actually
	// going to be awarded
	public static string formatNumberTextSuffix(long value, int decimalPoints = 1, bool shouldRoundUp = true, bool doAllCaps = true, bool shouldRemoveTrailingDecimalZeros = true)
	{
		string result = "";
		if (value >= QUADRILLION)
		{
			result = generateTextSuffixNumberString(value, QUADRILLION, decimalPoints, shouldRoundUp, doAllCaps, shouldRemoveTrailingDecimalZeros);
		}
		else if (value >= TRILLION)
		{
			result = generateTextSuffixNumberString(value, TRILLION, decimalPoints, shouldRoundUp, doAllCaps, shouldRemoveTrailingDecimalZeros);
		}
		else if (value >= BILLION)
		{
			result = generateTextSuffixNumberString(value, BILLION, decimalPoints, shouldRoundUp, doAllCaps, shouldRemoveTrailingDecimalZeros);
		}
		else if (value >= MILLION)
		{
			result = generateTextSuffixNumberString(value, MILLION, decimalPoints, shouldRoundUp, doAllCaps, shouldRemoveTrailingDecimalZeros);
		}
		else if (value >= THOUSAND)
		{
			result = generateTextSuffixNumberString(value, THOUSAND, decimalPoints, shouldRoundUp, doAllCaps, shouldRemoveTrailingDecimalZeros);
		}
		else
		{
			// It is too small to be shortened
			result = CommonText.formatNumber(value);
		}
		return result;
	}
	
	// Get a truncated version of a number, either using an abbreviated letter, or a full text string to represent the magnitude
	// of the number
	private static string generateTextSuffixNumberString(long value, long orderOfMagnitude, int decimalPoints = 1, bool shouldRoundUp = true, bool doAllCaps = true, bool shouldRemoveTrailingDecimalZeros = true)
	{
		string result = "";
		string suffix = getOrderOfMagnitudeText(orderOfMagnitude, doAllCaps);

		double valueDouble = System.Convert.ToDouble(value);
		double shrunkValue = (valueDouble / orderOfMagnitude);
		string shrunkValueStr = formatDecimalNumberToString(shrunkValue, decimalPoints, shouldRoundUp, shouldRemoveTrailingDecimalZeros);

		result = shrunkValueStr + " " + suffix;
		return result;
	}

	// Returns a text string for the order of magnitude passed
	// for instance 1000 will return thousand
	private static string getOrderOfMagnitudeText(long orderOfMagnitude, bool doAllCaps = true)
	{
		string locKey = "";

		switch (orderOfMagnitude)
		{
			case THOUSAND:
				locKey = "thousand";
				break;

			case MILLION:
				locKey = "million";
				break;

			case BILLION:
				locKey = "billion";
				break;

			case TRILLION:
				locKey = "trillion";
				break;

			case QUADRILLION:
				locKey = "quadrillion";
				break;

			default:
				Debug.LogError("CommonText.getOrderOfMagnitudeText() - Unhandled orderOfMagnitude: " + orderOfMagnitude + "; return empty string!");
				return "";
		}

		if (doAllCaps)
		{
			return Localize.textUpper(locKey);
		}
		else
		{
			return Localize.text(locKey);
		}
	}

	// Returns a formatted decimal number as a string
	private static string formatDecimalNumberToString(double shrunkValue, int decimalPoints = 1, bool shouldRoundUp = true, bool shouldRemoveTrailingDecimalZeros = true)
	{
		if (shouldRoundUp)
		{ 
			shrunkValue = System.Math.Round(shrunkValue, decimalPoints, MidpointRounding.AwayFromZero);
			return shrunkValue.ToString();
		}
		else
		{
			if (decimalPoints == 0)
			{
				// just truncate
				double shrunkValueTruncated = System.Math.Truncate(shrunkValue);
				return shrunkValueTruncated.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
			}
			else
			{
				// need to ensure we truncate to the decimal place we want to avoid rounding when converting to string
				double truncatePoint = Mathf.Pow(10, decimalPoints);
				double shrunkValueTruncated = System.Math.Truncate(shrunkValue * truncatePoint) / truncatePoint;
				string formattedStr = shrunkValueTruncated.ToString("F" + decimalPoints, System.Globalization.CultureInfo.InvariantCulture);

				if (shouldRemoveTrailingDecimalZeros)
				{
					return removeTrailingDecimalZeros(formattedStr);
				}
				else
				{
					return formattedStr;
				}
			}
		}
	}

	// Removes any trailing zeros after the decimal place (and the decimal place if all zeros follow it)
	// will ignore numbers that don't contain a decimal place
	private static string removeTrailingDecimalZeros(string shrunkValueStr)
	{
		if (shrunkValueStr.Contains("."))
		{
			int numberOfCharactersToRemove = 0;
			while (shrunkValueStr[shrunkValueStr.Length - (numberOfCharactersToRemove + 1)] == '0' || shrunkValueStr[shrunkValueStr.Length - (numberOfCharactersToRemove + 1)] == '.')
			{
				bool isDecimalPlace = shrunkValueStr[shrunkValueStr.Length - (numberOfCharactersToRemove + 1)] == '.';
				// remove the last character which is either a 0 or a decimal place
				numberOfCharactersToRemove++;

				if (isDecimalPlace)
				{
					// stop once we remove the decimal place
					break;
				}
			}

			shrunkValueStr = shrunkValueStr.Substring(0, shrunkValueStr.Length - numberOfCharactersToRemove);

			return shrunkValueStr;
		}
		else
		{
			// no decimal so just return the original string
			return shrunkValueStr;
		}
	}
	
	// Since CultureInfo usage causes build problems,
	// we need to manually massage our formatting for different languages.
	public static string localizeNumber(string numString, bool doGrouping, int decimalIndex = 0)
	{
		// The string comes in as English, so only do custom formatting if not English.
		if (Localize.language != "english")
		{
			char groupingChar = ',';	// The character that replaces the , in english
			char decimalChar = '.';		// The character that replaces the . in english
			int groupingAmount = 4; 	// How far apart each of the group character is going to be groupsize + 1
			
			// To date, these are the only languages we support...
			switch (Localize.language)
			{
			case "english":
				// Already good to go. Only here so all cases are covered.
				break;
				
			case "french":
				// French has to be the most unique, with spaces for grouping separators, and commas for decimals.
				groupingChar = ' ';
				decimalChar = ',';
				break;
				
			case "portuguese":
			case "german":
			case "dutch":
			case "spanish":
			case "turkish":
				// All these languages use periods for grouping, and commas for decimals.
				groupingChar = '.';
				decimalChar = ',';
				break;
			}
			char[] numCharArray = numString.ToCharArray();
			if (doGrouping)
			{
				// We can go through this array by the grouping amount because we know the groups are always the same distance apart.
				int endingIndex = decimalIndex != 0? decimalIndex : numCharArray.Length; // This should end at the decimal point, which may be the end of the number.
				for (int i = numString.IndexOf(','); i > -1 && i < endingIndex; i += groupingAmount)
				{
					numCharArray[i] = groupingChar;
				}
			}
			// There will only be one place that has a decimal point.
			if (decimalIndex != 0)
			{
				if (decimalIndex < numCharArray.Length)
				{
					numCharArray[decimalIndex] = decimalChar;
				}
				else
				{
					Debug.LogError("Something went wrong with calculating the decimal location. Floating point precision?");
				}
			}
			numString = new string(numCharArray);
		}
		return numString;
	}

	// Format the DateTime object to return a date & time string for the current player's locale and timezone.
	// note this will often throw a TimeZoneNotFoundException until we update to .net 4.6 in player settings
	// https://answers.unity.com/questions/1176513/timezone-not-found-exception.html
	public static string formatDateTimeZoned(System.DateTime dateTime)
	{
		try
		{
			TimeZoneInfo infos = TimeZoneInfo.Local;

			if (infos != null)
			{
				dateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, infos);

				string zone = infos.StandardName;
				if (infos.IsDaylightSavingTime(dateTime))
				{
					zone = infos.DaylightName;
				}

				// As of now, all formats are simply the date before the time, separated by a single space.
				return string.Format("{0} {1} {2}",
					formatDate(dateTime),
					formatTime(dateTime),
					zone
				);
			}
		}
		catch (TimeZoneNotFoundException e)
		{
			// players system failed to provide timezoneinfo, player only gets pacific time zone now, because California is most awesome
			string zone = "";
			if (isPacificDaylightSavingsTime())
			{
				zone = "PDT";
				dateTime = dateTime.AddHours(-7);
			}
			else
			{
				zone = "PST";
				dateTime = dateTime.AddHours(-8);
			}

			Debug.LogError("No Time zone info returned. Caught exception message " + e.Message);

			// As of now, all formats are simply the date before the time, separated by a single space.
			return string.Format("{0} {1} {2}",
				formatDate(dateTime),
				formatTime(dateTime),
				zone
			);
		}

		return formatDateTime(dateTime);
	}

    /// <summary>
    /// returns if the pacific timnezone is in daylight savings time.
    /// </summary>
    private static bool? _isPacificDaylightTime = null;
    public static bool isPacificDaylightSavingsTime()
    {
        if (!_isPacificDaylightTime.HasValue)
        {
            DateTime curTime = DateTime.Now;
            int daylightStart = findDay(curTime.Year, 3, DayOfWeek.Sunday, 2);      // find 2nd sunday of march
            int daylightEnd = findDay(curTime.Year, 11, DayOfWeek.Sunday, 1);       // find first sunday of november

            DateTime startDate = new DateTime(curTime.Year, 3, daylightStart, 2, 0, 0);
            DateTime endDate = new DateTime(curTime.Year, 11, daylightEnd, 2, 0, 0);
            _isPacificDaylightTime = (curTime >= startDate && curTime <= endDate);
       }

       return (bool)_isPacificDaylightTime; 
    }

    /// <summary>
    /// Used to find the date of a day occurance in a month, to find the date for the 2nd Sunday of march 2018 you would pass (2018,3, DayOfWeek.Sunday, 2)
    /// </summary>
    public static int findDay(int year, int month, DayOfWeek day, int occurance)
    {
        if (occurance <= 0 || occurance > 5) 
        {
            Debug.LogError("occurance is invalid");
            return 1;
        }

        DateTime firstDayOfMonth = new DateTime(year, month, 1);
        //Substract first day of the month with the required day of the week 
        int daysneeded = (int)day - (int)firstDayOfMonth.DayOfWeek;
        //if it is less than zero we need to get the next week day (add 7 days)
        if (daysneeded < 0) daysneeded = daysneeded + 7;
        //DayOfWeek is zero index based; multiply by the Occurance to get the day
        int resultedDay =  (daysneeded + 1)+ (7*(occurance-1));

        if(resultedDay > (firstDayOfMonth.AddMonths(1) - firstDayOfMonth).Days)
        {
            Debug.LogError(String.Format("No {0} occurance(s) of {1} in the required month", occurance, day.ToString()));
        }

        return resultedDay; 
    }  	
	
	// Format the DateTime object to return a date & time string for the current player's locale.
	public static string formatDateTime(System.DateTime dateTime)
	{
		// As of now, all formats are simply the date before the time, separated by a single space.
		return string.Format("{0} {1}",
			formatDate(dateTime),
			formatTime(dateTime)
		);
	}
	
	// Format the DateTime object to return a date string that's somewhat international friendly.
	// We don't do locale-specific formatting because it doesn't work in Mono/Unity apparently.
	public static string formatDate(System.DateTime dateTime)
	{
		return dateTime.ToString("MMM d, yyyy");
	}

	// Format the DateTime object to return a time string that's mostly US friendly.
	// We don't do locale-specific formatting because it doesn't work in Mono/Unity apparently.
	public static string formatTime(System.DateTime dateTime)
	{
		return dateTime.ToString("h:mm tt");
	}

	/// Formats a number to display vertically in a wheel
	public static string makeVertical(string s)
	{
		string result = Regex.Replace(s, ".{1}", "$0\n");
		//Removed the last return added by the replace.
		if (result.Length > 0)
		{
			return result.Substring(0, result.Length - 1);
		}
		return "";
	}
	
	public static string replaceSpacesWithNewLines(string original)
	{
		return Regex.Replace(original, @"\s", "\n", RegexOptions.Multiline);
	}

	/// Friendly string representation of time, such as "2:30" for 150 seconds.
	public static string secondsFormatted(int totalSeconds, bool useAbbreviaition = false)
	{
		if (useAbbreviaition)
		{
			return formatTimeSpanAbrreviated(System.TimeSpan.FromSeconds(totalSeconds));
		}
		
		return formatTimeSpan(System.TimeSpan.FromSeconds(totalSeconds));
	}

	public static string formatTimeSpan(System.TimeSpan t)
	{
		if (t.Days > 0)
		{
			return string.Format("{0}{1}:{2}{3}:{4:00}{5}",
								 t.Days,
								 Localize.text("days_abbreviation"),
								 t.Hours,
								 Localize.text("hours_abbreviation"),
								 t.Minutes,
								 Localize.text("minutes_abbreviation")
								 );
		}
		else if (t.Hours > 0)
		{
			// (e.g. 5:01:08)
			return string.Format("{0}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
		}
		else
		{
			return string.Format("{0:00}:{1:00}", t.Minutes, t.Seconds);
		}
	}
	
	public static string formatTimeSpanAbrreviated(System.TimeSpan t)
	{
		if (t.Days > 0)
		{
			return string.Format("{0}{1}:{2}{3}:{4:00}{5}",
				t.Days,
				Localize.text("days_abbreviation"),
				t.Hours,
				Localize.text("hours_abbreviation"),
				t.Minutes,
				Localize.text("minutes_abbreviation")
			);
		}
		
		if (t.Hours > 0)
		{
			// (e.g. 5:01:08)
			return string.Format("{0}{1}:{2:00}{3}:{4:00}{5}", 
				t.Hours,
				Localize.text("hours_abbreviation"), 
				t.Minutes,
				Localize.text("minutes_abbreviation"), 
				t.Seconds,
				Localize.text("seconds_abbreviation"));
		}
		
		return string.Format("{0:00}{1}:{2:00}{3}", 
			t.Minutes,
			Localize.text("minutes_abbreviation"),
			t.Seconds,
			Localize.text("seconds_abbreviation"));
	}

	// Friendly string representation of time, such as "2:30" for 150 seconds.
	// Now with a MonoSpace tag in there to use with TextMeshPro labels.
	public static string secondsFormattedMS(int totalSeconds, float spacing)
	{
		return string.Format("<mspace={0}>{1}</mspace>", spacing, secondsFormatted(totalSeconds));
	}

	/// Wraps text at spaces. Doesn't do anything special with line breaks passed in with original text.
	/// Output is a single string with line breaks added where appropriate.
	public static string wrapText(string text, int lineChars)
	{
		string[] words = text.Split(' ');
		StringBuilder sb = new StringBuilder();
		
		
		int lineLength = 0;
		bool firstWord = true;
		
		foreach (string word in words)
		{
			// Split further for line breaks within the word.
			string[] words2 = word.Split('\n');
			
			bool lineBreak = false;
			
			foreach (string word2 in words2)
			{
				if (!lineBreak && lineLength + word2.Length + 1 <= lineChars)
				{
					// This word fits on the line, so add a space if it isn't the first word on the line.
					if (lineLength > 0)
					{
						sb.Append(" ");
						lineLength++;
					}
					lineLength += word2.Length;
				}
				else if (!firstWord)
				{
					// This word and a space doesn't fit on the line,
					// or we've already added a word in this loop, so add a line break.
					sb.Append("\n");
					lineLength = word2.Length;
				}
				
				// Add the word.
				sb.Append(word2);
				
				lineBreak = true;
			}
			firstWord = false;
		}
		return sb.ToString();
	}

	/// Makes a string a friendly server-key-looking thing
	public static string makeIdentifier(string s)
	{
		List<char> sequence = new List<char>();
		char[] items = s.ToLower().ToCharArray();
		int i = 0;
		
		// Skip to the first letter
		for (; i < items.Length; i++)
		{
			if (char.IsLetter(items[i]))
			{
				break;
			}
		}
		
		// Collect up everything that is valid
		for (; i < items.Length; i++)
		{
			if (char.IsLetterOrDigit(items[i]) || items[i] == '_')
			{
				sequence.Add(items[i]);
			}
		}
		
		return new string(sequence.ToArray());
	}

	/// Returns the rightmost length of characters in the given string.
	public static string stringRight(string text, int length)
	{
		//Check if the value is valid
		if (string.IsNullOrEmpty(text))
		{
			// Set valid empty string as string could be null
			text = "";
		}
		else if (text.Length > length)
		{
			// Make the string no longer than the desired length
			text = text.Substring(text.Length - length, length);
		}
		return text;
	}

	/// Join a list of longs together into a string, because for some reason
	/// string.Join() doesn't work on a list of longs.
	public static string joinLongs(string delimiter, List<long> longs)
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		
		for (int i = 0; i < longs.Count; i++)
		{
			if (i > 0)
			{
				builder.Append(delimiter);
			}
			
			builder.Append(longs[i].ToString());
		}
		
		return builder.ToString();
	}
	
	/// Join a list of ints together into a string, because for some reason
	/// string.Join() doesn't work on a list of ints.
	public static string joinInts(string delimiter, List<int> ints)
	{
		return joinInts(delimiter, ints.ToArray());
	}
	
	public static string joinInts(string delimiter, int[] ints)
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		
		for (int i = 0; i < ints.Length; i++)
		{
			if (i > 0)
			{
				builder.Append(delimiter);
			}
			
			builder.Append(ints[i].ToString());
		}
		
		return builder.ToString();
	}
	
	// Inserts a given number of spaces between each normal character of a string,
	// and returns the trimmed result.
	public static string spaceOut(string original, int spaces)
	{
		if (spaces <= 0)
		{
			return original;
		}
		
		string spaceString = new System.String(' ', spaces);
		
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		
		for (int i = 0; i < original.Length; i++)
		{
			builder.Append(original.Substring(i, 1));
			builder.Append(spaceString);
		}
		
		return builder.ToString().Trim();
	}

	// A common way to return the first name and last initial for a set of names,
	// which we may or may not want to localize in the future.
	public static string firstNameLastInitial(string firstName, string lastName)
	{
		if (firstName == null)
		{
			firstName = "";
		}
		if (lastName == null)
		{
			lastName = "";
		}
		string name = firstName;
		if (lastName != "")
		{
			name += " " + lastName.Substring(0, 1) + ".";
		}
		return name;
	}
	
	/// Efficiently combines any number of strings.
	/// Only use this if you are combining lots of things.
	public static string stringCombine(params string[] items)
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		foreach (string item in items)
		{
			builder.Append(item);
		}
		return builder.ToString();
	}

	public static int countNumberOfSpecificCharacter(string s, char match)
	{
		int count = 0;
		if (!string.IsNullOrEmpty(s))
		{
			foreach (char c in s) 
			{
				if (c == match)
				{
					count++;
				}
			}
		}
		return count;
	}

	// Append a querystring to a url.
	public static string appendQuerystring(string url, string suffix)
	{
		// In URL, ? indicates the first para while "&" indicates the second or later para.
		if (url.Contains('?'))
		{
			return url + "&" + suffix;
		}
		else
		{
			return url + "?" + suffix;
		}
	}

	// Append a bunch of query strings to a url as key-value pairs based on a dictionary.
	public static string appendQueryStringsDictionaryToUrl(string url, Dictionary<string,object> queryParams)
	{
		StringBuilder paramsBuilder = new StringBuilder();
		bool firstTime = true;
		foreach (KeyValuePair<string,object> entry in queryParams)
		{
			string key = entry.Key;
			string value = entry.Value.ToString();
			if (string.IsNullOrEmpty(key))
			{
				continue;
			}
			if (value == null)
			{
				value = string.Empty;
			}

			if (!firstTime)
			{
				firstTime = false;
				paramsBuilder.Append('&');
			}

			paramsBuilder.Append(key).Append('=').Append(Uri.EscapeDataString(value));
		}
		return CommonText.appendQuerystring(url, paramsBuilder.ToString());
	}

	// Sets the label's font sizes to the smallest value found in the list of labels, and turns off the enableAutoSize functionality
	// so that TMPro doesn't change the labels back to their original size at the end of the frame.
	public static void makeLabelFontSizesEqual(List<LabelWrapperComponent> lables)
	{
		float minFontSize = float.MaxValue;
		for (int i = 0; i < lables.Count; i++)
		{
			if (lables[i] != null)
			{
				lables[i].forceUpdate(); // Force the update here so we can get the font size since something could have changed earlier in this frame.
				if (lables[i].fontSize < minFontSize)
				{
					minFontSize = lables[i].fontSize;
				}
			}
		}
		
		// Go through the rest of the labels and set the font size.
		for (int i = 0; i < lables.Count; i++)
		{
			if (lables[i] != null)
			{
				lables[i].enableAutoSize = false; // We're going to change the size here, so we need to stop TMPro from updating it automatically.
				lables[i].fontSize = minFontSize;
			}
		}
	}

	// Simple wrapper. Returns the first string if not null or empty, else the second string.
	public static string emptyCheck(string text1, string text2)
	{
		return !string.IsNullOrEmpty(text1) ? text1 : text2;
	}

	// this exists in .NET 4.0, but not in the old .NET framework Unity uses, so add as an extension
	public static bool IsNullOrWhiteSpace(this string str)
	{
		if (str != null)
		{
			for (int i = 0; i < str.Length; i++)
			{
				if (!char.IsWhiteSpace(str[i]))
				{
					return false;
				}
			}
		}
		return true;
	}

	// Converts a string in snake case to pascal case (e.g. "bonus_anticipate_03" to "BonusAnticipate03").
	public static string snakeCaseToPascalCase(string s)
	{
		if (s == null)
		{
			return null;
		}

		System.Text.StringBuilder result = new System.Text.StringBuilder(s.Length);

		bool lastCharWasUnderscore = true;
		for(int i=0; i<s.Length; i++)
		{
			char c = s[i];
			if (c != '_')
			{
				if (lastCharWasUnderscore)
				{
					result.Append(char.ToUpper(c));
				}
				else
				{
					result.Append(c);
				}
				lastCharWasUnderscore = false;
			}
			else
			{
				lastCharWasUnderscore = true;
			}
		}

		return result.ToString();
	}

	// Returns a lowercase bool string. JSON and XML need lowercase booleans.
	public static string lowercaseBool(bool value)
	{
		if (value)
		{
			return "true";
		}
		return "false";
	}

	// These are copied from Unity's MiscBestPractices doc.
	// Unity claims these are 30-50x faster than standard string.StartsWith()/EndsWith()
	// Implemented as Extensions of string type to maintain intuitive syntax
	// (Bugfixes: The unity code had bugs with substrings > strings, and empty strings)
	public static bool FastStartsWith(this string str, string prefixStr)
	{
		if (prefixStr == null)
		{
			//throw System.ArgumentNullException;   // real StartsWith() throws ArgumentNullException for null
			return false;
		}

		if (str.Length < prefixStr.Length)
		{
			return false;  // early out if string is shorter than prefix
		}

		int i = 0;
		int prefixLength = prefixStr.Length;
		while (i < prefixLength && str[i] == prefixStr[i]) 
		{
			i++;
		}

		return (i == prefixLength);
	}

	public static bool FastEndsWith(this string str, string suffixStr) 
	{
		if (suffixStr==null)
		{
			//throw System.ArgumentNullException;   // real EndsWith() throws ArgumentNullException for null
			return false;
		}

		if (str.Length < suffixStr.Length)
		{
			return false;  // early out if string is shorter than suffix
		}

		int ap = str.Length - 1;
		int bp = suffixStr.Length - 1;
		while (bp >= 0 && str[ap] == suffixStr[bp]) 
		{
			ap--;
			bp--;
		}

		return (bp < 0);
	}

	// An overloaded version of string.Contains(...) that searches for a single-character.
	//
	// This is several times faster than string.Contains(string) for single-character length strings.
	// Implemented as Extensions of string type to maintain intuitive syntax.
	public static bool Contains(this string str, char c)
	{
		return (str.IndexOf(c) >= 0);
	} 

	// Formats a number and adds the appropriate place (1st, 2nd, 3rd, 4th, etc)
	// if setSuperscript is set to true, will return a TMP formatted super script string. eg 1 will return "1<sup>st</sup>"
	public static string formatContestPlacement(int place, bool setSuperscript = false)
	{
		string placeString = place.ToString();
		string suffix = "";

		// If we're in the multi digits
		if (placeString.Length > 1 && placeString[placeString.Length - 2].ToString() == "1")
		{
			// To string because we're referencing a char here.
			// Also if it's in the teens, just return th
			suffix = "th";
			
		}
		else
		{
			switch (placeString[placeString.Length - 1].ToString())
			{
				case "1":
					suffix = "st";
					break;
				case "2":
					suffix = "nd";
					break;
				case "3":
					suffix = "rd";
					break;
				default:
					suffix = "th";
					break;
			}
		}

		if (setSuperscript)
		{
			return placeString + "<sup>" + suffix + "</sup>";
		}

		return placeString + suffix;
	}

	public static string toString(Vector3 vector)
	{
		return string.Format("(x: {0}, y: {1}, z: {2}", vector.x, vector.y, vector.z);
	}

	public static string listToString<T>(List<T> list)
	{
		string result = "{";
		for (int i = 0; i < list.Count; i++)
		{
			if (i != 0)
			{
				result += ",";
			}
			result += ((T)list[i]).ToString();
		}
		result += "}";
		return result;
		
	}

	public static string digitToText(string digit)
	{
		switch(digit)
		{
			case "0":
				return "zero";
				
			case "1":
				return "one";

			case "2":
				return "two";

			case "3":
				return "three";

			case "4":
				return "four";

			case "5":
				return "five";

			case "6":
				return "six";

			case "7":
				return "seven";

			case "8":
				return "eight";

			case "9":
				return "nine";

		}

		return digit;
	}

	public static string getColorHexStringFromString(string text)
	{
		int startIndex = text.IndexOf("<#") + 1;
		int stopIndex = text.IndexOf('>', startIndex);
		int length = stopIndex - startIndex;
		string colorString = text.Substring(startIndex, length);
		return colorString;
	}

	// Function that splits a string with whitespace
	public static string[] splitWhitespace(string input)
	{
		char[] whitespace = new char[] { ' ', '\t' };
		return input.Split(whitespace);
	}
}
