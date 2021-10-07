using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CollectableCardHighQuality : MonoBehaviour 
{
	public TextMeshPro cardDescriptionTitle;
	public TextMeshPro cardDescriptionBody;
	public CollectableCard cardTemplate;
	private bool isCollected = false;

	private const string CARD_TITLE_LOCALIZATION_POSTFIX = "_title";
	private const string CARD_DESCRIPTION_LOCALIZATION_POSTFIX = "_description";

	private CollectableCard createdCard = null;

	public void setup(CollectableCardData data, UIAtlas atlas = null, Dictionary<string, Texture2D> nonAtlassedTextures = null)
	{
		if (data == null)
		{
			Debug.LogError("Passed card data was null");
			return;
		}

		isCollected = data.isCollected;
		cardDescriptionBody.text = Localize.text(data.keyName + CARD_DESCRIPTION_LOCALIZATION_POSTFIX);
		cardDescriptionTitle.text = Localize.text(data.keyName + CARD_TITLE_LOCALIZATION_POSTFIX);
		if (createdCard == null)
		{
			GameObject cardObject = NGUITools.AddChild(this.gameObject, cardTemplate.gameObject);
			createdCard = cardObject.GetComponent<CollectableCard>();
		}
		createdCard.init(data, CollectableCard.CardLocation.DETAILED_VIEW, atlas, nonAtlassedTextures);
	}

	public void reset()
	{
		createdCard.reset();
	}
}
