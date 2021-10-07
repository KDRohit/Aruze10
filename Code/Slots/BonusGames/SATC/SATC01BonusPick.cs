using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * This is the class for Sex and the city bonus game, this controls how individual floors behave
 * 
 */
public class SATC01BonusPick : TICoroutineMonoBehaviour 
{
	private const float TIME_BETWEEN_REVEALS = 0.5f;
	private const float MIN_TIME_BETWEEN_SHAKES = 2.0f;			// Minimum time between shakes
	private const float MAX_TIME_BETWEEN_SHAKES = 3.0f;			// Maximum time between shakes

	public GameObject[] buttonSelections;
	public UILabel[] revealTexts;	// To be removed when prefabs are updated.
	public LabelWrapperComponent[] revealTextsWrapperComponent;

	public List<LabelWrapper> revealTextsWrapper
	{
		get
		{
			if (_revealTextsWrapper == null)
			{
				_revealTextsWrapper = new List<LabelWrapper>();

				if (revealTextsWrapperComponent != null && revealTextsWrapperComponent.Length > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in revealTextsWrapperComponent)
					{
						_revealTextsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in revealTexts)
					{
						_revealTextsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _revealTextsWrapper;
		}
	}
	private List<LabelWrapper> _revealTextsWrapper = null;	
	
	public GameObject collectAllGameObject;
	public GameObject gameOverGameObject;
	public SATC01BonusGame bonusGameParent;
	public GameObject revealAllEffectPrefab;
	public GameObject revealEffectPrefab;
	
	private WheelOutcome outcome;
	private WheelPick pick;
	private List<GameObject> createdGameObjects = new List<GameObject>();
	private bool gameOver = false;
	private int lastRnd = -1;									// Used to prevent the same object from shaking twice
	private bool nextPickAcquired = false;
	private bool isRevealingPicks = false;						// Tracks if this pick is revealing, in which case shaking should be stopped
	private float shakeTimer = 0;				// Used to track tha time till a shake is next played
	private SkippableWait revealWait = new SkippableWait();

	/**
	Handle cleanup of dynamically created objects
	*/
	private void OnDestroy()
	{
		foreach (GameObject gb in createdGameObjects)
		{
			gb.SetActive(false);
			Destroy(gb);
		}
	}

	public void Update () 
	{
		if (bonusGameParent.hasLoaded)
		{
			if (!nextPickAcquired)
			{
				if (BonusGameManager.instance.outcomes.ContainsKey(BonusGameType.CHALLENGE))
				{
					nextPickAcquired = true;
					outcome = BonusGameManager.instance.outcomes[BonusGameType.CHALLENGE] as WheelOutcome;
					pick = outcome.getNextEntry();
					
					foreach (LabelWrapper revealText in revealTextsWrapper)
					{
						revealText.alpha = 0;
					}
				}
				else
				{
					Debug.LogError("SATC01BonusPick.Update:  No challenge outcome?!");
				}
			}
			
			if (!isRevealingPicks)
			{
				shakeTimer -= Time.deltaTime;

				if (shakeTimer <= 0)
				{
					int rnd = Random.Range(0, buttonSelections.Length - 1);

					// ensure the same one doesn't happen twice in a row
					if (rnd == lastRnd)
					{
						rnd += 2;
						rnd = rnd % buttonSelections.Length;
					}

					iTween.ShakeRotation(buttonSelections[rnd], iTween.Hash("amount", new Vector3(0, 0, 5), "time", 0.5f));
					lastRnd = rnd;

					// get a new time till next shake
					shakeTimer = Random.Range(MIN_TIME_BETWEEN_SHAKES, MAX_TIME_BETWEEN_SHAKES);
				}
			}
		}
	}
	
	void Start ()
	{
		shakeTimer = Random.Range(MIN_TIME_BETWEEN_SHAKES, MAX_TIME_BETWEEN_SHAKES);
	}
	
	// Check if the current result index in pick.wins array is a collect all.
	private bool isCollectAll(int index)
	{
		long winAmount = 0;
		long unWonAmount = 0;
		long winMax = 0;

		winAmount = pick.wins[index].credits;
		winMax =  winAmount;
		
		int i = 0;
		foreach (WheelPick card in pick.wins)
		{
			long lCredit = card.credits;
			if (i == index)
			{
				i++;
				continue;
			}
			if (lCredit == 0)
			{
				continue;
			}
			unWonAmount += lCredit;
				
			if (lCredit > winMax)
			{
				winMax = lCredit;
			}
			
			i++;
		}
		return (winAmount == winMax) && (winAmount == unWonAmount);
	}
	
	public void enablePicks(bool enable)
	{
		for (int i = 0; i < buttonSelections.Length; i++)
		{
			buttonSelections[i].GetComponent<Collider>().enabled = enable;
		}
	}
	
	// Stuff to do when you select a pick
	public void onPickSelected(GameObject button)
	{
		NGUITools.SetActive(button, false);
	
		// Let's find which button index was clicked on.	
		int index = System.Array.IndexOf(buttonSelections, button);
		
		bool collectAll = false;
		
		// If it's a pick which has credits display the credit and check if its a collect all
		// If it's a collect All display the collect All GameObject
		if (pick.credits != 0)
		{
			long credits = pick.wins[pick.winIndex].credits;
			revealTextsWrapper[index].gameObject.SetActive(true);
			revealTextsWrapper[index].alpha = 1;
			revealTextsWrapper[index].text = CreditsEconomy.convertCredits(credits);
			BonusGamePresenter.instance.currentPayout += credits;
			
			bonusGameParent.updateScore();
			if (isCollectAll(pick.winIndex))
			{
				if (collectAllGameObject)
				{
					collectAll = true;
					GameObject gb = (GameObject)NGUITools.AddChild(revealTextsWrapper[index].transform.parent.gameObject, collectAllGameObject);
					gb.SetActive(true);
					gb.transform.localPosition = new Vector3(gb.transform.localPosition.x, gb.transform.localPosition.y, -2);
					createdGameObjects.Add(gb);

					revealTextsWrapper[index].text = "";
					Audio.play ("RevealWinAllBonusSATC01");
					Audio.play("bgTheLadiesEnjoySleightOfHand", 1, 0, 1f);
					GameObject particleGb = (GameObject)NGUITools.AddChild(revealTextsWrapper[index].transform.parent.gameObject, revealAllEffectPrefab);
					particleGb.SetActive(true);
					createdGameObjects.Add(particleGb);
				}
				else
				{
					revealTextsWrapper[index].text = "";
				}
			}
			else
			{
				Audio.play ("BonusRegisterSATC01");
				GameObject particleGb = (GameObject)NGUITools.AddChild(revealTextsWrapper[index].transform.parent.gameObject, revealEffectPrefab);
				particleGb.SetActive(true);
				createdGameObjects.Add(particleGb);
				CommonTransform.setZ(particleGb.transform, -20);
			}
		}
		else if (pick.multiplier != 0)
		{
			collectAll = true;
			revealTextsWrapper[index].gameObject.SetActive(true);
			revealTextsWrapper[index].alpha = 1;
			revealTextsWrapper[index].text = "";//Localize.text("{0}X", (1 + _pick.multiplier));
			BonusGamePresenter.instance.currentPayout *= (1 + pick.multiplier);
			bonusGameParent.updateScore();

			if (collectAllGameObject)
			{
				collectAll = true;
				GameObject gb = (GameObject)NGUITools.AddChild(revealTextsWrapper[index].transform.parent.gameObject, collectAllGameObject);
				gb.SetActive(true);
				gb.transform.localPosition = new Vector3(gb.transform.localPosition.x, gb.transform.localPosition.y, -2);
				createdGameObjects.Add(gb);

				Audio.play ("RevealWinAllBonusSATC01");
				GameObject particleGb = (GameObject)NGUITools.AddChild(revealTextsWrapper[index].transform.parent.gameObject, revealAllEffectPrefab);
				particleGb.SetActive(true);
				createdGameObjects.Add(particleGb);
			}
			
			Audio.play("cbHowDidYouEvenGetHere");
		}
		
		// If it's not a continue card, show the game over object instead	
		if (!pick.wins[pick.winIndex].canContinue && collectAll == false)
		{
			gameOver = true;
			if (gameOverGameObject) 
			{
				if (bonusGameParent.isFinalState)
				{
					revealTextsWrapper[index].alpha = 1;
					revealTextsWrapper[index].text = CreditsEconomy.convertCredits(pick.credits);
				}
				else
				{
						
					GameObject gb = (GameObject)NGUITools.AddChild(revealTextsWrapper[index].transform.parent.gameObject, gameOverGameObject);
					gb.SetActive(true);
					createdGameObjects.Add(gb);
					revealTextsWrapper[index].text = "";
				}
				
			}
			else
			{
				revealTextsWrapper[index].text = "";
			}
			Audio.play ("ClosedSATC01");
		}
		
		// Disable all colliders after a pick is selected
		for (int i = 0; i < buttonSelections.Length; i++)
		{
			buttonSelections[i].GetComponent<Collider>().enabled = false;
		}
		
		StartCoroutine(revealRemainingPicks());
	}
	
	// Pretty print the WheelPick class
	private void print_p(WheelPick pick)
	{
		for (int i = 0; i < pick.wins.Count; i++)
		{
			Debug.Log("index  " + i + " has continue set to " + pick.wins[i].canContinue);
		}
	}
	
	// Reveal all other picks
	private IEnumerator revealRemainingPicks()
	{
		isRevealingPicks = true;

		// wait just a bit before starting to reveal them
		yield return new TIWaitForSeconds(0.5f);

		int numPicks = (bonusGameParent.isFinalState) ? 3 : 5;

		for (int revealIndex = 0; revealIndex <= numPicks; revealIndex++)
		{
			bool collectAll = false;

			// skip the index which was won
			if (revealIndex == pick.winIndex)
			{
				revealIndex++;
			}

			if(!revealWait.isSkipping)
			{
				Audio.play ("reveal_others");
			}

			// Loop throught all unrevealed RevealTexts and show the values from _pick.wins there
			for (int i = 0; i < revealTextsWrapper.Count; i++)
			{
				if (revealTextsWrapper[i].alpha == 0)
				{
					NGUITools.SetActive(buttonSelections[i], false);
					long credits 	= pick.wins[revealIndex].credits;
					int multiplier  = pick.wins[revealIndex].multiplier;
					if (credits != 0)
					{
						revealTextsWrapper[i].gameObject.SetActive(true);
						revealTextsWrapper[i].alpha = 1;
						revealTextsWrapper[i].text = CreditsEconomy.convertCredits(credits);
						
						if (pick.multiplier == 0 && !isCollectAll(pick.winIndex))
						{
							revealTextsWrapper[i].color = Color.gray;
						}
						
						if (isCollectAll(revealIndex))
						{
							if (collectAllGameObject)
							{
								collectAll = true;
								GameObject gb = (GameObject)NGUITools.AddChild(revealTextsWrapper[i].transform.parent.gameObject, collectAllGameObject);
								gb.SetActive(true);
								gb.transform.localPosition = new Vector3(gb.transform.localPosition.x, gb.transform.localPosition.y, -2);
								
								// gray out icon as it's unselected
								UISprite ut = gb.GetComponentInChildren<UISprite>();
								ut.color = Color.gray;
								
								createdGameObjects.Add(gb);
								revealTextsWrapper[i].text = "";
							}
							else
							{
								revealTextsWrapper[i].text = "";
							}
						}
					}
					else if (multiplier != 0)
					{	
						collectAll = true;
						revealTextsWrapper[i].gameObject.SetActive(true);
						revealTextsWrapper[i].alpha = 1;
						revealTextsWrapper[i].text =  "";

						if (collectAllGameObject)
						{
							GameObject gb = (GameObject)NGUITools.AddChild(revealTextsWrapper[i].transform.parent.gameObject, collectAllGameObject);
							gb.SetActive(true);
							gb.transform.localPosition = new Vector3(gb.transform.localPosition.x, gb.transform.localPosition.y, -2);

							// gray out icon as it's unselected
							UISprite ut = gb.GetComponentInChildren<UISprite>();
							ut.color = Color.gray;

							createdGameObjects.Add(gb);
						}
					}
					
					if (!pick.wins[revealIndex].canContinue && collectAll == false)
					{
						if (gameOverGameObject) 
						{
							if (bonusGameParent.isFinalState)
							{
								revealTextsWrapper[i].alpha = 1;
								revealTextsWrapper[i].text = CreditsEconomy.convertCredits(credits);
								revealTextsWrapper[i].color = Color.gray;
							}
							else
							{
								revealTextsWrapper[i].text = "";
								GameObject gb = (GameObject)NGUITools.AddChild(revealTextsWrapper[i].transform.parent.gameObject, gameOverGameObject);
								gb.SetActive(true);
								createdGameObjects.Add(gb);
							
								UISprite ut = gb.GetComponentInChildren<UISprite>();
								ut.color = Color.gray;
							
								UILabel ul = gb.GetComponentInChildren<UILabel>();
								ul.color = Color.gray;
							
							}
						}
						else
						{
							revealTextsWrapper[i].text = "";
						}
					}
					break;
				}
			}

			// only wait if another reveal is going to happen
			if (revealIndex != numPicks)
			{
				yield return StartCoroutine(revealWait.wait(TIME_BETWEEN_REVEALS));
			}
		}
		
		yield return new TIWaitForSeconds(1.5f);
		
		if (gameOver)
		{
			endSummary();
		}
		else
		{
			endGame();
		}
	}
	
	// This is the last floor or game over card was selected, therefore show the summary end dialog
	private void endSummary()
	{
		bonusGameParent.summaryEnd();
	}
	
	// This floor is over move to the next floor
	// Also cleanup objects created here
	private void endGame()
	{
		bonusGameParent.gameEnd();
	}
}

