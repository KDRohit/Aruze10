using UnityEngine;
using System.Collections.Generic;

/**
 * Extended version of SlideController that uses a limited number of objects and recycles them when they go
 * far enough outside of a view area defined by a UIPanel.
 *
 * Creation Date: 5/21/2020
 * Original Author: Scott Lepthien
 */
public class ViewRegionSlideController : SlideController
{
	public delegate GameObject GetItemForIndexDelegate(int index); // Delegate called to get the object that should currently be displayed at the index
	public delegate void OnIndexChangedForItem(GameObject item, int index); // Delegate for handling updating the position of an item when its index is changed
	public delegate void OnRemoveItem(GameObject item); // Delegate for getting rid of an item when it is no longer needed

	[Tooltip("Panel that defines the visible area where the sliding items will be displayed.")]
	[SerializeField] protected UIPanel panel;

	private LinkedList<DisplayObject> allSlidingObjects = new LinkedList<DisplayObject>(); // The list of all the objects that the slide controller is displaying (although they will not all be in the visible region)
	private List<DisplayObject> objectsToAddToEndOfList = new List<DisplayObject>(); // List that stores items that need to be added to the end of the list.  We need this since we can't modify allSlidingObjects while we iterate on it.
	private List<DisplayObject> objectsToAddToFrontOfList = new List<DisplayObject>(); // List that stores items that need to be added to the front of the list.  We need this since we can't modify allSlidingObjects while we iterate on it.
	private Vector3 clipAreaTop; // Cached value of the top of the clip area in the sliding content local space
	private Vector3 clipAreaBottom; // Cached value of the bottom of the clip area in the sliding content local space
	private Vector3 clipAreaLeft; // Cached value of the left of the clip area in the sliding content local space
	private Vector3 clipAreaRight; // Cached value of the right of the clip area in the sliding content local space
	protected int paddedElements; // Elements above and below the display area, used to determine when to swap elements from one side of the the list to the other
	protected float elementSize; // Either the width or height of your object, depending on the slide controller orientation
	protected GetItemForIndexDelegate getItemForIndexHandler; // Function delegate to handle getting an object to display for an index
	protected OnIndexChangedForItem indexChangedForItemHandler; // Function delegate to handle updating the positions of displayed objects when indexs are changed (namely when an element is deleted)
	protected OnRemoveItem removeItemHandler; // Function to handle items which aren't going to be part of allSlidingObjects anymore

	private const int TOTAL_OBJECT_COUNT_MULTIPLIER = 3; // Constant that is multiplied by the number of visible items that will fit in the panel display area to get the total number of items we will store and recycle
	/// <summary>
	/// Called by the owner of this ViewRegionSlideController in order to initialize and set it up to be used
	/// </summary>
	public void init(GetItemForIndexDelegate getItemForIndexHandler, OnIndexChangedForItem indexChangedForItemHandler, OnRemoveItem removeItemHandler, float elementSize)
	{
		calculateClipAreaExtents();
		setHandlers(getItemForIndexHandler, indexChangedForItemHandler, removeItemHandler);
		this.elementSize = elementSize;
		calculateTotalSlidingObjectCount();
	}

	/// <summary>
	/// Set the function delegate handlers that control what to do during key events with regards to the GameObjects that ViewRegionSlideController is displaying
	/// </summary>
	private void setHandlers(GetItemForIndexDelegate getItemForIndexHandler, OnIndexChangedForItem indexChangedForItemHandler, OnRemoveItem removeItemHandler)
	{
		this.getItemForIndexHandler = getItemForIndexHandler;
		this.indexChangedForItemHandler = indexChangedForItemHandler;
		this.removeItemHandler = removeItemHandler;
	}
	
	/// <summary>
	/// Determine the display count to use for the size of allSlidingObjects which will be the total amount of recycled objects used
	/// by this slide controller. This also sets the paddedElements size which determines when items need to be recycled.
	/// </summary>
	private void calculateTotalSlidingObjectCount()
	{
		int totalSlidingObjectCount = 0;
		int visibleItemCount = 0;
		float panelSize = 0.0f;
	
		// Determine how many display objects we need based on the size of the displayed objects
		// and the size of the panel that will contain the visible ones.
		switch (currentOrientation)
		{
			case Orientation.VERTICAL:
				panelSize = getClipAreaHeight();
				visibleItemCount = (int)(panelSize / elementSize);
				break;
				
			case Orientation.HORIZONTAL:
				panelSize = getClipAreaWidth();
				visibleItemCount = (int)(panelSize / elementSize);
				break;
				
			case Orientation.All:
				Debug.LogError("ViewRegionSlideController.setTotalDisplayCount() - currentOrientation is set to unsupported type ALL!");
				break;
		}

		if (visibleItemCount > 0)
		{
			paddedElements = visibleItemCount - 1;
			totalSlidingObjectCount = visibleItemCount * TOTAL_OBJECT_COUNT_MULTIPLIER;
		}
		else
		{
			Debug.LogError("ViewRegionSlideController.setTotalDisplayCount() - Couldn't determine a visibleItemCount, this probably means that the element size is too big or the panel they are displayed in is too small."
							+ " Check the values. elementSize = " + elementSize + "; panelSize = " + panelSize);
		}

		if (allSlidingObjects.Count > 0)
		{
			Debug.LogWarning("ViewRegionSlideController.setDisplayCount() - totalSlidingObjectCount = " + totalSlidingObjectCount + "; attempting to change the count size, this probably isn't a good idea!");
			clearAllDisplayObjects();
		}

		for (int i = 0; i < totalSlidingObjectCount; i++)
		{
			allSlidingObjects.AddLast(new DisplayObject(i, null));
		}

		// Initialize the objects if their is a valid object to grab for the index
		foreach (DisplayObject item in allSlidingObjects)
		{
			if (getItemForIndexHandler != null)
			{
				GameObject gameObjForIndex = getItemForIndexHandler(item.index);
				item.obj = gameObjForIndex;
			}
		}
	}

	/// <summary>
	/// Built in Unity update function, needs to call the base for default SlideController functionality and then
	/// handle the list of recyclable objects it is managing.
	/// </summary>
	public override void Update()
	{
		base.Update();
		
		if (momentum != 0.0f || scrollBarMomentum != 0.0f)
		{
			// We need to loop until we don't perform anymore updates.
			// This ensures that even if scrolling is happening extremely
			// fast that no items get skipped.
			updateAllDisplayObjects();
		}
	}

	/// <summary>
	/// Remove a specific object from the list of allSlidingObjects. This will also allow
	/// the objects after the deletion to handle repositioning, and then recycle the removed
	/// object to the end of the list.
	/// </summary>
	/// <param name="obj">The GameObject to search for in order to be removed as an element of the allSlidingObjects</param>
	public void removeObject(GameObject obj)
	{
		DisplayObject objToRemove = null;

		foreach (DisplayObject currentDisplayObj in allSlidingObjects)
		{
			if (objToRemove != null)
			{
				currentDisplayObj.index -= 1;
				if (indexChangedForItemHandler != null && currentDisplayObj.obj != null)
				{
					indexChangedForItemHandler(currentDisplayObj.obj, currentDisplayObj.index);
				}
			}
		
			if (currentDisplayObj.obj == obj)
			{
				objToRemove = currentDisplayObj;
			}
		}

		if (objToRemove != null)
		{
			// Remove the object and move it to the end of the list (which
			// will also check to see if we need to grab a new entry).  Always
			// move it to the end of the list since the list will slide up to
			// replace deleted entries.
			moveDisplayObjectToEndOfList(objToRemove, isFromRemoveObject:true);
		}
	}

	/// <summary>
	/// Discard the whole list of items that this SlideController was managing
	/// </summary>
	public void clearAllDisplayObjects()
	{
		List<DisplayObject> currentItems = new List<DisplayObject>(allSlidingObjects);
		foreach (DisplayObject item in currentItems)
		{
			if (item.obj != null  && removeItemHandler != null)
			{
				removeItemHandler(item.obj);
			}
		}
	
		allSlidingObjects.Clear();
	}

	/// <summary>
	/// Cycles through all the allSlidingObjects based on what direction the SlideController is moving
	/// and determines if any objects need to be recycled and updated to a new element.
	/// </summary>
	private void updateAllDisplayObjects()
	{
		if (momentum > 0 || scrollBarMomentum > 0)
		{
			foreach (DisplayObject item in allSlidingObjects)
			{
				updateDisplayObject(item);
			}
		}
		else
		{
			// We need to start at the end of the list and check from there
			// when moving in the opposite direction
			LinkedListNode<DisplayObject> reverseIter = allSlidingObjects.Last;
			while (reverseIter != null)
			{
				updateDisplayObject(reverseIter.Value);
				reverseIter = reverseIter.Previous;
			}
		}

		foreach (DisplayObject item in objectsToAddToEndOfList)
		{
			moveDisplayObjectToEndOfList(item, isFromRemoveObject:false);
		}
		objectsToAddToEndOfList.Clear();
		
		foreach (DisplayObject item in objectsToAddToFrontOfList)
		{
			allSlidingObjects.Remove(item);
			item.index = allSlidingObjects.First.Value.index - 1;
			if (item.obj != null && removeItemHandler != null)
			{
				removeItemHandler(item.obj);
			}

			if (getItemForIndexHandler != null)
			{
				item.obj = getItemForIndexHandler(item.index);
			}

			allSlidingObjects.AddFirst(item);
		}
		objectsToAddToFrontOfList.Clear();
	}

	/// <summary>
	/// Update an individual DisplayObject determining if it needs to be recycled.
	/// Calculations are based on what orientation the SlideController move sin.
	/// </summary>
	/// <param name="item">The DisplayObject that will be checked to determine if it should be recycled.</param>
	private void updateDisplayObject(DisplayObject item)
	{
		if (item != null)
		{
			if (currentOrientation == Orientation.HORIZONTAL)
			{
				updateObjectHorizontal(item);
			}
			else
			{
				updateObjectVertical(item);
			}
		}
	}

	/// <summary>
	/// Move an element from inside of allSlidingObjects to the end of the list.
	/// Recycling the object it was currently displaying and getting a new one
	/// for its new index to display.
	/// </summary>
	/// <param name="item">The element to move.</param>
	/// <param name="isFromRemoveObject">Tells if this call is coming from a singular object being removed.  Otherwise we assume that this is triggering due to the player scrolling.</param>
	private void moveDisplayObjectToEndOfList(DisplayObject item, bool isFromRemoveObject)
	{
		// Check if we should move this item to the end of the list.
		// If an item is actually being deleted and removed, we always
		// need to handle this.
		// ---
		// Otherwise, if we are just scrolling, we should check if the last item in the
		// list is a null object, then we don't need to move anymore objects to that side
		// of the list since they would also be null.  And if the slide controller overshoots
		// by too much which is possible using a track pad, then we might actually move too many
		// objects.
		if (isFromRemoveObject || allSlidingObjects.Last.Value.obj != null)
		{
			allSlidingObjects.Remove(item);
			item.index = allSlidingObjects.Last.Value.index + 1;
			if (item.obj != null && removeItemHandler != null)
			{
				removeItemHandler(item.obj);
			}

			if (getItemForIndexHandler != null)
			{
				item.obj = getItemForIndexHandler(item.index);
			}

			allSlidingObjects.AddLast(item);
		}
	}

	/// <summary>
	/// Determines if an item needs to be recycled due to horizontal scrolling.
	/// NOTE: This code is untested so far.  Initially this class was only
	/// being used for the vertical scrolling in the Inbox Dialog.
	/// </summary>
	/// <param name="item">The element to check.</param>
	private void updateObjectHorizontal(DisplayObject item)
	{
		if (item.obj != null)
		{
			Vector3 itemPos = item.obj.transform.position;
			itemPos = content.transform.parent.transform.InverseTransformPoint(itemPos);
			float boundOffset = elementSize * paddedElements;

			// Need to determine what to do based on the direction things are scrolling
			if (momentum > 0 || scrollBarMomentum > 0)
			{
				// Need to check if items at the right are far enough beyond the
				// visible region and if so recycle them to the left
				if (itemPos.x >= clipAreaRight.x + boundOffset)
				{
					// Check if we need to move this element down to the end of the list or not
					if (allSlidingObjects.Last.Value.obj != null)
					{
						objectsToAddToEndOfList.Add(item);
					}
				}
			}
			else
			{
				// Need to check if items at the left are far enough beyond the
				// visible region and if so recycle them to the right
				if (itemPos.x <= clipAreaLeft.x - boundOffset)
				{
					// Check if the top of the list is already the top index, in which case we aren't going to move anything
					if (allSlidingObjects.First.Value.index - objectsToAddToFrontOfList.Count > 0)
					{
						objectsToAddToFrontOfList.Add(item);
					}
				}
			}
		}
		else
		{
			// Only Need to deal with null objects at the bottom, since we can't end up with them at the front of the list unless
			// the list isn't displaying anything.  If we have nulls at the bottom of the list and the user is scrolling up we should
			// just move them to the front of the list if possible.
			if (momentum < 0 || scrollBarMomentum < 0)
			{
				if (allSlidingObjects.First.Value.index - objectsToAddToFrontOfList.Count > 0)
				{
					objectsToAddToFrontOfList.Add(item);
				}
			}
		}
	}

	/// <summary>
	/// Determines if an item needs to be recycled due to vertical scrolling.
	/// </summary>
	/// <param name="item">The element to check.</param>
	private void updateObjectVertical(DisplayObject item)
	{
		if (item.obj != null)
		{
			Vector3 itemPos = item.obj.transform.position;
			itemPos = content.transform.parent.transform.InverseTransformPoint(itemPos);
			float boundOffset = elementSize * paddedElements;

			// Need to determine what to do based on the direction things are scrolling
			if (momentum > 0 || scrollBarMomentum > 0)
			{
				// Need to check if items at the top are far enough above the visible region
				// and if so recycle them to the bottom
				if (itemPos.y >= clipAreaTop.y + boundOffset)
				{
					// Check if we need to move this element down to the end of the list or not
					if (allSlidingObjects.Last.Value.obj != null)
					{
						objectsToAddToEndOfList.Add(item);
					}
				}
			}
			else if (momentum < 0 || scrollBarMomentum < 0)
			{
				// Need to check if items at the bottom are far enough below the visible region
				// and if so recycle them to the top
				if (itemPos.y <= clipAreaBottom.y - boundOffset)
				{
					// Check if the top of the list is already the top index, in which case we aren't going to move anything
					if (allSlidingObjects.First.Value.index - objectsToAddToFrontOfList.Count > 0)
					{
						objectsToAddToFrontOfList.Add(item);
					}
				}
			}
		}
		else
		{
			// Only Need to deal with null objects at the bottom, since we can't end up with them at the front of the list unless
			// the list isn't displaying anything.  If we have nulls at the bottom of the list and the user is scrolling up we should
			// just move them to the front of the list if possible.
			if (momentum < 0 || scrollBarMomentum < 0)
			{
				if (allSlidingObjects.First.Value.index - objectsToAddToFrontOfList.Count > 0)
				{
					objectsToAddToFrontOfList.Add(item);
				}
			}
		}
	}
	
	/// <summary>
	/// Calculate the clip area extents and store them out, since they aren't going to change.
	/// This way we need to only calculate them once.
	/// </summary>
	private void calculateClipAreaExtents()
	{
		calculateClipAreaTop();
		calculateClipAreaBottom();
		calculateClipAreaLeft();
		calculateClipAreaRight();
	}
	
	/// <summary>
	/// Transform a point from the panel space into the content space
	/// </summary>
	/// <param name="point">The point to transform.</param>
	private Vector3 panelPointToContentLocalSpace(Vector3 point)
	{
		point = panel.transform.TransformPoint(point);
		point = content.transform.parent.transform.InverseTransformPoint(point);
		return point;
	}
	
	/// <summary>
	/// Calculate and store the clip area top point translated to be in the space
	/// that the sliding objects live
	/// </summary>
	private void calculateClipAreaTop()
	{
		clipAreaTop = new Vector3(0, panel.clipRange.y + (panel.clipRange.w / 2), 0);
		clipAreaTop = panelPointToContentLocalSpace(clipAreaTop);
	}
	
	/// <summary>
	/// Calculate and store the clip area bottom point translated to be in the space
	/// that the sliding objects live
	/// </summary>
	private void calculateClipAreaBottom()
	{
		clipAreaBottom = new Vector3(0, panel.clipRange.y - (panel.clipRange.w / 2), 0);
		clipAreaBottom = panelPointToContentLocalSpace(clipAreaBottom);
	}
	
	/// <summary>
	/// Get the clip area height in the same space that the sliding objects live
	/// </summary>
	private float getClipAreaHeight()
	{
		return clipAreaTop.y - clipAreaBottom.y;
	}
	
	/// <summary>
	/// Calculate and store the clip area left point translated to be in the space
	/// that the sliding objects live
	/// </summary>
	private void calculateClipAreaLeft()
	{
		clipAreaLeft = new Vector3(panel.clipRange.x - (panel.clipRange.z / 2), 0, 0);
		clipAreaLeft = panelPointToContentLocalSpace(clipAreaLeft);
	}
	
	/// <summary>
	/// Calculate and store the clip area right point translated to be in the space
	/// that the sliding objects live
	/// </summary>
	private void calculateClipAreaRight()
	{
		clipAreaRight = new Vector3(panel.clipRange.x + (panel.clipRange.z / 2), 0, 0);
		clipAreaRight = panelPointToContentLocalSpace(clipAreaRight);
	}
	
	// Get the clip area width in the same space that the sliding objects live
	private float getClipAreaWidth()
	{
		return clipAreaRight.x - clipAreaLeft.x;
	}

	/// <summary>
	/// Class to represent an object being displayed by this SlideController.
	/// Tracks the index the element is linked to as well as the GameObject that
	/// was retrieved for that index.
	/// </summary>
	private class DisplayObject
	{
		public int index = 0; // Index of this object, used to track what index a particular object is displaying
		public GameObject obj; // The actual object being displayed (can be null if nothing is being displayed at this position)

		public DisplayObject(int index, GameObject obj)
		{
			this.index = index;
			this.obj = obj;
		}
	}

	protected override void scrollBarChanged(UIScrollBar scrollBar)
	{
		//Scroll bar movement doesn't change the momentum so we need to check in here which direction the scrollbar is moving
		Transform contentTransform = content.transform;
		Vector3 currentPos = contentTransform.localPosition;
		base.scrollBarChanged(scrollBar);
		Vector3 newPos = contentTransform.localPosition;
		
		//Just need to determine which direction the scroll bar is moving
		if (currentOrientation == Orientation.VERTICAL)
		{
			scrollBarMomentum = newPos.y > currentPos.y ? 1 : -1;
		}
		else
		{
			scrollBarMomentum = newPos.x > currentPos.x ? 1 : -1;
		}

		updateAllDisplayObjects();
	}
} 
