using UnityEngine;

public class SlideController: MonoBehaviour
{
	// =============================
	// PROTECTED
	// =============================
	protected Vector3 finalAnimiationPos = Vector3.zero;

	// Controls if the content is animating at all and if it can be interupted
	protected bool isAnimation = false;
	protected bool isAnimationForced = false;

	// Controls animating by Unity AnimationCurve
	protected float curveTime;
	protected float curveDuration;
	protected Vector3 curveStart;
	protected bool useAnimationCurve;

	// =============================
	// PUBLIC
	// =============================
	public SlideContent content;
	public SwipeArea swipeArea;

	// Offsets control how fast a given UV pans compared to the slide content. Useful in parrallax scrolling scenarios.
	public float[] uvPanningOffsets;
	public UVPanning[] uvPanning; // Always in Superforeground, Foreground, midground, background

	public float gizmoHeight = 10f;
	public float gizmoWidth = 10f;
	public float leftBound = 0;
	public float rightBound = 0;
	public float topBound = 0;
	public float bottomBound = 0;
	public float momentumModifier = 1.0f;
	public float maxMomentum = 0.0f;
    public float friction = 0.8f;
	public bool shouldUseMouseScroll = true;

	public delegate void onAnimationComplete(Dict args = null);
	public event onAnimationComplete onEndAnimation;

	// what direction we want to drag in. Default to horizantal to not break old stuff
	public enum Orientation { VERTICAL, HORIZONTAL, All };
	public Orientation currentOrientation = Orientation.HORIZONTAL;

	public delegate void onContentMovedDelegate(Transform content, Vector2 delta);
	public event onContentMovedDelegate onContentMoved;

   	public AnimationCurve curve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

	// Scroll Bar Functionality
	public UIScrollBar scrollBar;
    public float totalBounds;
    public float mouseScrollSpeed = 6;

	// =============================
	// CONST
	// =============================
	protected const int SCROLL_LIMIT_OFFSET = 300;
	protected const int TRANSITION_SPEED = -18;
	
	protected SlideController currentScrollingSlider = null;
	protected float scrollBarMomentum = 0;

	void Start()
	{
		currentScrollingSlider = this;
	}

	public void resetEvents()
	{
		onContentMoved = null;
		onEndAnimation = null;
	}

	public void toggleScrollBar(bool setActive = false)
	{
		scrollBar.gameObject.SetActive(setActive);
	}
	
	public void resetPosition()
	{
	    if (currentOrientation == Orientation.HORIZONTAL)
		{
			switch(content.justified)
			{
				case SlideContent.Justification.LEFT:
					CommonTransform.setX(content.transform, 0);
					break;
				case SlideContent.Justification.CENTER:
					CommonTransform.setX(content.transform, content.width/2);
					break;
				case SlideContent.Justification.RIGHT:
					CommonTransform.setX(content.transform, content.width);
					break;					
			}
		}
		else if (currentOrientation == Orientation.VERTICAL)
		{
			switch (content.justified)
			{
				case SlideContent.Justification.BOTTOM:
					CommonTransform.setY(content.transform, -content.height);
					break;
				case SlideContent.Justification.TOP:
					CommonTransform.setY(content.transform, 0);					
					break;
				case SlideContent.Justification.CENTER:
					CommonTransform.setY(content.transform, -content.height/2);
					break;				
			}
		}
	}

	public void setBounds(float leftOrTop, float rightOrBottom)
	{
		if (this != null && gameObject != null)
		{
			if (currentOrientation == Orientation.HORIZONTAL)
			{
				this.leftBound = leftOrTop;
				this.rightBound = rightOrBottom;
				totalBounds = Mathf.Abs(rightBound - leftBound);
			}
			else if (currentOrientation == Orientation.VERTICAL)
			{
				this.topBound = leftOrTop;
				this.bottomBound = rightOrBottom;
				totalBounds = Mathf.Abs(bottomBound - topBound);
			}

			if (totalBounds < content.width)
			{
				this.enabled = false;
				//Debug.LogError("Deactivating slide controller, as the content is bigger than the set bounds");
			}
			else
			{
				this.enabled = true;
			}

			// This function gets called as an init. So moving scroll registation here.
			if (scrollBar != null)
			{
				scrollBar.onChange -= scrollBarChanged;
				scrollBar.onChange += scrollBarChanged;

				scrollBar.onDragFinished -= onDragFinished;
				scrollBar.onDragFinished += onDragFinished;
			}
		}
	}

	private void onDragFinished()
	{
		scrollBarMomentum = 0;
	}

	protected virtual void scrollBarChanged(UIScrollBar sb)
	{
		float currentScrollValue = 0;
		float adjustedScrollValue = 0;
		float finalValue = 0f;
		switch (currentOrientation)
		{
			case Orientation.HORIZONTAL:
				// When we move the bar to the right, the content should approach the left bound.
				currentScrollValue = (1 - sb.scrollValue);
				adjustedScrollValue = currentScrollValue * (totalBounds - content.width);
				finalValue = (leftBound + adjustedScrollValue);
				break;
			case Orientation.VERTICAL:
				// When we move the bar to the bottom, the content should approach the top bound.
				currentScrollValue = (1 - sb.scrollValue);
				adjustedScrollValue = currentScrollValue * (totalBounds - content.height);
				finalValue = (topBound - adjustedScrollValue);
				break;
		}
		
		setContentPosition(finalValue);
	}

	public bool isAnimating()
	{
		return isAnimation;
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		float uiScale = transform.root.localScale.x;		
		Vector3 topLeft = Vector3.zero;
		Vector3 topRight = Vector3.zero;
		Vector3 bottomLeft = Vector3.zero;
		Vector3 bottomRight = Vector3.zero;
		if (currentOrientation == Orientation.HORIZONTAL)
		{
			topLeft = new Vector3(transform.position.x - (leftBound * uiScale) , transform.position.y - gizmoHeight/2, 1.0f);
			topRight = new Vector3(transform.position.x + (rightBound * uiScale), transform.position.y - gizmoHeight/2, 1.0f);
			bottomLeft = new Vector3(transform.position.x - (leftBound * uiScale), transform.position.y + gizmoHeight/2, 1.0f);
			bottomRight = new Vector3(transform.position.x + (rightBound * uiScale), transform.position.y + gizmoHeight/2, 1.0f);
			Gizmos.DrawLine(topLeft, bottomLeft);
			Gizmos.DrawLine(topRight, bottomRight);
		}

		// Now draw the top/bottom lines.
		else
		{
			topLeft = new Vector3(transform.position.x - gizmoWidth/2, transform.position.y + (topBound * uiScale), 1.0f);
			topRight = new Vector3(transform.position.x + gizmoWidth/2, transform.position.y + (topBound * uiScale), 1.0f);
			bottomLeft = new Vector3(transform.position.x - gizmoWidth/2, transform.position.y + (bottomBound * uiScale), 1.0f);
			bottomRight = new Vector3(transform.position.x + gizmoWidth/2, transform.position.y + (bottomBound * uiScale), 1.0f);
			Gizmos.DrawLine(topLeft, topRight);
			Gizmos.DrawLine(bottomLeft, bottomRight);		
		}
		
		//Gizmos.DrawWireCube(transform.position, new Vector3( * transform.root.localScale.x, , 1.0f))
	}
	
	public virtual void Update()
	{
		if (!enabled)
		{
			// If we are't enabled, then don't do squat.
			return;
		}
		
		if (currentScrollingSlider == null || (TouchInput.isDragging && TouchInput.swipeArea == swipeArea))
		{
			currentScrollingSlider = this;
		}
		
		float mouseScroll = -Input.mouseScrollDelta.y * mouseScrollSpeed;
		bool isMouseScrolling = Mathf.Abs(mouseScroll) > 0.5f && currentScrollingSlider == this && shouldUseMouseScroll;

		if (!Mathf.Approximately(momentum, 0))
		{
			switch (currentOrientation)
			{
				case Orientation.HORIZONTAL:
					updateHorizontalPosition();
					break;

				// Assumes the top of the list is what is seen first, not the bottom.
				case Orientation.VERTICAL:
					updateVerticalPosition();
					break;
			}
		}
		
		if (!isAnimation && isMouseScrolling)
		{
			if (currentOrientation != Orientation.All)
			{
				momentum += mouseScroll;
			}
		}
		else if (!isAnimation && TouchInput.isDragging && (swipeArea == null || TouchInput.swipeArea == swipeArea))
		{
			switch (currentOrientation)
			{
				case Orientation.HORIZONTAL:
					momentum += TouchInput.speed.x;
					break;

				case Orientation.VERTICAL:
					momentum += TouchInput.speed.y;
					break;
			}
		}
		else if (isAnimation && (TouchInput.isDragging || isMouseScrolling) && !isAnimationForced)
		{
			isAnimation = false;
			if (onEndAnimation != null)
			{
				onEndAnimation();
			}
		}
		else
		{
			updateAnimationCurve();		// sets _momentum based on animation curve
			checkBoundaries();
		}

		if (momentum < 1 && momentum > -1)
		{
			momentum = 0;						
		}

		if (scrollBar != null)
		{
			updateScrollBar();
		}		
	}

	private void checkBoundaries()
	{
		switch (currentOrientation)
		{
			case Orientation.HORIZONTAL:
				if (content.leftPosition < leftBound)
				{
					CommonTransform.setX(content.transform, leftBound + (leftBound - content.leftPosition));
				}
				else if (content.rightPosition > rightBound)
				{
					CommonTransform.setX(content.transform, content.transform.localPosition.x - (content.rightPosition - rightBound));
				}
				break;

			case Orientation.VERTICAL:
				if (content.bottomPosition < bottomBound)
				{
					CommonTransform.setY(content.transform, content.transform.localPosition.y + (bottomBound - content.bottomPosition));
				}
				else if (content.topPosition > topBound)
				{
					CommonTransform.setY(content.transform, content.transform.localPosition.y - (content.topPosition - topBound));
				}
				break;
		}
	}

	
	//Applies the current momentum to the position and limits movement amount to be within the content bounds
	private float getDesiredMovePosition()
	{
		float desiredPosition = 0;
		switch (currentOrientation)
		{
			case Orientation.HORIZONTAL:
				if (content.leftPosition + momentum < leftBound)
				{
					momentum = leftBound - content.leftPosition;
					desiredPosition = content.transform.localPosition.x + momentum;
				}
				else if (content.rightPosition + momentum > rightBound)
				{
					momentum = rightBound - content.rightPosition;
					desiredPosition = content.transform.localPosition.x + momentum;
				}
				else
				{
					desiredPosition = content.transform.localPosition.x + momentum;
				}
				break;

			case Orientation.VERTICAL:
				if (content.bottomPosition + momentum < bottomBound)
				{
					desiredPosition = content.transform.localPosition.y + (bottomBound - content.bottomPosition);
				}
				else if (content.topPosition + momentum > topBound)
				{
					desiredPosition = content.transform.localPosition.y - (content.topPosition - topBound);
				}
				else
				{
					desiredPosition = content.transform.localPosition.y + momentum;
				}
				break;
		}

		return desiredPosition;
	}

	private void updateHorizontalPosition()
	{
		// Move stuff
		setContentPosition(getDesiredMovePosition());
		
		if (content.leftPosition < leftBound || content.rightPosition > rightBound)
		{
			checkBoundaries();

			momentum = 0;

			if (onEndAnimation != null)
			{
				onEndAnimation();
			}
		}

		if (isAnimation)
		{
			//Stop the animation if the distance  from our current pos & the final pos is less than our momentum
			if (Mathf.Abs(content.transform.localPosition.x - finalAnimiationPos.x) <= Mathf.Abs(momentum))
			{
				isAnimation = false;

				CommonTransform.setX(content.transform, finalAnimiationPos.x);
				momentum = 0;

				if (onEndAnimation != null)
				{
					onEndAnimation();
				}
			}
			else if (content.leftPosition <= finalAnimiationPos.x && momentum < 0)
			{
				isAnimation = false;

				CommonTransform.setX(content.transform, finalAnimiationPos.x);
				momentum = 0;

				if (onEndAnimation != null)
				{
					onEndAnimation();
				}
			}
			else if (content.leftPosition >= finalAnimiationPos.x && momentum > 0)
			{
				isAnimation = false;
				CommonTransform.setX(content.transform, finalAnimiationPos.x);
				momentum = 0;

				if (onEndAnimation != null)
				{
					onEndAnimation();
				}
			}
		}

		updateUVScrolling(momentum);

		if (!isAnimation)
		{
			momentum *= friction; // Friction should ignore the modifier.
		}
	}

	private void updateUVScrolling(float m)
	{
		if (content.leftPosition >= leftBound && content.rightPosition <= rightBound)
		{
			for (int i = 0; i < uvPanning.Length; i++)
			{
				uvPanning[i].setOffset(m * uvPanningOffsets[i]);
			}
		}
	}

	private void updateVerticalPosition()
	{
		// Move stuff
		setContentPosition(getDesiredMovePosition());

		// Out of bounds!
		if (content.bottomPosition < bottomBound || content.topPosition > topBound)
		{
			if (isAnimation && onEndAnimation != null)
			{
				onEndAnimation();
			}
			isAnimation = false;
			_momentum = 0;
		}
		else if (isAnimation && (Mathf.Abs(content.transform.localPosition.y - finalAnimiationPos.y) <= Mathf.Abs(momentum) || content.transform.localPosition.y >= topBound || content.transform.localPosition.y <= bottomBound))
		{
			isAnimation = false;
			CommonTransform.setY(content.transform, finalAnimiationPos.y);
			_momentum = 0;

			if (onEndAnimation != null)
			{
				onEndAnimation();
			}
		}

		if (!isAnimation)
		{
			momentum *= friction; // Friction should ignore the modifier.
		}
	}

	/// <summary>
	/// Returns a boolean if the child gameobject is within the bounds
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	///
	private UIPanel _panel;
	public bool isInView(GameObject obj)
	{
		if (obj != null)
		{
			Vector3 pos = content.transform.TransformPoint(obj.transform.localPosition);
			pos = transform.InverseTransformPoint(pos);

			if (_panel == null)
			{
				_panel = GetComponent<UIPanel>();
			}

			if (_panel != null)
			{
				return pos.x >= -_panel.clipRange.z/2 &&
				       pos.x <= _panel.clipRange.z/2 &&
				       pos.y >= -_panel.clipRange.w/2 &&
				       pos.y <= _panel.clipRange.w/2;
			}
			// no ui panel? so everything must be visible when it was added
			return true;
		}

		return false;
	}

	private void updateScrollBar()
	{
		float value = 0f;
		float currentPosition = 0f;
		float total;
		
		switch (currentOrientation)
		{
			case Orientation.HORIZONTAL:
				// If we are in horizontal mode, then use the right distance for calculation.
				currentPosition = rightBound - content.rightPosition;
				total = (totalBounds == content.width) ? 1f : (totalBounds - content.width);
				value = currentPosition / total;
				break;
			case Orientation.VERTICAL:
				// If we are in vertical mode, then use the bottom distance for calculation.
				currentPosition = topBound - content.topPosition;
				total = (totalBounds == content.height) ? 1f : (totalBounds - content.height);
				value = currentPosition / total;
				value = (1 - value); // Invert thisaq
				break;
			default:
				break;
		}
#if UNITY_EDITOR
		UnityEngine.Debug.Assert(!float.IsNaN(value), "content height or width hasn't been set");
#endif
		if (!scrollBar.isSelected)
		{
			scrollBar.setScrollValue(value);
		}
	}
	
	private void updateAnimationCurve()
	{
		if (useAnimationCurve && isAnimation)
		{
			curveTime += Time.deltaTime;
			float percentFinished = curveTime / curveDuration;

			Vector3 newPosition = Vector3.Lerp(curveStart, finalAnimiationPos, curve.Evaluate(percentFinished));
			Vector2 delta = newPosition - content.transform.localPosition;

			switch (currentOrientation)
			{
				case Orientation.HORIZONTAL:
					_momentum = delta.x;
					break;
				case Orientation.VERTICAL:
					_momentum = delta.y;
					break;
			}		
		}
		else
		{
			useAnimationCurve = false;
		}
	}

	protected virtual void setContentPosition(float newValue)
	{
		if (onContentMoved != null)
		{
			// Only do this if we have a registered delegate.
			Vector2 delta = Vector2.zero;
			switch (currentOrientation)
			{
				case Orientation.HORIZONTAL:
					delta.x = content.transform.localPosition.x - newValue;
					CommonTransform.setX(content.transform, newValue);
					break;
				case Orientation.VERTICAL:
					delta.y = content.transform.localPosition.y - newValue;
					CommonTransform.setY(content.transform, newValue);
					break;
			}
			onContentMoved(content.transform, delta);
		}
		else
		{
			switch (currentOrientation)
			{
				case Orientation.HORIZONTAL:
					CommonTransform.setX(content.transform, newValue);
					break;
				case Orientation.VERTICAL:
					CommonTransform.setY(content.transform, newValue);
					break;
			}
		}
	}

	public void AddXOffset(float xDelta)
	{
		float finalXPoint;

		if (content.transform.localPosition.x - xDelta < leftBound)
		{
			finalXPoint = leftBound;
		}
		else
		{
			finalXPoint = content.transform.localPosition.x - xDelta;
		}

		CommonTransform.setX(content.transform, finalXPoint);
	}

	public virtual void AnimateToXOffset(float xDelta, float animationDuration)
	{
		// Don't animate if we're already at the limit.
		bool shouldAnimate = !(content.transform.localPosition.x - xDelta < leftBound);

		finalAnimiationPos = content.transform.localPosition;
		finalAnimiationPos.x -= xDelta;
		isAnimation = true;
		momentum = TRANSITION_SPEED;
	}

	// This assumes top to bottom
	public void scrollToVerticalPosition(float newYPosition, float tansitionSpeedOverride = -TRANSITION_SPEED, bool isForced = false)
	{
		isAnimationForced = isForced;
		isAnimation = true;
		finalAnimiationPos.y = newYPosition + content.transform.localPosition.y;
		momentum = tansitionSpeedOverride;
	}

	public void scrollToAbsoluteVerticalPosition(float newYPosition, float tansitionSpeedOverride = -TRANSITION_SPEED, bool isForced = false)
	{
		isAnimationForced = isForced;
		isAnimation = true;
		finalAnimiationPos.y = newYPosition;
		momentum = tansitionSpeedOverride;
	}

	// This is left to right
	public void scrollToHorizantalPosition(float newXPosition, float tansitionSpeedOverride = TRANSITION_SPEED, bool isForced = false)
	{
		isAnimationForced = isForced;
		isAnimation = true;
		finalAnimiationPos.x = newXPosition + content.transform.localPosition.x;
		momentum = tansitionSpeedOverride;
	}

	public virtual void safleySetXLocation(float x)
	{
		float xDifference = content.transform.localPosition.x;
		CommonTransform.setX(content.transform, x);
		if (content.leftPosition < leftBound)
		{
			CommonTransform.setX(content.transform, leftBound + TRANSITION_SPEED);
		}
		else if(content.rightPosition > rightBound)
		{
			CommonTransform.setX(content.transform, rightBound - TRANSITION_SPEED);
		}
	}
	
	public virtual void safleySetYLocation(float y)
	{
		float yDifference = content.transform.localPosition.y;
		CommonTransform.setY(content.transform, y);
		if (content.topPosition > topBound)
		{
			CommonTransform.setY(content.transform, topBound + TRANSITION_SPEED);
		}
		else if (content.bottomPosition < bottomBound)
		{
			CommonTransform.setY(content.transform, bottomBound - TRANSITION_SPEED);
		}
	}


	// scroll to a position over time using an AnimationCurve to change _momentum
	// curve can be edited in Inspector for ease in/out fx
	public void scrollWithAnimationCurve(float endX, float endY, float duration)
	{
		_momentum = 0;
		useAnimationCurve = true;
		isAnimation = true;
		curveTime = 0;
		curveDuration = duration;
		curveStart = finalAnimiationPos = content.transform.localPosition;

		// check bounds
		finalAnimiationPos.x = Mathf.Clamp(endX, leftBound, rightBound);
		finalAnimiationPos.y = Mathf.Clamp(endY, bottomBound, topBound);	
	}

	public void addMomentum(float delta)
	{
		momentum += delta;
	}

	public void preventScrolling()
	{
		isAnimationForced = true;
		isAnimation = true;
		_momentum = 0;
	}

	public void enableScrolling()
	{
		isAnimationForced = false;
		isAnimation = false;
	}

	protected float _momentum = 0;
	protected float momentum
	{
		get
		{
			return _momentum;
		}
		set
		{
			float delta = value - _momentum;
			_momentum += (delta * momentumModifier);
			if (maxMomentum > 0)
			{
				// Momentum can go in either direction.
				_momentum = Mathf.Clamp(_momentum, -maxMomentum, maxMomentum);
			}
		}
	}

}
