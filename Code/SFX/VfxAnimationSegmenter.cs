using UnityEngine;
using System.Collections;

/**
 * Splits an VisualEffectComponent into segments that are specified in editor
 * Play times are in seconds. For instance, playTimes = [1, 2, 3]
 * would break the vfx into 3 sections that would each play for a second then pause until started again.
 * To play the next segment on this component you call:
 * segmenter.PlayNextSegment();
 */
public class VfxAnimationSegmenter : TICoroutineMonoBehaviour
{
	
	public float[] playTimes;
	
	private VisualEffectComponent vfx;
	private int currentSegment = 0;
	private float currentTime = 0;
	
	void Start ()
	{
		vfx = GetComponent<VisualEffectComponent>();
	}
	
	void Update ()
	{
		if(vfx == null) return;
		
		if(currentSegment >= playTimes.Length) return;
		
		if(vfx.IsPlaying && !vfx.IsPaused)
		{
			currentTime += Time.deltaTime;
			if(currentTime >= playTimes[currentSegment])
			{
				vfx.Pause();
				currentSegment++;
			}
		}
	}
	
	public void PlayNextSegment()
	{
		vfx.Resume();
	}
	
	public void Reset()
	{
		currentSegment = 0;
		currentTime = 0;
	}
}
