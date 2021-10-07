using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * In board mini slot object, the multiplier is equal to number of attached spaces lit
 */
public class BoardGameSpaceMiniSlot : TICoroutineMonoBehaviour
{
	[Tooltip("All animation states with multipliers")]
	[SerializeField] private BoardGameSpaceMiniSlotMultiplierAnimationInfo[] multiplierAnimations;
	
	public int currentMultiplier { get; private set; }
	
	private Dictionary<int, BoardGameSpace> attachedSpaces = new Dictionary<int, BoardGameSpace>();
	
	[Tooltip("Useful for nested mini-games to show the appropriate color")]
	public ColorType color;
	
	public enum ColorType
	{
		MAGENTA,
		GREEN,
		BLUE,
		ORANGE,
		CYAN,
		PURPLE,
		RED,
		YELLOW
	}

	public void addToAttachedSpaces(int index, BoardGameSpace space)
	{
		if (attachedSpaces.ContainsKey(index))
		{
			Debug.LogError("Space already registered");
			return;
		}
		attachedSpaces.Add(index, space);
	}

	public IEnumerator playIdleAnimations()
	{
		int newMultiplier = 0;
		foreach (KeyValuePair<int, BoardGameSpace> kvp in attachedSpaces)
		{
			if (kvp.Value.isLanded)
			{
				newMultiplier++;
			}
		}

		if (currentMultiplier == newMultiplier)
		{
			yield break;
		}

		// turn off old multiplier
		yield return StartCoroutine(playEnableMultiplierAnimation(false));

		currentMultiplier = newMultiplier;
		
		// turn on new multiplier
		yield return StartCoroutine(playEnableMultiplierAnimation(true));
	}
	
	private IEnumerator playEnableMultiplierAnimation(bool enable)
	{
		for (int i = 0; i < multiplierAnimations.Length; i++)
		{
			if (currentMultiplier == multiplierAnimations[i].multiplier)
			{
				yield return StartCoroutine(AnimationListController.playListOfAnimationInformation(multiplierAnimations[i].getAnimation(enable)));
			}
		}
	}	
	
	/// <summary>
	/// Represents animation for individual multiplier
	/// </summary>
	[System.Serializable]
	class BoardGameSpaceMiniSlotMultiplierAnimationInfo
	{
		[Tooltip("Multiplier value")]
		public int multiplier;
		
		[Tooltip("On animation for corresponding multiplier value")]
		[SerializeField] AnimationListController.AnimationInformationList turnOnAnimations;
		[Tooltip("Off animation for corresponding multiplier value")]
		[SerializeField] AnimationListController.AnimationInformationList turnOffAnimation;

		public AnimationListController.AnimationInformationList getAnimation(bool turnOn)
		{
			if (turnOn)
			{
				return turnOnAnimations;
			}
			return turnOffAnimation;
		}
	}

	
	
}