using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Controls flipbook style animation of UISprites.
Sprite names must use the convention "baseSpriteName FrameNumber".
*/

[ExecuteInEditMode]
public class UISpriteAnimator : TICoroutineMonoBehaviour
{
	public UISprite sprite = null;
	public string spriteBaseName = "";

	// Data structure to allow mapping any frame of animation
	// in any order, so flipbook animations don't have to be
	// linear. Frames may be reused in an animation sequence.
	// Must be the format "<image frame>.<frame count>".
	// Using an array of strings here instead of FrameMap objects
	// because it's much easier to see and enter all the data into
	// an array of strings than another structure with multiple properties.
	public string[] frameData;

	public float durationSeconds = 1f;	// How long to spread the animation out over.		
	public float fadeOutSeconds = 0f;	// Only applies if non-looping.
	public bool playUponAwake = false;
	public int loopCount = 1;			// Number of times it loops before ending, or 0 for infinite.
	
	private int loopsRemaining = 0;
	private bool isPlaying = false;
	private List<FrameMap> frameMap = new List<FrameMap>();

	private int frameCount
	{
		get
		{
			if (_frameCount > -1 && Application.isPlaying)
			{
				// At runtime, only parse the frame data once.
				return _frameCount;
			}
			
			_frameCount = UISpriteAnimator.updateFrameMap(frameMap, frameData, gameObject);
			
			return _frameCount;
		}
	}
	private int _frameCount = -1;
	
	void Awake()
	{
		if (Application.isPlaying)
		{
			if (sprite == null || frameCount <= 0)
			{
				enabled = false;
			}
			
			if (playUponAwake)
			{
				StartCoroutine(play());
			}
		}
	}
	
	void Update()
	{
		if (sprite == null)
		{
			sprite = GetComponent<UISprite>();
		}
		
		if (sprite == null || frameCount <= 0)
		{
			return;
		}
	}
	
	// Plays the animation.
	public IEnumerator play()
	{
		if (isPlaying || sprite == null || frameCount <= 0)
		{
			// Prevent double-playing or playback if not configured correctly.
			yield break;
		}
		
		isPlaying = true;
		
		loopsRemaining = 1;
		if (loopCount > 0)
		{
			loopsRemaining = loopCount;
		}
		
		// Flatten the frame map once before animating, for performance during animation.
		List<int> frames = UISpriteAnimator.buildFrameList(frameMap);
		
		sprite.alpha = 1f;
		
		float elapsed = 0f;
				
		while (elapsed < durationSeconds)
		{
			int frameNo = Mathf.FloorToInt(elapsed / durationSeconds * frames.Count);
			if (frameNo >= frames.Count - 1)
			{
				// Finished a loop.
				if (loopCount > 0)
				{
					loopsRemaining--;
				}
				
				if (loopsRemaining == 0)
				{
					// If finished playing, make sure the last frame is used.
					frameNo = frames.Count - 1;
					// Make sure the playback ends.
					elapsed = durationSeconds;
				}
				else
				{
					// There is another loop to play, so alter the frame number so it loops around to the start.
					frameNo = frameNo % frames.Count;
					// Reset the elapsed timer so it will keep playing.
					elapsed = 0;
				}
			}
						
			sprite.spriteName = string.Format("{0} {1}", spriteBaseName, frames[frameNo]);
			sprite.MakePixelPerfect();
			elapsed += Time.deltaTime;
			yield return null;
		}
		
		// Make sure the last frame is displayed before fading out.
		sprite.spriteName = string.Format("{0} {1}", spriteBaseName, frames[frames.Count - 1]);
		sprite.MakePixelPerfect();
		
		// Fade out over time.
		elapsed = 0f;
		while (elapsed < fadeOutSeconds)
		{
			sprite.alpha = (1f - (elapsed / fadeOutSeconds));
			elapsed += Time.deltaTime;
			yield return null;
		}
		
		// Make sure it's totally faded.
		sprite.alpha = 0;
		
		isPlaying = false;
	}

	/// Static function to update a frame map, used here and in the custom inspector class, returns the number of frames
	public static int updateFrameMap(List<FrameMap> frameMap, string[] frameData, GameObject gameObject)
	{
		int frameCount = 0;

		if (frameMap != null)
		{
			frameMap.Clear();

			foreach (string rawData in frameData)
			{
				string data = rawData.Trim();
				if (data != "")
				{
					string[] parts = data.Split('.');
				
					FrameMap map = new FrameMap();
					try
					{
						map.frame = int.Parse(parts[0]);
						map.displayCount = int.Parse(parts[1]);
						
						frameMap.Add(map);
						frameCount += map.displayCount;
					}
					catch
					{
						Debug.LogError("Invalid data entered for frame data: " + data + ". Must be in the format \"<frame no>.<display count>\"", gameObject);
					}
				}
			}
		}

		return frameCount;
	}

	/// Static function to build the list of frames used when animating, used here and in the custom inspector class
	public static List<int> buildFrameList(List<FrameMap> frameMap)
	{
		List<int> frames = new List<int>();
		foreach (FrameMap map in frameMap)
		{
			for (int i = 0; i < map.displayCount; i++)
			{
				frames.Add(map.frame);
			}
		}

		return frames;
	}
	
	// Data structure to allow mapping any frame of animation
	// in any order, so flipbook animations don't have to be
	// linear. Frames may be reused in an animation sequence.
	public struct FrameMap
	{
		public int frame;
		public int displayCount;	// The number of times in the animation sequence to show this given frame.
	}
}
