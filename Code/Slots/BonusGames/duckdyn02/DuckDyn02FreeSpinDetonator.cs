using UnityEngine;
using System.Collections;

/**
Handles the detonators in the duckdyn02 free spin intro pick
*/
public class DuckDyn02FreeSpinDetonator : TICoroutineMonoBehaviour 
{
	[SerializeField] private Animation plungerAnim = null;			// Plunger drop animation
	[SerializeField] private Animator fuseAnim = null;				// Fuse animation
	[SerializeField] private Animation pickMeAnim = null;			// Pick me attention getter animation
	[SerializeField] private Renderer revealRenderer = null;		// The renderer that will show the reveal image
	[SerializeField] private UISprite detonatorSprite = null;		// The sprite for the detonator, grayed out during reveal
	[SerializeField] private UISprite plungerSprite = null;			// The sprite for the plunger, grayed out during reveal

	public bool isRevealed
	{
		get { return _isRevealed; }
	}
	private bool _isRevealed = false;								// tells if this detonator has been revealed (i.e. picked or shown) yet

	public const float REVEAL_GROW_TIME = 0.3f;									// Time it takes for the reveal image to grow to full size
	public const float FUSE_ANIM_LENGTH = 1.166f;								// Length of the fuse animation
	private const string FUSE_LIT_ANIM = "fuse_lit";							// Fuse aniamtion name
	private const string DETONATOR_SOUND = "PickADetonatorRevealSymbol";		// Sound made when the detonator is picked

	/// Play the detonation animation of the plunger dropping and the fuse burning
	public IEnumerator playDetonatorAnimations()
	{
		pickMeAnim.Stop();

		_isRevealed = true;

		Audio.play(DETONATOR_SOUND);

		plungerAnim.Play();

		// wait for the plunger to drop
		while (plungerAnim.isPlaying)
		{
			yield return null;
		}

		// now play the fuse
		fuseAnim.Play(FUSE_LIT_ANIM);
		yield return new TIWaitForSeconds(FUSE_ANIM_LENGTH);

		fuseAnim.gameObject.SetActive(false);
	}

	/// Gray out the detonator, for unpicked detonators
	public void grayOut()
	{
		detonatorSprite.color = Color.gray;
		plungerSprite.color = Color.gray;
	}

	/// Play the attention getting pick me animation
	public IEnumerator playPickAnimation()
	{
		pickMeAnim.Play();

		// wait for the pick me animation to finish
		while (pickMeAnim.isPlaying)
		{
			yield return null;
		}
	}

	/// Reveals using the passed in material
	public IEnumerator playRevealAnimation(Material material)
	{
		_isRevealed = true;

		// set the material to the reveal
		revealRenderer.material = material;
		revealRenderer.material.color = Color.gray;
		
		yield return new TITweenYieldInstruction(iTween.ScaleTo(revealRenderer.gameObject, iTween.Hash("scale", new Vector3(250.0f, 250.0f, 0.0f), "time", REVEAL_GROW_TIME, "easetype", iTween.EaseType.easeInCubic, "islocal", true)));
	}
}
