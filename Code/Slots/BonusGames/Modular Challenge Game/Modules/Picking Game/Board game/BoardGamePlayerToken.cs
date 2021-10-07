using System.Collections;
using UnityEngine;

/*
 * Player token, and its movement around the board
 */
public class BoardGamePlayerToken : TICoroutineMonoBehaviour
{
	[SerializeField] private iTween.EaseType easeType = iTween.EaseType.linear;
	
	[Tooltip("All token animations")]
	[SerializeField] private BoardGamePlayerTokenAnimationsInfo[] animations;

	private BoardGameSpace currentSpace;
	
	[SerializeField] private string TOKEN_MOVEMENT_AMBIENT_SOUND = "BoardPieceMoveBGCommon";

	public IEnumerator setParentSpaceAndAnimate(BoardGameSpace space, bool animateTokenIntro = false)
	{
		currentSpace = space;
		transform.parent = space.tokenMoveTarget;
		transform.localPosition = Vector3.zero;
		if (animateTokenIntro)
		{
			yield return StartCoroutine(playAnimation(space.isLanded
				? BoardGamePlayerTokenState.DropHigh
				: BoardGamePlayerTokenState.DropLow));
		}
		else
		{
			yield return StartCoroutine(playAnimation(space.isLanded
				? BoardGamePlayerTokenState.DefaultHigh
				: BoardGamePlayerTokenState.DefaultLow));
		}
	}

	public IEnumerator moveToSpace(BoardGameSpace newSpace, float timeToMove, bool useSpeedUpAnimations)
	{
		yield return StartCoroutine(playAnimation(getJumpAnimationState(newSpace, useSpeedUpAnimations)));
		yield return new TITweenYieldInstruction(
			iTween.MoveTo(gameObject, iTween.Hash("position", newSpace.tokenMoveTarget, "islocal", false, "time", timeToMove, "easetype", easeType)));
		transform.parent = newSpace.tokenMoveTarget;
	}
	
	// Call this before starting the move
	// For future improvements, we can change this to coroutine and play custom prepare to move animations
	public void prepareToMove()
	{
		// Start playing ambient sound, it needs to play until the movement has stopped
		Audio.play(TOKEN_MOVEMENT_AMBIENT_SOUND);
	}

	// Call this immediately when movement is finished
	public IEnumerator onMoveComplete()
	{
		// Stop the ambient movement sound
		Audio.stopSound(Audio.findPlayingAudio(TOKEN_MOVEMENT_AMBIENT_SOUND));
		
		yield return StartCoroutine(playAnimation(currentSpace.isLanded
			? BoardGamePlayerTokenState.DefaultHigh
			: BoardGamePlayerTokenState.DefaultLow));
	}

	// Call this when all the modules are finished and next pick is available to player
	public IEnumerator readyForNextRoll()
	{
		yield return StartCoroutine(playAnimation(currentSpace.isLanded
			? BoardGamePlayerTokenState.ReadyForNextRoundHigh
			: BoardGamePlayerTokenState.ReadyForNextRoundLow));
	}

	public IEnumerator playCelebrationAnimation()
	{
		yield return StartCoroutine(playAnimation(currentSpace.isLanded
			? BoardGamePlayerTokenState.CelebrationHigh
			: BoardGamePlayerTokenState.CelebrationLow));
	}

	private BoardGamePlayerTokenState getJumpAnimationState(BoardGameSpace newSpace, bool speedUp)
	{
		if (currentSpace.isLanded && newSpace.isLanded)
		{
			return speedUp ? BoardGamePlayerTokenState.JumpHighToHighFast : BoardGamePlayerTokenState.JumpHighToHigh;
		}
		
		if (currentSpace.isLanded && !newSpace.isLanded)
		{
			return speedUp ? BoardGamePlayerTokenState.JumpHighToLowFast : BoardGamePlayerTokenState.JumpHighToLow;
		}
		
		if (!currentSpace.isLanded && newSpace.isLanded)
		{
			return speedUp ? BoardGamePlayerTokenState.JumpLowToHighFast : BoardGamePlayerTokenState.JumpLowToHigh;
		}
		
		return speedUp ? BoardGamePlayerTokenState.JumpLowToLowFast : BoardGamePlayerTokenState.JumpLowToLow;
	}
	
	private IEnumerator playAnimation(BoardGamePlayerTokenState state)
	{
		for (int i = 0; i < animations.Length; i++)
		{
			if (animations[i].state == state)
			{
				yield return StartCoroutine(
					AnimationListController.playListOfAnimationInformation(animations[i].animations));
			}
		}
	}

	[System.Serializable]
	public class BoardGamePlayerTokenAnimationsInfo {
		public BoardGamePlayerTokenState state;
		
		[Tooltip("Set animations for specified state" +
		         "\n DefaultHigh/Low - default animation" +
		         "\n JumpHighToHigh - Jump from lit space(landed) to another lit space" +
		         "\n JumpHighToLow - Jump from lit space to unlit space" +
		         "\n JumpLowToLow - Jump from unlit to unlit space" +
		         "\n JumpLowToHigh - Jump from unlit to lit space" +
		         "\n CelebrationHigh/low - Celebration anim when board completes" +
		         "\n DropHigh/Low - Initial drop animation on board launch")]
		public AnimationListController.AnimationInformationList animations;
	}
	
	public enum BoardGamePlayerTokenState
	{
		DefaultHigh = 0,
		DefaultLow = 1,
		JumpHighToHigh = 2,
		JumpHighToHighFast = 3,
		JumpLowToLow = 4,
		JumpLowToLowFast = 5,
		JumpLowToHigh = 6,
		JumpLowToHighFast = 7,
		JumpHighToLow = 8,
		JumpHighToLowFast = 9,
		CelebrationHigh = 10,
		CelebrationLow = 11,
		DropHigh = 12,
		DropLow = 13,
		ReadyForNextRoundHigh = 14,
		ReadyForNextRoundLow = 15
	}
}