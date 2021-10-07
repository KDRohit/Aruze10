using UnityEngine;
using System.Collections;

public class DoSomethingZadeXpromoCarousel : DoSomethingAction
{
	public override void doAction(string parameter)
	{
		// Specific for the zade xpromo carousel.
		CarouselPanelZade.carouselClicked();
	}
}
