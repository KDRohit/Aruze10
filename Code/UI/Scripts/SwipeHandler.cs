using UnityEngine;
using System.Collections;


public class SwipeHandler : MonoBehaviour
{
	public delegate void onSwipeDelegate();
	
	public event onSwipeDelegate OnSwipeRight;
	public event onSwipeDelegate OnSwipeLeft;

	public SwipeArea swipeArea;
	public bool isAnimation = false;

	public void Update()
	{
		if (TouchInput.isDragging && (swipeArea == null || TouchInput.swipeArea == swipeArea) && !isAnimation )
		{
			if (TouchInput.didSwipeLeft)
			{
				OnSwipeLeft();
			}
			else if (TouchInput.didSwipeRight)
			{
				OnSwipeRight();
			}
		}
	}
}