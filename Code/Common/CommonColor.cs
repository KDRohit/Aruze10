using UnityEngine;




public static class CommonColor  {
	
	/// Returns a lerped color from an array of colors based on the given normalized float value
	public static Color colorRangeSelect(float value, Color[] colors)
	{
		float select = (float)(colors.Length - 1) * Mathf.Clamp01(value);
		int lowerIndex = Mathf.Max(0, Mathf.FloorToInt(select));
		int upperIndex = Mathf.Min(colors.Length - 1, Mathf.CeilToInt(select));
		float weight = select - (float)lowerIndex;
		return Color.Lerp(colors[lowerIndex], colors[upperIndex], weight);
	}

	// Returns a solid color generated from some hex string, e.g. "0f0f0f".
	// If alpha is to be included, it should be BEFORE the RGB values, such as "AARRGGBB"
	public static Color colorFromHex(string hex)
	{
		try
		{
			if (hex.Length == 6)
			{
				// Support alpha. Make it full solid by default.
				hex = "FF" + hex;
			}
			
			int colorBits = System.Convert.ToInt32(hex, 16);
			float a = Mathf.Clamp01((float)((colorBits & 0xFF000000) >> 24) / 255f);
			float r = Mathf.Clamp01((float)((colorBits & 0x00FF0000) >> 16) / 255f);
			float g = Mathf.Clamp01((float)((colorBits & 0x0000FF00) >> 8) / 255f);
			float b = Mathf.Clamp01((float)(colorBits & 0x000000FF) / 255f);
			
			return new Color(r, g, b, a);
		}
		catch
		{
			Debug.LogWarning("Invalid hex string for color: " + hex);
			return Color.grey;
		}
	}
	
	/// Returns a 6-character hex string for the given color, excluding alpha.
	public static string colorToHex(Color32 color)
	{
		return color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
	}
	
	/// Returns a 8-character hex string for the given color, including alpha.
	public static string colorToHexWithAlpha(Color32 color)
	{
		return color.a.ToString("X2") + color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
	}
	
	public static bool isColorsEqual(Color color1, Color color2)
	{
		return (
			color1.r == color2.r &&
			color1.g == color2.g &&
			color1.b == color2.b &&
			color1.a == color2.a
			);
	}
	
	public static Color adjustAlpha(Color color, float alpha)
	{
		color.a = alpha;
		return color;
	}

	public static Color32 getColor32FromUint(uint color)
	{
		Color32 color32 = new Color32();

		color32.r = (byte)((color & 0xFF000000) >> 24);
		color32.g = (byte)((color & 0x00FF0000) >> 16);
		color32.b = (byte)((color & 0x0000FF00) >>  8);
		color32.a = (byte)(color & 0xFF);
		return color32;
	}

	public static Color getColorForGame(string gameKey)
	{

		SlotResourceData resourceData = SlotResourceMap.getData(gameKey);
		Color gameColor = Color.grey;
		if (resourceData == null)
		{
			if (SlotResourceMap.isPopulated)
			{
				gameColor = new Color(0.9f, 0.1f, 0.1f);
			}
			else
			{
				// map data isn't currently populated so just default everything to grey for now
				gameColor = Color.grey;
			}
		}
		else
		{

			switch (resourceData.gameStatus)
			{
				case SlotResourceData.GameStatus.PRODUCTION_READY:				
					gameColor = new Color(0.7f, 0.7f, 1.0f);
					break;				
				case SlotResourceData.GameStatus.PRODUCTION_READY_REFACTORED:				
					gameColor = new Color(1.0f, 1.0f, 0.0f);
					break;				
				case SlotResourceData.GameStatus.PRODUCTION_READY_POSSIBLY_REFACTORED:				
					gameColor = new Color(0.0f, 1.0f, 0.0f);
					break;
				case SlotResourceData.GameStatus.LICENSE_LAPSED:				
					gameColor = new Color(0.5f, 0.5f, 0.5f);
					break;
				case SlotResourceData.GameStatus.PORT:				
					gameColor = Color.cyan;
					break;
				case SlotResourceData.GameStatus.PORT_NEEDS_ART:				
					gameColor = Color.red;
					break;
				case SlotResourceData.GameStatus.NON_PRODUCTION_READY: // Note: Fall through
				default:
					gameColor = Color.grey;
					break;
			}
		}

		return gameColor;
	}

	public static string getStatusForGame(string gameKey)
	{
		SlotResourceData resourceData = SlotResourceMap.getData(gameKey);
		string status = "";
		if (resourceData == null)
		{
			status = "[ERROR]";
		}
		else
		{
			switch (resourceData.gameStatus)
			{
				case SlotResourceData.GameStatus.PRODUCTION_READY:				
					break;				
				case SlotResourceData.GameStatus.PRODUCTION_READY_REFACTORED:				
					break;				
				case SlotResourceData.GameStatus.PRODUCTION_READY_POSSIBLY_REFACTORED:				
					break;
				case SlotResourceData.GameStatus.LICENSE_LAPSED:				
					status = "[LL]";
					break;
				case SlotResourceData.GameStatus.PORT:				
					status = "[Port]";
					break;
				case SlotResourceData.GameStatus.PORT_NEEDS_ART:				
					status = "[PNA]";
					break;
				case SlotResourceData.GameStatus.NON_PRODUCTION_READY: // Note: Fall through
				default:
					status = "[DRAFT]";
					break;
			}
		}

		return status;
	}
}
