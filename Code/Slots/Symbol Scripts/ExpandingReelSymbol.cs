using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
This is the derived controller for expanding reel symbols which are built through code, allows for one texture to be used for 1x1, 1x2, 1x3, 3x3, etc..
*/
[RequireComponent(typeof(UIPanel))]
public class ExpandingReelSymbol : ExpandingReelSymbolBase
{
	private static int[] backgroundTriangles = new int[] {0, 2, 1, 2, 3, 1}; // Triangle indecies to create a simple quad.

	[SerializeField] private MeshFilter backgroundFilter; 											// Used to render the symbols main image  
    [SerializeField] private List<AnchorData> anchoredPieces = new List<AnchorData>();				// A list of transform, and their respective anchoring wrt.
	[SerializeField] private List<SizingData> widgets = new List<SizingData>();						// A List of widget that need sizing with the symbol
	[SerializeField] private List<ConfigurationData> symbolData = new List<ConfigurationData>();	// A List of configurations for this symbol.

	private ConfigurationData currentData;								// Current configuration
    private Dictionary<string, ConfigurationData> symbolDataLookup;   	// Configuration lookup, base on key "widthXheight"
	private Camera renderCam;											// Used to position and scale NGUI ui in perspective space
	private UIPanel framePanel;											// Used to toggle rendering of UI peices.
	private Rect currentSize;  											// Cache of the current screen space of the symbol.
	
	protected override void OnEnable()
	{
		base.OnEnable();

		this.backgroundFilter.GetComponent<Renderer>().enabled = true;
		this.framePanel.enabled = true;
	}

	protected override void OnDisable()
	{
		base.OnDisable();

		this.backgroundFilter.GetComponent<Renderer>().enabled = false;
		this.framePanel.enabled = false;
	}
	
    /// Init function, ensures the dictionary has been set up properly
    public override void init()
    {
		framePanel = GetComponent<UIPanel>();
        symbolDataLookup = new Dictionary<string, ConfigurationData>();
        foreach (ConfigurationData data in this.symbolData)
        {
            symbolDataLookup.Add(data.cellsWide + "x" + data.cellsHigh, data);
        }

		//Find Reel Camera
		foreach (Camera cam in Camera.allCameras)
		{
			if (cam.gameObject.layer == this.gameObject.layer)
			{
				renderCam = cam;
				break;
			}
		}

		base.init();
	} 

    /// Sets the size for the overlay
	public override void setSize(ReelGame reelGame, int cellsWide, int cellsHigh)
	{
		playExpandingSymbolSound(cellsWide, reelGame.isFreeSpinGame());

		string key = cellsWide + "x" + cellsHigh;
		
		if (symbolDataLookup.ContainsKey(key))
		{
			this.rebuild(reelGame, symbolDataLookup[key]);            
		}
		else
		{
			Debug.LogException(new KeyNotFoundException("Data for a cell size of [" + cellsWide + ", " + cellsHigh + "] not found!"), this);
		}
	}

	/// Get a list of the supported sizes for this symbol
	protected override List<ExpandingReelSymbolBase.SupportedSize> getSupportedSizeList()
	{
		List<ExpandingReelSymbolBase.SupportedSize> supportedSizeList = new List<ExpandingReelSymbolBase.SupportedSize>();

		foreach (ConfigurationData data in symbolData)
        {
        	supportedSizeList.Add(new SupportedSize(data.cellsWide, data.cellsHigh, data.cellsWide + "x" + data.cellsHigh));
        }

        return supportedSizeList;
	}

	/// Set the alpha for the symbol
	protected override void setSymbolAlpha(float alpha)
	{
		if (alpha == 0.0f)
		{
			this.backgroundFilter.GetComponent<Renderer>().material.color = Color.clear;
		}
		else if (alpha == 1.0f)
		{
			this.backgroundFilter.GetComponent<Renderer>().material.color = Color.white;
		}
		else
		{
			this.backgroundFilter.GetComponent<Renderer>().material.color = new Color(1, 1, 1, alpha);
		}

		this.framePanel.alpha = alpha;
	}
	
	#region Private Methods
    /// <summary>
    /// Rebuilds the meshes for the overlay
    /// </summary>
    /// <param name="reelGame"></param>
	private void rebuild(ReelGame reelGame, ConfigurationData data)
	{
		//Reset Previous Scales
		if (this.currentData != null)
		{
			foreach (ConfigurationData.ScaleData scaleData in this.currentData.scales)
			{
				foreach(Transform tx in scaleData.transforms)
				{
					tx.localScale = Vector3.one;
				}
			}
		}

		Vector2 payBoxSize = new Vector2(1.8f, 1.8f);
		Vector2 reelBoxPadding = new Vector2();
		float spaceBetweenReels = 0f;
		Vector2 margins = new Vector2(0.04f, 0.08f);
		int totalCellsHigh = 3;
		int totalCellsWide = 5;
		
		if (reelGame != null)
		{
			payBoxSize = reelGame.payBoxSize;
			reelBoxPadding = reelGame.reelBoxPadding;
			spaceBetweenReels = reelGame.spaceBetweenReels;
			margins = new Vector2(
				reelGame.getReelRootsAt(1).transform.position.x - reelGame.getReelRootsAt(0).transform.position.x,
				reelGame.getSymbolVerticalSpacingAt(0) - payBoxSize.y);
			SlotReel[]reelArray = reelGame.engine.getReelArray();
			totalCellsHigh = reelArray[0].visibleSymbols.Length;
			totalCellsWide = reelArray.Length;
		}
		
		this.currentData = data;
		Rect rect = 
			new Rect(
				transform.position.x,
				transform.position.y,
				payBoxSize.x * currentData.cellsWide + 2 * reelBoxPadding.x + spaceBetweenReels * (currentData.cellsWide - 1),
				payBoxSize.y * currentData.cellsHigh + 2 * reelBoxPadding.y); //Outer vertices rect

		// nothing seems to use the rectangle position, so i'm not sure what's happening here?
		rect.x += (currentData.cellsWide - 1) * margins.x;
		rect.y += (currentData.cellsHigh - 1) * margins.y;
		if (currentData.cellsHigh >= totalCellsHigh)
		{
			rect.x += 2 * margins.x;
		}
		if (currentData.cellsWide >= totalCellsWide)
		{
			rect.y += 2 * margins.y;
		}
		rect = new Rect(rect.x - (rect.width / 2), rect.y - (rect.height / 2), rect.width, rect.height);
		this.currentSize = rect;
			
		this.computeBackgroundMesh(this.backgroundFilter.mesh);
		foreach (SpriteSwapData swap in this.currentData.spriteSwaps)
		{
			swap.sprite.spriteName = swap.spriteName;
			if (swap.makePixelPerfect)
			{
				swap.sprite.MakePixelPerfect();
			}
		}
		foreach (AnchorData item in this.anchoredPieces)
		{
			this.setPositionFromAnchor(item);
		}
		foreach (SizingData wdata in this.widgets)
		{
			this.setSizeData(wdata);
		}
		foreach (SizingData wdata in this.currentData.sizes)
		{
			this.setSizeData(wdata);
		}
		foreach (ConfigurationData.ScaleData scaleData in this.currentData.scales)
		{
			foreach(Transform tx in scaleData.transforms)
			{
				tx.localScale = scaleData.scale;
			}
		}
	}

    private void computeBackgroundMesh(Mesh mesh)
    {
		if (mesh == null)
		{
        	mesh = new Mesh();
		}
		
        //Set up the rectangles
		Rect uvo = currentData.backgroundUV; //Outer uv rect
		Rect vo = new Rect(0, 0, this.currentSize.width, this.currentSize.height); //Outer vertices rect
        
        //Center Align
        vo = new Rect(vo.x - (vo.width / 2),
                      vo.y - (vo.height / 2),
                      vo.width,
                      vo.height);

        //Vertices
        Vector3[] verts = new Vector3[4];
        verts[0] = new Vector3(vo.xMin, vo.yMin, 0); //Bottom Left
        verts[1] = new Vector3(vo.xMax, vo.yMin, 0); //Bottom Right
        verts[2] = new Vector3(vo.xMin, vo.yMax, 0); //Top Left
        verts[3] = new Vector3(vo.xMax, vo.yMax, 0); //Top Right

        //UVs
        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(uvo.xMin, uvo.yMin); //Top Left
        uvs[1] = new Vector2(uvo.xMax, uvo.yMin); //Top Right       
        uvs[2] = new Vector2(uvo.xMin, uvo.yMax); //Bottom Left
        uvs[3] = new Vector2(uvo.xMax, uvo.yMax); //Bottom Right

        //Update that mesh!
        mesh.vertices = verts; ///Note: we do not need to call mesh.RecalculateBounds because setting the vertices does that for us
        mesh.triangles = ExpandingReelSymbol.backgroundTriangles;
        mesh.uv = uvs;
    }
	/// <summary>
	/// Positions a anchor data acording to the current size.
	/// </summary>
	/// <param name="borderItem">Border item.</param>
    private void setPositionFromAnchor(AnchorData borderItem)
    {
		Rect vo = new Rect(0, 0, currentSize.width, currentSize.height); //Outer vertices rect
        vo = new Rect(vo.x - (vo.width / 2), vo.y - (vo.height / 2), vo.width, vo.height);

        Vector3 position = this.transform.position;
               
        switch (borderItem.anchor)
        {
            case UIWidget.Pivot.TopLeft:
                position += new Vector3(vo.xMin, vo.yMax, 0);
                break;
            case UIWidget.Pivot.Top:
				position += new Vector3(0, vo.yMax, 0);
                break;
            case UIWidget.Pivot.TopRight:
                position += new Vector3(vo.xMax, vo.yMax, 0);
                break;
            case UIWidget.Pivot.Left:
                position += new Vector3(vo.xMin, 0, 0);
                break;
            case UIWidget.Pivot.Center:
                position += new Vector3(0, 0, 0);
                break;
            case UIWidget.Pivot.Right:
                position += new Vector3(vo.xMax, 0, 0);
                break;
            case UIWidget.Pivot.BottomLeft:
                position += new Vector3(vo.xMin, vo.yMin, 0);
                break;
            case UIWidget.Pivot.Bottom:
                position += new Vector3(0, vo.yMin, 0);
                break;
            case UIWidget.Pivot.BottomRight:
                position += new Vector3(vo.xMax, vo.yMin, 0);
                break;
            default:
                break;
        }
		position.z = borderItem.item.position.z;
        borderItem.item.position = position;
    }


	/// <summary>
	/// Sizes a peice of Sizing Data, which contains a Sprite, and directive on how it should be sized.
	/// </summary>
	/// <param name="data">The sizing data to be set.</param>
	private void setSizeData(SizingData data)
	{
		Rect rect = new Rect(this.currentSize);
		rect = new Rect(rect.x - (rect.width / 2), rect.y - (rect.height / 2), rect.width, rect.height);

		// Find the tl/br positions.
		Vector2int tl = NGUIExt.screenPositionOfWorld(renderCam, new Vector3(rect.xMin, rect.yMax, transform.position.z));
		Vector2int br = NGUIExt.screenPositionOfWorld(renderCam, new Vector3(rect.xMax, rect.yMin, transform.position.z));
		tl.x = (int)((float)tl.x / NGUIExt.pixelFactor);
		tl.y = (int)((float)tl.y / NGUIExt.pixelFactor);
		br.x = (int)((float)br.x / NGUIExt.pixelFactor);
		br.y = (int)((float)br.y / NGUIExt.pixelFactor);
		
		int width = br.x - tl.x;
		int height = tl.y - br.y;
		Vector3 size = new Vector3(width, height, 0);
		UIAtlas.Sprite sprite = data.sprite.atlas.GetSprite(data.sprite.spriteName);
		if (data.isVertical)
		{
			float tmp = size.x;
			size.x = size.y;
			size.y = tmp;
		}
		size.Scale(data.scale);

		if  (data.sprite.type == UISprite.Type.Filled)
		{
			data.sprite.fillAmount = size.x / (sprite.outer.width * data.sprite.atlas.pixelSize);
		}
		else
		{
			if (data.pixelPerfectWidth)
			{
				size.x = (sprite.outer.width * data.sprite.atlas.pixelSize);
			}
			if (data.pixelPerfectHeight)
			{
				size.y = (sprite.outer.height * data.sprite.atlas.pixelSize);
			}

			data.sprite.transform.localScale = size;
		}
	}
	#endregion Private Methods

	#region Data Classes
	[System.Serializable()]
	public class AnchorData : System.Object
	{
		#region Class Variables
		// The transform to be anchored
		public Transform item;
		//The anchor relative to the center of the background mesh.
		public UIWidget.Pivot anchor;
		#endregion Class Variables
	}

	/// <summary>
	/// This will be used to hold data for the expanding reel symbols
	/// </summary>
	[System.Serializable()]
	public class ConfigurationData : System.Object 
	{	
		#region Class Variables
		//Defines the number of cells wide this data is authored for.
		public int cellsWide;
		//Defines the number of cells high this data is authored for.
		public int cellsHigh;
		//Defines the UV's to use on the background mesh filter.
		public Rect backgroundUV;
		//Change widget sizes for this configuration.
		public List<SizingData> sizes;
		//Defines a list of scalled items for this configuration.
		public List<ScaleData> scales;
		//Defines a list of sprites that need to be swapped for this configuration.
		public List<SpriteSwapData> spriteSwaps = new List<SpriteSwapData>(); /// A List of sprite, and sprite name to swap them to.  Please be sure to always specify a sprite swap for all configurations if you specify one for any configuration.  Otherwise the sprite may look wrong.
		#endregion Class Variables

		[System.Serializable()]
		public class ScaleData : System.Object
		{
			#region Class Variables
			//The transform to be scaled
			public List<Transform> transforms;
			//The scale for the transform.
			public Vector3 scale;
			#endregion Class Variables
		}
	}

	[System.Serializable()]
	public class SizingData : System.Object
	{
		#region Class Variables
		//The sprite to be sized.  The size is relative to the size of the background image for the current configuration.
		public UISprite sprite;
		//The scale, relative to the size of the configuration... NOTE scale if off by a factor of two, not sure why, please use (2,2,1) for full size, or (1,1,1) for half size.
		public Vector3 scale;
		//Overrides the scaled width, and uses the pixel perfect width of the sprite.
		public bool pixelPerfectWidth;
		//Overrides the scaled height, and uses the pixel perfect height of the sprite.
		public bool pixelPerfectHeight;
		//Defines that the sprite has been rotated to be vertical.
		public bool isVertical;
		#endregion Class Variables
	}

	[System.Serializable()]
	public class SpriteSwapData : System.Object
	{
		#region Class Variables
		//The sprite to be swapped.
		public UISprite sprite;
		//The name of the new sprite to be swapped in.
		public string spriteName;
		//True to make pixel perfect.
		public bool makePixelPerfect;
		#endregion Class Variables
	}
	#endregion Data Classes
}