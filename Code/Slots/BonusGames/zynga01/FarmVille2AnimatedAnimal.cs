using UnityEngine;
using System.Collections;

/**
Handles playing animations for all of the animals for the FarmVille2 game
*/
public class FarmVille2AnimatedAnimal : TICoroutineMonoBehaviour 
{
	private const string IDLE_POSE_ANIM_NAME = "icon_pose";					// Animation of the animal just standing still
	private const string HAPPY_ANIM_NAME = "happy";							// Name for the happy jumping animations of the animals
	private const string IDLE_ANIM_NAME = "idle";							// Animation for the animal to be standing idle
	private const string FIDGET_ANIM_NAME = "fidget_A";						// Animaiton for the animal to fidget, used for pick me animation
	private const string SHADOW_JUMP_ANIM_NAME = "shadow_jump";				// Animation to play for the shadow when the animal jumps
	private const string SCALE_UP_ANIM_NAME = "scale_up";					// Animation to scale up a new age state of an animal
	private const string NOT_SCALING = "not_scaling";						// Animation to set by default to stop the scaling.
	private const string PICK_ME_SCALE_ANIM = "child_pick_me_scale";		// Animation to attract the user to pick this animal

	private static bool IS_USING_FIDGET_ANIM_FOR_PICK_ME = true;			// Use this to control whether a scale or fidget animation is used for the pick me anim
																			// Don't use const because it causes compiler warnings when false.
	public enum AnimalAgeEnum {Child = 0, Adult};

	[HideInInspector] public bool disablePickMeAnim = false;				// Flag which can be set by the pickem game to tell objects to cancel animating if reveals are starting 

	[SerializeField] private Animator childAnimator = null;					// Child visuals for the animal
	[SerializeField] private Animator adultAnimator = null;					// Adult visuals for the animal
	[SerializeField] private ParticleSystem dustPoof = null;				// Dust poof particle effect for changing animal age
	[SerializeField] private AnimalAgeEnum animalAge = AnimalAgeEnum.Child;	// Controls what visual is used to represent the animal
	
	private Animator curAgeAnimator = null;		// Reference to the object representing the age of the animal
	private Animation animalAnimations = null; 	// Animations for the animal like static idle and jumping, dynamically set based on which age of the animal is being used
	private Animator shadowMeshAnimator = null;	// Shadow mesh animator, animates when the animal is in a jump anim, dynamically set based on which age of the animal is being used
	private Animator animalMeshAnimator = null;	// The mesh animator for the animal, used for a fade animation, dynamically set based on which age of animal is being used 

	private bool isPlayingFidgetAnim = false;	// Tracks if the animal is playing its fidget animaiton

	private bool isLoadedCorrectly = true;		// Tracks if the animal loaded correctly, serves as a fail safe so that animal animations can be ignored if it will cause exceptions

	private void Awake() 
	{
		// if the child or adult objects are null, try to grab them through code
		if (childAnimator == null)
		{
			childAnimator = tryGetAgeAnimator(AnimalAgeEnum.Child);
		}

		if (adultAnimator == null)
		{
			adultAnimator = tryGetAgeAnimator(AnimalAgeEnum.Adult);
		}

		if (childAnimator == null || adultAnimator == null)
		{
			// missing animation, tryGetAgeAnimtor() will have logged an error about which Animator is missing
			isLoadedCorrectly = false;
			return;
		}

		else
		{
			// setup whatever initial state this animal is in
			changeAnimalAge(animalAge);
		}
	}

	/**
	Try to grab the animal age animator object if it wasn't set in the editor
	*/
	private Animator tryGetAgeAnimator(AnimalAgeEnum age)
	{
		Animator ageAnimator = null;
		GameObject searchedObject = CommonGameObject.findChild(gameObject, age.ToString());
		if (searchedObject != null)
		{
			ageAnimator = searchedObject.GetComponent<Animator>();
		}

		if (ageAnimator == null)
		{
			Debug.LogError("Animal named: " + gameObject.name + " is missing a " + age.ToString() + " Animator object!");
			isLoadedCorrectly = false;
		}

		return ageAnimator;
	}

	/**
	Change the animals age, and reassign the animation variables to reference the ones for that age
	*/
	public void changeAnimalAge(AnimalAgeEnum age, bool skipScaleAnim = true)
	{
		if (isLoadedCorrectly)
		{
			animalAge = age;
			
			switch (age)
			{
				case AnimalAgeEnum.Child:
					curAgeAnimator = childAnimator;
					break;
				case AnimalAgeEnum.Adult:
					curAgeAnimator = adultAnimator;
					break;
				default:
					Debug.LogError("Don't know how to handle AnimalAgeEnum = " + age.ToString());
					isLoadedCorrectly = false;
					break;
			}

			// make sure that the age object was loaded correctly before trying to grab stuff inside of it

			shadowMeshAnimator = CommonGameObject.findChild(curAgeAnimator.gameObject, "Shadow Mesh").GetComponent<Animator>();

			GameObject animalAnimationObject = CommonGameObject.findChild(curAgeAnimator.gameObject, "Animal Animations");
			animalAnimations = animalAnimationObject.GetComponent<Animation>();
			animalMeshAnimator = CommonGameObject.findChild(animalAnimationObject, "Animal Mesh").GetComponent<Animator>();

			if (skipScaleAnim)
			{
				// start the animal at full size
				curAgeAnimator.gameObject.transform.localScale = Vector3.one;
			}
			else
			{
				// ensure animal is totally scaled down for the start of the scaling animation
				curAgeAnimator.gameObject.transform.localScale = Vector3.zero;
			}

			switch (age)
			{
				case AnimalAgeEnum.Child:
					childAnimator.gameObject.SetActive(true);
					adultAnimator.gameObject.SetActive(false);
					break;
				case AnimalAgeEnum.Adult:
					adultAnimator.gameObject.SetActive(true);
					childAnimator.gameObject.SetActive(false);
					break;
				default:
					Debug.LogError("Don't know how to handle AnimalAgeEnum = " + age.ToString());
					break;
			}
		}
	}

	/**
	Play the jump animation for the animal, including the shadow animation which goes with it
	*/
	public IEnumerator playJumpAnimation()
	{
		if (isLoadedCorrectly)
		{
			// cancel a pick me animation if it's playing
			if (animalAge == AnimalAgeEnum.Child && !disablePickMeAnim)
			{
				childAnimator.Play(NOT_SCALING);
				childAnimator.gameObject.transform.localScale = Vector3.one;
			}
			disablePickMeAnim = true;

			Debug.Log("playJumpAnimation");
			animalAnimations.Play(HAPPY_ANIM_NAME);
			shadowMeshAnimator.Play(SHADOW_JUMP_ANIM_NAME);

			while (animalAnimations.isPlaying)
			{
				yield return null;
			}

			// switch back to idle
			animalAnimations.Play(IDLE_ANIM_NAME);
		}
	}

	/**
	Play the dust poof effect for transitioning from one 
	*/
	private IEnumerator playDustPoof()
	{
		dustPoof.gameObject.SetActive(true);
		dustPoof.Play();

		while (dustPoof.isPlaying)
		{
			yield return null;
		}

		dustPoof.gameObject.SetActive(false);
		dustPoof.Stop();
		dustPoof.Clear();
	}

	/**
	Coroutine that plays the growup animation which transitions between child and adult, could want to add in one more stage 
	if these animals can be shared with the reel symbols
	*/
	public IEnumerator playGrowAnimalTo(AnimalAgeEnum age)
	{
		if (isLoadedCorrectly)
		{
			yield return StartCoroutine(playJumpAnimation());

			// hide the current age object before we do the dust poof
			curAgeAnimator.gameObject.SetActive(false);

			StartCoroutine(playDustPoof());

			// change to the new age for the animal
			changeAnimalAge(age, false);

			// scale this new age in
			curAgeAnimator.Play(SCALE_UP_ANIM_NAME);
		}
	}

	/**
	Play the growup animation which transitions between child and adult, could want to add in one more stage 
	if these animals can be shared with the reel symbols
	*/ 
	public void growAnimalTo(AnimalAgeEnum age)
	{
		if (isLoadedCorrectly)
		{
			StartCoroutine(playGrowAnimalTo(age));
		}
	}

	/**
	Turn an animal to monochrome grey, used for unpicked reveals
	*/
	public void greyAnimalOut()
	{
		if (isLoadedCorrectly)
		{
			// store out the texture that we want to use with the monochrome shader
			Texture mainFadeTexture = animalMeshAnimator.gameObject.GetComponent<Renderer>().material.GetTexture ("_StartTex");

			// change the shader to monochrome
			animalMeshAnimator.gameObject.GetComponent<Renderer>().material.shader = ShaderCache.find("Unlit/GUI Texture Monochrome");

			// copy the texture back into the monochrome's primary texture
			animalMeshAnimator.gameObject.GetComponent<Renderer>().material.SetTexture("_MainTex", mainFadeTexture);
		}
	}

	/**
	Play the child animal's pick me animation
	*/
	public void playChildPickMeAnimation()
	{
		// only play animation if stuff loaded correctly, and the animal is still a child and can be picked
		if (isLoadedCorrectly && animalAge == AnimalAgeEnum.Child && !disablePickMeAnim)
		{
			if (IS_USING_FIDGET_ANIM_FOR_PICK_ME)
			{
				StartCoroutine(playAnimalFidgetAnimation());
			}
			else
			{
				childAnimator.Play(PICK_ME_SCALE_ANIM);
			}
		}
	}

	/**
	Play the fidget animation for the animal (children only since this is a pick me animation)
	*/
	private IEnumerator playAnimalFidgetAnimation()
	{
		// make sure everything is loaded correctly and the animal is a child since only children can fidget
		if (isLoadedCorrectly && animalAge == AnimalAgeEnum.Child && !disablePickMeAnim)
		{
			isPlayingFidgetAnim = true;

			// only fidget once
			animalAnimations[FIDGET_ANIM_NAME].wrapMode = WrapMode.Once;
			animalAnimations.Play(FIDGET_ANIM_NAME);
			
			// wait on the animation before marking the animation as not playing anymore
			while (animalAnimations.isPlaying)
			{
				yield return null;
			}

			isPlayingFidgetAnim = false;

			// switch back to idle only if the pick me hasn't been disabled while waiting for the animatio to end
			if (!disablePickMeAnim)
			{
				animalAnimations.Play(IDLE_ANIM_NAME);
			}
		}
	}

	/**
	Tells if this animal is currently running a child pick me animation
	*/
	public bool isPlayingChildPickMeAnimation()
	{
		if (IS_USING_FIDGET_ANIM_FOR_PICK_ME)
		{
			return !disablePickMeAnim && isPlayingFidgetAnim;
		}
		else
		{
			return !disablePickMeAnim && childAnimator.GetCurrentAnimatorStateInfo(0).IsName(PICK_ME_SCALE_ANIM) == false;
		}
	}
}
