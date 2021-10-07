using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Not a "true" dialog, 
public class BonusPackInfoDialog : MonoBehaviour 
{
	public ButtonHandler closeButton;
	[SerializeField] private GameObject[] stars;
	[SerializeField] private GameObject starDescriptionParent;
	[SerializeField] private LabelWrapperComponent starPackText;
	[SerializeField] private CollectionsDuplicateMeter starMeter;
	[SerializeField] private Renderer packIcon;

	// Use this for initialization
	void Start () 
	{
		CollectableAlbum currentAlbum = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);
		AssetBundleManager.load(currentAlbum.cardPackIconTexturePath, packLoadedSuccess, packLoadFail);
		starMeter.init(currentAlbum);
		closeButton.registerEventDelegate(onClickClose);
		CollectablePackData starPackData = Collectables.Instance.findPack(currentAlbum.starPackName);
		if (starPackData != null && starPackData.constraints != null && starPackData.constraints.Length > 0)
		{
			starPackText.text = string.Format("Guarantees at least {0} cards with", starPackData.constraints[0].guaranteedPicks);
			for (int i = 0; i < starPackData.constraints[0].minRarity; i++)
			{
				if (i < stars.Length)
				{
					stars[i].SetActive(true);
				}
				else
				{
					break;	
				}
			}
		}
		else
		{
			starDescriptionParent.SetActive(false); //Hide this if data isn't valid
		}
	}

	private void onClickClose(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "extras_bonus",
			family: "close",
			genus: "click");
		
		Destroy(gameObject);
	}

	private void packLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load set image at " + assetPath);
	}

	private void packLoadedSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this != null && this.gameObject != null)
		{
			Material material = new Material(packIcon.material);
			material.mainTexture = obj as Texture2D;
			packIcon.material = material;
			packIcon.gameObject.SetActive(true);
		}
	}
}
