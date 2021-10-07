using UnityEngine;
using System.Collections;

public class SATC01BonusGame : ChallengeGame 
{
	public GameObject[] sectionSelections;
	public GameObject[] rightPanelButtons;
	public GameObject[] rightPanelTypeButton;
	public UILabel winCountLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winCountLabelWrapperComponent;

	public LabelWrapper winCountLabelWrapper
	{
		get
		{
			if (_winCountLabelWrapper == null)
			{
				if (winCountLabelWrapperComponent != null)
				{
					_winCountLabelWrapper = winCountLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winCountLabelWrapper = new LabelWrapper(winCountLabel);
				}
			}
			return _winCountLabelWrapper;
		}
	}
	private LabelWrapper _winCountLabelWrapper = null;
	
	public UILabel winCountAlternate;	// To be removed when prefabs are updated.
	public LabelWrapperComponent winCountAlternateWrapperComponent;

	public LabelWrapper winCountAlternateWrapper
	{
		get
		{
			if (_winCountAlternateWrapper == null)
			{
				if (winCountAlternateWrapperComponent != null)
				{
					_winCountAlternateWrapper = winCountAlternateWrapperComponent.labelWrapper;
				}
				else
				{
					_winCountAlternateWrapper = new LabelWrapper(winCountAlternate);
				}
			}
			return _winCountAlternateWrapper;
		}
	}
	private LabelWrapper _winCountAlternateWrapper = null;
	
	public GameObject bottomPanel;
	public GameObject rightPanel;
	public GameObject doorPanel;
	public GameObject winBoxAnimationPrefab;
	public GameObject finalWinBoxAnimationPrefab;
	public GameObject topRing;
	
	private long initialCredits = 0;

	private enum SATCPickStateEnum
	{
		Cosmetic = 0,
		Fragrance,
		Shoes,
		Handbag,
		Jewelry,
		Ring
	}

	[HideInInspector] private SATCPickStateEnum currentBonusState = SATCPickStateEnum.Cosmetic;	// Track what the current state of the bonus game is, simplifies how we tell when we reach the final stage
	public bool isFinalState // Tells if this is the final stage of the bonus
	{
		get { return currentBonusState == SATCPickStateEnum.Ring; }
	}


	public bool hasLoaded = false;
	
	private enum animStates
	{
		opening,
		closing,
		rising,
		idle
	}
	
	private animStates state;
	
	public override void init() 
	{
		initialCredits = BonusGamePresenter.instance.currentPayout;
		state = animStates.idle;
		hasLoaded = true;
		_didInit = true;
		setButtons(0);
	}
	
	protected override void OnEnable()
	{
		base.OnEnable();
	}
	
	
	// setButtons disables the right and bottom panel when needed
	// it also highlights/disables the appropriate button
	private void setButtons(SATCPickStateEnum pickState)
	{
		currentBonusState = pickState;
		sectionSelections[(int)currentBonusState].SetActive(true);
		
		// Do not disable colliders if this is the ring game.
		if (currentBonusState != SATCPickStateEnum.Ring)
		{
			sectionSelections[(int)currentBonusState].GetComponent<SATC01BonusPick>().enablePicks(false);
		}
		
		if (currentBonusState == SATCPickStateEnum.Cosmetic)
		{
			doorPanel.SetActive(true);
			openDoors();
		}
		
		if (currentBonusState == SATCPickStateEnum.Ring)
		{
			if (bottomPanel)
			{
				bottomPanel.SetActive(false);
			}
			if (rightPanel)
			{
				rightPanel.SetActive(false);
			}
				
			return;
		}
		else
		{
			if (bottomPanel)
			{
				bottomPanel.SetActive(true);
			}
			if (rightPanel)
			{
				rightPanel.SetActive(true);
			}
		}
		
		foreach (Transform t  in rightPanelButtons[(int)currentBonusState].transform)
		{
			if (t.gameObject.name.ToLower() == "off")
			{
				t.gameObject.SetActive(false);
			}
			else
			{
				t.gameObject.SetActive(true);
			}
		}
		
		for (int i = 0; i < rightPanelTypeButton.Length; i++)
		{
			if ((int)currentBonusState == i)
			{
				foreach (Transform t in rightPanelTypeButton[i].transform)
				{
					if (t.gameObject.name.ToLower() == "on")
					{
						t.gameObject.SetActive(true);
					}
					else if (t.gameObject.name.ToLower() == "effect")
					{
						t.gameObject.SetActive(true);
					}
					else
					{
						t.gameObject.SetActive(false);
					}
				}
			}
			else
			{
				foreach (Transform t in rightPanelTypeButton[i].transform)
				{
					if (t.gameObject.name.ToLower() == "active")
					{
						t.gameObject.SetActive(true);
					}
					else if (t.gameObject.name.ToLower() == "effect")
					{
						t.gameObject.SetActive(false);
					}
					else
					{
						t.gameObject.SetActive(false);
					}
				}
			}
		}
	}
	
	public void updateScore()
	{
		StartCoroutine(doRollup());
	}
	
	private IEnumerator doRollup()
	{
		LabelWrapper label = winCountLabelWrapper;
		
		// show the paybox rollup effect
		if (isFinalState)
		{
			label = winCountAlternateWrapper;
			finalWinBoxAnimationPrefab.SetActive(true);
		}
		else
		{
			winBoxAnimationPrefab.SetActive(true);
		}

		// prevent processing the pick selection press as a rollup skip press
		yield return null;

		yield return StartCoroutine(SlotUtils.rollup(initialCredits, BonusGamePresenter.instance.currentPayout, label));

		initialCredits = BonusGamePresenter.instance.currentPayout;

		// hide the paybox rollup effect
		if (isFinalState)
		{
			finalWinBoxAnimationPrefab.SetActive(false);
		}
		else
		{
			winBoxAnimationPrefab.SetActive(false);
		}
	}

	public void summaryEnd()
	{
		BonusGamePresenter.instance.gameEnded();
	}
	
	public void gameEnd()
	{
		if (isFinalState)
		{
			BonusGamePresenter.instance.gameEnded();
		}
		else 
		{
			closeDoors();
		}	
	}
	
	//closeDoors closes the elevator doors, upon animation completion onDoorsClosed is called
	private void closeDoors()
	{
		//Debug.Log("close doors start");
		
		//reverse the opening animation
		doorPanel.SetActive(true);
		state = animStates.closing;
		doorPanel.GetComponent<Animation>()["SATC01 Youre the One Door open"].speed = -0.5f;
		doorPanel.GetComponent<Animation>()["SATC01 Youre the One Door open"].time = doorPanel.GetComponent<Animation>()["SATC01 Youre the One Door open"].length;
		doorPanel.GetComponent<Animation>().Play("SATC01 Youre the One Door open");
		
		Audio.play("ElevatorDoorsCloseRunStop");
	}

	/**
	Hide all of the buttons, used before lighting up the ring to show going to the ring stage of the game
	*/
	private void disableAllRightPanelButtons()
	{
		for (int i = 0; i < rightPanelTypeButton.Length; i++)
		{
			foreach (Transform t in rightPanelTypeButton[i].transform)
			{
				if (t.gameObject.name.ToLower() == "active")
				{
					t.gameObject.SetActive(true);
				}
				else if (t.gameObject.name.ToLower() == "effect")
				{
					t.gameObject.SetActive(false);
				}
				else
				{
					t.gameObject.SetActive(false);
				}
			}
		}
	}
	
	//onDoorsClosed is called when elevator door closing animation completes. It will either start the ring game or start the next round.
	private void onDoorsClosed()
	{
		if (currentBonusState == SATCPickStateEnum.Jewelry)
		{
			// disable the buttons on the right panel
			disableAllRightPanelButtons();

			// turn the right side panel ring indicator on
			CommonGameObject.parentsFirstSetActive(topRing, true);

			// activate the ring level
			StartCoroutine(startRingLevel());
			state = animStates.idle;
		}
		else
		{
			sectionSelections[(int)currentBonusState].SetActive(false);
			currentBonusState++;
			setButtons(currentBonusState);
			moveElevator();
		}
	}
	
	// starts the elevator rising animatoin, on animation completion elevatorStopped is called
	private void moveElevator()
	{
		state = animStates.rising;
		doorPanel.GetComponent<Animation>()["SATC01 Youre the One Door Rising"].speed = 0.5f;
		doorPanel.GetComponent<Animation>().Play("SATC01 Youre the One Door Rising");
	}
	
	private void elevatorStopped()
	{
		openDoors();
	}
	
	private void openDoors()
	{
		state = animStates.opening;
		//make sure the animation is playing forwards at the correct speed
		doorPanel.GetComponent<Animation>()["SATC01 Youre the One Door open"].speed = 0.5f;
		doorPanel.GetComponent<Animation>()["SATC01 Youre the One Door open"].time = 0;
		doorPanel.GetComponent<Animation>().Play("SATC01 Youre the One Door open");
		
		Audio.play("DingElevatorOpens");
		
	}
	
	private void onDoorsOpened()
	{
		//enable the pick items colliders now that the door is moved and they are visible
		sectionSelections[(int)currentBonusState].GetComponent<SATC01BonusPick>().enablePicks(true);
		state = animStates.idle;
		doorPanel.SetActive(false);
	}
	
	public IEnumerator startRingLevel()
	{
		Audio.switchMusicKey("BonusPickemSATC");
		Audio.playMusic("BonusPickemWelcomeSATC");
		
		yield return new WaitForSeconds(2.0f);
		doorPanel.SetActive(false);
		
		sectionSelections[(int)currentBonusState].SetActive(false);
		currentBonusState++;
		setButtons(currentBonusState);
		winCountAlternateWrapper.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
	}
	
	protected override void Update()
	{
		base.Update();
		if (!_didInit)
		{
			return;
		}
		
		if (!doorPanel.GetComponent<Animation>().isPlaying)
		{
			switch (state)
			{
				case animStates.idle:
				{
					break;
				}
				case animStates.opening:
				{
					onDoorsOpened();
					break;
				}
				case animStates.closing:
				{
					onDoorsClosed();
					break;
				}
				case animStates.rising:
				{
					elevatorStopped();
					break;
				}
			}
			
		}
	}
}

