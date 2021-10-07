using UnityEngine;
using System.Collections;

public class PageIndicatorV3 : MonoBehaviour
{
	[SerializeField] private UISprite disabledPage;
	[SerializeField] private UISprite enabledPage;
	[SerializeField] private UISprite currentPage;
	[SerializeField] private UISprite glow;
	[SerializeField] private ObjectSwapper eliteSwapper;

	private PageController controller;
	private int page = 0;

	public void init(PageController controller, int page, bool showElite = false)
	{
		ClickHandler handler = GetComponent<ClickHandler>();
		handler.registerEventDelegate(onClick);
		this.controller = controller;
		this.page = page;
		if (showElite)
		{
			enableElite();
		}
		else
		{
			disableElite();
		}
	}

	public void setSize(float value)
	{
		disabledPage.transform.localScale = new Vector3(value, disabledPage.transform.localScale.y, 1);
		enabledPage.transform.localScale = new Vector3(value, enabledPage.transform.localScale.y, 1);
		currentPage.transform.localScale = new Vector3(value, currentPage.transform.localScale.y, 1);
		glow.transform.localScale = new Vector3(value + 40, glow.transform.localScale.y, 1);

		BoxCollider collider = gameObject.GetComponent<BoxCollider>();
		if (collider != null)
		{
			collider.size = new Vector3(value + 2.0f, collider.size.y, collider.size.y);
		}
	}

	public void onClick(Dict args)
	{
		controller.goToPage(page);

		StatsManager.Instance.LogCount
		(
			  counterName: "lobby"
			, kingdom: "page_dot"
			, phylum: "page_" + page.ToString()
			, klass: ""
		  	, family: ""
			, genus: "click"
		);

		MainLobby.pageBeforeGame = page;
	}

	public void setDisabled()
	{
		disabledPage.gameObject.SetActive(true);
		enabledPage.gameObject.SetActive(false);
		currentPage.gameObject.SetActive(false);
		glow.gameObject.SetActive(false);
	}

	public void setEnabled()
	{
		disabledPage.gameObject.SetActive(false);
		enabledPage.gameObject.SetActive(true);
		currentPage.gameObject.SetActive(false);
		glow.gameObject.SetActive(false);
	}

	public void setCurrent()
	{
		disabledPage.gameObject.SetActive(false);
		enabledPage.gameObject.SetActive(false);
		currentPage.gameObject.SetActive(true);
		glow.gameObject.SetActive(true);
	}

	public void enableElite()
	{
		if (eliteSwapper != null && eliteSwapper.gameObject != null)
		{
			eliteSwapper.setState("elite");
		}
	}
	
	public void disableElite()
	{
		if (eliteSwapper != null && eliteSwapper.gameObject != null)
		{
			eliteSwapper.setState("default");	
		}
	}
}