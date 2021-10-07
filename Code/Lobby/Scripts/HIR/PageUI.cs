using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PageUI : MonoBehaviour
{
	// =============================
	// PRIVATE
	// =============================
	[SerializeField] private GameObject pageIndicator;
	[SerializeField] private GameObject indicatorContainer;
	[SerializeField] private PageController pageController;
	[SerializeField] private UIAnchor leftAnchor;
	[SerializeField] private UIAnchor rightAnchor;
	private List<PageIndicatorV3> pageIndicators = new List<PageIndicatorV3>();
	private int pages = 0;

	// =============================
	// CONST
	// =============================
	private const int MAX_INDICATORS_BEFORE_RESIZE = 30;
	private const float INDICATOR_PADDING = 3f;

	public void init(int pages)
	{
		this.pages = pages;
		createPageIndicators();
	}

	public void setCurrentPage(int page)
	{
		for (int i = 0; i < pageIndicators.Count; ++i)
		{
			pageIndicators[i].setEnabled();
		}
			
		pageIndicators[page].setCurrent();
	}

	public void enableEliteIndicators()
	{
		for (int i = 0; i < pageIndicators.Count; i++)
		{
			pageIndicators[i].enableElite();
		}
	}

	public void disableEliteIndicators()
	{
		for (int i = 0; i < pageIndicators.Count; i++)
		{
			pageIndicators[i].disableElite();
		}
	}
	
	private void createPageIndicators()
	{
		
		float containerSize = Mathf.Abs(rightAnchor.transform.localPosition.x) + Mathf.Abs(leftAnchor.transform.localPosition.x);
		float size = (containerSize - INDICATOR_PADDING * (pages - 1)) / pages;
		bool showElite = EliteManager.isActive && EliteManager.hasActivePass;
		
		for (int i = 0; i < pages; i++)
		{
			GameObject indicator = CommonGameObject.instantiate(pageIndicator) as GameObject;
			indicator.transform.parent = indicatorContainer.transform;
			indicator.transform.localScale = Vector3.one;

			PageIndicatorV3 p = indicator.GetComponent<PageIndicatorV3>();
			pageIndicators.Add(p);
			p.init(pageController, i, showElite);
			p.setSize(size);

			Vector3 pos = new Vector3((size + INDICATOR_PADDING) * i, 0, -1);
			indicator.transform.localPosition = pos;
		}

		indicatorContainer.transform.localPosition = new Vector3(leftAnchor.transform.localPosition.x + size/2f, 0, 0);
	}
}
