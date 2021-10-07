using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Attached to generic carousel panels that don't need complex logic like a live timer.
Any necessary UI elements are linked to this to get for setting up.
*/

public class CarouselPanelGeneric : CarouselPanelBase
{
	public UILabelStaticText[] labels;	// Use UILabelStaticText scripts so we can use it to determine capitalization formatting.
	public Renderer[] renderers;
	
	public override void init()
	{
		// Deal with labels.
		for (int i = 0; i < labels.Length && i < data.texts.Length; i++)
		{
			if (labels[i] != null)
			{
				if (string.IsNullOrEmpty(data.texts[i].Trim()))
				{
					labels[i].text = "";
				}
				else
				{
					labels[i].doLocalization(data.texts[i]);
				}
			}
		}
		
		// Deal with textures.
		for (int i = 0; i < renderers.Length && i < data.imageUrls.Length; i++)
		{
			if (data.imageUrls[i] != "" && renderers[i] != null)
			{
				loadTexture(renderers[i], "lobby_carousel/" + data.imageUrls[i]);
			}
		}
	}
}
