using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PaginationController : MonoBehaviour
{
	[SerializeField] private GameObject pipExemplar;
	[SerializeField] private UICenteredGrid pipGrid;
	
	private List<PaginationPip> allPips;
	private PageController pageController;

	public void initWithPageController(int numPages, int currentPage = 0, PageController controller = null)
	{
		init(numPages, currentPage);
		this.pageController = controller;
	}
	
	public void init(int numPages, int currentPage = 0)
	{
		// To allow for re-use, destroy all pips before initializing;
		if (allPips != null)
		{
			allPips.Clear();
			CommonGameObject.destroyChildren(pipGrid.gameObject);
		}
		else
		{
			allPips = new List<PaginationPip>();
		}

		GameObject newPipObject;
		PaginationPip newPip;
		pipExemplar.SetActive(true); // Make sure the exemplar is ON before we clone it.
		for (int i = 0; i < numPages; i++)
		{
			// Create the pip
			newPipObject = GameObject.Instantiate(pipExemplar, pipGrid.transform);
			if (newPipObject != null)
			{
				newPip = newPipObject.GetComponent<PaginationPip>();
				if (newPip != null)
				{
					newPip.init(i, this);
					allPips.Add(newPip);
				}
				else
				{
					// Otherwise throw an error and delete the new pip.
					Debug.LogErrorFormat("PaginationController.cs -- init() -- no PaginationPip on the new object.");
					Destroy(newPipObject);
				}
			}
			else
			{
				Debug.LogErrorFormat("PaginationController.cs -- init()  -- failed to create pip object from exemplar.");
			}
		}
		pipExemplar.SetActive(false); //Make sure we turn the exemplar back off.
		pipGrid.reposition();
		selectPage(currentPage);
	}

	public void selectPage(int page)
	{
		if (allPips == null)
		{
			Debug.LogErrorFormat("PaginationController.cs -- selectPage() -- trying to select a page before initializing");
			return;
		}
		for (int i = 0; i < allPips.Count; i++)
		{
			allPips[i].toggle(i == page);
		}
	}

	public void onPipClicked(int page)
	{
		if (pageController != null)
		{
			pageController.goToPage(page);
		}
	}
}
