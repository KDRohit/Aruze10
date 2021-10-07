using UnityEngine;
using System.Collections;

public class ZombieHuntShotgunShell : TICoroutineMonoBehaviour
{
	public Animator baseAnim;
	public Animator highlightAnim;
	public Animator explosionAnim;
	public UILabel label;	// To be removed when prefabs are updated.
	public LabelWrapperComponent labelWrapperComponent;

	public LabelWrapper labelWrapper
	{
		get
		{
			if (_labelWrapper == null)
			{
				if (labelWrapperComponent != null)
				{
					_labelWrapper = labelWrapperComponent.labelWrapper;
				}
				else
				{
					_labelWrapper = new LabelWrapper(label);
				}
			}
			return _labelWrapper;
		}
	}
	private LabelWrapper _labelWrapper = null;
	

	public bool fired = false;
}

