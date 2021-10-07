using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/**
Controls loading and displaying wing textures for bonus games.
Includes common functions that are also used by BigWinWings.cs.
*/

public class BonusGameWings : TICoroutineMonoBehaviour
{
	public MeshFilter leftMeshFilter;
	public MeshFilter rightMeshFilter;

	public static BonusGameWings instance = null;

	private const string WINGS_IMAGE_PATH = "assets/data/common/bundles/initialization/images/wings/default_wing.png";

	public enum WingSideEnum
	{
		Left = 0,
		Right = 1
	}

	void Awake()
	{
		instance = this;
		leftMeshFilter.mesh = leftWingMesh;
		rightMeshFilter.mesh = rightWingMesh;
	}
	
	void Update()
	{
		// If the wings are shown in the lobby somehow,
		// hide them. This could happen in a weird situation
		// where the game is reset in the middle of a bonus game.
		if (GameState.isMainLobby)
		{
			hide();
		}
	}

	public void show()
	{
		if (ChallengeGame.instance != null)
		{
			adjustWingZDepth(ChallengeGame.instance.wingsInForeground);
		}
		else
		{
			Debug.LogWarning("Wings should have a challenge game instance to check, but this one does not!");
		}

		loadTextures(leftMeshFilter, rightMeshFilter, false);
		gameObject.SetActive(true);
	}

	/// Automatically adjust the wing depth based on if they were flagged to sit in the foreground
	private void adjustWingZDepth(bool wingsInForeground)
	{
		if (wingsInForeground)
		{
			CommonTransform.setZ(gameObject.transform, -500);
		}
		else
		{
			CommonTransform.setZ(gameObject.transform, 500);
		}
	}

	// This function is just like the the show function, but it bypasses a check to make sure that we load the right wings based
	// off of which game mode we are in. Some newer games need both challenge game wings and freespin wings. This lets that happen.
	public void forceShowNormalWings(bool wingsInForeground)
	{
		adjustWingZDepth(wingsInForeground);

		forceLoadBaseTextures(leftMeshFilter, rightMeshFilter);
		gameObject.SetActive(true);
	}

	public void forceShowChallengeWings(bool wingsInForeground)
	{
		adjustWingZDepth(wingsInForeground);

		forceLoadChallengeTextures(leftMeshFilter, rightMeshFilter);
		gameObject.SetActive(true);
	}

	public void forceShowSecondaryChallengeWings(bool wingsInForeground)
	{
		adjustWingZDepth(wingsInForeground);

		forceLoadSecondaryChallengeTextures(leftMeshFilter, rightMeshFilter);
		gameObject.SetActive(true);
	}

	public void forceShowThirdChallengeWings(bool wingsInForeground)
	{
		adjustWingZDepth(wingsInForeground);

		forceLoadThirdChallengeTextures(leftMeshFilter, rightMeshFilter);
		gameObject.SetActive(true);
	}

	public void forceShowFourthChallengeWings(bool wingsInForeground)
	{
		adjustWingZDepth(wingsInForeground);

		forceLoadFourthChallengeTextures(leftMeshFilter, rightMeshFilter);
		gameObject.SetActive(true);
	}

	public void forceShowPortalWings(bool wingsInForeground)
	{
		adjustWingZDepth(wingsInForeground);

		forceLoadPortalTextures(leftMeshFilter, rightMeshFilter);
		gameObject.SetActive(true);
	}

	public void forceLoadFreeSpinIntroTextures(bool wingsInForeground)
	{
		adjustWingZDepth(wingsInForeground);

		forceLoadFreeSpinIntroTextures(leftMeshFilter, rightMeshFilter);
		gameObject.SetActive(true);
	}

	public void forceShowFreeSpinWings(bool wingsInForeground, int wingVariant = 1)
	{
		adjustWingZDepth(wingsInForeground);

		if (wingVariant == 1)
		{
			forceLoadFreeSpinTextures(leftMeshFilter, rightMeshFilter);
		}
		else if (wingVariant == 2)
		{
			forceLoadFreeSpinVariantTwoTextures(leftMeshFilter, rightMeshFilter);
		}
		else
		{
			forceLoadFreeSpinVariantThreeTextures(leftMeshFilter, rightMeshFilter);
		}
		gameObject.SetActive(true);
	}

	public void hide()
	{
		gameObject.SetActive(false);
	}

	/// Get the bounds of a wing, useful if you need positioing information about the size and placement
	public Bounds getWingBounds(WingSideEnum side)
	{
		if (side == WingSideEnum.Left)
		{
			return BonusGameWings.getWingBounds(leftMeshFilter);
		}
		else
		{
			return BonusGameWings.getWingBounds(rightMeshFilter);
		}
	}

	/// Get the position of a wing
	public Vector3 getWingLocalPosition(WingSideEnum side)
	{
		if (side == WingSideEnum.Left)
		{
			return leftMeshFilter.gameObject.transform.localPosition;
		}
		else
		{
			return rightMeshFilter.gameObject.transform.localPosition;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////
	/// This set of functions is static so they can be reused by BigWinWings too.
	public static void loadTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter, bool isBigWin)
	{
		// Store the filters
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);

		// if this is a big win check if the game uses big win wings that are different form the base game
		if (isBigWin && SlotResourceMap.isGameUsingBigWinWings(GameState.game.keyName))
		{
			// load big win version of wings
			SlotResourceMap.getBigWinWings(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
		}
		else
		{
			// load base game wings
			SlotResourceMap.getWings(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
		}
	}

	private static void forceLoadBaseTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter)
	{
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);
		SlotResourceMap.getNormalWings(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
	}

	private static void forceLoadPortalTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter)
	{
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);
		SlotResourceMap.getPortalWings(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
	}

	private static void forceLoadChallengeTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter)
	{
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);
		SlotResourceMap.getChallengeWings(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
	}

	private static void forceLoadSecondaryChallengeTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter)
	{
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);
		SlotResourceMap.getChallengeWingsVariant(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
	}

	private static void forceLoadThirdChallengeTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter)
	{
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);
		SlotResourceMap.getChallengeWingsVariantTwo(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
	}

	private static void forceLoadFourthChallengeTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter)
	{
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);
		SlotResourceMap.getChallengeWingsVariantThree(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
	}

	public static void forceLoadFreeSpinIntroTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter)
	{
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);
		SlotResourceMap.getFreeSpinIntroWings(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);		
	}

	public static void forceLoadFreeSpinTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter)
	{
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);
		SlotResourceMap.getFreeSpinWings(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
	}

	public static void forceLoadFreeSpinVariantTwoTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter)
	{
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);
		SlotResourceMap.getFreeSpinWingsVariantTwo(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
	}

	public static void forceLoadFreeSpinVariantThreeTextures(MeshFilter leftMeshFilter, MeshFilter rightMeshFilter)
	{
		Dict data = Dict.create(D.OPTION1, leftMeshFilter, D.OPTION2, rightMeshFilter);
		SlotResourceMap.getFreeSpinWingsVariantThree(GameState.game.keyName, loadWingsCallback, failWingsCallback, data);
	}

	private static void loadWingsCallback(string asset, Object obj, Dict data)
	{
		Texture wingTexture = null;
		if (obj != null)
		{
			wingTexture = obj as Texture;
		}
		wingLoadedHandler(wingTexture, data);
	}

	private static void failWingsCallback(string asset, Dict data)
	{
		wingLoadedHandler(null, data);
	}

	/// Callback when the texture is finished loading.
	private static void wingLoadedHandler(Texture texture, Dict data)
	{
		if (texture == null)
		{
			Debug.LogWarning("failed to load wings");
			texture = SkuResources.getObjectFromMegaBundle<Texture>(WINGS_IMAGE_PATH);
		}

		if (texture != null)
		{
			MeshFilter leftMeshFilter = data.getWithDefault(D.OPTION1, null) as MeshFilter;
			MeshFilter rightMeshFilter = data.getWithDefault(D.OPTION2, null) as MeshFilter;

			Material mat = new Material(ShaderCache.find("Unlit/GUI Texture"));
			mat.mainTexture = texture;
			
			leftMeshFilter.GetComponent<Renderer>().sharedMaterial = mat;
			rightMeshFilter.GetComponent<Renderer>().sharedMaterial = mat;
		}
	}

	/// Get the bounds of a wing, useful if you need positioing information about the size and placement
	public static Bounds getWingBounds(MeshFilter wingFilter)
	{
		return CommonGameObject.getObjectBounds(wingFilter.gameObject);
	}
	
	// Returns a left wing mesh (suffers from pride, lust, envy, and sloth)
	public static Mesh leftWingMesh
	{
		get
		{
			if (_leftWingMesh == null)
			{
				_leftWingMesh = buildWingMesh(true);
			}
			return _leftWingMesh;
		}
	}
	private static Mesh _leftWingMesh = null;
	
	
	// Returns a right wing mesh (suffers from pride, wrath, greed, and gluttony)
	public static Mesh rightWingMesh
	{
		get
		{
			if (_rightWingMesh == null)
			{
				_rightWingMesh = buildWingMesh(false);
			}
			return _rightWingMesh;
		}
	}
	private static Mesh _rightWingMesh = null;
	
	
	// Builds a wings mesh, which was originally a simple normalized XY plane quad
	private static Mesh buildWingMesh(bool isLeft)
	{
		// Mesh vertex layout:
		
		// 0--2--4
		// | /| /|
		// |/ |/ |
		// 1--3--5
		
		// Vertices 0, 1, 2, and 3 define the basic quad.
		// Vertices 2, 3, 4, and 5 define a tilted extra flap for overlap.
		
		Mesh mesh = new Mesh();
		Vector3[] vertices = new Vector3[6];
		Vector3[] normals = new Vector3[6];
		Vector2[] uvs = new Vector2[6];
		int[] triangles;
		
		float dir;
		float outerUV;
		float innerUV;
		float edgeUV;
		
		if (isLeft)
		{
			dir = -1.0f;
			outerUV = 0.0f;
			innerUV = 0.499f;
			edgeUV = 0.4995f;
			
			triangles = new int[]
			{
				3, 2, 4,
				4, 5, 3,
				3, 1, 2,
				2, 1, 0
			};
		}
		else
		{
			dir = 1.0f;
			outerUV = 1.0f;
			innerUV = 0.501f;
			edgeUV = 0.5005f;
			
			triangles = new int[]
			{
				0, 1, 2,
				2, 1, 3,
				3, 5, 4,
				4, 2, 3
			};
		}
		
		vertices[0] = new Vector3(0.5f * dir, 0.5f, 0.0f);
		vertices[1] = new Vector3(0.5f * dir, -0.5f, 0.0f);
		vertices[2] = new Vector3(-0.5f * dir, 0.5f, 0.0f);
		vertices[3] = new Vector3(-0.5f * dir, -0.5f, 0.0f);
		vertices[4] = new Vector3(-0.501f * dir, 0.5f, 0.01f);
		vertices[5] = new Vector3(-0.501f * dir, -0.5f, 0.01f);
		
		normals[0] = new Vector3(0.0f, 0.0f, 1.0f);
		normals[1] = new Vector3(0.0f, 0.0f, 1.0f);
		normals[2] = new Vector3(0.0f, 0.0f, 1.0f);
		normals[3] = new Vector3(0.0f, 0.0f, 1.0f);
		normals[4] = new Vector3(0.0f, 0.0f, 1.0f);
		normals[5] = new Vector3(0.0f, 0.0f, 1.0f);
		
		uvs[0] = new Vector2(outerUV, 1.0f);
		uvs[1] = new Vector2(outerUV, 0.0f);
		uvs[2] = new Vector2(innerUV, 1.0f);
		uvs[3] = new Vector2(innerUV, 0.0f);
		uvs[4] = new Vector2(edgeUV, 1.0f);
		uvs[5] = new Vector2(edgeUV, 0.0f);
		
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		
		mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
		
		return mesh;
	}

	private void wingsTextureLoadSuccess(string path, Object obj, Dict data = null)
	{
		Texture texture = obj as Texture;
		MeshFilter leftMeshFilter = data.getWithDefault(D.OPTION1, null) as MeshFilter;
		MeshFilter rightMeshFilter = data.getWithDefault(D.OPTION2, null) as MeshFilter;

		Material mat = new Material(ShaderCache.find("Unlit/GUI Texture"));
		mat.mainTexture = texture;

		leftMeshFilter.GetComponent<Renderer>().sharedMaterial = mat;
		rightMeshFilter.GetComponent<Renderer>().sharedMaterial = mat;
	}

	private void wingsTextureLoadFailure(string path, Dict args = null)
	{
		Debug.LogError("Failed to load the default wings texture from path: " + path);
	}
}
