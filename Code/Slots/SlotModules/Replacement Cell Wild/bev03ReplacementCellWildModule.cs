using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class bev03ReplacementCellWildModule : ReplacementCellWildModule
{
	[SerializeField] private string rocketFireSoundKey = "basegame_vertical_wild_init";
	[SerializeField] private string rocketBgMusicSoundKey = "basegame_vertical_wild_bg";
	[SerializeField] private string wildRevealRocketSoundKey = "basegame_vertical_wild_reveal";
	[SerializeField] private string wild2RevealRocketSoundKey = "basegame_vertical_wild2x_reveal";
	[SerializeField] private GameObject rocketPrefab = null;
	[SerializeField] private GameObject shroudPrefab = null;
	[SerializeField] private int maxRockets = 6;
	[SerializeField] private float delayBeforeRocketTween = 1.0f;
	[SerializeField] private float rocketTweenTime = 1.0f;
	[SerializeField] private float rocketStartY = -10.0f;
	[SerializeField] private float rocketStartZ = -25.0f;

	private List<GameObject> rocketInstances = new List<GameObject>();

	private int activeRockets = 0;

	public override void Awake()
	{
		for (int i = 0; i < maxRockets; i++)
		{
			rocketInstances.Add(CommonGameObject.instantiate(rocketPrefab) as GameObject);
			rocketInstances[i].transform.position = new Vector3(0.0f, 0.0f, rocketStartZ);
			rocketInstances[i].SetActive(false);
		}
		shroudPrefab.SetActive(false);
		base.Awake ();
	}

	public override void PlayIntroSounds()
	{
		Audio.play(Audio.soundMap(rocketFireSoundKey));
		Audio.play(Audio.soundMap(rocketBgMusicSoundKey));
	}

	public override IEnumerator executePreReelsStopSpinning()
	{
		shroudPrefab.SetActive(true);
		shroudPrefab.GetComponent<Animator>().Play("shroud_in");
		yield return StartCoroutine(base.executePreReelsStopSpinning());
		shroudPrefab.GetComponent<Animator>().Play("shroud_out");
		shroudPrefab.SetActive(false);
	}


	protected override IEnumerator mutateOneSymbol (SlotSymbol targetSymbol, GameObject effectPrefab, string symbolName, int reelIndex, int symbolIndex)
	{
		if (activeRockets < rocketInstances.Count)
		{
			int curRocket = activeRockets;
			activeRockets++;
			rocketInstances[curRocket].SetActive(true);
			rocketInstances[curRocket].transform.position = new Vector3 (targetSymbol.transform.position.x, rocketStartY, rocketStartZ);
			Vector3 targetPosition = reelGame.engine.getReelRootsAt (reelIndex, symbolIndex).transform.position;
			targetPosition = new Vector3 (targetPosition.x, targetPosition.y + (symbolIndex * reelGame.symbolVerticalSpacingWorld), rocketStartZ);
			yield return new TIWaitForSeconds(delayBeforeRocketTween);
			yield return new TITweenYieldInstruction(iTween.MoveTo(rocketInstances[curRocket], iTween.Hash("position", targetPosition, "time", rocketTweenTime, "islocal", false, "easetype", iTween.EaseType.linear)));
			if(symbolName.Contains("WD"))
			{
				Audio.play(Audio.soundMap(wildRevealRocketSoundKey));
			}
			else if(symbolName.Contains("W2"))
			{
				Audio.play(Audio.soundMap(wild2RevealRocketSoundKey));
			}
			rocketInstances[curRocket].SetActive(false);
			activeRockets--;
		}

		yield return StartCoroutine(base.mutateOneSymbol (targetSymbol, effectPrefab, symbolName, reelIndex, symbolIndex));
	}
}
