using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Wow04 has a chance to get progressive bonuses. We need to update those in here.
*/

public class Wow04FreeSpins : FreeSpinGame
{
	public GameObject[] progressiveTexts; // < The Progressive Text values
	public GameObject[] progressiveParents;
	public GameObject confettiParent;
	private Vector3 oldProgressiveLocation;
	private Vector3 amountToMoveBy;
	public Object fireworkVFX;
	public Object confettiVFX;
	private GameObject _confettiVFX;
	private GameObject _leftFireworkVFX;
	private GameObject _rightFireworkVFX;
	private float[] _leftFireworkParticleStartSizes;
	private float[] _rightFireworkParticleStartSizes;
	private float[] _leftFireworkParticleStartSpeeds;
	private float[] _rightFireworkParticleStartSpeeds;
	private ParticleSystem[] _leftParticleSystems;
	private ParticleSystem[] _rightParticleSystems;
	private Vector3 originalProgressiveSize; //Used to get the change in scale of particle systems
	private bool progressiveMoving = false;
	private bool quickRemoval = false;
	private int currentIndex = 0;

	protected override void startGame()
	{
		originalProgressiveSize = progressiveParents[0].transform.localScale;//Used to scale the particles when they move to the center of the screen.
		updateProgressiveAmounts();
		//When we get to the freespin, sometimes we have a portal value that neededs to be added to the currentPayout
		BonusGamePresenter.instance.currentPayout = BonusGamePresenter.portalPayout * SlotBaseGame.instance.relativeMultiplier;
		runningPayoutRollupValue = BonusGamePresenter.instance.currentPayout;
		BonusSpinPanel.instance.winningsAmountLabel.text = CreditsEconomy.convertCredits(BonusGamePresenter.instance.currentPayout);
		base.startGame();
	}
	
	//Makes sure that the confetti animaions are inited, if they are not then it will init them for you.
	//Use this before you want to do anything with the confetti animaitons so you don't get null pointers.
	private bool initConfettiAnimations()
	{
		if (confettiVFX == null)
		{
			Debug.LogWarning("Confetti Animation not set");
			return false;
		}
		
		if (_confettiVFX == null)
		{
			_confettiVFX = CommonGameObject.instantiate( confettiVFX ) as GameObject;
			if (_confettiVFX != null)
			{
				CommonGameObject.setLayerRecursively( _confettiVFX, Layers.ID_NGUI );
				_confettiVFX.transform.localScale =  new Vector3(.01f,.01f,.01f); //Set the initial scale.
				_confettiVFX.transform.parent = confettiParent.transform;
				_confettiVFX.transform.localPosition = new Vector3(0,0,0);
			}
			else
			{
				Debug.LogWarning("Could not initilize animation");
				return false;
			}
		}
		return true;
	}

	//Starts the ConfettiAnimation
	private void playConfettiAnimation()
	{
		if (initConfettiAnimations())
		{
			VisualEffectComponent vfxComp = _confettiVFX.GetComponent<VisualEffectComponent>();
			if (vfxComp == null)
			{
				vfxComp = _confettiVFX.AddComponent<VisualEffectComponent>();
				vfxComp.playOnAwake = true;
				vfxComp.durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
			}
			vfxComp.Reset();
			vfxComp.Play(); //Once it's done it will just sit there idly.
		}
	}

	//Makes sure that the firework animaions are inited, if they are not then it will init them for you.
	//Use this before you want to do anything with the firework animaitons so you don't get null pointers.
	private bool initFireworkAnimations()
	{
		if (fireworkVFX == null)
		{
			Debug.LogWarning("Firework Animation not set");
			return false;
		}
		
		if (_leftFireworkVFX == null && _rightFireworkVFX == null)
		{
			_leftFireworkVFX = CommonGameObject.instantiate( fireworkVFX ) as GameObject;
			_rightFireworkVFX = CommonGameObject.instantiate( fireworkVFX ) as GameObject;
			if( _leftFireworkVFX != null && _rightFireworkVFX != null )
			{
				//Left side
				CommonGameObject.setLayerRecursively( _leftFireworkVFX, Layers.ID_NGUI );
				_leftFireworkVFX.transform.localScale = new Vector3(.1f,.1f,1); //Set the initial scale.
				_leftParticleSystems = _leftFireworkVFX.GetComponentsInChildren<ParticleSystem>();
				_leftFireworkParticleStartSizes = new float[_leftParticleSystems.Length];
				_leftFireworkParticleStartSpeeds = new float[_leftParticleSystems.Length];
				for (int i = 0; i < _leftParticleSystems.Length; i++)
				{
					ParticleSystem.MainModule particleSystemMainModule = _leftParticleSystems[i].main;
					particleSystemMainModule.startSize = .05f;
					_leftFireworkParticleStartSizes[i] = _leftParticleSystems[i].main.startSizeMultiplier;
					_leftFireworkParticleStartSpeeds[i] = _leftParticleSystems[i].main.startSpeedMultiplier;
				}
				//Right side.
				CommonGameObject.setLayerRecursively( _rightFireworkVFX, Layers.ID_NGUI );
				_rightParticleSystems = _rightFireworkVFX.GetComponentsInChildren<ParticleSystem>();
				_rightFireworkVFX.transform.localScale =  new Vector3(.1f,.1f,1/768); //Set the initial scale.
				_rightFireworkParticleStartSizes = new float[_rightParticleSystems.Length];
				_rightFireworkParticleStartSpeeds = new float[_rightParticleSystems.Length];
				for (int i = 0; i < _rightParticleSystems.Length; i++)
				{
					ParticleSystem.MainModule particleSystemMainModule = _rightParticleSystems[i].main;
					particleSystemMainModule.startSize = .05f;
					_rightFireworkParticleStartSizes[i] = _rightParticleSystems[i].main.startSizeMultiplier;
					_rightFireworkParticleStartSpeeds[i] = _rightParticleSystems[i].main.startSpeedMultiplier;
				}
			}
			else
			{
				Debug.LogWarning("Could not initilize animation");
				return false;
			}
		}
		return true;
	}
	
	protected override void startNextFreespin()
	{
		//Update the progressive amounts because they may have changed.
		if (engine.progressivesHit > engine.progressiveThreshold)
		{
			updateProgressiveAmounts();
			if (progressiveMoving)
			{
				progressiveMoving = false;
				quickRemoval = true;
				currentIndex = engine.progressivesHit - engine.progressiveThreshold-1;
				return;
			}
			else
			{
				_outcomeDisplayController.StartCoroutine(resetProgressivePosition(engine.progressivesHit - engine.progressiveThreshold-1));
			}
			//Now we want to start spinning again...
		}
		
		base.startNextFreespin();
	}

	private IEnumerator playReelsStopped()
	{
		if (engine.progressivesHit > engine.progressiveThreshold)
		{
			//Here we want to move our animaiton to the center.
			playConfettiAnimation();
			yield return StartCoroutine(moveProgressiveToCenter(engine.progressivesHit - engine.progressiveThreshold-1));
			//Now we want to allow roll up...
			//And to play some confetti!
			
		}
		
		base.reelsStoppedCallback();
	}

	/// reelsStoppedCallback - called when all reels have come to a stop.
	override protected void reelsStoppedCallback()
	{
		StartCoroutine(playReelsStopped());
	}

	//Updates every progressive amount.
	private void updateProgressiveAmounts()
	{
		JSON[] progressivePoolsJSON = SlotBaseGame.instance.slotGameData.progressivePools; // These pools are from highest to lowest.
		//Start id of the paytable as defined in scat (25,26,27.. and so on)
		int scatterWinId = 25;
		for (int k = 0; k < progressiveTexts.Length; k++)
		{
			UILabel label = progressiveTexts[k].GetComponent<UILabel>();
			if (progressivePoolsJSON != null && progressivePoolsJSON.Length > 0)
			{
				label.text = CommonText.formatNumber(SlotsPlayer.instance.progressivePools.getPoolCredits(progressivePoolsJSON[k].getString("key_name", ""), multiplier, false));
			}
			else
			{
				PayTable paytable = PayTable.find("wow_fs_paytable");
				label.text = CommonText.formatNumber(paytable.scatterWins[scatterWinId + k].credits * SlotBaseGame.instance.multiplier * GameState.baseWagerMultiplier);
			}
		}


	}

	//Attaches an animation to the progressive that matches the index position.
	private void attachAnimations(int index)
	{
		if (initFireworkAnimations())
		{
			//Left firework
			_leftFireworkVFX.SetActive(true);
			_leftFireworkVFX.transform.parent = progressiveParents[index].transform;
			_leftFireworkVFX.transform.localRotation = Quaternion.identity;
			_leftFireworkVFX.transform.localPosition = new Vector3(-134,-16,0);

			if (_leftFireworkVFX.GetComponent<VisualEffectComponent>() == null)
			{
				VisualEffectComponent vfxComp = null;
				vfxComp = _leftFireworkVFX.AddComponent<VisualEffectComponent>();
				vfxComp.playOnAwake = true;
				vfxComp.durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
			}
			//Right firework
			_rightFireworkVFX.SetActive(true);
			_rightFireworkVFX.transform.parent = progressiveParents[index].transform;
			_rightFireworkVFX.transform.localRotation = Quaternion.identity;
			_rightFireworkVFX.transform.localPosition = new Vector3(134,-16,0);

			if (_rightFireworkVFX.GetComponent<VisualEffectComponent>() == null)
			{
				VisualEffectComponent vfxComp = null;
				vfxComp = _rightFireworkVFX.AddComponent<VisualEffectComponent>();
				vfxComp.playOnAwake = true;
				vfxComp.durationType = VisualEffectComponent.EffectDuration.ScriptControlled;
			}
		}
	}

	//Makes the firework animation no longer active.
	private void hideFireworkAnimations()
	{
		if (_leftFireworkVFX != null && _rightFireworkVFX != null)
		{
			_leftFireworkVFX.SetActive(false);
			_rightFireworkVFX.SetActive(false);
		}
	}

	//Moves the progressive to the center, attaches animations and then yields control over so that you can count up coins.
	private IEnumerator moveProgressiveToCenter(int index)
	{
		if (index < progressiveParents.Length && index >= 0)
		{
			amountToMoveBy = progressiveParents[index].transform.localPosition;// - new Vector3(0,0,0); //Distance
			float time = 2;
			//We need to make the firework animations to put on either side of the progressive
			attachAnimations(index);

			progressiveMoving = true;
			iTween.MoveTo(progressiveParents[index],iTween.Hash("position", progressiveParents[index].transform.localPosition - amountToMoveBy,
				"islocal", true, "time", time, "oncomplete", "finishedMoving", "oncompletetarget", gameObject));
			iTween.ScaleBy(progressiveParents[index],iTween.Hash("amount", new Vector3(2,2,2), "islocal", true,"time", time, "onupdatetarget", gameObject, "onupdate", "updateParticleScale", "onupdateparams" , index));
			yield return new WaitForSeconds(time);
		}
		else
		{
			Debug.LogError("moveProgressiveToCenter on an array index that is out of bounds index = " + index + " should be less than " + progressiveParents.Length);
			yield return new WaitForSeconds(0);
		}
	}
	
	private void finishedMoving()
	{
		progressiveMoving = false;
		
		if (quickRemoval)
		{
			_outcomeDisplayController.StartCoroutine(resetProgressivePosition(currentIndex));
			quickRemoval = false;
			base.startNextFreespin();
		}
	}

	//Moves the progressive back to where it used to be and turns off the animations, yields when it's back.
	private IEnumerator resetProgressivePosition(int index)
	{
		if (index < progressiveParents.Length && index >= 0)
		{
			float time = .75f;
			iTween.MoveTo(progressiveParents[index],iTween.Hash("position", progressiveParents[index].transform.localPosition + amountToMoveBy,
				"islocal", true, "time", time));
			iTween.ScaleBy(progressiveParents[index],iTween.Hash("amount", new Vector3(.5f,.5f,.5f), "islocal", true, "time", time,
			 "onupdatetarget", gameObject, "onupdate", "updateParticleScale", "onupdateparams" , index,
			 "oncompletetarget", gameObject, "oncomplete", "hideFireworkAnimations"));
			yield return new WaitForSeconds(time);
		}
		else
		{
			Debug.LogError("resetProgressivePosition on an array index that is out of bounds index = " + index + " should be less than " + progressiveParents.Length);
			yield return new WaitForSeconds(0); //We don't need to wait for anything b/c something went wrong.
		}
	}

	//Particles don't scale like everyhing else so when you scale the progressive you need to make sure this is called by the scaleBy Itween.
	private void updateParticleScale(int index)
	{
		float scale = progressiveParents[index].transform.localScale.x / originalProgressiveSize.x; //This works because we are scaling everything linearly
		updateParticleScale(scale);
	}

	//Helper method for updateParticleScale(index).
	private void updateParticleScale(float scale)
	{
		ParticleSystem.MainModule particleSystemMainModule;

		for (int i = 0; i < _leftParticleSystems.Length; i++)
		{
			particleSystemMainModule = _leftParticleSystems[i].main;
			particleSystemMainModule.startSize = _leftFireworkParticleStartSizes[i] * scale;
			particleSystemMainModule.startSpeed = _leftFireworkParticleStartSpeeds[i] * scale;
		}

		for (int i = 0; i < _rightParticleSystems.Length; i++)
		{
			particleSystemMainModule = _rightParticleSystems[i].main;
			particleSystemMainModule.startSize = _rightFireworkParticleStartSizes[i] * scale;
			particleSystemMainModule.startSpeed = _rightFireworkParticleStartSpeeds[i] * scale;
		}
	}
}
