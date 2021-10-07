using UnityEngine;
using System.Collections;

/*
*	Class that holds color constants
*/
public static class GameColor 
{
	// Terraforming and other mesh highlights (tile)
	public static Color terraformHighlightGreen	= new Color(0, .81f, 0, .75f);
	public static Color terraformHighlightRed	= new Color(.81f, 0, 0, .75f);
	
	public static Color tileHighlightRed		= new Color(1, .25f, .25f, .75f);
	public static Color tileObjectHighlightRed	= new Color(1f, 0.5f, 0.25f, 1f);
	
	public static Color tileHighlightGreen			= new Color(0.0f, 1.0f, .25f, .75f);
	public static Color tileObjectHighlightGreen	= new Color(0f, 1f, 0f, 1f);
	
	public static Color tutorialHighlightGreen 	= new Color (0, 1, 0, 0.75f);
	
	public static Color meterLimitTween			= new Color(.19f, .98f, .51f, 1.0f);
	public static Color meterEnergyYellow		= new Color(1.0f, 1.0f, 0, 1.0f);
	
	public static Color fontDarkBrown			= new Color(0.196f, 0.130f, 0.019f); //CommonColor.colorFromHex("322103"); // (50, 33, 3)
	public static Color fontDarkBlue			= new Color(0.027f, 0.204f, 0.259f); //CommonColor.colorFromHex("073442"); // (7, 52, 66)
	
	public static Color maroon					= new Color(.5f, 0, 0, 1);
	public static Color blackInvisible			= new Color(0, 0, 0, 0); // cause why now :)
	
	public static Color HolidayRed				= new Color(.78f, 0f, 0f);

	public static Color buyOfferHighlight		= new Color(0, 1.0f, 1.0f);
}

