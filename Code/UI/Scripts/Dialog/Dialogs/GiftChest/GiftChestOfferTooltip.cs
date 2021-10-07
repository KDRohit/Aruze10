using UnityEngine;
using System.Collections;
using TMPro;

public class GiftChestOfferTooltip : MonoBehaviour
{
	public ButtonHandler handler;
	public TextMeshPro infoLabel;

	// Use this for initialization
	void Start()
	{
		handler.registerEventDelegate(onClickHandler);
	}

	public void setLabel(string text)
	{
		infoLabel.text = text;
	}

	private void onClickHandler(Dict args = null)
	{
		Overlay.instance.top.inboxButtonClicked("top_nav");
	}
}
