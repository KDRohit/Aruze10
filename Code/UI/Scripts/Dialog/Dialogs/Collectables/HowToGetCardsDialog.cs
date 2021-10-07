using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Not a "true" dialog, 
public class HowToGetCardsDialog : MonoBehaviour 
{
	public ButtonHandler okButton;
	public ButtonHandler closeButton;
	public string currentSet = "";

	[SerializeField] private Renderer logoImage;

	// Use this for initialization
	void Start () 
	{
		okButton.registerEventDelegate(onClickClose, Dict.create(D.OPTION, "continue"));
		closeButton.registerEventDelegate(onClickClose, Dict.create(D.OPTION, "close"));

		CollectableAlbum currentAlbum = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);
		AssetBundleManager.load(currentAlbum.logoTexturePath, logoLoadedSuccess, bundleLoadFail);
	}

	private void bundleLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load set image at " + assetPath);
	}

	private void logoLoadedSuccess(string assetPath, Object obj, Dict data = null)
	{
		Material material = new Material(logoImage.material.shader);
		material.mainTexture = obj as Texture2D;
		logoImage.material = material;
	}

	private void onClickClose(Dict args = null)
	{
		StatsManager.Instance.LogCount(counterName:"dialog",
			kingdom: "hir_collection",
			phylum: "how_to",
			klass: currentSet,
			family: (string)args.getWithDefault(D.OPTION, "close"),
			genus: "click");
		
		Destroy(gameObject);
	}
}
