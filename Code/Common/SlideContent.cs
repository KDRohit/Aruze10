using UnityEngine;
using System.Collections;

public class SlideContent : MonoBehaviour
{
	public enum Justification
	{
		TOP,
		LEFT,
		CENTER,
		RIGHT,
		BOTTOM
	}

	public Justification justified;

	public float width;
	public float height;

	private float _position;

	public float topPosition
	{
		get
		{
			_position = transform.localPosition.y;
			switch(justified)
			{
				case Justification.TOP:
					return _position;
				case Justification.BOTTOM:
					return _position + height;
				case Justification.CENTER:
					return _position + (height/2);
			}
			return _position;
		}
	}

	public float bottomPosition
	{
		get
		{
			_position = transform.localPosition.y;
			switch(justified)
			{
				case Justification.TOP:
					return _position - height;
				case Justification.BOTTOM:
					return _position;
				case Justification.CENTER:
					return _position - (height/2);
			}
			return _position;
		}
	}	
	
	public float leftPosition
	{
		get
		{
			_position = transform.localPosition.x;
			switch (justified)
			{
			case Justification.LEFT:
				return _position;
			case Justification.RIGHT:
				return _position - width;
			case Justification.CENTER:
				return _position - (width / 2);
			}
			return _position;
		}
	}

	public float rightPosition
	{
		get
		{
			_position = transform.localPosition.x;
			switch (justified)
			{
			case Justification.LEFT:
				return _position + width;
			case Justification.RIGHT:
				return _position;
			case Justification.CENTER:
				return _position + (width / 2);
			}
			return _position;
		}
	}

}