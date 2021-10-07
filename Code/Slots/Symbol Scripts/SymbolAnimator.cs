#if UNITY_EDITOR
	#define DEBUG_BOUNDS
#endif

using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;

/**
Lives on a symbol prefab instance, acting as an anchor point for various visual effects.
ALL SYMBOLS ANIMATIONS ARE SHARED ACROSS ALL GAMES, DO NOT ALLOW GAME-SPECIFIC SYMBOL ANIMATIONS!
Games can all pick from the common pool of available animations.
*/
public class SymbolAnimator : TICoroutineMonoBehaviour
{
	public const int BASE_SYMBOL_RENDER_QUEUE_VALUE = 3000;
	private const int CUSTOM_SYMBOL_ANIMATION_QUEUE_INCREASE = 100;

	private const float DEFAULT_QUAD_SCALE = 0.44f;
	private const float STATIC_THROB_SCALE = DEFAULT_QUAD_SCALE * 1.3f;
	private const float STATIC_THROB_DURATION = 0.8f;
	private static Vector3 DEFAULT_QUAD_SCALE_VEC3 = new Vector3(DEFAULT_QUAD_SCALE, DEFAULT_QUAD_SCALE, 1f);
	
	[SerializeField] protected SkinnedMeshRenderer skinnedRenderer;		// Direct link to associated renderer
	[SerializeField] protected MeshRenderer staticRenderer;				// The renderer for the static quad.
	public GameObject scalingSymbolPart;				// The part of the symbol which will take on the SymbolInfo scaling, basically sits below the root Symbol object so scaling is only applied to certain objects
	public GameObject symbolAnimationRoot;              // Root for all things that will be tied directly to the symbol animation, like wild overlays that need to animate at the same time for instance
	public SymbolLayerReorganizer symbolReorganizer = null;
	public bool isSkippingAnimations;					// Sometimes, like when a banner covers up a reel of symbols, we don't want to actually animate, use this switch for those cases, resets every time a symbol is activated

	[HideInInspector] public SymbolInfo info;			// Basic info about the symbol animation usage
	[System.NonSerialized] public string symbolInfoName; // Name used when creating this SymbolAnimator, has to be tracked since SymbolInfo can contain a list of names
	[HideInInspector] public Texture2D wildTexture;		// The texture to use for overlaying WILD
	[HideInInspector] public GameObject wildOverlayGameObjectPrefab; // Prefab to instantiate GameObject version of the overlaying WILD
	[HideInInspector] public bool wildHidesSymbol;		// Flag to tell if the wild overlay texture should hide the symbol behind it to prevent strange overlapping
	[HideInInspector] public bool disableWildOverlayGameObject;	// We don't always want to instantiate a wild overlay over a wild symbol (Mainly if it already has a wild animation)
	[HideInInspector] public Vector3 positionOffset;	// Positional offset for symbol
	[HideInInspector] public Vector3 scaleOffset;		// Scaling offset for symbol
	[HideInInspector] public bool skipSymbolCaching = false;	// This is here so that if you dirty a symbol such that it should no longer be cached you can mark it as such

	[HideInInspector] public bool isMutatedToWD = false; // Check if this symbol has been mutated to look like a WD, since it keeps it's other properties
	[System.NonSerialized] public bool isFlattened = false;

	public bool isAnimating { get; protected set; }	// Is this symbol currently animating?
	public bool isWildShowing { get; protected set; }	// Is the symbol curently showing as a WILD?

	// set to false when the animation is done. Or else the symbol may not get deallocated.
	public bool isTumbleSquashAndStretching = false;

	protected SlotSymbol slotSymbol = null;
	private string mutateName = "";
	protected VisualEffectComponent vfx;
	public GameObject symbolGO = null;					// The Gameobject that holds the the instantiated prefab from Slotinfo for this symbol.
	private GameObject wildOverlayGameObject; 			// GameObject version of the overlaying WILD
	private Dictionary<Material, float> gameObjectAlphaMap = null;	// Maps the alpha values of the materials on symbolGO before they are modified by a fading function
	private AnimatingDelegate animatingCallback = null;
	private bool isFadingOverTime = false; // Track if this animator is fading a symbol in/out over time to prevent further fade calls from triggering on it

	public Material material { get; protected set; }

	[SerializeField] protected SpecialRenderQueueData[] renderQueueData; //special data for setting render queue values for particular game objects within this symbol

	private Texture storedMainSymbolTexture = null;		// Used for cases where the main symbol texture is hidden when the wild overlay happens
	[System.NonSerialized] public TICoroutine fadingCoroutine = null; //Used so we can stop symbols mid-fade and properly restore them

	private LabelWrapperComponent _dynamicLabel = null; //Label used on symbols for text that is dynamically changed (eg. Scatter feature in Slingo01 for server credit values)
	private LabelWrapperComponent[] _allLabels = null; // Cache all labels on the animator for changing alphas

	// Cached Animaiton and Rendering data
	protected bool isAnimationAndRenderingDataCached = false;
	private List<Animation> symbolAnimations = null;
	private List<Animator> symbolAnimators = null;
	private List<ParticleSystem> symbolParticleSystems = null;
	private List<Renderer> symbolRenderers = new List<Renderer>();
	private List<Renderer> symbolRenderersToRemove = new List<Renderer>(); // We need to track the renderers that are destroyed when the object is created to ensure we don't hang onto them (because they will still try to be cached when they aren't destroyed till the next frame)


	[System.Serializable]
	public class SpecialRenderQueueData
	{
		public GameObject specialRenderQueueObject;
		public int specialRenderQueueIncrementValue;
	}

	/// Is this symbol doing... anything?
	public bool isDoingSomething
	{
		get
		{
			return
				isAnimating ||
				isWildShowing ||
				isMutating ||
				isTumbleSquashAndStretching ||
				(vfx != null && vfx.IsPlaying)
				;
		}
	}
	
	/// Is this symbol doing a mutation?
	public bool isMutating { get; protected set; }
	
	/// Is this symbol active?
	/// When changing this state, it will enable/disable the children renderer's as appropriate.
	public bool isSymbolActive
	{
		get
		{
			return _isSymbolActive;
		}
	}
	// Since this can be costly, we set via a function call instead of property setter
	protected void setIsSymbolActive(bool active)
	{
		if (_isSymbolActive != active)
		{
			_isSymbolActive = active;
			updateSymbolRendererVisibility();
		}
	}
	private bool _isSymbolActive = false;


	// Get/Set this Symbol's manually managed "culled" state
	// When changing this state, it will enable/disable the children renderer's as appropriate.
	public bool isCulled
	{
		get
		{
			return _isCulled;
		}
	}
	// Since this can be costly, it's a function instead of a property setter
	public void setIsCulled(bool culled)
	{
		if (_isCulled != culled)
		{
			_isCulled = culled;
			updateSymbolRendererVisibility();
		}
	}
	private bool _isCulled = false;


	// Track the skinnedRenderer enabled state here, instead of setting it directly
	public bool skinnedRendererEnabled
	{
		get 
		{ 
			return _skinnedRendererEnabled;
		}
		set
		{
			_skinnedRendererEnabled = value;
			updateSkinnedRendererEnabledState();
		}
	}
	private bool _skinnedRendererEnabled;

	// Track the staticRenderer enabled state here, instead of setting it directly
	public bool staticRendererEnabled
	{
		get 
		{
			return _staticRendererEnabled; 
		}
		set
		{
			_staticRendererEnabled = value;
			updateStaticRendererEnabledState();
		}
	}
	protected bool _staticRendererEnabled;

	// Helper to get the position without the symbol offset 
	// (useful for when you need the positioning of the symbol location
	// and not just the symbol with the offset included)
	public Vector3 getPositionWithoutSymbolInfoOffset(bool isLocal)
	{
		if (isLocal)
		{
			return transform.localPosition - info.positioning;
		}
		else
		{
			// we want the world position, so get the local and then convert it
			Vector3 localPosition = transform.localPosition - info.positioning;
			return transform.TransformPoint(localPosition);
		}
	}

	/// Positioning setter
	public Vector3 positioning
	{
		set
		{
			transform.localPosition = new Vector3
			(
				value.x + info.positioning.x, 
				value.y + info.positioning.y, 
				value.z + info.positioning.z
			);
		}
	}

	/// Scaling setter
	public Vector3 scaling
	{
		set
		{
			scalingSymbolPart.transform.localScale = new Vector3(
				value.x * info.scaling.x,
				value.y * info.scaling.y,
				value.z * info.scaling.z);
		}
	}

	[SerializeField] protected ResetBonePose resetBonePose;
	private bool isResetBonePoseSaved = false;

	/// The basic shaders used by symbols
	public static Shader defaultShader(string name = null)
	{
		if (string.IsNullOrEmpty(name))
		{
			name = "Unlit/GUI Texture";
		}
		if (_defaultShaders.ContainsKey(name))
		{
			return _defaultShaders[name];
		}
		else
		{
			Shader shader = ShaderCache.find(name);
			_defaultShaders.Add(name, shader);
			return shader;
		}
	}
	private static Dictionary<string, Shader> _defaultShaders = new Dictionary<string, Shader>();

	/// The overlay shader used by symbols
	public static Shader overlayShader
	{
		get
		{
			if (_overlayShader == null)
			{
				_overlayShader = ShaderCache.find("Unlit/GUI Texture Overlay (+100)");
			}
			return _overlayShader;
		}
	}
	private static Shader _overlayShader = null;

	/// The shader used if wild overlays are hiding the symbols underneath them
	public static Shader wildSymbolHideShader
	{
		get
		{
			if (_wildSymbolHideShader == null)
			{
				_wildSymbolHideShader = ShaderCache.find("Unlit/Special Fade Two (+100)");
			}
			return _wildSymbolHideShader;
		}
	}
	private static Shader _wildSymbolHideShader = null;
	
	/// Makes sure the symbol's default bone pose is perfectly in place
	void Start()
	{
		if (symbolAnimationRoot == null)
		{
			symbolAnimationRoot = gameObject;
		}

		if (scalingSymbolPart == null)
		{
			scalingSymbolPart = gameObject;
		}

		// Reset bones
		Animation scalingSymbolPartAnimation = scalingSymbolPart.GetComponent<Animation>();
		if (scalingSymbolPartAnimation != null)
		{
			scalingSymbolPartAnimation.Play("reset_anim", PlayMode.StopAll);
			scalingSymbolPartAnimation.Sample();
			scalingSymbolPartAnimation.Stop();
		}
			
		setupSpecialRenderQueue();
	}

	private SymbolLayerReorganizer getSymbolLayerReorganizer()
	{
		return symbolAnimationRoot.GetComponentInChildren<SymbolLayerReorganizer>();	  
	}

	protected void setupSpecialRenderQueue()
	{
		for (int i=0; i < renderQueueData.Length; i++)
		{
			Renderer[] renderers = renderQueueData[i].specialRenderQueueObject.GetComponentsInChildren<Renderer>(true);
			// Lets go though here and find the lowest renderer currently in the object so we can set the base level
			foreach (Renderer renderer in renderers)
			{
				foreach (Material material in renderer.materials)
				{
					if (material != null)
					{
						material.renderQueue += renderQueueData[i].specialRenderQueueIncrementValue;
					}
				}
			}
		}
	}

	public void setAnimatingDelegate(AnimatingDelegate callback)
	{
		animatingCallback = callback;
	}

	public LabelWrapperComponent getDynamicLabel()
	{
		return _dynamicLabel;
	}

	public LabelWrapperComponent[] getAllLabels()
	{
		return _allLabels;
	}


#if UNITY_EDITOR
	private int animationCallsThisFrame = 0;
	private string stackTraces = "";
#endif
	
	/// Reset the animation count on enable
	protected override void OnEnable()
	{
		Profiler.BeginSample("onEnable");

		base.OnEnable();

#if UNITY_EDITOR
		animationCallsThisFrame = 0;
		stackTraces = "";
#endif

		// Welcome to a special edition of Unity makes no sense and is kind of sort of buggy
		// On todays episode we set an animator speed value to what it already was because apparently Unity tries to do this internally,
		// but lets some time actually happen before doing so causing the animators to advance through their animations.
		// If we update Unity and find this is fixed we can take it out, till then though I'd suggest leaving it be.
		if (symbolGO != null)
		{
			if (symbolAnimators != null)
			{
				foreach (Animator animator in symbolAnimators)
				{
					if (animator == null)
					{
						continue;
					}
					else
					{
						animator.speed = animator.speed;
					}
				}
			}
		}

		Profiler.EndSample();
	}

#if UNITY_EDITOR
	/// Sanity check to make sure nobody does anything stupid with symbols.
	void Update()
	{
		if (animationCallsThisFrame > 1)
		{
			string msg = string.Format(
				"Multiple animation calls for symbol {0} at getSymbolPositionId() {1} on the same frame: count={2}{3}\n----END----",
				symbolInfoName,
				slotSymbol.getSymbolPositionId(),
				animationCallsThisFrame,
				stackTraces
			);
			
			stackTraces = "";
			
			Debug.LogError(msg, gameObject);
			//Debug.Break();
			
			// If you are encountering the above error, it means there is conflicting code telling this symbol to do
			// different things in the same frame. Please rework the code so that symbols aren't told to do conflicting
			// things in the same frame. Do not fix this by simply delaying a second call by a frame.
		}
		animationCallsThisFrame = 0;
		if (skinnedRenderer != null && material != null && staticRenderer != null && skinnedRenderer.sharedMaterial != staticRenderer.sharedMaterial && skinnedRenderer.gameObject.activeSelf)
		{
			Debug.LogError(string.Format("MATERIAL MISMATCH! {0} {1} {2}", material.GetInstanceID(), skinnedRenderer.sharedMaterial.GetInstanceID(), staticRenderer.sharedMaterial.GetInstanceID()), gameObject);
			//Debug.Break();
			
			// If you are encountering the above error, it means that you assumed there is only one renderer on a symbol.
			// There are either one or two renderers, depending on device type, which share a material.
			// Whatever you were trying to do can probably be done using the SymbolAnimator.material property.
		}
	}
	
	/// Used for editor-only checking of animation calls on symbols
	private void incrementAnimationCalls()
	{
		if (gameObject.activeInHierarchy)
		{
			animationCallsThisFrame++;

			stackTraces += string.Format(
				"\n> Call #{0}:\n{1}",
				animationCallsThisFrame,
				UnityEngine.StackTraceUtility.ExtractStackTrace()
			);
		}
	}
#endif
	
	/// Turns on a symbol (activates it), which may include some special startup code.
	/// Override this for special symbol prefab types.
	public virtual void activate(bool isFlattenedSymbol)
	{
		Profiler.BeginSample("SymAnim.activate");

		// Cancel all pending invoke calls just to be safe
		CancelInvoke();

		// These all set the backing variable directly, we refresh the renderVisibility states below
		_isSymbolActive = true;
		_isCulled = false;
		_staticRendererEnabled = true;
		_skinnedRendererEnabled = false;

		isFlattened = isFlattenedSymbol;
		isWildShowing = false;
		isMutating = false;
		isSkippingAnimations = false;
		// Make sure there isn't any roatation.
		transform.localEulerAngles = Vector3.zero;

		if (info.symbolPrefab == null)
		{
			// Save the quad transform info to restore it
			if (resetBonePose != null && !isResetBonePoseSaved)
			{
				resetBonePose.saveDefaultPose();
				isResetBonePoseSaved = true;
			}
		
			// Sync up the material(s)
			if (material == null)
			{
				if (skinnedRenderer != null)
				{
					if (Application.isPlaying)
					{
						material = skinnedRenderer.material;
					}
					else
					{
						// When making temporary symbols we don't want to spit out a ton of errors.
						Material tempMaterial = new Material(skinnedRenderer.sharedMaterial);
						material = tempMaterial;
					}
					staticRenderer.material = material;
				}
				else
				{
					material = staticRenderer.material;
				}
			}
			
			// Reset the shader to default.
			material.shader = defaultShader(info.shaderName);

			info.applyTextureToMaterial(material);
			_staticRendererEnabled = true; // sets the backing variable, will refresh below
		}
		else
		{
			// We want to attach the premade game object, for custom symbols art sends to help keep texture sizes low.
			if (symbolGO == null)
			{
				// Make the symbol from the predefined information.
				if (!isFlattenedSymbol)
				{
					symbolGO = CommonGameObject.instantiate(info.symbolPrefab) as GameObject;
				}
				else
				{
					symbolGO = CommonGameObject.instantiate(info.flattenedSymbolPrefab) as GameObject;
				}
				symbolGO.transform.parent = scalingSymbolPart.transform; 
				symbolGO.transform.localScale = transform.localScale;
				symbolGO.transform.localPosition = transform.localPosition;
				symbolGO.SetActive(true);
				symbolGO = symbolAnimationRoot;

				_allLabels = symbolGO.GetComponentsInChildren<LabelWrapperComponent>(true);
				if (_allLabels != null && _allLabels.Length > 0)
				{
					_dynamicLabel = _allLabels[0];
				}

				// if we're using a prefab grab the renderers, but destroy ones we aren't going to use
				initSymbolPrefabRenderers();

				// grab the symbol reorganizer here, so we have it even before the symbols are animating
				if (symbolReorganizer == null)
				{
					symbolReorganizer = getSymbolLayerReorganizer();
				}

				Collider[] colliders = symbolGO.GetComponentsInChildren<Collider>(true);
				foreach (Collider hitbox in colliders)
				{
					if (Application.isPlaying)
					{
						Destroy(hitbox);
					}
					else
					{
						DestroyImmediate(hitbox);
					}
				}
			}
		}

		// create a GameObject wild overlay if needed, ignore flattened symbols because they aren't going to animate
		
		if (wildOverlayGameObjectPrefab != null && !disableWildOverlayGameObject && !isFlattenedSymbol)
		{
			if (wildOverlayGameObject == null)
			{
				// Make the symbol from the predefined information.
				wildOverlayGameObject = CommonGameObject.instantiate(wildOverlayGameObjectPrefab) as GameObject;
				wildOverlayGameObject.transform.parent = symbolAnimationRoot.transform; 
				wildOverlayGameObject.transform.localPosition = transform.localPosition;
			}
		}
		
		if (!isAnimationAndRenderingDataCached)
		{
			cacheAnimationAndRenderingComponents();
			isAnimationAndRenderingDataCached = true;
		}

		if (symbolGO != null)
		{
			// Make sure no animations are playing.
			playCustomAnimation(false);
		}
		
		// Now that the symbol should be frozen we should be able to turn the object off
		// we don't want it off when we play the custom animation to freeze it though
		// otherwise it wouldn't work
		if (wildOverlayGameObject != null)
		{
			wildOverlayGameObject.SetActive(false);
		}
				
		if (!gameObject.activeSelf)
		{
			gameObject.SetActive(true);
		}

		if (skinnedRenderer != null)
		{
			scalingSymbolPart.GetComponent<Animation>().enabled = false;
			_skinnedRendererEnabled = false; // sets the backing variable, will refresh below
		}

		// Update all the symbol renderer's visibility states based on isSymbolActive, isCulled, etc.
		// (At this point in time, symbol should be considered active & non-culled)
		updateSymbolRendererVisibility(); 

		// ensure symbol is faded in, but ignore warning about the fade map if the symbol wasn't faded out, since we don't know if it was
		fadeSymbolInImmediate(false);
		
		// Make sure we're on the default reel layer
		if ( (FreeSpinGame.instance == null && SlotBaseGame.instance != null && SlotBaseGame.instance.engine is LayeredSlotEngine) || 
				 (FreeSpinGame.instance != null && FreeSpinGame.instance.engine is LayeredSlotEngine) )
		{
			CommonGameObject.setLayerRecursively(gameObject, Layers.ID_SLOT_FOREGROUND_REELS);
		}
		else if (FreeSpinGame.instance == null && SlotBaseGame.instance != null && SlotBaseGame.instance.engine.reelSetData.isIndependentReels)
		{
			if (transform.parent != null)
			{
				GameObject animatorParent = transform.parent.gameObject;
				CommonGameObject.setLayerRecursively(gameObject, animatorParent.layer);
			}
			else
			{
				CommonGameObject.setLayerRecursively(gameObject, Layers.ID_SLOT_REELS);
			}
		}
		else if (symbolGO != null)
		{
			// Don't do anything with the layering for now.
		}
		else
		{
			CommonGameObject.setLayerRecursively(gameObject, Layers.ID_SLOT_REELS);
		}

		scaling = Vector3.one; // Needed to init non-prefab based symbols (like elvira01)

		if (Glb.enableSymbolCullingSystem) // master kill-switch
		{
			initialBoundsInfo = setupBoundsAfterActivation();
		}

		Profiler.EndSample();
	}
	
	// If we add something to the animation root we need to re-cache
	// the rendering and animaiton components as both could have new things
	public void addObjectToSymbolAnimationRoot(GameObject objectToAdd)
	{
		objectToAdd.transform.parent = symbolAnimationRoot.transform;
		addObjectsAnimationsAndRenderingComponents(objectToAdd);
	}
	
	// If we add something to the root of the animator it will only affect
	// the renderer list, so only re-cache that
	public void addObjectToAnimatorObject(GameObject objectToAdd)
	{
		objectToAdd.transform.parent = gameObject.transform;
		// only add the renderers since this object isn't being added under the animation root
		addObjectsRenderingComponents(objectToAdd);
	}
	
	// Add the animation and rendering componenets of an object being added to this symbol
	private void addObjectsAnimationsAndRenderingComponents(GameObject objectToAdd)
	{
		Animation[] animationArray = objectToAdd.GetComponentsInChildren<Animation>(true);
		foreach (Animation anim in animationArray)
		{
			symbolAnimations.Add(anim);
		}
		
		Animator[] animatorArray = objectToAdd.GetComponentsInChildren<Animator>(true);
		foreach (Animator animator in animatorArray)
		{
			symbolAnimators.Add(animator);
		}
		
		ParticleSystem[] particleSystemArray = objectToAdd.GetComponentsInChildren<ParticleSystem>(true);
		foreach (ParticleSystem particleSys in particleSystemArray)
		{
			symbolParticleSystems.Add(particleSys);
		}

		addObjectsRenderingComponents(objectToAdd);
	}
	
	// Add the rendering componenets of an object which is being added to this symbol
	private void addObjectsRenderingComponents(GameObject objectToAdd)
	{
		Renderer[] rendererArray = objectToAdd.GetComponentsInChildren<Renderer>(true);
		foreach (Renderer objRenderer in rendererArray)
		{
			symbolRenderers.Add(objRenderer);
		}
	}
	
	// When an object is removed we need to re-cache everything to ensure
	// we have the correct stuff
	public void removeObjectFromSymbol(GameObject objectToRemove)
	{
		objectToRemove.transform.parent = null;
		// Try and remove everything, even if it wasn't put at the animation
		// root those removals will just fail since it isn't going to find
		// those in the cached lists
		removeObjectsAnimationsAndRenderingComponents(objectToRemove);
	}
	
	// Remove the cached animation and rednering componenets for an object that was
	// attached to the symbol and is now being removed
	private void removeObjectsAnimationsAndRenderingComponents(GameObject objectToRemove)
	{
		Animation[] animationArray = objectToRemove.GetComponentsInChildren<Animation>(true);
		foreach (Animation anim in animationArray)
		{
			symbolAnimations.Remove(anim);
		}
		
		Animator[] animatorArray = objectToRemove.GetComponentsInChildren<Animator>(true);
		foreach (Animator animator in animatorArray)
		{
			symbolAnimators.Remove(animator);
		}
		
		ParticleSystem[] particleSystemArray = objectToRemove.GetComponentsInChildren<ParticleSystem>(true);
		foreach (ParticleSystem particleSys in particleSystemArray)
		{
			symbolParticleSystems.Remove(particleSys);
		}
		
		Renderer[] rendererArray = objectToRemove.GetComponentsInChildren<Renderer>(true);
		foreach (Renderer objRenderer in rendererArray)
		{
			symbolRenderers.Remove(objRenderer);
		}
	}
	
	// Initialize the lists of animated and rendering elements that will be used for animation
	protected void cacheAnimationAndRenderingComponents()
	{
		if (symbolAnimationRoot != null)
		{
			symbolAnimations = new List<Animation>();
			Animation[] animationArray = symbolAnimationRoot.GetComponentsInChildren<Animation>(true);
			foreach (Animation anim in animationArray)
			{
				symbolAnimations.Add(anim);
			}

			symbolAnimators = new List<Animator>();
			Animator[] animatorArray = symbolAnimationRoot.GetComponentsInChildren<Animator>(true);
			foreach (Animator animator in animatorArray)
			{
				symbolAnimators.Add(animator);
			}

			symbolParticleSystems = new List<ParticleSystem>();
			ParticleSystem[] particleSystemArray = symbolAnimationRoot.GetComponentsInChildren<ParticleSystem>(true);
			foreach (ParticleSystem particleSys in particleSystemArray)
			{
				symbolParticleSystems.Add(particleSys);
			}
		}

		cacheRenderingComponents();
	}
	
	// Store out all the rendering components attached to this symbol
	private void cacheRenderingComponents()
	{
		symbolRenderers.Clear();
		Renderer[] rendererArray = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < rendererArray.Length; i++)
		{
			symbolRenderers.Add(rendererArray[i]);
		}

		// remove any renderers that were set to be removed (should only happen on initial creation for now)
		for (int i = 0; i < symbolRenderersToRemove.Count; i++)
		{
			bool result = symbolRenderers.Remove(symbolRenderersToRemove[i]);
		}
	}
	
	// Debug function to look at what is in a list of renderers
	private void outputCachedRendererList(string info)
	{
		string outputStr = "SymbolAnimator.outputCachedRendererList() - " + info + "; symbolRenderers = {\n";
		foreach (Renderer renderer in symbolRenderers)
		{
			if (renderer != null)
			{
				outputStr += renderer.name + ",\n";
			}
			else
			{
				outputStr += "<null>,\n";
			}
		}
		outputStr += "}";
		Debug.Log(outputStr);
	}

	// Update all the symbol renderer's visibility states based on isSymbolActive, isCulled, etc.
	// Actual enabling/disabling states depend on:
	//   If the symbol is currently Active
	//   If the symbol is currently Culled
	//   special case support for the staticRenderer & skinnedRenderer
	private void updateSymbolRendererVisibility()
	{
		Profiler.BeginSample("updateSymbolRendererVisibility");

		bool shouldBeEnabled = !isCulled && isSymbolActive;

		if (symbolRenderers != null)
		{
			foreach (Renderer renderer in symbolRenderers)
			{
				if (renderer != null)
				{
					if (renderer.enabled != shouldBeEnabled)
					{
						renderer.enabled = shouldBeEnabled;
					}

					// Update the SpriteMask component so that masks in the cache don't count toward the limit (HIR-57671)
					SpriteMask rendererMask = renderer.gameObject.GetComponent<SpriteMask>();
					if (rendererMask != null && rendererMask.enabled != shouldBeEnabled)
					{
						rendererMask.enabled = shouldBeEnabled;
					}
				}
			}
		}

		// And re-apply the separate static/skinned renderer states...
		updateSkinnedRendererEnabledState();
		updateStaticRendererEnabledState();

		Profiler.EndSample();
	}

	private void updateSkinnedRendererEnabledState()
	{
		bool shouldBeEnabled = !isCulled && isSymbolActive && skinnedRendererEnabled;
		if (skinnedRenderer != null  &&  skinnedRenderer.enabled != shouldBeEnabled)
		{
			skinnedRenderer.enabled = shouldBeEnabled;
		}
	}

	private void updateStaticRendererEnabledState()
	{
		bool shouldBeEnabled = !isCulled && isSymbolActive && staticRendererEnabled;
		if (staticRenderer != null  &&  staticRenderer.enabled != shouldBeEnabled)
		{
			staticRenderer.enabled = shouldBeEnabled;
		}
	}

	private void addRenderersToRenderersToRemoveList(Transform destroyedObjTrans)
	{
		Renderer[] rendererArray = destroyedObjTrans.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < rendererArray.Length; i++)
		{
			symbolRenderersToRemove.Add(rendererArray[i]);
		}
	}

	protected void initSymbolPrefabRenderers()
	{
		symbolRenderersToRemove.Clear();
	
		if (staticRenderer != null && staticRenderer.gameObject != null)
		{
			addRenderersToRenderersToRemoveList(staticRenderer.transform);
		
			if (Application.isPlaying)
			{
				Destroy(staticRenderer.gameObject);
			}
			else
			{
				DestroyImmediate(staticRenderer.gameObject);
			}
		}
		
		if (symbolGO != null && symbolGO.transform != null)
		{
			Transform t;
			t = scalingSymbolPart.transform.Find("root");
			if (t != null && t.gameObject != null)
			{
				addRenderersToRenderersToRemoveList(t);
			
				if (Application.isPlaying)
				{
					Destroy(t.gameObject);
				}
				else
				{
					DestroyImmediate(t.gameObject);
				}
			}

			// The "root" object holds this script so null it out since we just destroyed it
			resetBonePose = null;
			
			t = scalingSymbolPart.transform.Find("symbol_hi");
			if (t != null && t.gameObject != null)
			{
				addRenderersToRenderersToRemoveList(t);
			
				if (Application.isPlaying)
				{
					Destroy(t.gameObject);
				}
				else
				{
					DestroyImmediate(t.gameObject);
				}
			}
			
			t = scalingSymbolPart.transform.Find("Background Scaler");
			if (t != null && t.gameObject != null)
			{
				addRenderersToRenderersToRemoveList(t);
			
				if (Application.isPlaying)
				{
					Destroy(t.gameObject);
				}
				else
				{
					DestroyImmediate(t.gameObject);
				}
			}
		}
	}

	/// Turns off a symbol (deactivates it), which may include some special cleanup/reset code.
	/// Override this for special symbol prefab types.
	public virtual void deactivate()
	{
		Profiler.BeginSample("SymAnim.deactivate");

#if UNITY_EDITOR
		// reset this info so it doesn't exist if we grab another copy right away
		animationCallsThisFrame = 0;
		stackTraces = "";
#endif

		// Make sure the symbol isn't animating from a prior life
		if (isAnimating)
		{
			stopAnimation();
		}
		
		if (resetBonePose != null)
		{
			resetBonePose.resetToDefaultPose();
		}
		
		if (staticRenderer != null)
		{
			staticRenderer.transform.localScale = DEFAULT_QUAD_SCALE_VEC3;
			_staticRendererEnabled = false; // sets the backing variable, will refresh below
		}
		stopVfx();
		
		if (skinnedRenderer != null)
		{
			scalingSymbolPart.GetComponent<Animation>().enabled = false;
			_skinnedRendererEnabled = false; // sets the backing variable, will refresh below
		}

		if (symbolGO != null)
		{
			// Make sure no animations are playing
			playCustomAnimation(false);
		}

		// hide the wild overlay
		hideWild();

		// Make sure there isn't an angle on these.
		transform.localEulerAngles = Vector3.zero;
		
		_isSymbolActive = false; // sets the backing variable, will refresh below

		// Update all the symbol renderer's visibility states based on isSymbolActive, isCulled, etc.
		updateSymbolRendererVisibility();
		
		// If there is a SymbolLayerReorganizer then we will tell it that it should run again the next time the symbol is activated
		// in case the layering of the symbol is changed, for instance if it is pulled out of the symbol cache and placed on an Independent
		// Reel that alters its default layering.
		if (symbolReorganizer != null)
		{
			symbolReorganizer.onSymbolDeactivate();
		}

		if(isTumbleSquashAndStretching)
		{
			Debug.LogWarning("-=-=-=-= Deactivating Object with isTumbleSquashAndStretching " + isTumbleSquashAndStretching);
		}

		Profiler.EndSample();
	}

	/// Stops all animation and makes sure things are in a good default state.
	public virtual void stopAnimation(bool force = false)
	{
		if (!isAnimating && !force)
		{
			// If we aren't animating, then we don't need to stop animating.
			return;
		}
		
		if (!isWildShowing)
		{
			// Reset the shader to default when stopping animation,
			// but not if showing wild, because resetting it hides the wild.
			if (material != null)
			{
				material.shader = defaultShader(info.shaderName);
			}
		}
		
		// Transition to a reset animation, which is just some keyframes in the default position.
		if (info.symbolPrefab != null)
		{
			playCustomAnimation(false);
		}
		else
		{
			Animation scalingSymbolPartAnimation = scalingSymbolPart.GetComponent<Animation>();

			if (scalingSymbolPartAnimation != null)
			{
				if (scalingSymbolPartAnimation.isPlaying)
				{
					// Gracefully slip into default bone positions (TWSS)
					AnimationState resetAnimationState = scalingSymbolPartAnimation["reset_anim"];
					resetAnimationState.wrapMode = WrapMode.Once;
					resetAnimationState.time = 0f;
					scalingSymbolPartAnimation.CrossFade("reset_anim", 0.2f);
				}
				else if (resetBonePose != null)
				{
					// Make sure the bones are all in default positions
					resetBonePose.resetToDefaultPose();
				}
			}
		}

		// Cancel any pending coroutine
		StopCoroutine(endPlaying(null));

		isAnimating = false;
		
		// Some stuff only do if the symbol is active
		if (isSymbolActive)
		{
			if (info.cellsHigh == 1)
			{
				// Set us back to a sane layer
				if ( (FreeSpinGame.instance == null && SlotBaseGame.instance != null && (SlotBaseGame.instance.engine is LayeredSlotEngine || SlotBaseGame.instance.engine.reelSetData.isIndependentReels)) || 
					 (FreeSpinGame.instance != null && FreeSpinGame.instance.engine is LayeredSlotEngine) )
				{
					if (transform.parent != null)
					{
						GameObject animatorParent = transform.parent.gameObject;
						CommonGameObject.setLayerRecursively(gameObject, animatorParent.layer);
					}
				}
				else if (symbolGO != null)
				{
					if (slotSymbol != null)
					{
						foreach(SlotModule module in slotSymbol.reel._reelGame.cachedAttachedSlotModules)
						{
							if (module.needsToExecuteChangeSymbolLayerAfterSymbolAnimation(slotSymbol))
							{
								module.executeChangeSymbolLayerAfterSymbolAnimation(slotSymbol);
							}
						}
					}
					// Don't do anything with the layering for now.
				}
				else
				{
					CommonGameObject.setLayerRecursively(gameObject, Layers.ID_SLOT_REELS);
				}
			}
		
			// Show the static quad again instead of the animated stuff.
			if (info.symbolPrefab == null)
			{
				staticRendererEnabled = true;
			}

			if (skinnedRenderer != null && info.symbolPrefab == null)
			{
				scalingSymbolPart.GetComponent<Animation>().enabled = false;
				skinnedRendererEnabled = false;
			}
		}

		if (symbolGO != null)
		{
			if (symbolReorganizer != null && symbolReorganizer.enabled)
			{
				symbolReorganizer.restoreOriginalLayers();
			}
		}
	}

	public delegate void OnSymbolTweenFinishDelegate(SlotSymbol symbol);
	public OnSymbolTweenFinishDelegate onSymbolTweenFinish;
	private SlotSymbol targetTweenSymbol;

	public void setTweenFinishDelegateAndTargetSymbol(OnSymbolTweenFinishDelegate onFinishDelegate, SlotSymbol tweenSymbol)
	{
		onSymbolTweenFinish = onFinishDelegate;
		targetTweenSymbol = tweenSymbol;
	}
	
	void onSymbolTweenComplete()
	{
		if (onSymbolTweenFinish != null)
		{
			onSymbolTweenFinish(targetTweenSymbol);
			onSymbolTweenFinish = null;
		}
	}

	/// Plays the anticipation animation sequence, based on a SymbolAnimationType.
	/// Override this for special symbol prefab types.
	public virtual void playAnticipation(SlotSymbol targetSymbol)
	{ 
#if UNITY_EDITOR
		incrementAnimationCalls();
#endif
		setSymbol(targetSymbol);
		playAnimation(info.anticipationAnimation, true);
	}

	/// Plays the outcome animation sequence, based on a SymbolAnimationType.
	/// Override this for special symbol prefab types.
	public virtual void playOutcome(SlotSymbol targetSymbol)
	{
#if UNITY_EDITOR
		incrementAnimationCalls();
#endif
		setSymbol(targetSymbol);
		playAnimation(info.outcomeAnimation, true);
		
		if (slotSymbol != null && (slotSymbol.name == "WD" || isMutatedToWD) && ReelGame.activeGame != null && ReelGame.activeGame.isWdSymbolUsingWildOverlay)
		{
			showWild();
		}
	}

	/// Plays the mutate-to-this animation sequence, based on a SymbolAnimationType.
	/// Override this for special symbol prefab types.
	public virtual void playMutateFrom(SlotSymbol targetSymbol, string targetMutateName, bool playVfx = true)
	{
#if UNITY_EDITOR
		incrementAnimationCalls();
#endif
		isMutating = true;
		setSymbol(targetSymbol, targetMutateName);
		playAnimation(info.mutateFromAnimation, playVfx);
	}

	/// Plays the mutate-to-this animation sequence, based on a SymbolAnimationType.
	/// Override this for special symbol prefab types.
	public virtual void playMutateTo(SlotSymbol targetSymbol)
	{
#if UNITY_EDITOR
		incrementAnimationCalls();
#endif
		setSymbol(targetSymbol);
		playAnimation(info.mutateToAnimation);
	}

	// Returns the time of the longest animation.
	private float turnOffAnimations(GameObject symbolGO, bool shouldPlay)
	{
		float timeOfLongestAnimation = 0;
		// Grab all of the animations that are attached to the symbol prefab and turn them on.
		if (symbolAnimations != null)
		{
			foreach (Animation animation in symbolAnimations)
			{
				if (animation == null || animation.clip == null)
				{
					continue;
				}
				animation[animation.clip.name].time = 0;
				animation.Sample();
				timeOfLongestAnimation = Mathf.Max(animation.clip.length, timeOfLongestAnimation);
				
				if (shouldPlay)
				{
					animation.Play();
				}
				else
				{
					animation.Stop();
				}
			}
		}
		return timeOfLongestAnimation;
	}

	private void turnOffAnimators(GameObject symbolGO, bool shouldPlay)
	{
		// Grab all of the animators that are attached to the symbol prefab.
		if (symbolAnimators != null)
		{
			foreach (Animator animator in symbolAnimators)
			{
				if (animator == null || animator.gameObject == null || !animator.gameObject.activeInHierarchy)
				{
					continue;
				}
				
				// Set the time of the animation to the starting state.
				AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
				if (shouldPlay)
				{
					animator.Play(currentAnimatorStateInfo.fullPathHash, -1, 0.0f); // Start the animation from the begining every time we play
					// Play the animation normaly
					animator.speed = 1;
				}
				else
				{
					// allow modules to control what setting the animator uses, this is useful
					// if for instance you want to avoid stop call forcing the animation to the last
					// frame pre-maturely, but do want the animation to freeze on the last frame after
					// animating
					bool isStopPointControlledByModule = false;
					if (slotSymbol != null && slotSymbol.reel != null)
					{
						foreach(SlotModule module in slotSymbol.reel._reelGame.cachedAttachedSlotModules)
						{
							if (module.needsToSetSymbolAnimatorStopPoint(slotSymbol))
							{
								float stopPoint = module.executeSetSymbolAnimatorStopPoint(slotSymbol);
								animator.Play(currentAnimatorStateInfo.fullPathHash, -1, stopPoint);
								if (isStopPointControlledByModule)
								{
									Debug.LogError("SymbolAnimator.turnOffAnimators() - Multiple modules are trying to control the animator stop point, but only one can!");
								}
								else
								{
									isStopPointControlledByModule = true;
								}
							}
						}
					}

					if (!isStopPointControlledByModule)
					{
						if (info.endAnimatorAtNormalizedTime1) 
						{
							// if the symbol info specifies then when we stop the animation we will stop it on the last frame
							// instead of the first one
							animator.Play(currentAnimatorStateInfo.fullPathHash, -1, 1.0f); 
						}
						else
						{
							animator.Play(currentAnimatorStateInfo.fullPathHash, -1, 0.0f); // Start the animation from the begining.
						}
					}

					// Make the animation not play.
					animator.speed = 0;
				}
			}
		}
	}

	private void turnOffParticleSystems(GameObject symbolGO, bool shouldPlay)
	{
		// If there are particle systems we want to control them too.
		if (symbolParticleSystems != null)
		{
			foreach (ParticleSystem particleSys in symbolParticleSystems)
			{
				if (particleSys == null)
				{
					continue;
				}
				
				if (shouldPlay)
				{ 
					particleSys.Play();
				}
				else
				{
					particleSys.Stop();
					particleSys.Clear();
				}
				CommonEffects.setEmissionEnable(particleSys, shouldPlay);
			}
		}
	}


	// Plays the custom animation on the symbolGO
	private void playCustomAnimation(bool shouldPlay, string animationName = null, bool playVfx = true)
	{
		float timeOfLongestAnimation = 0.0f;
		if (symbolGO != null)
		{
			if (shouldPlay && slotSymbol != null && info.isSymbolSplitable && !slotSymbol.isWhollyOnScreen)
			{
				slotSymbol.splitSymbol();
			}
			else
			{
				timeOfLongestAnimation = turnOffAnimations(symbolGO, shouldPlay);
				turnOffAnimators(symbolGO, shouldPlay);
				turnOffParticleSystems(symbolGO, shouldPlay);
			}
		}

		if (shouldPlay)
		{
			if (symbolGO != null)
			{
				if (symbolReorganizer == null)
				{
					symbolReorganizer = getSymbolLayerReorganizer();
				}

				if (symbolReorganizer != null && symbolReorganizer.enabled)
				{
					symbolReorganizer.reorganizeLayers(slotSymbol.isWhollyOnScreen);
				}
			}

			isAnimating = true;
			Hashtable args = new Hashtable();
			args.Add("playVfx", playVfx);
			// Get the time to play the animation
			if (info.customAnimationDurationOverride != 0.0f)
			{
				// going to use override timing for the animation
				args.Add("animationDuration", info.customAnimationDurationOverride);
			}
			else if (animationName != null && timeOfLongestAnimation == 0.0f && GetComponent<Animation>() != null)
			{
				float animationDuration = 0;
				AnimationState animationState = GetComponent<Animation>()[animationName];
				if (animationState != null)
				{
					animationState.time = 0f;
					// Setup an invoke to signal the end of playing
					animationDuration = animationState.length * animationState.speed;
				}
				args.Add("animationDuration", animationDuration);
			}
			else
			{
				args.Add("animationDuration", timeOfLongestAnimation);
			}

			StartCoroutine(endPlaying(args));
		}
	}

	/// Plays a specific common animation type.
	public void playAnimation(SymbolAnimationType animationType, bool playVfx = true)
	{
		if (isSkippingAnimations)
		{
			// This symbol was flagged to skip animations, so just terminate early
			if (slotSymbol != null)
			{
				if (mutateName != "")
				{
					// Tell the SlotSymbol to switch over mid-mutation
					slotSymbol.mutateSwitchOver(mutateName, playVfx);
				}
				else
				{
					// Tell the SlotSymbol that the animation is complete
					isMutating = false;
					slotSymbol.animationDone();
				}
			}

			// Clear the symbol information
			setSymbol();

			return;
		}

		// Get the animation name 
		string animationName = null;
		switch (animationType)
		{
			case SymbolAnimationType.NONE:
				break;

			case SymbolAnimationType.ANTICIPATE_01:
				animationName = "anticipation_small_v01";
				break;

			case SymbolAnimationType.ANTICIPATE_02:
				animationName = "anticipation_small_v02b";
				break;

			case SymbolAnimationType.ANTICIPATE_03:
				animationName = "anticipation_small_v03";
				break;

			case SymbolAnimationType.ANTICIPATE_04:
				animationName = "anticipation_small_v04";
				break;

			case SymbolAnimationType.ANTICIPATE_05:
				animationName = "anticipation_small_v05";
				break;

			case SymbolAnimationType.ANTICIPATE_06:
				animationName = "anticipation_small_v06";
				break;

			case SymbolAnimationType.ANTICIPATE_07:
				animationName = "anticipation_small_v07";
				break;

			case SymbolAnimationType.ANTICIPATE_08:
				animationName = "anticipation_small_v08b";
				break;

			case SymbolAnimationType.ANTICIPATE_09:
				animationName = "anticipation_small_v09";
				break;

			case SymbolAnimationType.OUTCOME_01:
				animationName = "outcome_v01";
				break;

			case SymbolAnimationType.OUTCOME_02:
				animationName = "outcome_v02";
				break;

			case SymbolAnimationType.OUTCOME_03:
				animationName = "outcome_v03";
				break;

			case SymbolAnimationType.OUTCOME_04:
				animationName = "outcome_v04";
				break;

			case SymbolAnimationType.OUTCOME_05:
				animationName = "outcome_v05";
				break;

			case SymbolAnimationType.OUTCOME_06:
				animationName = "outcome_v06";
				break;

			case SymbolAnimationType.OUTCOME_07:
				animationName = "outcome_v07";
				break;

			case SymbolAnimationType.OUTCOME_08:
				animationName = "outcome_v08";
				break;

			case SymbolAnimationType.OUTCOME_09:
				animationName = "outcome_v09";
				break;

			case SymbolAnimationType.MUTATE_01_A:
				animationName = "mutation_v01_A";
				break;

			case SymbolAnimationType.MUTATE_01_B:
				animationName = "mutation_v01_B";
				break;

			case SymbolAnimationType.MUTATE_02_A:
				animationName = "mutation_v02_A";
				break;
				
			case SymbolAnimationType.MUTATE_02_B:
				animationName = "mutation_v02_B";
				break;

			case SymbolAnimationType.CUSTOM:
				animationName = "custom";
				break;
			
		default:
				Debug.LogWarning("SymbolAnimationType has no definition: " + animationType.ToString());
				break;
		}

		if (animationName != null)
		{
			if (animatingCallback != null)
			{
				animatingCallback(this);
			}

			if (info.symbolPrefab != null)
			{
				playCustomAnimation(true, animationName, playVfx);
			}
			else
			{
				playOnce(animationName, playVfx);
			}
		}

		if (slotSymbol != null && slotSymbol.reel != null)
		{
			foreach(SlotModule module in slotSymbol.reel._reelGame.cachedAttachedSlotModules)
			{
				if (module.needsToExecuteOnSymbolAnimatorPlayed(slotSymbol))
				{
					module.executeOnSymbolAnimatorPlayed(slotSymbol);
				}
			}
		}
	}

	public int getSymbolIndex()
	{
		if (slotSymbol != null)
		{
			return slotSymbol.index;
		}
		return -1;
	}

	/// Set the symbol and/or mutation name, which is used after an animation completes
	protected void setSymbol(SlotSymbol newSymbol = null, string newMutateName = "")
	{
		slotSymbol = newSymbol;
		mutateName = newMutateName;
	}
	
	private void playOnce(string animationName, bool playVfx = true)
	{
		if (!isSymbolActive)
		{
			Debug.LogWarning("Inactive symbols should not be animating!", gameObject);
			return;
		}
		
		stopAnimation();
		isAnimating = true;
		
		Hashtable args = new Hashtable();
		args.Add("playVfx", playVfx);

		if (info.cellsHigh == 1)
		{
			// Only put the symbol in front of the frame if it's not multiple rows tall,
			// because tall symbols may be partially off the top or bottom of the reels.
			if ( (FreeSpinGame.instance == null && SlotBaseGame.instance != null && (SlotBaseGame.instance.engine is LayeredSlotEngine || SlotBaseGame.instance.engine.reelSetData.isIndependentReels)) || 
				 (FreeSpinGame.instance != null && FreeSpinGame.instance.engine is LayeredSlotEngine) )
			{
				// We are using a sliding slot engine.
				GameObject animatorParent = transform.parent.gameObject;
				CommonGameObject.setLayerRecursively(gameObject, animatorParent.layer);
			}
			else
			{
				// Regular slot engine.
				CommonGameObject.setLayerRecursively(gameObject, Layers.ID_SLOT_FRAME);
			}

		}

		Animation scalingSymbolPartAnimation = scalingSymbolPart.GetComponent<Animation>();
		if (scalingSymbolPartAnimation != null)
		{
			AnimationState animationState = scalingSymbolPart.GetComponent<Animation>()[animationName];

			// Just in case
			if (animationState == null)
			{
				Debug.LogWarning("Unable to find animation in symbol: " + animationName);
				args.Add("animationDuration", 0.1f);
				StartCoroutine(endPlaying(args));
			}
			else
			{
				staticRendererEnabled = false;
				skinnedRendererEnabled = true;
				scalingSymbolPartAnimation.enabled = true;
				
				if (scalingSymbolPartAnimation.isPlaying)
				{
					scalingSymbolPartAnimation.Stop();
				}

				animationState.wrapMode = WrapMode.Once;
				animationState.time = 0f;
				scalingSymbolPartAnimation.Play(animationName);

				// Setup an invoke to signal the end of playing
				float animationDuration = animationState.length * animationState.speed;
				args.Add("animationDuration", animationDuration);
				StartCoroutine(endPlaying(args));
			}
		}
	}

	/// This is invoked after the duration of a played animation
	private IEnumerator endPlaying(Hashtable args)
	{
		float animationDuration = (float)args["animationDuration"];
		bool playVfx = (bool)args["playVfx"];
		yield return new TIWaitForSeconds(animationDuration);

		stopAnimation();

		if (slotSymbol != null)
		{
			if (mutateName != "")
			{
				// Tell the SlotSymbol to switch over mid-mutation
				slotSymbol.animationDone();
				slotSymbol.mutateSwitchOver(mutateName, playVfx);
			}
			else
			{
				// Tell the SlotSymbol that the animation is complete
				isMutating = false;
				slotSymbol.animationDone();
			}

			if(slotSymbol.reel != null)
			{
				foreach (SlotModule module in slotSymbol.reel._reelGame.cachedAttachedSlotModules)
				{
					if (module.needsToExecuteOnSymbolAnimationFinished(slotSymbol))
					{
						yield return StartCoroutine(module.executeOnSymbolAnimationFinished(slotSymbol));
					}
				}
			}
		}

		// Clear the symbol information
		setSymbol();
	}

	public void showWild()
	{
		if (!isSymbolActive || (wildTexture == null && wildOverlayGameObject == null))
		{
			return;
		}

		if (wildTexture != null)
		{
			StopCoroutine("fastWildFadeOut");

			if (wildHidesSymbol)
			{
				storedMainSymbolTexture =  material.GetTexture("_MainTex");
				material.shader = wildSymbolHideShader;
				material.SetTexture("_StartTex", storedMainSymbolTexture);
				material.SetTexture("_EndTex", wildTexture);
				material.SetFloat("_Softness", 0.5f);
			}
			else
			{
				material.shader = overlayShader;
				material.SetTexture("_OverlayTex", wildTexture);
			}
			
			StopCoroutine(fastWildFadeIn(0));
			StartCoroutine(fastWildFadeIn(0.2f));
		}
		else if (!disableWildOverlayGameObject)
		{
			// GameObject wild overlay, going to handle things differently
			if (wildOverlayGameObject != null)
			{
				wildOverlayGameObject.SetActive(true);
			}
		}

		isWildShowing = true;
	}

	public void hideWild()
	{
		if (wildTexture != null)
		{
			// Cancel any pending invoking
			StopCoroutine(fastWildFadeIn(0));
			StartCoroutine(fastWildFadeOut(0.5f));
		}
		else
		{
			// GameObject wild overlay
			if (wildOverlayGameObject != null)
			{
				wildOverlayGameObject.SetActive(false);
			}
		}
	}

	private IEnumerator fastWildFadeIn(float duration)
	{
		if (isSymbolActive)
		{
			float step = 1f / duration;
			float fade = material.GetFloat("_Fade");
			while(fade < 1)
			{
				fade += Time.deltaTime * step;
				material.SetFloat("_Fade", fade);
				yield return null;
			}
		}

		material.SetFloat("_Fade", 1f);
	}

	private IEnumerator fastWildFadeOut(float duration)
	{
		if (material != null)
		{
			if (material.HasProperty("_Fade"))
			{
				if (isSymbolActive)
				{
					float step = 1f / duration;
					float fade = material.GetFloat("_Fade");
					while(fade > 0)
					{
						fade -= Time.deltaTime * step;
						material.SetFloat("_Fade", fade);
						yield return null;
					}
				}

				material.SetFloat("_Fade", 0);
			}

			if (wildHidesSymbol)
			{
				material.SetTexture("_StartTex", null);
				material.SetTexture("_EndTex", null);
				material.shader = defaultShader(info.shaderName);

				// set the main texture back to the one we stored before the shader swap
				material.SetTexture("_MainTex", storedMainSymbolTexture);
				storedMainSymbolTexture = null;
			}
			else
			{
				material.SetTexture("_OverlayTex", null);
				material.shader = defaultShader(info.shaderName);
			}
		}
		
		isWildShowing = false;
	}

	public void startFadeOut(float duration)
	{
		StartCoroutine(fadeSymbolOutOverTime(duration));
	}

	public bool hasGameObjectAlphaMap()
	{
		return gameObjectAlphaMap != null;
	}

	// Does the same thing as fade out over time but immediate, still saves out alpha maps to restore though
	public void fadeSymbolOutImmediate()
	{
		if (!isFadingOverTime)
		{
			if (symbolGO != null)
			{
				gameObjectAlphaMap = CommonGameObject.getAlphaValueMapForGameObject(symbolGO);

				CommonGameObject.alphaGameObject(symbolGO, 0.0f);
			}
			else
			{
				CommonMaterial.setAlphaOnMaterial(material, 0.0f);
			}
		}
	}

	// Allows a symbol to be faded out over time (used for transitions)
	public IEnumerator fadeSymbolOutOverTime(float duration)
	{
		if (!isFadingOverTime)
		{
			isFadingOverTime = true;

			if (symbolGO != null)
			{
				gameObjectAlphaMap = CommonGameObject.getAlphaValueMapForGameObject(symbolGO);

				float elapsedTime = 0;

				while (elapsedTime < duration)
				{
					elapsedTime += Time.deltaTime;
					CommonGameObject.alphaGameObject(symbolGO, 1 - (elapsedTime / duration));
					yield return null;
				}

				CommonGameObject.alphaGameObject(symbolGO, 0.0f);
			}
			else
			{
				float elapsedTime = 0;

				while (elapsedTime < duration)
				{
					elapsedTime += Time.deltaTime;
					CommonMaterial.setAlphaOnMaterial(material, 1 - (elapsedTime / duration));
					yield return null;
				}

				CommonMaterial.setAlphaOnMaterial(material, 0.0f);
			}
			fadingCoroutine = null; //Don't need this anymore if our symbol finished fading out normally
			isFadingOverTime = false;
		}
	}

	//Stop the coroutine fading the symbol while also resetting the isFadingOvertime flag so we can fade the symbol in immediately when it needs to be reactivated
	public void haltFade()
	{
		if (fadingCoroutine != null)
		{
			StopCoroutine(fadingCoroutine);
			fadingCoroutine = null;
		}
		isFadingOverTime = false;
	}
	// Does the same thing as fade in over time but immediate, still will use alpha map if available
	public void fadeSymbolInImmediate(bool shouldWarnAboutAlphaMap = true)
	{
		if (!isFadingOverTime)
		{
			if (symbolGO != null)
			{
				if (gameObjectAlphaMap != null)
				{
					CommonGameObject.restoreAlphaValuesToGameObjectFromMap(symbolGO, gameObjectAlphaMap);
					gameObjectAlphaMap.Clear();
					gameObjectAlphaMap = null;
				}
				else
				{
					if (shouldWarnAboutAlphaMap)
					{
						Debug.LogWarning("SymbolAnimator.fadeSymbolInImmediate() - gameObjectAlphaMap wasn't set, going to assume this symbol was never faded out, so ignoring this call!");
					}
				}
			}
			else
			{
				CommonMaterial.setAlphaOnMaterial(material, 1.0f);
			}
		}
	}

	// Allows a symbol to be faded in over time (used for transitions)
	public IEnumerator fadeSymbolInOverTime(float duration)
	{
		if (!isFadingOverTime)
		{
			isFadingOverTime = true;

			if (symbolGO != null)
			{
				if (gameObjectAlphaMap != null)
				{
					yield return StartCoroutine(CommonGameObject.restoreAlphaValuesToGameObjectFromMapOverTime(symbolGO, gameObjectAlphaMap, duration));
					gameObjectAlphaMap.Clear();
					gameObjectAlphaMap = null;
				}
				else
				{
					Debug.LogWarning("gameObjectAlphaMap wasn't set, going to set all Alpha values to 1.0f, this may not be correct!");

					float elapsedTime = 0;

					while (elapsedTime < duration)
					{
						elapsedTime += Time.deltaTime;
						CommonGameObject.alphaGameObject(symbolGO, elapsedTime / duration);
						yield return null;
					}

					CommonGameObject.alphaGameObject(symbolGO, 1.0f);
				}
			}
			else
			{
				float elapsedTime = 0;

				while (elapsedTime < duration)
				{
					elapsedTime += Time.deltaTime;
					CommonMaterial.setAlphaOnMaterial(material, elapsedTime / duration);
					yield return null;
				}

				CommonMaterial.setAlphaOnMaterial(material, 1.0f);
			}

			isFadingOverTime = false;
			fadingCoroutine = null; //Don't need this anymore if our symbol finished fading in normally
		}
	}

	// create a new game object with the exact same transform values as this game object on which to play the VFX (in case this game object gets destroyed)
	public GameObject playVfxOnTempGameObject(bool shouldRenderAboveAll = false)
	{
		GameObject tempObject = new GameObject();
		tempObject.layer = gameObject.layer;
		tempObject.transform.parent = gameObject.transform.parent;
		tempObject.transform.localPosition = gameObject.transform.localPosition;
		tempObject.transform.localScale = gameObject.transform.localScale;
		tempObject.transform.localRotation = gameObject.transform.localRotation;
		tempObject.name = "VFX Object";
		tempObject.AddComponent<UIPanel>();

		if (info.vfxPrefab != null)
		{
			vfx = VisualEffectComponent.Create(info.vfxPrefab, tempObject);
			if (shouldRenderAboveAll)
			{
				vfx.GetComponent<Renderer>().material.renderQueue = 7000;
			}
		}

		return tempObject;
	}

	public void playVfx(bool shouldRenderAboveAll = false)
	{
		// stop any vfx already playing
		stopVfx();

		if (info.vfxPrefab != null)
		{
			vfx = VisualEffectComponent.Create(info.vfxPrefab, gameObject);
			if (shouldRenderAboveAll)
			{
				vfx.GetComponent<Renderer>().material.renderQueue = 7000;
			}
		}
	}

	public void stopVfx()
	{
		if(vfx != null)
		{
			vfx.Finish();
			vfx = null;
		}
	}

	// Get the base render level of a symbol animator
	public virtual int getBaseRenderLevel()
	{
		return BASE_SYMBOL_RENDER_QUEUE_VALUE;
	}

	// Goes through every MeshRenderer and sets the render queue to queue.
	public virtual void changeRenderQueue(int queue)
	{
		Profiler.BeginSample("SymAnim.changeRenderQueue");

		if (symbolGO != null)
		{
			if (symbolRenderers != null)
			{
				// Need to try set all render queue values on all renderers
				// if this isn't what you want you should consider using the isHandlingOwnSymbolRenderQueues for ReelGame
				foreach (Renderer renderer in symbolRenderers)
				{
					if (renderer != null)
					{
						foreach (Material material in renderer.materials)
						{
							if (material != null)
							{
								material.renderQueue = queue;
							}
						}
					}
				}
			}
		}
		else
		{
			if (material != null)
			{
				material.renderQueue = queue;
			}
			else
			{
				Debug.LogWarning("For some reason " + symbolInfoName + " doesn't have a material, so changeRenderQueue() will do nothing");
			}
		}

		Profiler.EndSample();
	}

	// Adds a value to the render queue of every MeshRenderer
	public virtual void addRenderQueue(int amount)
	{
		Profiler.BeginSample("addRenderQueue");

		if (symbolGO != null)
		{
			if (symbolRenderers != null)
			{
				foreach (Renderer renderer in symbolRenderers)
				{
					if (renderer != null)
					{
						foreach (Material material in renderer.materials)
						{
							if (material != null)
							{
								material.renderQueue += amount;
							}
						}
					}
				}
			}
		}
		else
		{
			if (material != null)
			{
				material.renderQueue += amount;
			}
			else
			{
				Debug.LogWarning("For some reason " + symbolInfoName + " doesn't have a material, so addRenderQueue() will do nothing");
			}
		}

		Profiler.EndSample();
	}


	public delegate void AnimatingDelegate(SymbolAnimator animator);


	//=======================================================================================
	// Symbol Bounding Box information...
	// This is used to perform above/below reel ("buffered symbol") culling against the SwipeableReels.
	//
	// We only gather bounds for the initial, pre-animated symbol state at "activation" time, 
	// as buffered symbols should generally be in that initial state.
	//
	// Each symbol can be rendered various ways, so we gather bounds from:
	//   BoxColliders (has priority if exists) 
	//   SpriteRenderers
	//   MeshRenderers
	//   SkinnedMeshRenderers 
	// And create a single expanded bounds for culling purposes. 
	//
	// Computing bounds can be slow, so we cache the results per ReelGame by SymbolName.
	//
	// Bounds are often setup to be 'oversized' or larger than the visible portion of the bitmap. :-(
	// We may have to re-address that if it's a large problem (bounds checking will either have to 
	// render out symbols and read back their graphics, or we can add more BoxColliders to symbols, etc).

#if DEBUG_BOUNDS
	const bool isDebuggingBounds = true;
#else
	const bool isDebuggingBounds = false;
#endif

	// All bounds are relative to the SymbolAnimator (should be the root of each symbol)
	public class BoundsInfo 
	{
		// Single expanded Bound that contains only colliders (if they exist), else everything. 
		// This is what we cull against.
		public Bounds combinedLocalBounds; 

#if DEBUG_BOUNDS
		// Extra debug info here...
		public List<Bounds> meshBounds = new List<Bounds>();
		public List<Bounds> spriteBounds = new List<Bounds>();
		public List<Bounds> skinnedBounds = new List<Bounds>();
		public List<Bounds> colliderBounds = new List<Bounds>();
#endif
	}

	public BoundsInfo initialBoundsInfo; // our initial bounds information upon activation

	// Caching of bounds info...
	bool useCachedBounds = true; // should be true, else recalculates symbol bounds on every activation (for debugging)
	public Dictionary<string, BoundsInfo>  symbolBoundsCache; // Shared Dictionary lives in each ReelGame

	// Gather all the bounds info for a newly activated symbol 
	// If we can find BoxColliders - use them, if not, we use all the renderable bits we can find
	BoundsInfo setupBoundsAfterActivation()
	{
		BoundsInfo boundsInfo;
		string symbolKeyName = name; // ie: "Symbol M4_Flattened"
		if (useCachedBounds && symbolBoundsCache.TryGetValue(symbolKeyName, out boundsInfo))
		{
			return boundsInfo;
		}

		// Not cached? Create new entry, add it to the cache
		boundsInfo = new BoundsInfo();
		symbolBoundsCache[symbolKeyName] = boundsInfo;

		// Get the various types of bounds; we get them all in this SymbolAnimator's local space
		// If we find colliders, we exclusively use them for bounds. If not, gather all the renderable pieces.
		var expandedColliderBounds = new Bounds();
		var expandedOtherBounds = new Bounds();

		var colliders = GetComponentsInChildren<BoxCollider>(false);
		var colliderBoundsList = new List<Bounds>(colliders.Length);
		foreach (var collider in colliders)
		{
			var colliderBounds = new Bounds(collider.center, collider.size);
			var colliderToLocalMatrix = this.transform.worldToLocalMatrix * collider.transform.localToWorldMatrix;
			var bounds = CommonBounds.transformBounds(colliderToLocalMatrix, colliderBounds); 
			colliderBoundsList.Add(bounds);
			expandedColliderBounds.Encapsulate(bounds);
		}

		if (colliders.Length == 0 || isDebuggingBounds)
		{
			bool foundStaticQuad = false;

			var meshRenderers = GetComponentsInChildren<MeshRenderer>(false);
			var meshBoundsList = new List<Bounds>(meshRenderers.Length);
			foreach (var meshRenderer in meshRenderers)
			{
				// Render out the mesh to determine it's best fitting bounds...
				var bounds = CommonBounds.getRenderedMeshBoundsRelativeTo(meshRenderer, this.transform); 
				meshBoundsList.Add(bounds);
				expandedOtherBounds.Encapsulate(bounds);

				foundStaticQuad |= (meshRenderer.name == "Static Quad");
			}

			var spriteRenderers = GetComponentsInChildren<SpriteRenderer>(false);
			var spriteBoundsList = new List<Bounds>(spriteRenderers.Length);
			foreach (var spriteRenderer in spriteRenderers)
			{
				// Render out the sprite to determine it's best fitting bounds...
				var bounds = CommonBounds.getRenderedSpriteBoundsRelativeTo(spriteRenderer, this.transform);
				spriteBoundsList.Add(bounds);
				expandedOtherBounds.Encapsulate(bounds);

			}

			var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(false);
			var skinnedBoundsList = new List<Bounds>(skinnedRenderers.Length);
			foreach (var skinnedRenderer in skinnedRenderers)
			{
				// If we found a "static quad", then we can skip the paired "symbol_hi" SkinnedMeshRenderer from the basic 1x1 prefabs
				if (foundStaticQuad && skinnedRenderer.name == "symbol_hi")
				{
					continue;
				}

				var bounds = CommonBounds.getSkinnedMeshRendererBoundsRelativeTo(skinnedRenderer, this.transform);
				skinnedBoundsList.Add(bounds);
				expandedOtherBounds.Encapsulate(bounds);
			}

#if DEBUG_BOUNDS
			boundsInfo.meshBounds = meshBoundsList;
			boundsInfo.spriteBounds = spriteBoundsList;
			boundsInfo.skinnedBounds = skinnedBoundsList;
			boundsInfo.colliderBounds = colliderBoundsList;
#endif
		}

		// Use expanded colliderBounds (if exists), else use all the other expanded bounds
		boundsInfo.combinedLocalBounds = (colliders.Length > 0) ? expandedColliderBounds : expandedOtherBounds;


		// Empty (or nearly empty) bounds?  Log info to look at later...
		if (boundsInfo.combinedLocalBounds.extents.x < 0.01f || boundsInfo.combinedLocalBounds.extents.y < 0.01f)
		{
			var gameName = (GameState.game != null) ? GameState.game.keyName : "<NoGameName>";
			Debug.LogWarning("BOUNDS: Empty bounds for game: " + gameName + ",  symbol: " + symbolKeyName );
		}

		// Return the final boundsInfo...
		return boundsInfo;
	}



	//===========================================================================
	// Everything below here is Gizmo / diagnostic rendering code only...

	// Diagnostic rendering of various bounding boxes...
	void OnDrawGizmosSelected()
	{
		// Diagnostic rendering of our single expanded bounds we cull against
		if (initialBoundsInfo != null)
		{
			Gizmos.matrix = transform.localToWorldMatrix; // relative to this SymbolAnimator
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireCube(initialBoundsInfo.combinedLocalBounds.center, initialBoundsInfo.combinedLocalBounds.size);
		}

#if DEBUG_BOUNDS
		// Diagnostic drawing of bounds we can find NOW, in various spaces & colors...
		//drawChildMeshBoundsGizmos();
		//drawChildSpriteBoundsGizmos();
		//drawChildSkinnedBoundsGizmos();
		//drawChildBoxColliderBoundsGizmos();

		// Draw 'initial bounds' we captured at activation
		//drawListOfBoundsGizmos(initialBoundsInfo.meshBounds, Color.red);
		//drawListOfBoundsGizmos(initialBoundsInfo.spriteBounds, Color.green);
		//drawListOfBoundsGizmos(initialBoundsInfo.skinnedBounds, Color.blue);
		//drawListOfBoundsGizmos(initialBoundsInfo.colliderBounds, Color.yellow);
#endif
	}

	void drawListOfBoundsGizmos(IEnumerable<Bounds> boundsList, Color color)
	{
		Gizmos.color = color;
		Gizmos.matrix = transform.localToWorldMatrix; // relative to this SymbolAnimator
		foreach(var bounds in boundsList)
		{
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}
	}

	// Looks for and draws various child bounds in various spaces (local, world, relative to SymbolAnimator, etc)
	void drawChildMeshBoundsGizmos()
	{
		// MeshRenderer test case... WONKA01 has tons in the symbols
		// Symbol M4 Head is a non-square mesh...  https://screencast.com/t/njjU5pVnLID5

		var meshRenderers = GetComponentsInChildren<MeshRenderer>(false);
		foreach (var meshRenderer in meshRenderers)
		{
			var meshFilter = meshRenderer.GetComponent<MeshFilter>();
			if (meshFilter == null || meshFilter.sharedMesh == null)
			{
				Debug.LogWarning("No matching meshFilter/mesh for MeshRenderer");
				continue;
			}

			// In local-space 
			Gizmos.color = Color.red;
			Gizmos.matrix = meshRenderer.localToWorldMatrix;
			var bounds = meshFilter.sharedMesh.bounds; // local
			Gizmos.DrawWireCube(bounds.center, bounds.size);

			// In world-space (just transformed local AABB; poorly fit)
			Gizmos.color = Color.green;
			Gizmos.matrix = Matrix4x4.identity;
			bounds = meshRenderer.bounds; //worldspace
			Gizmos.DrawWireCube(bounds.center, bounds.size);

			// Recalculated, relative to SymbolAnimators (better fit than rotating local AABB)  https://screencast.com/t/yGoZMU92CdYa
			Gizmos.color = Color.yellow;
			Gizmos.matrix = this.transform.localToWorldMatrix;
			var meshToLocalMatrix = this.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
			bounds = GeometryUtility.CalculateBounds(meshFilter.sharedMesh.vertices, meshToLocalMatrix);
			bounds.extents = new Vector3(bounds.extents.x + 0.01f, bounds.extents.y + 0.01f, bounds.extents.z); // for editor visualization
			Gizmos.DrawWireCube(bounds.center, bounds.size);

			// Rendered to get best-fit in SymbolAnimator's space
			Gizmos.color = Color.cyan;
			Gizmos.matrix = this.transform.localToWorldMatrix;
			bounds = CommonBounds.getRenderedMeshBoundsRelativeTo(meshRenderer, this.transform); 
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}

	}


	void drawChildSpriteBoundsGizmos()
	{
		// Sprite test case: Wonka01 symbol backgrounds

		var spriteRenderers = GetComponentsInChildren<SpriteRenderer>(false);
		foreach (var spriteRenderer in spriteRenderers)
		{
			// In Sprite's local space
			Gizmos.color = Color.red;
			Gizmos.matrix = spriteRenderer.localToWorldMatrix;
			var bounds = spriteRenderer.sprite.bounds; // local
			Gizmos.DrawWireCube(bounds.center, bounds.size);

			// In world space (just a transformed local bounds)
			Gizmos.color = Color.green;
			Gizmos.matrix = Matrix4x4.identity;
			bounds = spriteRenderer.bounds; // world
			Gizmos.DrawWireCube(bounds.center, bounds.size);

			// In SymbolAnimator's space
			Gizmos.color = Color.yellow;
			Gizmos.matrix = this.transform.localToWorldMatrix;
			var spriteToLocalMatrix = this.transform.worldToLocalMatrix * spriteRenderer.transform.localToWorldMatrix;
			bounds = CommonBounds.transformBounds(spriteToLocalMatrix, spriteRenderer.sprite.bounds); 
			Gizmos.DrawWireCube(bounds.center, bounds.size);

			// Rendered to get best-fit in SymbolAnimator's space
			Gizmos.color = Color.cyan;
			Gizmos.matrix = this.transform.localToWorldMatrix;
			bounds = CommonBounds.getRenderedSpriteBoundsRelativeTo(spriteRenderer, this.transform); 
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}
	}

	void drawChildSkinnedBoundsGizmos()
	{
		// SkinnedMesh Test: Elvira01 & Ted01 symbol animations (stretch/warp)
		var skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(false);
		foreach (var skinnedRenderer in skinnedMeshRenderers)
		{
			// In local space
			Gizmos.color = Color.red;
			Gizmos.matrix = skinnedRenderer.localToWorldMatrix;
			var bounds = CommonBounds.getSkinnedMeshRendererBounds(skinnedRenderer);
			Gizmos.DrawWireCube(bounds.center, bounds.size);

			// In world space
			Gizmos.color = Color.green;
			Gizmos.matrix = Matrix4x4.identity;
			bounds = CommonBounds.getSkinnedMeshRendererBoundsInWorldSpace(skinnedRenderer);
			bounds.extents = new Vector3(bounds.extents.x + 0.01f, bounds.extents.y + 0.01f, bounds.extents.z); // for editor visualization
			Gizmos.DrawWireCube(bounds.center, bounds.size);

			// In SymbolAnimator's space
			Gizmos.color = Color.cyan;
			Gizmos.matrix = transform.localToWorldMatrix;
			bounds = CommonBounds.getSkinnedMeshRendererBoundsRelativeTo(skinnedRenderer, this.transform);
			bounds.extents = new Vector3(bounds.extents.x + 0.02f, bounds.extents.y + 0.02f, bounds.extents.z); // for editor visualization
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}
	}

	void drawChildBoxColliderBoundsGizmos()
	{
		// Lots of Colliders on: Gen11  https://screencast.com/t/bevrB7dsIV27
		var colliders = GetComponentsInChildren<BoxCollider>(false);
		foreach (var collider in colliders)
		{
			// world-space bounds...
			Gizmos.color = Color.red;
			Gizmos.matrix = Matrix4x4.identity;
			var bounds = collider.bounds;  //world space
			Gizmos.DrawWireCube(bounds.center, bounds.size);

			// In desired space...
			Gizmos.color = Color.blue;
			Gizmos.matrix = this.transform.localToWorldMatrix;
			var colliderToLocalMatrix = this.transform.worldToLocalMatrix * collider.transform.localToWorldMatrix;
			var colliderBounds = new Bounds(collider.center, collider.size);
			bounds = CommonBounds.transformBounds(colliderToLocalMatrix, colliderBounds); 
			bounds.extents = new Vector3(bounds.extents.x + 0.01f, bounds.extents.y + 0.01f, bounds.extents.z); // for editor visualization
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}
	}
}

