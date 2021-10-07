using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Attached to the Targeted Sale carousel panel since it has a live timer.
Any necessary UI elements are linked to this to get for setting up.
*/

public class CarouselPanelSTUDSale : CarouselPanelBase
{
	public TextMeshPro header;
	public TextMeshPro saleLabel;
	public Renderer background;
	
	private string expiresInLocalized;
	private STUDSale sale;
	
	private const string CAROUSEL_IMAGE = "{0}/CarouselBG.jpg";
	
	public override void init()
	{
		header.text = Localize.textUpper(data.texts[0]);
		sale = STUDSale.getSaleByAction(data.action);
		
		if (sale == null)
		{
			// Theoretically this should never happen since we already do validation before creating the slide.
			Debug.LogError("CarouselPanelSTUDSale: STUDSale not found for action: " + data.action);
			data.deactivate();
			return;
		}
		
		if (sale.featureData == null)
		{
			// Theoretically this should never happen since we already do validation before creating the slide.
			Debug.LogError("CarouselPanelSTUDSale: STUDSale does not have a STUDAction for action: " + data.action);
			data.deactivate();
			return;
		}
		
		if (!sale.isActive)
		{
			// Theoretically this should never happen since we already do validation before creating the slide.
			Debug.LogError("CarouselPanelSTUDSale: Should not be showing a carousel slide for sale: " + sale.saleType);
			data.deactivate();
			return;
		}

		if (ExperimentWrapper.SaleDialogLevelGate.isLockingSaleDialogs)
		{
			Debug.LogWarning(string.Format("Deactivating Carousel {0} due to level gate.", data.actionName));
			data.deactivate(); //Deactivate sale carousel slides if the player's level isn't high enough
		}

		loadTexture(background, string.Format(CAROUSEL_IMAGE, (sale.featureData.imageFolderPath)));
		
		expiresInLocalized = Localize.textUpper("ends_in");

		if (!sale.featureData.timerRange.isActive)
		{
			// If there is no timer, then there is no end date for the sale. So don't show this label.
			saleLabel.gameObject.SetActive(false);
		}
	}
	
	void Update()
	{
		if (sale.featureData.timerRange.isActive)
		{
			// Keep the targeted sale timer updated.
			saleLabel.text = expiresInLocalized + sale.featureData.timerRange.timeRemainingFormatted;
		}
		else
		{
			// Prevent this from being called multiple times after the timer expires once.
			if (data.isActive)
			{
				data.deactivate();
			}
		}
	}
}
