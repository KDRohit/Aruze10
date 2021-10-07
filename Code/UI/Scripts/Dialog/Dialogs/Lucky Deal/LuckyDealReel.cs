using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class LuckyDealReel : TICoroutineMonoBehaviour
{
	public List<LuckyDealSymbol> symbols = new List<LuckyDealSymbol>();
	public AnimationListController.AnimationInformationList reelStopAnimations;		// animations to trigger when the reel stops

	public float symbolHeight;
	public bool isPercentReel;

    public AnimationCurve curve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
    private Vector3 start;
    private Vector3 end;
    public float duration = 3.0f;
    private float curTime;

    public float stopPos = 972.0f;

	private Animator symAnimator;

	private GameObject stopSymbol;
	public string stopText = "";

	private int reelStop;
	private int[] sortedInts;
	private int[] mapInts;

	public bool isSpinning;

	private float bottomY;
	private float topY;
	private float curY;

	private bool isStopping;

	private float span;
	public float direction = -1.0f;
	public float numLoops = 2.0f;

	private const float THROB_SCALE = 1.27f;
	private const float REEL_WAIT_TIME = 0.5f;

	public void init(JSON data, int stopIndex)
	{
		curTime = 0.0f;

		reelStop = stopIndex;
		mapSymbols(data);

		setSymbolTexts();

		topY = symbols[0].symbolContainer.transform.localPosition.y + symbolHeight;
		bottomY = symbols[symbols.Count-1].symbolContainer.transform.localPosition.y - symbolHeight;
		span = topY - bottomY;
		moveSymbolsToStartingPostions();

		if (Data.debugMode)
		{
			if (!DevGUIMenuLuckyDeal.overrideReels)
			{
				int i = direction > 0 ? 1 : 0;
				DevGUIMenuLuckyDeal.reelTimes[i] = duration;
				DevGUIMenuLuckyDeal.reelLoops[i] = numLoops;
			}
		}
	}

	private void initTweenData()
	{
		if (Data.debugMode)
		{
			if (DevGUIMenuLuckyDeal.overrideReels)
			{
				int i = direction > 0 ? 1 : 0;

				duration = DevGUIMenuLuckyDeal.reelTimes[i];
				numLoops = DevGUIMenuLuckyDeal.reelLoops[i];
			}
		}

		curY = stopSymbol.transform.localPosition.y;

		float totalDist = 0.0f;

		if (curY > stopPos)
		{
			totalDist += span - (curY - stopPos);
		}
		else
		{
			totalDist += (stopPos - curY);
		}

		totalDist += span * numLoops * direction;

		start.y = 0.0f;
		end.y = totalDist;
		curY = 0;
	}

	void Update()
	{
		if (isSpinning)
		{
			curTime += Time.deltaTime;
		    float prevY = curY;
		    float percentFinished = curTime / duration;
		    Vector3 newPos = Vector3.Lerp(start, end, curve.Evaluate(percentFinished));
		    curY = newPos.y;

		    if (percentFinished >= 1.0f)
		    {
				isSpinning = false;
				lockInSymbols();
		    }

			if (isSpinning)
			{
				foreach (LuckyDealSymbol symbol in symbols)
				{
					CommonTransform.setY(symbol.symbolContainer.transform, symbol.symbolContainer.transform.localPosition.y + (curY - prevY));
					checkForSymbolWrapAround(symbol);
				}
			}
		}
	}

	private void checkForSymbolWrapAround(LuckyDealSymbol symbol)
	{
		if (symbol.symbolContainer.transform.localPosition.y < bottomY)
		{
			float y = topY + (symbol.symbolContainer.transform.localPosition.y - bottomY);
			CommonTransform.setY(symbol.symbolContainer.transform, y);
		}
		else if (symbol.symbolContainer.transform.localPosition.y > topY)
		{
			float y = bottomY + (symbol.symbolContainer.transform.localPosition.y - topY);
			CommonTransform.setY(symbol.symbolContainer.transform, y);
		}
	}

	public void startSpinning()
	{
		StartCoroutine(spinSequence());
	}

	public IEnumerator spinSequence()
	{
		initTweenData();

		isSpinning = true;

		while (isSpinning)
		{
			yield return null;
		}

		if (symAnimator != null)
		{
			symAnimator.Play("Play");
		}
		else
		{
			CommonEffects.throbLoop(stopSymbol, THROB_SCALE);
		}

		StartCoroutine(playStopAnimations());

	}

	public IEnumerator playStopAnimations()
	{
		yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(reelStopAnimations));
	}

	public void lockInSymbols()
	{
		// lock into final position
		float  diff =  stopSymbol.transform.localPosition.y - stopPos;
		foreach (LuckyDealSymbol symbol in symbols)
		{
			CommonTransform.setY(symbol.symbolContainer.transform, symbol.symbolContainer.transform.localPosition.y - diff);
			checkForSymbolWrapAround(symbol);
		}
		isSpinning = false;
	}

	private void moveSymbolsToStartingPostions()
	{
		float y = symbols[0].symbolContainer.transform.localPosition.y;
		for (int i = 0; i < mapInts.Length; i++)
		{
			int stripPos = findSNextStripPos(sortedInts[i], mapInts);
			if (stripPos >= 0)
			{
				CommonTransform.setY(symbols[i].symbolContainer.transform, y - symbolHeight * (float)stripPos);
			}
		}
	}

	private int findSNextStripPos(int stripValue, int[] srcArray)
	{
		for (int i = 0; i < srcArray.Length; i++)
		{
			if (srcArray[i] == stripValue)
			{
				srcArray[i] = 0;
				return i;
			}
		}

		return -1;
	}

	private void setSymbolTexts()
	{
		for (int i = 0; i < sortedInts.Length; i++)
		{
			if (symbols[i].symbolText != null)
			{
				if (isPercentReel)
				{
					symbols[i].symbolText.text = sortedInts[i] + "<size=70%><voffset=0.2em>%</voffset></size>\r\n<size=75%>OFF</size>";		// tbd get from scat localize correctly
				}
				else
				{
					symbols[i].symbolText.text = "<size=70%><voffset=0.2em>$</voffset></size>" + sortedInts[i];		// tbd get from scat localize correctly
				}

				if (sortedInts[i] == reelStop)
				{
					stopSymbol = symbols[i].symbolContainer;
					symAnimator = stopSymbol.GetComponent<Animator>();

					if (isPercentReel)
					{
						stopText = Localize.text("{0}_percent_off", sortedInts[i]);
					}
					else
					{
						stopText = symbols[i].symbolText.text;
					}
				}

				if (i == sortedInts.Length - 1)		// at highest value symbol, see if we got a sprite we can use
				{
					symbols[i].symbolText.gameObject.SetActive(sortedInts[i] != 100);
					SafeSet.gameObjectActive(symbols[i].animatedSprite, sortedInts[i] == 100);
				}
			}
		}
	}

	// not used since we don't have packages for all the possible values, this means all countries see USD currency on the wheel
	private void convertToLocalCurrency(int i)
	{
		string symString = "0";
		string packageKey = "coin_package_" + sortedInts[i];
		PurchasablePackage pricePackage = PurchasablePackage.find(packageKey);

		if (pricePackage != null)
		{
			symString = pricePackage.getRoundedPrice();

			if (pricePackage.currencyCode == "USD" && symString.Length > 1)		// make it look nice for US currency
			{
				symbols[i].symbolText.text = "<size=70%><voffset=0.2em>" + symString.Substring(0,1) + "</voffset></size>" + symString.Substring(1, symString.Length-1);		// tbd get from scat localize correctly
			}
			else
			{
				symbols[i].symbolText.text = symString;
			}
		}
		else
		{
			Debug.LogError("Price package for currency conversion is null! " + packageKey);
			symbols[i].symbolText.text = "No Package For : " + sortedInts[i];
		}
	}

	// map the reel strips to the lowest to hightest symbols of which there is always 8 and keep track of strip position
	private void mapSymbols(JSON data)
	{
		string[] strArray = data.getStringArray("symbols");

		sortedInts = new int[strArray.Length];
		mapInts = new int[strArray.Length];

		int origStop = reelStop;

		for (int i = 0; i < strArray.Length; i++)
		{
			int symbolValue = -1;

			string symbolStr = strArray[i];

			Regex regexObj = new Regex(@"[^\d]");	// extract the digits
			string number = regexObj.Replace(symbolStr, "");

			if (number.Length > 0)
			{
				symbolValue = int.Parse(number);
			}
			else
			{
				Debug.LogError("Lucky Deal symbol in reelstrip has no value " + symbolStr);
			}

			if (i == origStop)
			{
				reelStop = symbolValue;
			}

			sortedInts[i] = symbolValue;
			mapInts[i] = symbolValue;		// where it is supposed to start

			int j = i;
			while ((j > 0) && (sortedInts[j] < sortedInts[j - 1]))
			{
				int k = j - 1;
				int temp = sortedInts[k];
				sortedInts[k] = sortedInts[j];
				sortedInts[j] = temp;
				j--;
			}
		}
	}

	[System.Serializable]
	public class LuckyDealSymbol
	{
		public GameObject	symbolContainer;
		public TextMeshPro 	symbolText;
		public GameObject	animatedSprite;
	}
}
