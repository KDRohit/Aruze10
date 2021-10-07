using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Creates and controls the display of a payline.

Instantiate the prefab Resources/prefabs/Paylines then call the init() method on the PaylineScript, like this:

	GameObject go = CommonGameObject.instantiate(paylinePrefab) as GameObject;
	PaylineScript payline = go.GetComponent<PaylineScript>();
	paylines.Add(payline);
	payline.init(paylinePositionArray, boxedReelsArray, lineColor);

You do that for each payline that you want to show, then you start showing the paylines
one at a time by looping in a coroutine like this:

	StartCoroutine(loopPaylines());

	private IEnumerator loopPaylines(bool onlyOnce = false)
	{
		while (SlotBaseGame.isGawkingAtWinnings)
		{
			foreach (PaylineScript payline in paylines)
			{
				yield return StartCoroutine(paylines[i]show(duration));
				if (SlotBaseGame.isGawkingAtWinnings)
				{
					break;
				}
			}

			if (onlyOnce)
			{
				break;
			}
		}

		// When done showing them, destroy them all.
		while (paylines.Count > 0)
		{
			Destroy(paylines[0].gameObject);
			paylines.RemoveAt(0);
		}
	}
*/

public class PaylineScript : OutcomeDisplayScript
{	
	public MeshFilter lineOnlyMeshFilter;	// The final mesh filter that is used for the lines-only display.

	protected PaylineLineDrawer _lineOnly = null;		// Used when showing the payline without any boxes.
	private int[] _positions = null;			// Passed into init.
	private int[] _boxedReels = null;			// Passed into init.
	private int[] _boxedReelOverCells = null;	// Passed into init.  Used to determine how many cells above to include in a box highlight
	private int[] _boxedReelUnderCells = null;	// Passed into init.	Used to determine how many cells below to include in a box highlight
	private int[] _boxedReelLeftCells = null;	// Passed into init.  Used to determine how many cells left to include in a box highlight
	private int[] _boxedReelRightCells = null;	// Passed into init.	Used to determine how many cells rightto include in a box highlight

	private float _halfWidth = 0;
	private float _halfHeight = 0;
	private int layer = -1;

	private const bool LEFT = false;
	private const bool RIGHT = true;
	protected Vector2[] lineOnlyPoints;

	// track if the payline was destroyed
	public bool wasDestroyed
	{
		get { return _wasDestroyed; }
	}
	private bool _wasDestroyed = false;

	/// Creates boxes and lines, and positions them where they should go.
	public void init(int[] positions, int[] boxedReels, int[] boxedReelOverCells, int[] boxedReelUnderCells, int[] boxedReelLeftCells, int[] boxedReelRightCells, Color color, ReelGame gameInstance, int paylineIndex, int layer, bool isDrawingBoxesOnly = false)
	{
		_positions = positions;
		_boxedReels = boxedReels;
		_boxedReelOverCells = boxedReelOverCells;
		_boxedReelUnderCells = boxedReelUnderCells;
		_boxedReelLeftCells = boxedReelLeftCells;
		_boxedReelRightCells = boxedReelRightCells;
		_gameInstance = gameInstance;
		this.layer = layer;
		this.paylineIndex = paylineIndex;
		if(gameInstance == null)
		{
			Debug.LogError("ReelGame instance is null");
			Destroy(gameObject);
			return;
		}

		//We may have games where we want paylines on the first three reels and then the fourth reel is a multiplier reel 
		//so our positions array can be less than the length of our reel roots array
		if (positions.Length > _gameInstance.getReelRootsLength())
		{
			Debug.LogError(string.Format("PaylineScript: A greater number of payline positions was specified ({0}) than the number of reels found ({1}) in the game ({2}).", positions.Length, _gameInstance.getReelRootsLength(), GameState.currentStateName), gameObject);
			Destroy(gameObject);
			_wasDestroyed = true;
		}

		_halfWidth = (_gameInstance.payBoxSize.x / 2);
		_halfHeight = (_gameInstance.payBoxSize.y / 2);

		Vector3 start = Vector3.zero;
		Vector3 end = Vector3.zero;

		_lineOnly = new PaylineLineDrawer();
		
		// Make sure the line-only has the correct number of control points (Transform objects).
		lineOnlyPoints = new Vector2[positions.Length + 2];

		// Do a separate loop for the line-only line,
		// so it isn't messed up by the weird logic of the box line
		// if there are no boxes on certain reels.
		SlotReel[] reelArray = _gameInstance.engine.getReelArray();

		for (int i = 0; i < positions.Length; i++)
		{
			// Position the line-only control point the center of where the box would be.
			Vector3 pointCenter = getReelCenterPosition(reelArray,i);
			lineOnlyPoints[i + 1] = new Vector2(pointCenter.x, pointCenter.y);
		}

		if(gameInstance.drawPaylines) 
		{
			for (int i = 0; i < positions.Length; i++) 
			{
				if (!isReelBoxed(i)) 
				{
					// If we are only drawing boxes we skip this whole section which is about drawing lines
					if (!isDrawingBoxesOnly)
					{
						// No box on this reel.
						// Create an empty line starting from the previous box to the reel with the next box (or the right edge of the screen).
						PaylineLineDrawer emptyLine = new PaylineLineDrawer();

						// Move forward in the for..i loop until we hit another box or the end.
						List<Vector3> points = new List<Vector3>();

						if (i == 0) 
						{
							// Add a point off the left of the screen.
							Vector3 offLeft = getReelCenterPosition(reelArray, 0);
							offLeft.x -= _gameInstance.payBoxSize.x * 0.5f;
							points.Add(offLeft);
						} 
						else 
						{
							points.Add(getReelLinePosition(reelArray, i - 1, RIGHT));
						}

						points.Add(getReelCenterPosition(reelArray, i));
						i++;

						while (i < positions.Length) 
						{
							if (!isReelBoxed(i)) 
							{
								// No box on this reel, create a point in the center and keep going.
								points.Add(getReelCenterPosition(reelArray, i));
								// Add a null in the boxes and lines arrays for this position.
								i++;
							} 
							else 
							{
								// There is a box on this reel, so create a point on the left side of it and break out of the loop.
								points.Add(getReelLinePosition(reelArray, i, LEFT));
								i--;    // Go back one in the loop so this box can get created as normal in the next iteration.
								break;
							}
						}

						if (i == positions.Length) 
						{
							// Add a point off the right of the screen.
							Vector3 offRight = getReelCenterPosition(reelArray, i - 1);
							offRight.x += _gameInstance.payBoxSize.x * 0.5f;
							points.Add(offRight);
						}

						// Apply all these points to the line.
						Vector2[] linePoints = new Vector2[points.Count];
						for (int j = 0; j < points.Count; j++) 
						{
							linePoints[j] = new Vector2(points[j].x, points[j].y);
						}

						prepareCombineParts(combineInstances, emptyLine.setPoints(linePoints));
					}
				} 
				else 
				{
					// Create and position the box.
					PaylineBoxDrawer box = new PaylineBoxDrawer(getReelCenterPosition(reelArray, i));

					// Size the box.
					box.boxSize = _gameInstance.payBoxSize * 0.5f;

					//Scale and offset the box for a multi cell box
					box.boxSize.Scale(this.getBoxedScale(i));
					box.boxCenterOffset = this.getBoxedCenterOffset(i);
					prepareCombineParts(combineInstances, box.refreshShape());

					// Create the line leading into the box from the left, but only if there is a box to the left or it's the first reel.
					// If we are only drawing boxes we skip this whole section which is about drawing lines
					if (!isDrawingBoxesOnly)
					{
						if (i == 0 || isReelBoxed(i - 1)) 
						{
							PaylineLineDrawer line = new PaylineLineDrawer();
							// Position the line.
							// Determine the start point of the line.
							if (i == 0) 
							{
								// Start of the first line is the same as the end of it,
								// except offset offscreen to the left by 5.
								start = getReelLinePosition(reelArray, 0, LEFT);
								//start.x -= 5;
							} 
							else 
							{
								start = getReelLinePosition(reelArray, i - 1, RIGHT);
							}

							end = getReelLinePosition(reelArray, i, LEFT);

							// Set the start and end points on the spline.
							prepareCombineParts(combineInstances, line.setPoints(new Vector2(start.x, start.y), new Vector2(end.x, end.y)));

							// if this is a wide box, skip the next reel(s) as we've already drawn them (checking for out of range index for non-square reelsets)
							if (i < boxedReelLeftCells.Length) 
							{
								i += boxedReelLeftCells[i];
							}
						}
					}
				}
			}
		}
		
		// Set the first and last line-only control points offscreen.
		Vector3 pos = getReelCenterPosition(reelArray,0);
		pos.x -= _gameInstance.payBoxSize.x * 0.5f;
		lineOnlyPoints[0] = new Vector2(pos.x, pos.y);

		pos = getReelCenterPosition(reelArray,positions.Length - 1);
		pos.x += _gameInstance.payBoxSize.x * 0.5f;
		lineOnlyPoints[lineOnlyPoints.Length - 1] = new Vector2(pos.x, pos.y);
		
		// Combine all the meshes.
		combineMeshes();

		// Set the colors after the boxes and lines have been created.
		this.color = color;
		this.alpha = 0;			// Make it invisible by default, until the show() method is called.
		this.lineOnlyAlpha = 0;	// Make it invisible by default, until the showLineOnly() method is called.
		this.transform.parent = gameInstance.activePaylinesGameObject.transform;
		// ensure that the paylines are at 0 offset in z after re-parent, only the parent should move all the paylines
		Vector3 currentLocalPos = this.transform.localPosition;
		this.transform.localPosition = new Vector3(currentLocalPos.x, currentLocalPos.y, 0.0f);
		this.name = "Payline " + paylineIndex;
	}

	// Combine all the meshes.
	protected override void combineMeshes()
	{
		// The line-only mesh is simpler than the one with boxes and lines. Combine into a single mesh with three materials.
		lineOnlyMeshFilter.mesh.CombineMeshes(_lineOnly.setPoints(lineOnlyPoints), false, false);
		
		// Must call the base function after combining the line mesh, because this also cleans up the temp meshes.
		base.combineMeshes();
	}

	private bool isReelBoxed(int reelIndex)
	{
		return (System.Array.IndexOf(_boxedReels, reelIndex) > -1);
	}
	
	
	/// <summary>
	/// Gets the boxed scale size depending on the number of connected cells above and below.
	/// </summary>
	/// <returns>
	/// The boxed scale.
	/// </returns>
	/// <param name='reelIndex'>
	/// Reel index.
	/// </param>
	private Vector2 getBoxedScale(int reelIndex)
	{
		int boxIndex = System.Array.IndexOf(_boxedReels, reelIndex);
		if (boxIndex < 0)
		{
			return new Vector2(1, 1);
		}
		else
		{
			int totalCellsHigh = 1 + _boxedReelOverCells[boxIndex] + _boxedReelUnderCells[boxIndex];
			int totalCellsWide = 1 + _boxedReelLeftCells[boxIndex] + _boxedReelRightCells[boxIndex];
			float spaceBetweenCells = _gameInstance.symbolVerticalSpacingWorld - _gameInstance.payBoxSize.y;
			return new Vector2(totalCellsWide, totalCellsHigh + spaceBetweenCells * (totalCellsHigh - 1) / _gameInstance.engine.getReelArray()[reelIndex].visibleSymbols.Length);
		}
	}
	
	/// <summary>
	/// Gets the boxed center offset depending on the number of connected cells above and below.
	/// </summary>
	/// <returns>
	/// The boxed center offset.
	/// </returns>
	/// <param name='reelIndex'>
	/// Reel index.
	/// </param>
	public Vector2 getBoxedCenterOffset(int reelIndex)
	{
		int boxIndex = System.Array.IndexOf(_boxedReels, reelIndex);
		if (boxIndex < 0)
		{
			return Vector2.zero;
		}
		else
		{
			float verticalCellOffset = _boxedReelUnderCells[boxIndex] - _boxedReelOverCells[boxIndex];
			float horizontalCellOffset = _boxedReelLeftCells[boxIndex] + _boxedReelRightCells[boxIndex];

			Vector2 offset = new Vector2(horizontalCellOffset / 2, verticalCellOffset / 2 );
			offset.Scale(new Vector2(_gameInstance.payBoxSize.x, _gameInstance.symbolVerticalSpacingWorld));
			return offset;
		}
	}

	/// Fades in the line-only payline and shows it for the specified number of seconds before fading out.
	/// Total lines is used to determine which sound should be played. If more lines are hit then a bigger sound is played.
	public IEnumerator showLineOnly(float seconds, float delay = 0.0f, bool playSound = false, int totalLines = 0)
	{
		if (delay > 0.0f)
		{
			yield return new WaitForSeconds(delay);
		}

		float fadeLife = 0;

		while (fadeLife < FADE_DURATION)
		{
			yield return null;
			fadeLife += Time.deltaTime;
			this.lineOnlyAlpha = fadeLife / FADE_DURATION;
		}

		if (seconds > 0)
		{
			// If seconds is 0, show the lines infinitely until fade is called separately.
			yield return new WaitForSeconds(seconds);
			yield return StartCoroutine(hideLineOnly());
		}

		if (playSound)
		{
			// Play the Outcome Sound.
			yield return StartCoroutine(playShowPaylineSound(totalLines));
		}
	}

	public IEnumerator playShowPaylineSound(int totalLines) 
	{
		if (totalLines >= 10) 
		{
			Audio.play(Audio.soundMap("show_payline_big1"));
			Audio.play(Audio.soundMap("show_payline_big2"));
		}
		else if (FreeSpinGame.instance == null) 
		{
			Audio.play(Audio.soundMap("show_payline_base"));
		} 
		else 
		{
			Audio.play(Audio.soundMap("show_payline_freespin"));
		}

		yield return null;
	}

	/// Starts fading the line-only then returns coroutine when done.
	public IEnumerator hideLineOnly()
	{
		float fadeLife = 0;

		while (fadeLife < FADE_DURATION)
		{
			yield return null;
			fadeLife += Time.deltaTime;
			// Use Min() just in case this is called before at full alpha.
			this.lineOnlyAlpha = Mathf.Min(this.lineOnlyAlpha, 1f - (fadeLife / FADE_DURATION));
		}
		
		this.lineOnlyAlpha = 0;
	}

	/// Setter/getter for all the line colors.
	public override Color color
	{
		set
		{
			_lineOnlyColor = value;
			_lineOnlyColor.a = _alpha;
			base.color = value;
		}

		get
		{
			return base.color;
		}
	}
	private Color _lineOnlyColor = Color.yellow;

	/// Setter/getter for the line-only alpha.
	public float lineOnlyAlpha
	{
		set
		{
			_lineOnlyAlpha = value;
			_lineOnlyColor.a = _lineOnlyAlpha;
			updateColors();
		}

		get
		{
			return _lineOnlyAlpha;
		}
	}
	private float _lineOnlyAlpha = 1f;

	/// Updates the colors for all boxes and lines of this payline.
	protected override void updateColors()
	{
		base.updateColors();
		
		// Handle the line-only color.
		if (lineOnlyMeshFilter != null)
		{
			// Apparently this can be null right after being destroyed,
			// but the fading coroutine still calls this probably one last time.
			lineOnlyMeshFilter.GetComponent<Renderer>().materials = getMaterials(true, _lineOnlyColor);
		}
	}

	// Returns the position of where a line would connect to this reel box.
	// leftRight = false means left, true means right.
	protected virtual Vector3 getReelLinePosition(SlotReel[] reelArray, int reelIndex, bool leftRight)
	{

		// Start with the center and adjust by half the width.
		Vector3 pos = getReelCenterPosition(reelArray,reelIndex);
		Vector3 pos2;
		int cellsAbove = 1;
		int cellsBelow = 1;
		if (this.isReelBoxed(reelIndex))
		{
			int boxIndex = System.Array.IndexOf<int>(_boxedReels, reelIndex);
			cellsAbove = _boxedReelOverCells[boxIndex];
			cellsBelow = _boxedReelUnderCells[boxIndex];
		}

		// See if there is a box to the left or right to adjust the Y.
		if (leftRight == RIGHT)
		{
			// Check the right.
			if (reelIndex == _gameInstance.getReelRootsLength() - 1)
			{
				// The rightmost reel.
				pos2 = pos;
				pos2.x += 5;
			}
			else
			{
				pos2 = getReelCenterPosition(reelArray,reelIndex + 1);
			}
		}
		else
		{
			// Check the left.
			if (reelIndex == 0)
			{
				// The leftmost reel.
				pos2 = pos;
				pos2.x -= 5;
			}
			else
			{
				pos2 = getReelCenterPosition(reelArray,reelIndex - 1);
			}
		}

		Vector2 intersect = rectIntersectPoint(pos.x, pos.y, pos2.x, pos2.y, cellsAbove, cellsBelow);

		// Convert the intersection points back to local scale for RageSpline.
		pos.x = intersect.x;
		pos.y = intersect.y;

		return pos;
	}

	/// Calculates where a line intersects a rectangle if the line starts at the center of
	/// the rectangle and ends at the given end point.
	public Vector2 rectIntersectPoint(float rectX, float rectY, float endX, float endY, int cellsAbove = 0, int cellsBelow = 0)
	{
		float distX = endX - rectX;
		float distY = endY - rectY;
		float x = 0;
		float y = 0;
		float halfWidth = _halfWidth;
		float halfHeight = _halfHeight;

		float cornerSlope = Mathf.Abs(_halfWidth / (_halfHeight));	// The slope required to go straight through a corner of the rectangle.
		if (cellsAbove != 0 || cellsBelow != 0)
	    {
			//recalculate corner, as it is multi cell highlight block.
			if (rectY > endY)
			{
				cornerSlope = Mathf.Abs(_halfWidth / ((2 * cellsAbove + 1) * _halfHeight));
			}
			else if (rectY < endY)
			{
				cornerSlope = Mathf.Abs(_halfWidth / ((2 * cellsBelow + 1) * _halfHeight));
			}
		}

		float slope = Mathf.Abs(distX / distY);

		// Determine if the line will intersect on the left/right or top/bottom.
		if (float.IsNaN(slope) || slope > cornerSlope)
		{
			// Intersects on left/right.
			x = halfWidth * Mathf.Sign(distX);
			y = halfWidth * (distY / Mathf.Abs(distX));
		}
		else
		{
			// Intersects on top/bottom or exactly at a corner.
			x = halfHeight * (distX / Mathf.Abs(distY));
			y = halfHeight * Mathf.Sign(distY);
		}

		x += rectX;
		y += rectY;

		return new Vector2(x, y);
	}
	
	/// Returns the position of the center of a reel's position in the payline.
	virtual protected Vector3 getReelCenterPosition(SlotReel[] reelArray,int reelIndex)
	{
		return getReelCenterPosition(reelArray,reelIndex + _gameInstance.spotlightReelStartIndex, _positions[reelIndex], layer);
	}
}
