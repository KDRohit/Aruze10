using UnityEngine;
using System.Collections;

//This moduel that will play an ambient scene effect, see gmg01 BaseGame
[HelpURL("https://wiki.corp.zynga.com/display/hititrich/Ambient+Effects+Module")]
public class AmbientEffectModule : SlotModule
{
	[SerializeField] private Animator ambientEffect = null;
	[SerializeField] private float minInterval = 1.5f;             // Minimum time an animation might take to play next
	[SerializeField] private float maxInterval = 4.0f;             // Maximum time an animation might take to play next	
	[SerializeField] private string AMBIENT_SOUND = "";
	[SerializeField] private string AMBIENT_ANIMATION_NAME = "ambient";

	private CoroutineRepeater ambientAnimationController;

	public override void Awake()
	{
		base.Awake();
		ambientAnimationController = new CoroutineRepeater(minInterval, maxInterval, animationCallback);
	}

	private void Update()
	{
		ambientAnimationController.update();
	}

	protected virtual IEnumerator animationCallback()
	{		
		if (ambientEffect != null && ambientEffect.isActiveAndEnabled)
		{				
			if (AMBIENT_SOUND != "" && !Audio.isPlaying(AMBIENT_SOUND))
			{
				Audio.play(AMBIENT_SOUND);
			}
			yield return StartCoroutine(CommonAnimation.playAnimAndWait(ambientEffect, AMBIENT_ANIMATION_NAME));
		}
	}
}
