using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class PopcornVariant
{

	public static bool isActive
	{
		get
		{
			// To avoid loading a sale that we dont support, check if the client has a case for that template before
			// allowing it.
			return ExperimentWrapper.PopcornVariantTest.isInExperiment &&
				folderMap.ContainsKey(ExperimentWrapper.PopcornVariantTest.template);
		}
	}
		
	private static Dictionary<string,string> folderMap = new Dictionary<string, string>
		{
			{"ovals", "PopcornSales_Ovals"},
			{"circles", "PopcornSales_Circles"},
			{"diamonds", "PopcornSales_Diamonds"},
			{"rectangles_stacked", "PopcornSales_Rectangles_Stacked"},
			{"rectangles_staggered", "PopcornSales_Rectangles_Staggered"}
	};
	
    private static string getFolderName(string template)
	{
		if (folderMap.ContainsKey(template))
		{
			return folderMap[template];
		}
		else
		{
			return "";
		}
	}

	public static string currentBackgroundPath
	{
		get
		{
			return backgroundPath(ExperimentWrapper.PopcornVariantTest.template, ExperimentWrapper.PopcornVariantTest.theme);
		}
	}

	public static string currentCarouselPath
	{
		get
		{
			return carouselPath(ExperimentWrapper.PopcornVariantTest.template, ExperimentWrapper.PopcornVariantTest.theme);
		}
	}

	public static string currentDialogKey
	{
		get
		{
			return dialogTypeKey(ExperimentWrapper.PopcornVariantTest.template);
		}
	}

	// Creates a file path from the experiment data, returns empty string if they arent in the experiment.
	public static string backgroundPath(string template, string theme)
	{
		if (!ExperimentWrapper.PopcornVariantTest.isInExperiment)
		{
			return "";
		}
		
		return string.Format("stud/popcorn_sales/{0}/{1}/{2}/DialogBG.png",
			SkuResources.skuString.ToUpper(),
		    getFolderName(template),
			theme
		);
	}

	public static string carouselPath(string template, string theme)
	{
		if (!ExperimentWrapper.PopcornVariantTest.isInExperiment)
		{
			return "";
		}
		
		return string.Format("stud/popcorn_sales/{0}/{1}/{2}/CarouselBG.jpg",
			SkuResources.skuString.ToUpper(),
		    getFolderName(template),
		    theme
		);		
	}

	public static string dialogTypeKey(string theme)
	{
		return "popcorn_sale_" + theme;
	}
}