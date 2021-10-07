using UnityEngine;
using System.Collections;

public class ZombieHuntZombie : MonoBehaviour
{
	public Animator captionAnim;
	public GameObject visuals;
	public GameObject effectAnchor;	// for the reveal effect and the score label
	public UITexture gameOverTexture;
	public UILabel scoreLabel;	// To be removed when prefabs are updated.
	public LabelWrapperComponent scoreLabelWrapperComponent;

	public LabelWrapper scoreLabelWrapper
	{
		get
		{
			if (_scoreLabelWrapper == null)
			{
				if (scoreLabelWrapperComponent != null)
				{
					_scoreLabelWrapper = scoreLabelWrapperComponent.labelWrapper;
				}
				else
				{
					_scoreLabelWrapper = new LabelWrapper(scoreLabel);
				}
			}
			return _scoreLabelWrapper;
		}
	}
	private LabelWrapper _scoreLabelWrapper = null;
	
}

