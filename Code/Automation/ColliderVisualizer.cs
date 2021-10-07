using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This class holds the collider visualizer information.
// This is separated from the other scripts so that non-editor scripts can also call and use these methods.
public class ColliderVisualizer : TICoroutineMonoBehaviour
{

	public static readonly Color colliderDisabledColor = new Color(219/255.0f, 0.0f, 0.0f, 1.0f);		// Dark Green
	public static readonly Color colliderEnabledColor =  new Color(0.0f, 219/255.0f, 0.0f, 1.0f);		// Light Green
	public static readonly Color colliderPressedColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);		// Light Blue

	// TODO this ShaderCache.find will not work in build!!!
	// Add it to LADI resources folder.
	private static Shader _lineShader = null;
	public static Shader lineShader
	{
		get {
			if (_lineShader == null)
			{
				_lineShader = ShaderCache.find("Particles/Alpha Blended");
			}
			return _lineShader;
		}
	}
	public const float LINE_STARTING_WIDTH = 0.049f;
	public const float LINE_ENDING_WIDTH = 0.019f;

	private Dictionary<LineRenderer, bool> allColliderRenderers; // boolean keeps track if thel ine renderer has been clicked on by tramp.
	private int emptyObjectCount = 0; // Amount of line renderers that no longer exist but are still tracked in our list.
	private const int maxEmptyObjects = 29; // Maximum amount of null game objects our list can store before resetting.
	private bool isContinouslyVisualizingColliders = false;
	private float visualizeColliderInterval = 1.0f;
	public bool enableContinuousVisualColliders
	{
		get
		{
			return _enableContinuousVisualColliders;
		}
		set
		{
			_enableContinuousVisualColliders = value;
			if (!_enableContinuousVisualColliders)
			{
				if (isContinouslyVisualizingColliders)
				{
					StopCoroutine("visualizeCollidersInIntervals");
					isContinouslyVisualizingColliders = false;
					disableVisualizer(true);
				}
			}
			else if (!isContinouslyVisualizingColliders) // Check that we don't already have a coroutine running.
			{
				active = true; // activate visual colliders
				isContinouslyVisualizingColliders = true;
				StartCoroutine(visualizeCollidersInIntervals(visualizeColliderInterval));
			}
		}
	}
	private bool _enableContinuousVisualColliders = false;

	public static ColliderVisualizer instance
	{
		get
		{
			if (_instance != null)
			{
				return _instance;
			}
			else
			{
				_instance = new GameObject("LADI Collider Visualizer").AddComponent<ColliderVisualizer>();
				return _instance;
			}
		}
		private set
		{
			_instance = value;
		}
	}
	private static ColliderVisualizer _instance;

	public bool active = false;

	void Awake()
	{
		DontDestroyOnLoad(this.gameObject);
	}

	// Removes existing colldiers in the scene.
	// If we ignore the colliders tramp clicks on then don't disable those.
	public void disableVisualizer(bool ignoreCollidersTrampClicked = false)
	{
		active = false;
		if (!active && allColliderRenderers != null)
		{
			if (emptyObjectCount > maxEmptyObjects)
			{
				Dictionary<LineRenderer, bool> nonemptyLineRenderers;
				nonemptyLineRenderers = new Dictionary<LineRenderer, bool>();
				emptyObjectCount = 0;
				foreach (KeyValuePair<LineRenderer, bool> line in allColliderRenderers)
				{
					if (line.Key != null)
					{
						// We 
						line.Key.enabled = false || (ignoreCollidersTrampClicked && line.Value);
						nonemptyLineRenderers.Add(line.Key, line.Value);
					}
				}
				allColliderRenderers.Clear();
				allColliderRenderers = nonemptyLineRenderers;
			}
			else
			{
				foreach (KeyValuePair<LineRenderer, bool> line in allColliderRenderers)
				{
					if (line.Key != null)
					{
						line.Key.enabled = false || (ignoreCollidersTrampClicked && line.Value);
					}
					else
					{
						emptyObjectCount++;
					}
				}
			}
		}
	}

	// Visualize an object given a Collider
	// Draws the collider and applies the appropriate colors (no animation).
	// Returns the line renderer that visualizes the collider.
	public static LineRenderer drawVisualCollider(Collider collider, bool animate = true, bool isClickedByTramp = false)
	{
		LineRenderer colliderRenderer = CommonGameObject.drawCollider(collider, LINE_ENDING_WIDTH);
		return setupLineRenderer(colliderRenderer, animate, isClickedByTramp, collider.enabled);
	}

	// Visualize an object given bounds. (i.e objects using SwipeArea)
	// Draws a box given bounds and applies the appropriate colors (no animation).
	// Returns the line renderer that visualizes the collider.
	public static LineRenderer drawVisualBounds(GameObject obj, Bounds bounds, bool animate = true, bool isClickedByTramp = false, bool isEnabled = true)
	{
		LineRenderer boundsRenderer = CommonGameObject.drawBounds(obj, bounds, LINE_ENDING_WIDTH);
		return setupLineRenderer(boundsRenderer, animate, isClickedByTramp, isEnabled);
	}

	// Helper function that sets up colors, width, and additional visuals for passed in LineRenderer
	private static LineRenderer setupLineRenderer(LineRenderer lineRenderer, bool animate = true, bool isClickedByTramp = false, bool isEnabled = true)
	{
		if (lineRenderer == null)
		{
			return null;
		}

		if (instance.allColliderRenderers == null)
		{
			instance.allColliderRenderers = new Dictionary<LineRenderer, bool>();
		}
		if (instance.allColliderRenderers != null && !instance.allColliderRenderers.ContainsKey(lineRenderer))
		{
			instance.allColliderRenderers.Add(lineRenderer, isClickedByTramp);
		}

		// Set the material properties and width if the shader is available.
		if (lineShader != null)
		{
			lineRenderer.material = new Material(lineShader);
			lineRenderer.widthMultiplier = LINE_ENDING_WIDTH;
			if (isEnabled)
			{
				lineRenderer.startColor = colliderEnabledColor;
				lineRenderer.endColor = colliderEnabledColor;

				// Animate the collider, otherwise just set it to the ending with and color.
				if (animate)
				{
					instance.StartCoroutine(CommonGameObject.fadeLineRendererColorAndWidth(lineRenderer, colliderPressedColor, colliderEnabledColor, LINE_STARTING_WIDTH, LINE_ENDING_WIDTH, 1.0f));
					lineRenderer.widthMultiplier = LINE_STARTING_WIDTH;
				}
			}
			else
			{
				lineRenderer.startColor = colliderDisabledColor;
				lineRenderer.endColor = colliderDisabledColor;
			}
		}

		return lineRenderer;
	}

	// Draw all colliders in the scene
	public void drawAllColliders(bool animate = true)
	{
		Collider [] collidersInScene = (Collider []) GameObject.FindObjectsOfType(typeof(Collider));
		foreach (Collider colliderObject in collidersInScene)
		{
			if (colliderObject != null)
			{
				drawVisualCollider(colliderObject, animate);
			}
		}
	}

	// Draw all objects that have a SwipeArea attached 
	public void drawAllSwipeAreas(bool animate = true)
	{
		SwipeArea [] swipeAreasInScene = GameObject.FindObjectsOfType<SwipeArea>();
		foreach (SwipeArea swipeArea in swipeAreasInScene)
		{
			if (swipeArea != null)
			{
				Bounds bounds = new Bounds();
				bounds.center = swipeArea.center;
				bounds.extents = swipeArea.size; // NOTE: Using reelBounds.size will result in the ColliderVisualizer displaying a smaller box
				drawVisualBounds(swipeArea.gameObject, bounds, animate: animate);
			}
		}
	}

	// draws all colliders in a given interval.
	private IEnumerator visualizeCollidersInIntervals(float interval)
	{
		while (enableContinuousVisualColliders)
		{
			drawAllColliders(false);
			drawAllSwipeAreas(false);
			yield return new WaitForSeconds(interval);
		}
	}

	void OnDestroy()
	{
		Destroy(this.gameObject);
		instance = null;
	}
}
