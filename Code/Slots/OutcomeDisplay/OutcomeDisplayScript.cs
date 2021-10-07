using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Abstract base class for all script that display outcomes, such as paylines & clusters.
*/

public abstract class OutcomeDisplayScript : TICoroutineMonoBehaviour
{
	private const int LINE_ONLY_OFFSET = 500;
	protected const float FADE_DURATION = .25f;	///< Amount of seconds it takes to fade the line in and out.

	public MeshFilter meshFilter;			// The final mesh filter that is used for the boxes/lines display.
	public Material lineMaterial;			// A copy of this is made for each of the three parts.
	public Color outlineColor = Color.black;
	public Color highlightColor = Color.white;
	public bool disableCachedMaterials = false;	// Need way to specify an outcome display script that has it's own set of materials

	[HideInInspector] public bool isShowing = false;

	protected int paylineIndex;	// This is also treated as clusterIndex when used by ClusterScript.

	protected ReelGame _gameInstance;
	protected ReelSetup reelSetup = null; // storing this when the game isn't running so we can use it to correctly position the 

	private Material[] instanceMaterials = null; // Materials will be created and stored for this instance, only if disableCachedMaterials is true for this instance

	// Prepare for combining lines and boxes into a single mesh.
	protected List<CombineInstance>[] combineInstances = new List<CombineInstance>[]
	{
		new List<CombineInstance>(),
		new List<CombineInstance>(),
		new List<CombineInstance>()
	};

	/// Setter/getter for all the line colors.
	public virtual Color color
	{
		set
		{
			_color = value;
			_color.a = _alpha;
			updateColors();
		}

		get
		{
			return _color;
		}
	}
	protected Color _color = Color.yellow;

	/// Setter/getter for the boxes and connecting lines alpha.
	public float alpha
	{
		set
		{
			_alpha = value;
			_color.a = _alpha;
			updateColors();
		}

		get
		{
			return _alpha;
		}
	}
	protected float _alpha = 1f;
	
	// Combines all the boxes and lines into a single mesh with three materials.
	protected virtual void combineMeshes()
	{
		// First combine outline parts into a single outline mesh, inline parts into a single inline mesh,
		// and highlight parts into a single highlight mesh. Each mesh will have a single material.
		CombineInstance[] finalCombine = new CombineInstance[3];
		Mesh[] partsMeshes = new Mesh[3];
		for (int i = 0; i < 3; i++)
		{
			partsMeshes[i] = new Mesh();
			partsMeshes[i].MarkDynamic();
			partsMeshes[i].CombineMeshes(combineInstances[i].ToArray(), true, false);
			// Prepare to use the newly combined mesh as part of the final combine.
			finalCombine[i] = createCombineInstance(partsMeshes[i]);
		}
		
		// Now combine the outline, inline and highlight meshes into a single mesh with three materials.
		if (Application.isPlaying)
		{
			meshFilter.mesh.CombineMeshes(finalCombine, false, false);
		}
		else
		{
			if (meshFilter.sharedMesh == null)
			{
				meshFilter.sharedMesh = new Mesh();
			}
			meshFilter.sharedMesh.CombineMeshes(finalCombine, false, false);
		}

		// Release the mesh parts back to the mesh pool.
		DrawerCache.releaseUsedMeshes();
	}

	/// Fades in the payline and shows it for the specified number of seconds before fading out.
	public virtual IEnumerator show(float seconds, float delay = 0.0f)
	{
		if (delay > 0.0f)
		{
			yield return new WaitForSeconds(delay);
		}

		isShowing = true;

		float fadeLife = 0;

		while (fadeLife < FADE_DURATION)
		{
			yield return null;
			fadeLife += Time.deltaTime;
			this.alpha = fadeLife / FADE_DURATION;
		}

		if (seconds > 0)
		{
			// If seconds is 0, show the lines infinitely until fade is called separately.
			yield return new WaitForSeconds(seconds);
			yield return StartCoroutine(hide());
		}
	}

	/// Starts fading the payline boxes then returns coroutine when done.
	public virtual IEnumerator hide()
	{
		float fadeLife = 0;

		while (fadeLife < FADE_DURATION)
		{
			yield return null;
			fadeLife += Time.deltaTime;
			// Use Min() just in case this is called before at full alpha.
			this.alpha = Mathf.Min(this.alpha, 1f - (fadeLife / FADE_DURATION));
		}

		isShowing = false;
	}

	// Get the offset for the independent reel that a symbol landed on, used to correctly determine where the symbol is
	private int getIndependentReelCenterPositionOffset(int reelIndex, int boxIndex)
	{
		int targetRow = boxIndex;

		for (int independentReelIndex = _gameInstance.engine.independentReelArray[reelIndex].Count - 1; independentReelIndex >= 0; independentReelIndex--)
		{
			SlotReel reel = _gameInstance.engine.independentReelArray[reelIndex][independentReelIndex];
			int rowDivision = targetRow / reel.reelData.visibleSymbols;
			int rowRemainder = targetRow % reel.reelData.visibleSymbols;

			if (rowDivision == 0 || (rowDivision == 1 && rowRemainder == 0))
			{
				return rowRemainder;
			}
			else
			{
				targetRow -= reel.reelData.visibleSymbols; 
			}
		}

		// probably shouldn't ever get here, but default to 0 if we do
		return 0;
	}

	/// Returns the position of the center of a reel's position in the payline.
	protected Vector3 getReelCenterPosition(SlotReel[] gameInstance_engine_reelArray, int reelIndex, int boxIndex, int layer = -1)
	{
		float spaceBetweenCells = _gameInstance.symbolVerticalSpacingWorld * boxIndex;
		if (_gameInstance.engine.reelSetData != null && _gameInstance.engine.reelSetData.isIndependentReels)
		{
			// We need to calculate the offset on the specific independentReel which this symbol lives on
			spaceBetweenCells = _gameInstance.symbolVerticalSpacingWorld * getIndependentReelCenterPositionOffset(reelIndex, boxIndex);
		}
		GameObject reel = _gameInstance.getReelRootsAt(reelIndex, _gameInstance.engine.getVisibleSymbolsCountAt(gameInstance_engine_reelArray,reelIndex,-1) - 1 - boxIndex, layer);
		return reel.transform.position + Vector3.up * spaceBetweenCells;
	}

	// Returns the position of the center of a reel's position in the payline when the game isn't running, relies on info from ReelSetup script
	protected Vector3 getReelCenterPositionWhileApplicationNotRunning(int reelIndex, int boxIndex, ReelSetup.LayerInformation info)
	{
		// we're going to need data from the ReelSetup script, so try and grab and cache that
		if (reelSetup == null)
		{
			// try and grab it from the game, every game should have one attached, at the same level as the ReelGame itself
			reelSetup = _gameInstance.GetComponent<ReelSetup>();

			if (reelSetup == null)
			{
				Debug.LogError("OutcomeDisplayScript.getReelCenterPosition() - Couldn't find ReelSetup script attached to game!");
				return Vector3.zero;
			}
		}

		float spaceBetweenCells = _gameInstance.symbolVerticalSpacingWorld * boxIndex;
		if (reelSetup.isIndependentReelGame)
		{
			// We need to calculate the offset on the specific independentReel which this symbol lives on
			spaceBetweenCells = _gameInstance.symbolVerticalSpacingWorld * reelSetup.getIndependentReelCenterPositionOffset(reelIndex, boxIndex, info.independentReelVisibleSymbolSizes);
		}

		GameObject reel = null;

		if (reelSetup.layerInformation != null && reelSetup.layerInformation.Length > 0 && info.layer < reelSetup.layerInformation.Length)
		{
			reel = _gameInstance.getReelRootsAtWhileApplicationNotRunning(reelIndex, boxIndex, info.layer, info.independentReelVisibleSymbolSizes);
		}

		if (reel == null)
		{
			Debug.LogError("OutcomeDisplayScript.getReelCenterPosition() - reel was null!");
			return Vector3.zero;
		}
		else
		{
			return reel.transform.position + Vector3.up * spaceBetweenCells;
		}
	}

	/// Updates the colors for all boxes and lines of this payline.
	protected virtual void updateColors()
	{
		// Handle the boxes/lines color.
		if (meshFilter != null)
		{
			// Apparently this can be null right after being destroyed,
			// but the fading coroutine still calls this probably one last time.
			meshFilter.GetComponent<Renderer>().materials = getMaterials(false, _color);
		}
	}

	protected Material[] getMaterials(bool isForLineOnly, Color newColor)
	{
		if (disableCachedMaterials)
		{
			return getInstanceMaterials(newColor);
		}
		else
		{
			return getCachedMaterials(isForLineOnly, newColor);
		}
	}

	/// Default way of getting materials, using a cached set
	protected Material[] getCachedMaterials(bool isForLineOnly, Color newColor)
	{
		int offset = paylineIndex + (isForLineOnly ? LINE_ONLY_OFFSET : 0);
		
		Material outlineMat = null;
		Material inlineMat = null;
		Material highlightMat = null;
		DrawerCache.materialPool.TryGetValue(DrawerCache.OUTLINE_KEY + offset, out outlineMat);
		DrawerCache.materialPool.TryGetValue(DrawerCache.INLINE_KEY + offset, out inlineMat);
		DrawerCache.materialPool.TryGetValue(DrawerCache.HIGHLIGHT_KEY + offset, out highlightMat);
		
		if (outlineMat == null)
		{
			outlineMat = new Material(lineMaterial);
			outlineMat.renderQueue = 3100;

			if (DrawerCache.materialPool.TryGetValue(DrawerCache.OUTLINE_KEY + offset, out Material oldMaterial))
			{
				if (oldMaterial != null)
				{
					Destroy(oldMaterial);
				}
			}
			DrawerCache.materialPool[DrawerCache.OUTLINE_KEY + offset] = outlineMat;
		}

		if (inlineMat == null)
		{
			inlineMat = new Material(lineMaterial);
			inlineMat.renderQueue = 3200;

			if (DrawerCache.materialPool.TryGetValue(DrawerCache.INLINE_KEY + offset, out Material oldMaterial))
			{
				if (oldMaterial != null)
				{
					Destroy(oldMaterial);	
				}
			}
			DrawerCache.materialPool[DrawerCache.INLINE_KEY + offset] = inlineMat;
		}

		if (highlightMat == null)
		{
			highlightMat = new Material(lineMaterial);
			highlightMat.renderQueue = 3300;

			if (DrawerCache.materialPool.TryGetValue(DrawerCache.HIGHLIGHT_KEY + offset, out Material oldMaterial))
			{
				if (oldMaterial != null)
				{
					Destroy(oldMaterial);	
				}
			}
			DrawerCache.materialPool[DrawerCache.HIGHLIGHT_KEY + offset] = highlightMat;
		}
			
		outlineMat.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a * newColor.a);
		inlineMat.color = newColor;
		highlightMat.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightColor.a * newColor.a);

		return new Material[]
		{
			outlineMat,
			inlineMat,
			highlightMat
		};
	}

	/// Get instance materials, only happens if disableCachedMaterials is true
	protected Material[] getInstanceMaterials(Color newColor)
	{
		Material outlineMat = null;
		Material inlineMat = null;
		Material highlightMat = null;

		if (instanceMaterials == null)
		{
			instanceMaterials = new Material[3];

			outlineMat = new Material(lineMaterial);
			outlineMat.renderQueue = 3100;

			inlineMat = new Material(lineMaterial);
			inlineMat.renderQueue = 3200;

			highlightMat = new Material(lineMaterial);
			highlightMat.renderQueue = 3300;

			instanceMaterials[0] = outlineMat;
			instanceMaterials[1] = inlineMat;
			instanceMaterials[2] = highlightMat;
		}
		else
		{
			outlineMat = instanceMaterials[0];
			inlineMat = instanceMaterials[1];
			highlightMat = instanceMaterials[2];
		}

		outlineMat.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, outlineColor.a * newColor.a);
		inlineMat.color = newColor;
		highlightMat.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightColor.a * newColor.a);

		return instanceMaterials;
	}

	// Helper function for preparing to combine payline parts into a single mesh.
	protected void prepareCombineParts(List<CombineInstance>[] combineInstances, CombineInstance[] parts)
	{
		for (int i = 0; i < 3; i++)
		{
			combineInstances[i].Add(parts[i]);
		}
	}

	// Creates a CombineInstance from the given MeshFilter, and returns it to be put into the collection to combine.
	public static CombineInstance createCombineInstance(Mesh mesh)
	{
		CombineInstance inst = new CombineInstance();
		inst.mesh = mesh;
		return inst;
	}
}
