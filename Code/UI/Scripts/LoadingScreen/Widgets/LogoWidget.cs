using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LogoWidget : LoadingWidget
{
	public UIAnchor anchor;
	public UISprite logo;

	// =============================
	// PRIVATE
	// =============================
	private string anchorPosition = "center";
	
	// =============================
	// CONST
	// =============================
	private const string LOGO_WIDGET_NAME = "logo";
	private const int Z_DEPTH = -10;

	private static Dictionary<string, UIAnchor> anchorLookup = null;

	public override void show()
	{
		base.show();

		JSON data = getWidgetData();
		if (data != null)
		{
			anchorPosition = data.getString("position", "center");
		}
		
		UIAnchor.Side side = (UIAnchor.Side)System.Enum.Parse(typeof(UIAnchor.Side), anchorPosition);
		anchor.side = side;

		if (side != UIAnchor.Side.Center)
		{
			int dirx = anchorPosition.Contains("Left") ? 1 : -1;
			int diry = 0;

			if (anchorPosition.Contains("Top") || anchorPosition.Contains("Bottom"))
			{
				diry = anchorPosition.Contains("Bottom") ? 1 : -1;
			}
			logo.transform.localPosition = new Vector3
			(
				  dirx * logo.transform.localScale.x/2f
				, diry * logo.transform.localScale.y/2f
				, Z_DEPTH
			);
		}
		else
		{
			logo.transform.localPosition = new Vector3(0, 0, Z_DEPTH);
		}
	}

	
	public override string name
	{
		get
		{
			return "logo";
		}
	}
}