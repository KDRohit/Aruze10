using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Data structure to hold info about a pinned lobby option.
*/

public class Pinned
{
	public int page = 0;
	public int x = 0;
	public int y = 0;
	public int width { get; private set; }
	public int height { get; private set; }
	public string imageFilename = "";
	public List<Vector2int> spots = null;	// A way to store what spots on the menu this pinned option covers.

	public enum Shape
	{
		NOT_SET,
		STANDARD_1X1,
		BANNER_1X2,
		BANNER_2X2,
		BANNER_3X2
	}
	
	private static Dictionary<Shape, string> shapeFilePathMap = new Dictionary<Shape,string>()
	{
		{Shape.NOT_SET, ""}, 
		{Shape.STANDARD_1X1, ""}, // Same as not set, but it is pinned
		{Shape.BANNER_1X2, "1X2"},
		{Shape.BANNER_2X2, "2X2"},
		{Shape.BANNER_3X2, "3X2"}
	};

	public static string getFilePathPostFix(Shape shape)
	{
		return shapeFilePathMap[shape];
	}

	public Pinned()
	{
	}

	// When setting the shape, parse it to set the width and height properties.
	public Shape shape
	{
		get { return _shape; }
		
		set
		{
			// Convert the raw data about pin shape and position into the specific spots the option covers on the page.
			_shape = value;
					
			// Rectangular shapes can be dealt with algorithmically, regardless of size.
			// We only support rectangular shapes now. No more T's and L's!
			switch (value)
			{
				case Shape.BANNER_1X2:
					width = 1;
					height = 2;
					break;
				case Shape.BANNER_2X2:
					width = 2;
					height = 2;
					break;
				case Shape.BANNER_3X2:
					width = 3;
					height = 2;
					break;
				default:
					width = height = 1;
					break;
			}
		}
	}
	private Shape _shape = Shape.NOT_SET;
}
