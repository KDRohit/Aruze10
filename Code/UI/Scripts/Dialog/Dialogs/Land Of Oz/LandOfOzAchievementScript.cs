using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Handles a single achievement on the LOZ achievements dialog or in-game UI.
*/

public class LandOfOzAchievementScript : MonoBehaviour, IResetGame
{
	private const float FADE_OUT_TIME = 0.5f;
	private const int PERCENT_LIMIT = 1000; // objective amount needed limitation before turning progerss to a percent
	
	public TextMeshPro descriptionLabel;
	public GameObject activeElements;
	public TextMeshPro activeLabel;
	public GameObject disabledElements;
	public TextMeshPro disabledLabel;
	public TextMeshPro disabledMinBetLabel;
	public GameObject achievedElements;
	public Animator checkmarkAnim;
	public ParticleSystem sparkleBlast;
	public MasterFader fader;

	private long previousValue = 0;
	private Objective objective;
	private bool hasCompleted = false;
	private LobbyGame currentGame;
	private List<Objective> cachedObjectives;
	
	private static int fadeCount = 0;	// The number of achievements currently fading.
	
	public void refresh(LobbyGame game, int index, Mission completedMission = null)
	{
		// object pooling for list scroller items can cause this to happen
		if (currentGame != null && currentGame != game)
		{
			reset();
		}
		
		currentGame = game;
		this.mission = completedMission;

		if (!hasCompleted && this.mission != null)
		{
			SafeSet.gameObjectActive(activeElements, true);
			SafeSet.gameObjectActive(disabledElements, false);
			SafeSet.gameObjectActive(achievedElements, false);

			objective = objectives[index];
			SafeSet.labelText(descriptionLabel, objective.description);

			if (objective.amountNeeded < PERCENT_LIMIT)
			{
				SafeSet.labelText(activeLabel, string.Format("{0}/{1}", objective.currentAmount, objective.amountNeeded));
			}
			else
			{
				SafeSet.labelText(activeLabel, Localize.text("{0}_percent", Mathf.Floor(objective.progressPercent * 100.0f)));
			}
		
			long currentValue = objective.currentAmount;
		
			// If the previous value is different than the new value, play the sparkles to indicate progress being made.
			if (previousValue != currentValue)
			{
				if (sparkleBlast != null)
				{
					sparkleBlast.Play();
				}
			
				previousValue = currentValue;

				if (objective.isComplete)
				{
					onComplete();
				}
			}
		}
		// objective finished
		else if (hasCompleted && completedMission != null)
		{
			LOZCampaign campaign = CampaignDirector.find(CampaignDirector.LOZ_CHALLENGES) as LOZCampaign;
			// has the mission changed?
			if (campaign.currentMission != null)
			{
				// check if this game is in the new mission, if it is then make sure we refresh
				hasCompleted = !campaign.currentMission.containsGame(game.keyName);
			}

			// new mission started for this game, user needs to refresh the current game objective list
			if (!hasCompleted)
			{
				mission = null;
				refresh(game, index);
			}
		}
	}

	protected void onComplete()
	{
		SafeSet.gameObjectActive(achievedElements, true);
		SafeSet.gameObjectActive(disabledElements, false);
		SafeSet.gameObjectActive(activeElements, false);

		if (!hasCompleted)
		{
			animateCheck();
		}

		hasCompleted = true;
	}

	public void OnDestroy()
	{
		objective = null;
	}
	
	public void animateCheck()
	{
		if (checkmarkAnim != null && achievedElements.activeSelf)
		{
			//SafeSet.gameObjectActive(activeElements, false);
			checkmarkAnim.Play("LOZ Achievement Unlock Anim");
			SafeSet.gameObjectActive(activeElements, false);

			if (sparkleBlast != null)
			{
				sparkleBlast.Play();
			}
		}
	}
	
	public IEnumerator fadeOut(float delay)
	{
		if (fader == null)
		{
			Debug.LogWarning("LandOfOzAchievementScript.fadeOut() called but there is no fader object");
			yield break;
		}
		
		fadeCount++;
		
		yield return new WaitForSeconds(delay);
		
		float elapsed = 0.0f;
		
		while (elapsed < FADE_OUT_TIME)
		{
			yield return null;
			elapsed += Time.deltaTime;
			fader.alpha = 1.0f - Mathf.Clamp01(elapsed / FADE_OUT_TIME);
		}
		
		gameObject.SetActive(false);
		
		fadeCount--;
	}
	
	public static bool isFading
	{
		get { return fadeCount > 0; }
	}

	public void reset()
	{
		previousValue = 0;
		hasCompleted = false;
		cachedObjectives = null;
	}
	
	public static void resetStaticClassData()
	{
		fadeCount = 0;
	}

	/*=========================================================================================
	GETTERS
	=========================================================================================*/
	protected List<Objective> objectives
	{
		get
		{
			if (cachedObjectives == null || cachedObjectives.Count < LobbyMission.OBJECTIVES_PER_GAME)
			{
				cachedObjectives = new List<Objective>();
				foreach (Objective objective in mission.objectives)
				{
					if (objective.game == currentGame.keyName)
					{
						cachedObjectives.Add(objective);
					}
				}
			}
			return cachedObjectives;
		}
	}

	private Mission _mission;
	protected Mission mission
	{
		get
		{
			if (_mission != null)
			{
				return _mission;
			}

			LOZCampaign campaign = CampaignDirector.find(CampaignDirector.LOZ_CHALLENGES) as LOZCampaign;
			
			if (currentGame != null && campaign != null)
			{
				if (campaign.currentMission != null && campaign.currentMission.containsGame(currentGame.keyName))
				{
					return campaign.currentMission;
				}

				return campaign.findWithGame(currentGame.keyName);
			}

			Debug.LogError("null mission found. This should not happen.");
			return null;
		}
		set
		{
			_mission = value;
		}
	}
}
