using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using CustomLog;

public class Osa03Pickem : PickingGame<PickemOutcome>
{
	[SerializeField] private float APPLE_THROW_SPEED;
	[SerializeField] private float INIT_DELAY_TIME;
	[SerializeField] private float SPARKLE_TRAIL_TWEEN_TIME;
	[SerializeField] private float EXPLOSION_DELAY;
	[SerializeField] private float MULTIPLIER_TWEEN_TIME;
	[SerializeField] private float MULTIPLIER_QUEUE_TWEEN_TIME;
	[SerializeField] private float PICKEM_POPIN_TIME;
	[SerializeField] private float TIME_BETWEEN_PICKEM_POPINS;
	[SerializeField] private float WITCH_LAUGH_DELAY;

	[SerializeField] private Vector3 MULTIPLIER_DEST_SCALE;

	[SerializeField] private string BANNER_PICKME_ANIM_STATE_NAME;
	[SerializeField] private string TREE_MULTIPLIER_ANIM_START_NAME;
	[SerializeField] private string TREE_MULTIPLIER_ANIM_THROW_NAME;
	[SerializeField] private string PICKEM_REVEALS_BONUS_ANIM_NAME;
	[SerializeField] private string PICKEM_REVEALS_BONUS_GRAY_ANIM_NAME;
	[SerializeField] private string CELEBRATION_METER_ON_ANIM_NAME;
	[SerializeField] private string REVEAL_CREDIT_ANIM_NAME;
	[SerializeField] private string REVEAL_CREDIT_GRAY_ANIM_NAME;

	[SerializeField] private GameObject celebrationMeter;
	[SerializeField] private GameObject sparkleExplosion;
	[SerializeField] private GameObject sparkleTrail;
	[SerializeField] private GameObject creepyTree;
	[SerializeField] private GameObject treeApple;
	[SerializeField] private GameObject banner;
	[SerializeField] private UILabel winLabel;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent winLabelWrapperComponent;

	public LabelWrapper winLabelWrapper
	{
		get
		{
			if (_winLabelWrapper == null)
			{
				if (winLabelWrapperComponent != null)
				{
					_winLabelWrapper = winLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_winLabelWrapper = new LabelWrapper(winLabel);
				}
			}
			return _winLabelWrapper;
		}
	}
	private LabelWrapper _winLabelWrapper = null;
	
	[SerializeField] private List<GameObject> multiplierObjects = new List<GameObject>();
	[SerializeField] private List<UILabel> multiplierLabels = new List<UILabel>();	// To be removed when prefabs are updated.
	[SerializeField] private List<LabelWrapperComponent> multiplierLabelsWrapperComponent = new List<LabelWrapperComponent>();

	public List<LabelWrapper> multiplierLabelsWrapper
	{
		get
		{
			if (_multiplierLabelsWrapper == null)
			{
				_multiplierLabelsWrapper = new List<LabelWrapper>();

				if (multiplierLabelsWrapperComponent != null && multiplierLabelsWrapperComponent.Count > 0)
				{
					foreach (LabelWrapperComponent wrapperComponent in multiplierLabelsWrapperComponent)
					{
						_multiplierLabelsWrapper.Add(wrapperComponent.labelWrapper);
					}
				}
				else
				{
					foreach (UILabel label in multiplierLabels)
					{
						_multiplierLabelsWrapper.Add(new LabelWrapper(label));
					}
				}
			}
			return _multiplierLabelsWrapper;
		}
	}
	private List<LabelWrapper> _multiplierLabelsWrapper = null;	
	
	[SerializeField] private GameObject tweenMultiplierObject;
	[SerializeField] private UILabel tweenMultiplierLabel;	// To be removed when prefabs are updated.
	[SerializeField] private LabelWrapperComponent tweenMultiplierLabelWrapperComponent;

	public LabelWrapper tweenMultiplierLabelWrapper
	{
		get
		{
			if (_tweenMultiplierLabelWrapper == null)
			{
				if (tweenMultiplierLabelWrapperComponent != null)
				{
					_tweenMultiplierLabelWrapper = tweenMultiplierLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_tweenMultiplierLabelWrapper = new LabelWrapper(tweenMultiplierLabel);
				}
			}
			return _tweenMultiplierLabelWrapper;
		}
	}
	private LabelWrapper _tweenMultiplierLabelWrapper = null;
	
	[SerializeField] private GameObject masterPickemObject;
	[SerializeField] private List<GameObject> pickObjectPositions;

	// We use the default, generic, sound effect for the oilcan popin animation
	private const string BACKGROUND_MUSIC_SOUND_KEY = "pickem_bg_music";
	private const string INTRO_VO_SOUND_KEY = "pickem_intro_vo";
	private const string OILCAN_POPIN_SOUND = "pickem_pick_object_appears";
	private const string DEFAULT_REVEAL_BAD2_SOUND_KEY = "pickem_reveal_bad2";
	private const string DEFAULT_MULTIPLIER_TRAVEL2_SOUND_KEY = "pickem_multiplier_travel2";
	private const string DEFAULT_MULTIPLIER_TRAVEL3_SOUND_KEY = "pickem_multiplier_travel3";
	private const string DEFAULT_MULTIPLIER_TRAVEL4_SOUND_KEY = "pickem_multiplier_travel4";
	private const string TREE_RELOAD_SOUND_KEY = "pickem_multiplier_arrive2";

	private List<GameObject> pickemObjectList = new List<GameObject>();

	private int curMultiplier = 1;

	private long previousPayout = 0;

	private Vector3 appleStartPos;
	private Vector3 treeAppleLastPos;
	private Vector3 tweenMultiplierObjectStartPos;
	private Vector3 lastMultiplierLabelPos;

	private List<Transform> treeAppleWaypointList = new List<Transform>();

	private IEnumerator popInPickemButtons()
	{
		inputEnabled = false;

		List<GameObject> popInObjects = new List<GameObject>(pickemObjectList);
		CommonDataStructures.shuffleList(popInObjects);

		Vector3 destVec = new Vector3(1.0f, 1.0f, 1.0f);

		// Clone the master pick object and store the instances in a list
		for(int i = 0; i < popInObjects.Count; i++)
		{
			Audio.play(Audio.soundMap(OILCAN_POPIN_SOUND));

			iTween.ScaleTo(
				popInObjects[i],
				iTween.Hash(
					"scale",
					destVec,
					"time",
					PICKEM_POPIN_TIME,
					"easetype",
					iTween.EaseType.linear
					)
				);

			// Dont really need to wait to pop again if this is the last pickem obhject
			if(i != popInObjects.Count-1)
				yield return new TIWaitForSeconds(TIME_BETWEEN_PICKEM_POPINS);
		}

		yield return new TIWaitForSeconds(PICKEM_POPIN_TIME);

		inputEnabled = true;
	}

	public void delayedInit()
	{
		// Call the base init here
		base.init();

		// PopIn the pickem buttons
		StartCoroutine(popInPickemButtons());

		// Play some music
		Audio.switchMusicKeyImmediate(Audio.soundMap(BACKGROUND_MUSIC_SOUND_KEY));

		// Oh! Apples!
		Audio.play(Audio.soundMap(INTRO_VO_SOUND_KEY));
	}

	private IEnumerator waitThenInit()
	{
		yield return new TIWaitForSeconds(INIT_DELAY_TIME); //make this an inspector variable
		delayedInit();
	}

	public override void init()
	{
		List<Animator> buttonAnimators = new List<Animator>();

		// Clone the master pick object and store the instances in a list
		for(int i = 0; i < pickObjectPositions.Count; i++)
		{
			GameObject obj = pickObjectPositions[i];
			GameObject newPickObject = CommonGameObject.instantiate(masterPickemObject) as GameObject;

			newPickObject.transform.parent = obj.transform;

			// Notice that all cans are started out scaled to 0,0,0. popInPickemButtons will scale them up.
			newPickObject.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
			newPickObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);

			buttonAnimators.Add(newPickObject.GetComponent<Animator>());

			pickemObjectList.Add(newPickObject);
		}

		// Populate the round button list(0, only one round) with the contents of the pickemObjectList.
		roundButtonList[0].goList = pickObjectPositions.ToArray();
		roundButtonList[0].buttonList = pickemObjectList.ToArray();
		roundButtonList[0].animatorList = buttonAnimators.ToArray();

		// Hide the master pickem object
		masterPickemObject.SetActive(false);

		// Hide the tweening multiplier object
		tweenMultiplierObject.SetActive(false);

		// Init the multiplier label values
		for (int i = 0; i < multiplierLabelsWrapper.Count; i++)
		{
			multiplierLabelsWrapper[i].text = Localize.text("{0}X", i + 1);
		}

		treeAppleWaypointList.Add((new GameObject()).transform);
		treeAppleWaypointList.Add((new GameObject()).transform);
		treeAppleWaypointList.Add((new GameObject()).transform);

		StartCoroutine(waitThenInit());
	}

	private IEnumerator animateCreditReveal(GameObject pickemButton, long totalCredits, UILabel revealNumberLabel)
	{
		
		Audio.play(Audio.soundMap(DEFAULT_MULTIPLIER_TRAVEL3_SOUND_KEY));

		// Put the sparkle explosion and the sparkle trail right where the apple landerd
		sparkleExplosion.transform.position = pickemButton.transform.position;
		sparkleTrail.transform.position = pickemButton.transform.position;

		// Activate the sparkle explosion
		sparkleExplosion.SetActive(true);
		sparkleTrail.SetActive(true);

		// Update the reveal text to reflect the totalCredits
		revealNumberLabel.text = CreditsEconomy.convertCredits(totalCredits);

		// Wait until the explosion is done
		yield return new TIWaitForSeconds(EXPLOSION_DELAY);

		// Hide the explositon
		sparkleExplosion.SetActive(false);

		// Animate the sparkle trail flying toward the score
		yield return new TITweenYieldInstruction(
			iTween.MoveTo(
				sparkleTrail,
				iTween.Hash(
					"position",
					winLabelWrapper.gameObject.transform.position,
					"time",
					SPARKLE_TRAIL_TWEEN_TIME,
					"islocal",
					false,
					"easetype",
					iTween.EaseType.easeInQuad
					)
				)
			);

		// Hide the sparkle trail
		sparkleTrail.SetActive(false);
	}

	private void resetTweenMultiplierObject()
	{
		tweenMultiplierObject.SetActive(false);
		tweenMultiplierObject.transform.position = tweenMultiplierObjectStartPos;
	}

	private IEnumerator animateMultiplierTween()
	{
		tweenMultiplierObjectStartPos = tweenMultiplierObject.transform.position;

		GameObject topObject = multiplierObjects[0];
		LabelWrapper topLabel = multiplierLabelsWrapper[0];

		Vector3 bottomObjectStartPos = multiplierObjects[multiplierObjects.Count-1].transform.position;

		topObject.SetActive(false);

		// Since the multiplier has already been update
		tweenMultiplierLabelWrapper.text = Localize.text("{0}X", curMultiplier);
		tweenMultiplierObject.SetActive(true);

		Audio.play(Audio.soundMap(DEFAULT_MULTIPLIER_TRAVEL_SOUND_KEY));

		yield return new TITweenYieldInstruction(iTween.MoveTo(
			tweenMultiplierObject,
			iTween.Hash(
				"position",
				treeApple.transform.position,
				"time",
				MULTIPLIER_TWEEN_TIME,
				"islocal",
				false,
				"easetype",
				iTween.EaseType.linear
				)
			)
		);

		// Animate tween queue. Each element takes the position of its predecessor.
		for(int i = 1; i < multiplierObjects.Count; i++)
		{
			if(i == multiplierObjects.Count-1)
			{
				yield return new TITweenYieldInstruction(
					iTween.MoveTo(
						multiplierObjects[i],
						iTween.Hash(
							"position",
							multiplierObjects[i-1].transform.position,
							"time",
							MULTIPLIER_QUEUE_TWEEN_TIME,
							"islocal",
							false,
							"easetype",
							iTween.EaseType.linear
							)
						)
					);
			}
			else
			{
				iTween.MoveTo(
					multiplierObjects[i],
					iTween.Hash(
						"position",
						multiplierObjects[i-1].transform.position,
						"time",
						MULTIPLIER_QUEUE_TWEEN_TIME,
						"islocal",
						false,
						"easetype",
						iTween.EaseType.linear
						)
					);
			}
		}

		treeAppleLastPos = treeApple.transform.position;

		// Swap positions of first and last object
		topObject.transform.position = bottomObjectStartPos;

		multiplierObjects.Remove(topObject);
		multiplierObjects.Add(topObject);

		multiplierLabelsWrapper.Remove(topLabel);
		multiplierLabelsWrapper.Add(topLabel);

		// Calculate the new multiplier value for the label
		topLabel.text = Localize.text("{0}X", curMultiplier + multiplierObjects.Count);
		
		topObject.SetActive(true);

		yield return null;
	}

	private void buildTreeApplePath(GameObject pickemButton)
	{

		Transform startPt = treeAppleWaypointList[0];
		Transform apexPt = treeAppleWaypointList[1];
		Transform endPt = treeAppleWaypointList[2];

		startPt.position = treeApple.transform.position;
		float xOffset = Math.Abs(pickemButton.transform.position.x - treeApple.transform.position.x) * 0.5f;
		apexPt.position = new Vector3(treeApple.transform.position.x - xOffset, treeApple.transform.position.y + 0.25f, treeApple.transform.position.z);
		endPt.position = pickemButton.transform.position;
	}

	private void updateTweenMultiplierObject()
	{
		Vector3 treeAppleDelta = treeApple.transform.position - treeAppleLastPos;
		tweenMultiplierObject.transform.position = tweenMultiplierObject.transform.position + treeAppleDelta;
		treeAppleLastPos = treeApple.transform.position;
	}

	private IEnumerator updateTreeApple()
	{
		float applePathPercent = 0.0f;

		Transform[] waypoints = treeAppleWaypointList.ToArray();

		while(!creepyTree.GetComponent<Osa03TreeAnimationCallbacks>().isThrowReady)
		{
			updateTweenMultiplierObject();
			yield return null;
		}

		while(applePathPercent <= 1.0f)
		{
			applePathPercent += APPLE_THROW_SPEED * Time.deltaTime;
			iTween.PutOnPath(treeApple, waypoints, applePathPercent);
			updateTweenMultiplierObject();
			yield return null;
		}

		treeApple.SetActive(false);
		//iTween.PutOnPath(treeApple, waypoints, 1.0f);
		resetTweenMultiplierObject();
	}

	private IEnumerator animateTreeThrow(GameObject pickemButton)
	{
		Animator treeAnimator = creepyTree.GetComponent<Animator>();
		yield return StartCoroutine(animateMultiplierTween());
		Audio.play(Audio.soundMap(DEFAULT_MULTIPLIER_TRAVEL2_SOUND_KEY));
		StartCoroutine(CommonAnimation.playAnimAndWait(treeAnimator, TREE_MULTIPLIER_ANIM_THROW_NAME));
		buildTreeApplePath(pickemButton);
		yield return StartCoroutine(updateTreeApple());
	}

	private IEnumerator animateTreeReload()
	{
		Animator treeAnimator = creepyTree.GetComponent<Animator>();
		Audio.play(Audio.soundMap(TREE_RELOAD_SOUND_KEY));
		yield return StartCoroutine(CommonAnimation.playAnimAndWait(treeAnimator, TREE_MULTIPLIER_ANIM_START_NAME));
	}

	private IEnumerator revealCredits(GameObject pickemButton, PickemPick pick, bool gray = false)
	{
		if(pick.isGameOver)
		{
			if(!gray)
			{
				Audio.play(Audio.soundMap(DEFAULT_REVEAL_BAD_SOUND_KEY));
				Audio.play(Audio.soundMap(DEFAULT_REVEAL_BAD2_SOUND_KEY), 1f, 0f, WITCH_LAUGH_DELAY);

				previousPayout = BonusGamePresenter.instance.currentPayout;
				BonusGamePresenter.instance.currentPayout += pick.credits;
				
				yield return StartCoroutine(
					SlotUtils.rollup(previousPayout,
						BonusGamePresenter.instance.currentPayout,
						winLabelWrapper
						)
					);

				yield return StartCoroutine(
					CommonAnimation.playAnimAndWait(
						pickemButton.GetComponent<Animator>(),
						PICKEM_REVEALS_BONUS_ANIM_NAME)
					);
			}
			else
			{
				if(!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap(DEFAULT_NOT_CHOSEN_SOUND_KEY));
				}
				pickemButton.GetComponent<Animator>().Play(PICKEM_REVEALS_BONUS_GRAY_ANIM_NAME);
				yield return StartCoroutine(revealWait.wait(revealWaitTime));
			}
		}
		else
		{
			if(!gray)
			{
				Audio.play(Audio.soundMap(DEFAULT_REVEAL_WIN_SOUND_KEY));

				UILabel revealNumberLabel = pickemButton.GetComponent<PickGameButton>().revealNumberLabel;
				long totalCredits = pick.credits * curMultiplier;

				revealNumberLabel.text = CreditsEconomy.convertCredits(pick.credits);
				previousPayout = BonusGamePresenter.instance.currentPayout;
				BonusGamePresenter.instance.currentPayout += totalCredits;

				yield return StartCoroutine(
						CommonAnimation.playAnimAndWait(
							pickemButton.GetComponent<Animator>(),
							REVEAL_CREDIT_ANIM_NAME
						)
					);

				yield return StartCoroutine(animateTreeThrow(pickemButton));
				yield return StartCoroutine(animateCreditReveal(pickemButton, totalCredits, revealNumberLabel));
				
				celebrationMeter.GetComponent<Animator>().Play(CELEBRATION_METER_ON_ANIM_NAME);
				
				yield return StartCoroutine(
					SlotUtils.rollup(
						previousPayout,
						BonusGamePresenter.instance.currentPayout,
						winLabelWrapper
						)
					);

				yield return StartCoroutine(animateTreeReload());

				curMultiplier++;
			}
			else
			{
				pickemButton.GetComponent<PickGameButton>().revealGrayNumberOutlineLabel.text = CreditsEconomy.convertCredits(pick.credits);
				if(!revealWait.isSkipping)
				{
					Audio.play(Audio.soundMap(DEFAULT_NOT_CHOSEN_SOUND_KEY));
				}
				pickemButton.GetComponent<Animator>().Play(REVEAL_CREDIT_GRAY_ANIM_NAME);
				yield return StartCoroutine(revealWait.wait(revealWaitTime));
			}
		}
	}

	protected override IEnumerator pickemButtonPressedCoroutine(GameObject pickemButton)
	{
		inputEnabled = false;

		PickemPick pick = outcome.getNextEntry();

		if(pick != null)
		{
			// Remove it from the pickme button list
			pickmeButtonList[currentStage].Remove(pickemButton);

			if(pick.isGameOver)
			{
				yield return StartCoroutine(revealCredits(pickemButton, pick));


				foreach(var go in pickmeButtonList[currentStage])
				{
					PickemPick reveal = outcome.getNextReveal();
					
					if(reveal == null)
					{
						break;
					}

					yield return StartCoroutine(revealCredits(go, reveal, true));
				}

				yield return new TIWaitForSeconds(1.0f);

				BonusGamePresenter.instance.gameEnded();
			}
			else
			{
				yield return StartCoroutine(revealCredits(pickemButton, pick));
				inputEnabled = true;
			}
		}
	}

	protected override IEnumerator pickMeAnimCallback()
	{

		banner.GetComponent<Animator>().Play(BANNER_PICKME_ANIM_STATE_NAME); 
		return base.pickMeAnimCallback();
	}
}

