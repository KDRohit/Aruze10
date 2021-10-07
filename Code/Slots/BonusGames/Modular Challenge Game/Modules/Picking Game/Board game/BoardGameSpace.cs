using System.Collections;
using UnityEngine;

/*
 * Represents individual space in board game.
 */
public class BoardGameSpace : TICoroutineMonoBehaviour
{
	[Tooltip("All the animation states for a space")] [SerializeField]
	private BoardGameSpaceAnimationsInfo[] spaceAnimations;

	[Tooltip(
		"Each space is attached to a mini-slot. Based on how many spaces are active, the minislot's multiplier will increase")]
	[SerializeField] private BoardGameSpaceMiniSlot attachedMiniSlot;

	[Tooltip("Target location for player token to land on")]
	public Transform tokenMoveTarget;
	
	private bool _isLanded;
	public bool isLanded
	{
		get => _isLanded;
		private set => _isLanded = value && (attachedMiniSlot != null);
	}

	public bool willBeNewlyLandedOnTokenArrival => !isLanded && attachedMiniSlot != null;

	private bool isTokenOnThisSpace = false;

	public void init(bool isLanded, bool isTokenOnThisSpace, int indexOnBoard)
	{
		this.isTokenOnThisSpace = isTokenOnThisSpace;
		this.isLanded = isLanded;
		if (attachedMiniSlot != null)
		{
			attachedMiniSlot.addToAttachedSpaces(indexOnBoard, this);
		}
	}

	public IEnumerator playIdleAnimations()
	{
		if (isLanded)
		{
			if (isTokenOnThisSpace)
			{
				yield return StartCoroutine(playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState.CurrentSpace));
			}
			else
			{
				yield return StartCoroutine(playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState.On));
			}
		}
		else
		{
			yield return StartCoroutine(playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState.Off));
		}
	}

	public IEnumerator playPathTrackingAnimation()
	{
		isTokenOnThisSpace = false;
		if (isLanded)
		{
			yield return StartCoroutine(playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState.PathTrackingOverOn));
		}
		else
		{
			yield return StartCoroutine(playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState.PathTrackingOverOff));
		}
	}

	public IEnumerator playStepOverAnimation(bool isTokenStoppingHere)
	{

		if (isLanded)
		{
			yield return StartCoroutine(playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState
				.StepOffOverOn));
		}
		else
		{
			yield return StartCoroutine(playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState
				.StepOffOverOff));
		}
		if (isTokenStoppingHere)
		{
			if (!isLanded)
			{
				yield return StartCoroutine(markAsLanded(true));
			}

			isTokenOnThisSpace = true;
			// this is needed to set current space animation
			yield return StartCoroutine(playIdleAnimations());
		}
	}

	public IEnumerator playBoardCompleteAnimation()
	{
		yield return StartCoroutine(playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState.Celebration));
	}

	public IEnumerator playOnTokenMovedAwayAnimations()
	{
		isTokenOnThisSpace = false;
		yield return StartCoroutine(playIdleAnimations());
	}

	/// <summary>
	/// Use this to manually set the space's landed state
	/// </summary>
	/// <param name="landed"></param>
	public IEnumerator markAsLanded(bool landed)
	{
		isLanded = landed;
		isTokenOnThisSpace = false;
		if (landed)
		{
			yield return StartCoroutine(
				playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState.SpaceGained));
		}
		else
		{
			yield return StartCoroutine(playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState.SpaceLost));
		}

		if (attachedMiniSlot != null)
		{
			yield return StartCoroutine(attachedMiniSlot.playIdleAnimations());
		}
	}

	public BoardGameSpaceMiniSlot miniSlot
	{
		get
		{
			return attachedMiniSlot;
		}
	}

	private IEnumerator playAnimation(BoardGameSpaceAnimationsInfo.BoardGameSpaceAnimationState animationState)
	{
		for (int i = 0; i < spaceAnimations.Length; i++)
		{
			if (spaceAnimations[i].animationState == animationState)
			{
				yield return StartCoroutine(
					AnimationListController.playListOfAnimationInformation(spaceAnimations[i].animations));
			}
		}
	}

	[System.Serializable]
	public class BoardGameSpaceAnimationsInfo
	{
		public enum BoardGameSpaceAnimationState
		{
			On,
			Off,
			CurrentSpace,
			SpaceGained,
			SpaceLost,
			PathTrackingOverOn,
			PathTrackingOverOff,
			StepOffOverOn,
			StepOffOverOff,
			Celebration
		}
		
		[Tooltip("Specify animations to play for each of this state" +
		         "\nOn - space is already landed" +
		         "\nOff - space is never landed on" +
		         "\nCurrentSpace- Token is currently on this space" +
		         "\nSpaceGained - Token just landed on new space/ mystery card granted this space" +
		         "\nSpaceLost - Lose a space animation" +
		         "\nPathTrackingOverOn/Off - Path animation over the space (before token starts to move)" +
		         "\nStepOffOverOn/Off - When the token leaves the space" +
		         "\nCelebration - Animation to play when board is complete")]
		public BoardGameSpaceAnimationState animationState;
		
		public AnimationListController.AnimationInformationList animations;
	}
}