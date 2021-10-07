using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Represents some data to display on a dialog related to challenges.
*/

public class DialogData
{
	public string titleText;
	public string description;
	public string backgroundImageURL;
	public string buttonText;
	public string buttonPath;

	public DialogData(JSON data)
	{   
		if (data != null)
		{
			parse(data);
		}
	}

	public void parse(JSON data)
	{
		titleText   = data.getString("title_text", string.Empty);
		description = data.getString("description", string.Empty);
		buttonText  = data.getString("button_text", string.Empty);
		buttonPath  = data.getString("button_path", string.Empty);
		backgroundImageURL = data.getString("background_image_url_mobile", string.Empty);
	}
}
