using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;
using CustomLog;

/**
Attach to a collider that is an area that can be swiped for paging, so that the source of the swipe area can be detected,
so that we can only do the swipe action on the thing being swiped for if there is multiple swipe areas on screen at once.

You can also attach a box collider to the gameObject and SwipeableReel will use the values from that for
the size and center values. In this way you can customize the size of the swipeable area for reels that have
different sizes like gen76 reel 4. This is especially important for symbol culling which also happens in this
component.
*/

public class SwipeableReel : SwipeArea
{
	public SlotReel myReel;
	public Camera cameraOnReel = null;
	public float pixelsBeforeSpin = 100;
	public float amountToMoveBeforeSpin;
	public bool wasSwipeTarget = false; // toggled when you switch from being the current swipeArea.
	private Vector3 originalPos;
	public int _numberOfSymbolsPerReel = 3;
	private ReelGame reelGame = null;
	private HashSet<SlotReel> linkedReelListForReel = null;
	private bool isSwipeLayerRestoreNeeded = false; // track if we need to restore the layers if the tween back is canceled before the oncomplete function is called

	// Some games (like Beverly Hillbillies and Riches of the Arena) have synced reels (if you drag one of the reels in the
	// middle, it drags all the reels in the middle).

	// Usually you can swipe the reels to spin whenever you want, but if you swipe synced reels, then you can't swipe to
	// spin again until all the synced reels have stopped moving (this way, it only calls validateSpin once instead of
	// calling it for each synced reel).
	public static bool canSwipeToSpin = true;
	
	public void init(SlotReel attachedReel, ReelGame reelGame)
	{
		myReel = attachedReel;
		this.reelGame = reelGame;
		if (reelGame == null || attachedReel == null)
		{
			// We don't want to init this without a reel game or a reel.
			return;
		}
		//Debug.LogError("init SwipeableReel(" + attachedReel + ");");
		if (myReel.visibleSymbols != null)
		{
			_numberOfSymbolsPerReel = myReel.visibleSymbols.Length;
		}
		else
		{
			_numberOfSymbolsPerReel = 1;
		}

		updateSwipeAreaSize();

		amountToMoveBeforeSpin = 0.5f; // Half of a symbol.
		canSwipeToSpin = true;
	}

	/// Returns the area as a Rect that is scaled properly for the screen.
	/// If camera is null then this retruns a Rect(0,0,0,0).
	public override Rect getScreenRect()
	{
		Vector3 topLeftWorld = transform.TransformPoint(new Vector3(center.x - size.x / 2, center.y + size.y / 2, 0));
		Vector3 bottomRightWorld = transform.TransformPoint(new Vector3(center.x + size.x / 2, center.y - size.y / 2, 0));
		Vector2int topLeft = NGUIExt.screenPositionOfWorld(cameraOnReel, topLeftWorld);
		Vector2int bottomRight = NGUIExt.screenPositionOfWorld(cameraOnReel, bottomRightWorld);

		float x = topLeft.x;
		float y = bottomRight.y;
		float w = bottomRight.x - topLeft.x;
		float h = topLeft.y - bottomRight.y;
		return new Rect(x, y, w, h);
	}

	// Updates the size of the swipe area relative to the number of visible symbols on the reel set.
	private void updateSwipeAreaSize()
	{
		// check for a BoxCollider on this gameObject that we can use for custom size and position
		BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();

		if (boxCollider != null)
		{
			size.x = boxCollider.size.x;
			size.y = boxCollider.size.y;
			center.x = boxCollider.center.x;
			center.y = boxCollider.center.y;
			boxCollider.enabled = false;
		}
		else
		{
			if (reelGame.payBoxSize.x <= 0f || reelGame.payBoxSize.y <= 0f)
			{
				Debug.LogWarning("SwipeableReel has invalid area! Please define paybox size or add a box collider");
			}
			float symbolHorizontalSpacing = reelGame.payBoxSize.x * (reelGame.getSymbolVerticalSpacingAt(myReel.reelID - 1) / reelGame.payBoxSize.y);
			size.x = symbolHorizontalSpacing;
			size.y = reelGame.getSymbolVerticalSpacingAt(myReel.reelID - 1) * _numberOfSymbolsPerReel;
			// This works for 3 and 4 vertical symbol games. Might need to be revisited once other games get implemented.
			center.y = reelGame.getSymbolVerticalSpacingAt(myReel.reelID - 1) + (_numberOfSymbolsPerReel - 3) * .5f * reelGame.getSymbolVerticalSpacingAt(myReel.reelID - 1);
		}
	}

	protected virtual void Update()
	{
		if (reelGame == null)
		{
			// Bad news bears.
			return;
		}

		if (!gameObject.activeInHierarchy || gameObject.layer == Layers.ID_HIDDEN)
		{
			return;
		}
		if (cameraOnReel == null)
		{
			int layerMask = 1 << gameObject.layer;
			cameraOnReel = CommonGameObject.getCameraByBitMask(layerMask);
		}
		if (!canSwipeToSpin)
		{
			// Not allowed to swipe again right now. Usually because a swipe already happened.
			// This gets reset to true when spinning returns to normal state in SlotBaseGame.
			return;
		}

		// Updates the swipe area in the event that the reel was resized.
		if (_numberOfSymbolsPerReel != myReel.visibleSymbols.Length) 
		{
			_numberOfSymbolsPerReel = myReel.visibleSymbols.Length;
			updateSwipeAreaSize();
		}

		// Do our above/below reel symbol culling
		doSymbolCulling();

		bool canSpin = (SlotBaseGame.instance != null && SpinPanel.instance != null && SpinPanel.instance.isButtonsEnabled); // Base Game
		// You shouldn't be able to do anything if the spin button isn't active.
		// Free spins games don't allow you do do anything but stop the spin,
		// so this first part only applies to SlotBaseGame.instance.
		if (canSpin && canSwipeToSpin && myReel.isStopped && !DevGUI.isActive && !Log.isActive && Glb.isNothingHappening)
		{
			bool upSpin = myReel.reelOffset <= -.5f; //.5 is half a symbol distance
			bool downSpin = myReel.reelOffset >= .5f;
			// Check to see if we have spun.
			if (upSpin || downSpin)
			{
				// It can only ever be up or down, it can't be both.
				bool didSpin = SlotBaseGame.instance.validateSpin(false,true, upSpin? SlotReel.ESpinDirection.Up : SlotReel.ESpinDirection.Down); //Spin!
				
				//If you can't spin then we need to reset the position of the reel, if you can we should log it.
				if (didSpin)
				{
					// NOTE: The swipe should be cancelled and the symbols restored above when the engine calls cancelSwipeAndRestoreSymbolsToOriginalLayersForSwipeableReels()
					// from SlotBaseGame.instance.validateSpin().
				
					StatsManager.Instance.LogCount("game_actions",
													"spin",
													StatsManager.getGameTheme(),
													StatsManager.getGameName(),
													"spin",
													"swipe",
													(SlotBaseGame.instance != null? SlotBaseGame.instance.betAmount : -1)); // Something is wrong and we shouldn't be swiping to spin
				}
				else
				{
					HashSet<SlotReel> linkedReelListForReel = reelGame.engine.getLinkedReelListForReel(myReel);

					// MCC adding a count check becuase the linkedReelList always returns non-null now.
					if (linkedReelListForReel != null && linkedReelListForReel.Count > 0)
					{
						// If you swiped a synced reel, then tween all the synced reels back to their original positions, and you can't
						// swipe to spin again until all of them have finished moving.
						//
						// If you're here, it's because you're out of credits.  If we didn't disable swiping, then it would call
						// validateSpin for each synced reel, one at a time, which would open the Need Credits Dialog several times in a
						// row.
						//
						// Note that, if you have enough credits, then we don't have to disable swiping to spin because, when you have
						// enough credits, validateSpin handles multiple calls by ignoring the extra calls.
						foreach (SlotReel reel in linkedReelListForReel)
						{
							iTween.ValueTo(
								reel.getReelGameObject(), iTween.Hash(
								"from", reel.reelOffset,
								"to", 0f,
								"time", .25f,
								"onupdate", "moveReels"));
						}
					}
					else
					{
						iTween.ValueTo(this.gameObject, iTween.Hash(
							"from", myReel.reelOffset,
							"to", 0f,
							"time", .25f,
							"onupdate", "moveReels"));
					}
					
					restoreSymbolsToOriginalLayersAfterSwipe();
				}				
				TouchInput.swipeArea = null;
			}
			// If we didn't end up spining move the reels / reset the reel positions
			else
			{
				//This is the swipe object that we are using, so we should move the reels and mark that we are using it.
				if (TouchInput.swipeObject == this.gameObject)
				{
					// This is the first finger touch for this swipe
					if (!wasSwipeTarget)
					{
						// Stop the tween so that there isn't a delay between what the player is doing and what is being shown.
						iTween.Stop(this.gameObject, false);

						if (isSwipeLayerRestoreNeeded)
						{
							finishedScrollingBack();
						}

						// Force the symbols to be on the reel layer so they clip correctly to the bounds of the reels
						forceSymbolsToLayerOfParentReel();
					}
					wasSwipeTarget = true;
					float amountToMovePerPixel = (amountToMoveBeforeSpin / pixelsBeforeSpin);
					float normalizedDistance = -1 * TouchInput.dragDistanceY * amountToMovePerPixel; //Fliping the sign becuase we want down to be positive.
					moveReels(normalizedDistance);
				}
				// The finger is lifted up. Clean up anything we were doing in the swipe.
				if (wasSwipeTarget && TouchInput.swipeObject != this.gameObject)
				{
					// track if we need to restore the layers if this tween is canceled before the oncomplete function is called
					isSwipeLayerRestoreNeeded = true;
					wasSwipeTarget = false;
					iTween.ValueTo(this.gameObject, iTween.Hash("from", myReel.reelOffset,
												"to", 0f,
												"time", .25f,
												"onupdate", "moveReels",
												"onupdatetarget", this.gameObject,
												"oncomplete", "finishedScrollingBack",
												"oncompletetarget", this.gameObject));
				}
			}
		}
		else
		{
			// If a reel is touched then stop spinning.
			// Added the Dialog isShowing check to prevent slam stopping when touching
			// for special win surfacing that happens before reels land.
			if (TouchInput.swipeObject == this.gameObject && !Dialog.instance.isShowing)
			{
				// This MUST remain a slam stop call so that it does not cancel 
				// autospins, and so that it works during free spins.
				reelGame.engine.slamStop();

				if (SpinPanel.instance != null && SpinPanel.instance.stopButton != null)
				{
					//setting the "STOP" button as disabled so that it displayed properly (HIR-4803)
					SpinPanel.instance.stopButton.isEnabled = false;
				}
				TouchInput.swipeArea = null;
			}
		}
	}

	// Called when the reel completes a scroll back after a press is released which didn't spin the reels
	private void finishedScrollingBack()
	{
		isSwipeLayerRestoreNeeded = false;
		restoreSymbolsToOriginalLayersAfterSwipe();
	}

	// Clears the linked reel list so it will be set the next time the player swipes a reel, should be called when a spin starts on all of the swipeable reels
	public void clearLinkedReelListForReel()
	{
		linkedReelListForReel = null;
	}

	// force the symbols on the reel that is moving to be using the parent reel layer,
	// this avoids strange visual issues where parts of the symbol slide off the reels
	// if a symbol was rendering above the reel masks
	private void forceSymbolsToLayerOfParentReel()
	{
		bool canSpin = (SlotBaseGame.instance != null && SpinPanel.instance != null && SpinPanel.instance.isButtonsEnabled); // Base Game

		// only refresh this once per spin, since if the reels can move from swiping that means the linking info isn't going to change
		if (canSpin && linkedReelListForReel == null)
		{
			linkedReelListForReel = reelGame.engine.getLinkedReelListForReel(myReel);
		}

		if (linkedReelListForReel != null && linkedReelListForReel.Count > 0)
		{
			foreach (SlotReel reel in linkedReelListForReel)
			{
				reel.changeSymbolsToThisReelsLayerForSwipe();
			}
		}
		else
		{
			myReel.changeSymbolsToThisReelsLayerForSwipe();
		}
	}
	
	// Called when a swipe is cancelled either from a spin starting,
	// or when a non-swipe spin happens, in order to make sure that
	// any reel being swiped when a non-swipe spin starts is still able
	// to correctly reset the layers of the symbols on it.
	public void cancelSwipeAndRestoreSymbolsToOriginalLayersIfNeeded()
	{
		if (wasSwipeTarget || isSwipeLayerRestoreNeeded)
		{
			// Kill any tweens, we don't want them to call complete
			// functions since we are terminating the swipe here.
			iTween.Stop(this.gameObject, false);

			wasSwipeTarget = false;
			finishedScrollingBack();
		}
	}

	// Restores teh symbols to the original layers they were on
	private void restoreSymbolsToOriginalLayersAfterSwipe()
	{
		if (linkedReelListForReel == null)
		{
			linkedReelListForReel = reelGame.engine.getLinkedReelListForReel(myReel);
		}

		if (linkedReelListForReel != null && linkedReelListForReel.Count > 0)
		{
			foreach (SlotReel reel in linkedReelListForReel)
			{
				reel.restoreSymbolsToOriginalLayersAfterSwipe();
			}
		}
		else
		{
			myReel.restoreSymbolsToOriginalLayersAfterSwipe();
		}
	}

#if ZYNGA_TRAMP
	// Special TRAMP function to simulate a reel being moved to trigger spins in different directions
	public void simulateReelSwipe(float normalizedDistance)
	{
		moveReels(normalizedDistance);
	}
#endif

	/// Moves the reels using the SlotReel update.
	private void moveReels(float normalizedDistance)
	{
		bool canSpin = (SlotBaseGame.instance != null && SpinPanel.instance != null && SpinPanel.instance.isButtonsEnabled); // Base Game

		// only refresh this once per spin, since if the reels can move from swiping that means the linking info isn't going to change
		if (canSpin && linkedReelListForReel == null)
		{
			linkedReelListForReel = reelGame.engine.getLinkedReelListForReel(myReel);
		}
		
		if (linkedReelListForReel != null && linkedReelListForReel.Count > 0)
		{
			foreach (SlotReel reel in linkedReelListForReel)
			{
				reel.slideSymbolsFromSwipe(normalizedDistance);
			}
		}
		else
		{
			myReel.slideSymbolsFromSwipe(normalizedDistance);
		}
	}

	// Performs visiblity culling of buffered symbols against this SwipableReel screen area; activate/deactive as needed
	protected void doSymbolCulling()
	{
		// Require culling system to be enabled (this is our kill-switch)
		if (!Glb.enableSymbolCullingSystem)
		{
			return;
		}

		if (myReel == null || myReel.numberOfTopBufferSymbols == 0)
		{
			return;
		}

		// TEMP diagnostic to toggle culling each frame, so we can see visual errors flashing on screen
		if (Glb.autoToggleSymbolCulling)
		{
			Glb.enableSymbolCulling = (Time.frameCount & 1) == 1;
		}

		Profiler.BeginSample("doSymbolCulling");

		// We're gonna do vertical bounds checks in the SwipeableReel's local space
		const float shrinkAmount = 0.02f; // shrink bounds, lots of symbols are on the edge of being culled
		float swipeableTop = center.y + size.y / 2 - shrinkAmount;    // example: +4.25
		float swipeableBottom = center.y - size.y / 2 + shrinkAmount; // example: -0.85

		// first & last symbolList indices that are in the known visible range
		int minVisibleIndex = myReel.numberOfTopBufferSymbols;
		int maxVisibleIndex = myReel.numberOfTopBufferSymbols + myReel.visibleSymbols.Length - 1;

		// Iterate over reel symbols (the visible symbols + the above & below buffered symbols)
		for (int symbolIndex = 0; symbolIndex < myReel.symbolList.Count; symbolIndex++)
		{
			SlotSymbol symbol = myReel.symbolList[symbolIndex];

			SymbolAnimator symbolAnimator = symbol.animator;
			if (System.Object.ReferenceEquals(symbolAnimator, null))
			{
				continue;
			}

			// Assume an object is visible, unless we otherwise determine it to be culled
			bool isVisible = true;

			if (Glb.enableSymbolCulling)
			{
				// We can trivially exclude symbols indexed completely above the buffered/main symbol border
				// We only have to do bounds checks where a symbol borders the buffered/mainSymbol border
				//
				// Sample reel symbolList index layout:
				//
				//  [0] above buffered symbol(s) (  invisible  )
				//  [1] above buffered symbol(s) (^^invisible^^)
				//  [2] above buffered symbol(s) (maybe visible)
				//  [3] MinIndex                 (maybe visible)
				//  [4] Symbol                   (   visible   )
				//  [5] Symbol                   (   visible   )
				//  [6] MaxIndex                 (maybe visible)
				//  [7] below buffered symbol    (maybe visible)

				Transform symbolTransform = symbolAnimator.transform;
				int symbolBottomIndex = symbolIndex + (int)symbol.getWidthAndHeightOfSymbol().y - 1;

				if (symbolTransform.parent != this.transform)
				{
					// Some transitions allow symbols to fly outside of reels (ie: dead01, shark01, sundy01 spinning transitions)
					// So if a symbol is not parented to this reel, assume it's visible.
					isVisible = true;
				}
				else if (symbolBottomIndex < minVisibleIndex - 1)
				{
					isVisible = false;
				}
				else if (symbolBottomIndex <= minVisibleIndex || symbolIndex >= maxVisibleIndex)
				{
					// This symbol borders the buffered/main symbol border, must do a bounds test

					// Get our symbol bounds (in it's symbol animator's space)
					var symbolBounds = symbolAnimator.initialBoundsInfo.combinedLocalBounds;

					// Transform symbol top/bottom to this SwipeableReel's space (simple scale & translation)
					float symbolTop = symbolBounds.center.y + symbolTransform.localPosition.y + symbolBounds.extents.y * symbolTransform.localScale.y;
					float symbolBottom = symbolBounds.center.y + symbolTransform.localPosition.y - symbolBounds.extents.y * symbolTransform.localScale.y;

					// Is symbol fully above the top or below the bottom?
					if (symbolBottom > swipeableTop || symbolTop < swipeableBottom)
					{
						isVisible = false;
					}
				}
			}

			// enable/disable the symbol culling
			symbolAnimator.setIsCulled( !isVisible );
		}
		Profiler.EndSample();
	}
}

