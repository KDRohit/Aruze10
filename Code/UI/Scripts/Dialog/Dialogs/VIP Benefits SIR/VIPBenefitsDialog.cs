using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using TMPro;
public class VIPBenefitsDialog : DialogBase 
{

	public GameObject benefitsParent;
	public GameObject overviewParent;
	public GameObject overviewFillObject;
	public GameObject benefitsFillObject;
	public Transform goldSlider;

	public GameObject gemProgressParent;
	public GameObject gemMaxLevelParent;
	public VIPNewIcon gemCurrent;
	public VIPNewIcon gemNext;
	public VIPNewIcon gemMaxLevel;
	public TextMeshPro vipPercentLabel;
	public TextMeshPro vipBenefitsLevelInfo;
	public TextMeshPro currentVIPPoints;

	public VIPBenefitsColumn[] benefitsColumns = null;

	private const int BENEFITS_FILL_ADJUSTMENT = 88;

	private VIPLevel currentLevel;
	private bool benefitsHasInitialized = false;

	public override void init()
	{   
		// Set up current level
		currentLevel = VIPLevel.find(SlotsPlayer.instance.vipNewLevel);

		// Put in the correct gems
		gemProgressParent.SetActive(currentLevel != VIPLevel.maxLevel && currentLevel.levelNumber < VIPLevel.maxLevel.levelNumber);
		gemMaxLevelParent.SetActive(currentLevel == VIPLevel.maxLevel && currentLevel.levelNumber <= VIPLevel.maxLevel.levelNumber);

		if (currentLevel != VIPLevel.maxLevel)
		{
			gemCurrent.setLevel(currentLevel);
			gemNext.setLevel(SlotsPlayer.instance.vipNewLevel + 1);
		}
		else
		{
			gemMaxLevel.setLevel(currentLevel);
		}

		// Get the max scale of the fill object
		float scaleMax = overviewFillObject.transform.localScale.x;

		CommonTransform.setWidth(overviewFillObject.transform, 0);
		
		currentVIPPoints.text = Localize.text("vip_points_{0}", CommonText.formatNumber(SlotsPlayer.instance.vipPoints));
		vipBenefitsLevelInfo.text = currentLevel.name;

		if (currentLevel.levelNumber < VIPLevel.maxLevel.levelNumber)
		{	
			VIPLevel nextLevel = VIPLevel.find(currentLevel.levelNumber + 1);
			
			float maxVIPVal = nextLevel.vipPointsRequired - currentLevel.vipPointsRequired;
			float currentVIPVal = SlotsPlayer.instance.vipPoints - currentLevel.vipPointsRequired;
			float percent = (currentVIPVal / maxVIPVal);
	
			iTween.ScaleTo(overviewFillObject, iTween.Hash("x", scaleMax * percent, "time", 3, "easetype", iTween.EaseType.easeOutSine));
			vipPercentLabel.text = Localize.text("{0}_percent", CommonText.formatNumber(Mathf.FloorToInt(100.0f * percent)));
		}
	}

	public void backToBenefits()
	{
		overviewParent.SetActive(false);
		benefitsParent.SetActive(true);
	  
		if (!benefitsHasInitialized)
		{
			for (int i = 0; i < benefitsColumns.Length; i++)
			{
				benefitsColumns[i].init(i);
			}

			benefitsHasInitialized = true;
		
			float scaleMax = benefitsFillObject.transform.localScale.x;
			float fillAmount = scaleMax;
			CommonTransform.setWidth(benefitsFillObject.transform, 10);

			if (currentLevel != VIPLevel.maxLevel)
			{      
				fillAmount = benefitsColumns[SlotsPlayer.instance.vipNewLevel].transform.localPosition.x + BENEFITS_FILL_ADJUSTMENT;
			}

			iTween.ScaleTo(benefitsFillObject, iTween.Hash("x", fillAmount, "time", 1, "easetype", iTween.EaseType.easeOutSine));
			
			goldSlider.parent = benefitsColumns[SlotsPlayer.instance.vipNewLevel].transform;
			CommonTransform.setX(goldSlider, 0.0f);
		}
	}
	
	
	public void backToOverview()
	{
		benefitsParent.SetActive(false);
		overviewParent.SetActive(true);        
	}

	public override void close()
	{
		// No special close code...
	}

	public void clickClose()
	{
		Dialog.close();        
	}

	public static void showDialog(bool shouldGoDirectToStatusMode = false, bool isIntro = false)
	{	
		Scheduler.addDialog("vip",
			Dict.create(
				D.OPTION, isIntro,
				D.OPTION1, shouldGoDirectToStatusMode
			)
		);
	}
}
