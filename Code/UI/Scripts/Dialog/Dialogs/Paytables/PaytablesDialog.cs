using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;

/*
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
*/

public class PaytablesDialog : DialogBase
{
	public static PaytablesDialog instance;

	public TextMeshPro subheadLabel;
	public PageScroller pageScroller;
	
	private const string INDEX_ROOT_PATH = "Prefabs/Dialogs/Paytables/Games/";

	private bool isPoolingSymbolPages = false;													// Controls if pages will be pooled for performance, currently only used if you want to have multpile symbol pages
	private Dictionary<int, GameObject> pooledSymbolPages = new Dictionary<int, GameObject>();	// Stores out pre-pooled versions of the symbol pages 

	[HideInInspector] public string gameKey;
	[HideInInspector] public PaytableDialogIndex dialogIndex;

	/// Initialization
	public override void init()
	{
		instance = this;
		this.gameKey = "";
		
		pageScroller.dialog = this;
		
		if (GameState.game == null)
		{
			// For testing straight from the lobby.
			// Change this to whatever game key you want to test without launching the game first.
			this.gameKey = "oz00";
		}
		else
		{
			this.gameKey = GameState.game.keyName;
		}

		GameObject prefab = SkuResources.loadSkuSpecificResourcePrefab(INDEX_ROOT_PATH + this.gameKey);
		if (prefab != null)
		{
			this.dialogIndex = prefab.GetComponent<PaytableDialogIndex>();
		}
		else
		{
			//Load our dynamic paytable index if a premade one doesn't exist
			prefab = SkuResources.loadSkuSpecificResourcePrefab(INDEX_ROOT_PATH + "paytable_dynamic");
			this.dialogIndex = prefab.GetComponent<PaytableDialogIndex>();
			this.dialogIndex.initInfo();
		}

		// check if we need to pre-create and pool the symbol pages (if there is more than one) 
		// which will improve visual performance at the cost of memory
		int numSymbolPages = 0;
		for (int i = 0; i < dialogIndex.pages.Length; ++i)
		{
			if (dialogIndex.pages[i].name.Contains("PageSymbols"))
			{
				numSymbolPages++;
			}

			if (numSymbolPages > 1)
			{
				isPoolingSymbolPages = true;
				break;
			}
		}

		if (isPoolingSymbolPages)
		{
			for (int i = 0; i < dialogIndex.pages.Length; ++i)
			{
				if (dialogIndex.pages[i].name.Contains("PageSymbols"))
				{
					GameObject pagePrefab = dialogIndex.pages[i];

					// ensure we don't double insert, since some paytables could use the same type of page
					if (pagePrefab != null)
					{
						pagePrefab = SmallDeviceGameObject.getGo(pagePrefab);
						
						GameObject pooledPage = CommonGameObject.instantiate(pagePrefab) as GameObject;
						pooledPage.transform.parent = gameObject.transform;
						pooledPage.transform.localScale = Vector3.one;
						pooledPage.transform.localPosition = Vector3.zero;

						// release symbols back into the wild so other pooled pages can use them
						PaytableGlyphIndex glyphIndex = pooledPage.GetComponentInChildren<PaytableGlyphIndex>();
						if (glyphIndex != null && glyphIndex.isUsingSymbolCaching)
						{
							glyphIndex.ReleaseSymbols();
						}

						pooledPage.SetActive(false);

						pooledSymbolPages.Add(i, pooledPage);
					}
					else
					{
						Debug.LogError("Could not find Paytables dialog prefab for: " + this.gameKey);
						return;
					}
				}
			}
		}

		pageScroller.init(this.dialogIndex.pages.Length, onCreatePanel, null, null, onDestroyPanel);
		pageScroller.onAfterScroll = onAfterScroll;
		onAfterScroll();	// Call it immediately to set things for the first page.
	}

	// Keep the header updated after a page scroll finishes.
	private void onAfterScroll()
	{
		subheadLabel.text = Localize.text(dialogIndex.titleKeys[pageScroller.scrollPos]);
		StatsManager.Instance.LogCount("dialog", "pay_table", StatsManager.getGameTheme(), StatsManager.getGameName(), "", "view");		
	}

	/**
	Need to reclaim the pooled page if we are using pooled pages and this is one
	*/
	private void onDestroyPanel(GameObject panel, int index)
	{
		if (isPoolingSymbolPages && pooledSymbolPages.ContainsKey(index))
		{
			GameObject pooledPage = pooledSymbolPages[index];
			pooledPage.transform.parent = gameObject.transform;

			// release symbols back into the wild so other pooled pages can use them
			PaytableGlyphIndex glyphIndex = pooledPage.GetComponentInChildren<PaytableGlyphIndex>();
			if (glyphIndex != null && glyphIndex.isUsingSymbolCaching)
			{
				glyphIndex.ReleaseSymbols();
			}

			pooledPage.SetActive(false);
		}
	}

	/// A page has been created. However, the page template is just an
	/// empty GameObject and now we need to fill it with the appropriate contents.
	private void onCreatePanel(GameObject panel, int index)
	{
		if (index >= dialogIndex.pages.Length)
		{
			Debug.LogError("Page index is higher than max index of defined pages on Paytables dialog!");
			return;
		}
		
		// Check if we have a cached version or need to generate a new one
		if (isPoolingSymbolPages && pooledSymbolPages.ContainsKey(index))
		{
			GameObject pooledPage = pooledSymbolPages[index];
			pooledPage.transform.parent = panel.transform;
			pooledPage.transform.localScale = Vector3.one;
			pooledPage.transform.localPosition = Vector3.zero;

			pooledPage.SetActive(true);

			// reload symbols, in case another pooled page is hanging onto pooled symbols
			PaytableGlyphIndex glyphIndex = pooledPage.GetComponentInChildren<PaytableGlyphIndex>();
			if (glyphIndex != null && glyphIndex.isUsingSymbolCaching)
			{
				glyphIndex.ReloadSymbols();
			}
		}
		else
		{
			// Instantiate the page's pre-designed contents from a prefab.
			GameObject prefab = dialogIndex.pages[index];

			if (prefab != null)
			{
				prefab = SmallDeviceGameObject.getGo(prefab);
				
				GameObject go = CommonGameObject.instantiate(prefab) as GameObject;
				go.transform.parent = panel.transform;
				go.transform.localScale = Vector3.one;
				go.transform.localPosition = Vector3.zero;
				
				PaytableBonusIndex bonusIndex = go.GetComponentInChildren<PaytableBonusIndex>();
				if (bonusIndex != null)
				{
					bonusIndex.init(dialogIndex.typeOffset[index]);
				}

				// Check if this is a paytable image and if so, load the image from a bundle
				PaytablePaylinePanel paylinePanel = go.GetComponentInChildren<PaytablePaylinePanel>();
				if (paylinePanel != null)
				{
					StartCoroutine(DisplayAsset.loadTextureFromBundle(
						"paytables/" + paylinePanel.paylineImageName,
						onPaylineImageLoadedCallback,
						Dict.create(
							D.IMAGE_TRANSFORM, paylinePanel.paylineUITexture.transform
						)
					));
				}
			}
			else
			{
				Debug.LogError("Could not find Paytables dialog prefab for: " + this.gameKey);
				return;
			}
		}
	}

	// Texture loaded callback for loadTexture().
	protected void onPaylineImageLoadedCallback(Texture2D tex, Dict texData)
	{
		if (tex != null)
		{
			Transform uiTextureTransform = texData.getWithDefault(D.IMAGE_TRANSFORM, null) as Transform;

			if (uiTextureTransform == null)
			{
				// This should never happen.
				Debug.LogError("PaytablesDialog.onPaylineImageLoadedCallback: No uiTextureTransform data found.");
				return;
			}
			else
			{
				UITexture uiTexture = uiTextureTransform.GetComponent<UITexture>();

				if (uiTexture == null)
				{
					// This should never happen.
					Debug.LogError("PaytablesDialog.onPaylineImageLoadedCallback: No UITexture data found.");
					return;
				}
				else
				{
					uiTexture.gameObject.SetActive(true);
					NGUIExt.applyUITexture(uiTexture, tex);
				}
			}
		}
	}
	
	void Update()
	{
		AndroidUtil.checkBackButton(closeClicked, "dialog", "pay_table", StatsManager.getGameTheme(), StatsManager.getGameName(), "back_to_game", "back");
	}
		
	public void closeClicked()
	{
		StatsManager.Instance.LogCount("dialog", "pay_table", StatsManager.getGameTheme(), StatsManager.getGameName(), "", "close");
		
		// reset any symbols that may have been altered on the reels (this may be due to showing the different levels of symbols for instance)
		SlotBaseGame.instance.resetSymbolsOnPaytableClose();
		
		Dialog.close();
	}
			
	/// Called by Dialog.close() - do not call directly.	
	public override void close()
	{
		// Do special cleanup.
	}
	
	public static void showDialog(SchedulerPriority.PriorityType p = SchedulerPriority.PriorityType.LOW)
	{
		Scheduler.addDialog("paytables", null, p);
	}
}
