using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DrawerCache
{
	public const int OUTLINE_KEY = 1000;
	public const int INLINE_KEY = 2000;
	public const int HIGHLIGHT_KEY = 3000;
	
	private static List<PaylineMesh> usedMeshes = new List<PaylineMesh>();	// Remember which ones are being used from the pool, so they can be released back to the pool all at once when done.
	
	/// A material pool for reducing material overhead when we know things are the same.
	public static Dictionary<int, Material> materialPool
	{
		get
		{
			if (_materialPool == null)
			{
				_materialPool = new Dictionary<int, Material>();
			}
			return _materialPool;
		}
	}
	private static Dictionary<int, Material> _materialPool = null;
	
	/// A mesh pool for not being so bitchy with accumulating meshes from paylines
	public static Dictionary<int, Dictionary<int, Stack<PaylineMesh>>> meshPool
	{
		get
		{
			if (_meshPool == null)
			{
				_meshPool = new Dictionary<int, Dictionary<int, Stack<PaylineMesh>>>();
			}
			return _meshPool;
		}
	}
	public static Dictionary<int, Dictionary<int, Stack<PaylineMesh>>> _meshPool = null;
	
	/// Gets a dynamic mesh
	public static PaylineMesh getPaylineMesh(int vertices, int triangles)
	{
		Dictionary<int, Stack<PaylineMesh>> vertLookup;
		if (meshPool.ContainsKey(vertices))
		{
			vertLookup = meshPool[vertices];
		}
		else
		{
			vertLookup = new Dictionary<int, Stack<PaylineMesh>>();
			meshPool.Add(vertices, vertLookup);
		}
		
		Stack<PaylineMesh> triangleLookup;
		if (vertLookup.ContainsKey(triangles))
		{
			triangleLookup = vertLookup[triangles];
		}
		else
		{
			triangleLookup = new Stack<PaylineMesh>();
			vertLookup.Add(triangles, triangleLookup);
		}
		
		while (triangleLookup.Count > 0)
		{
			PaylineMesh recycledMesh = triangleLookup.Pop();
			if (recycledMesh != null)
			{
				if (recycledMesh.mesh == null)
				{
					recycledMesh.mesh = new Mesh();
					recycledMesh.mesh.MarkDynamic();
				}
				else
				{
					recycledMesh.mesh.Clear();
				}
				
				usedMeshes.Add(recycledMesh);
				
				return recycledMesh;
			}
		}
		
		PaylineMesh newMesh = new PaylineMesh();
		newMesh.mesh = new Mesh();
		newMesh.mesh.MarkDynamic();
		newMesh.vertexCount = vertices;
		newMesh.triangleCount = triangles;
		
		usedMeshes.Add(newMesh);
		
		return newMesh;
	}
	
	/// Releases a dynamic mesh
	public static void releasePaylineMesh(PaylineMesh oldMesh)
	{
		Dictionary<int, Stack<PaylineMesh>> vertLookup;
		if (meshPool.ContainsKey(oldMesh.vertexCount))
		{
			vertLookup = meshPool[oldMesh.vertexCount];
		}
		else
		{
			vertLookup = new Dictionary<int, Stack<PaylineMesh>>();
			meshPool.Add(oldMesh.vertexCount, vertLookup);
		}
		
		Stack<PaylineMesh> triangleLookup;
		if (vertLookup.ContainsKey(oldMesh.triangleCount))
		{
			triangleLookup = vertLookup[oldMesh.triangleCount];
		}
		else
		{
			triangleLookup = new Stack<PaylineMesh>();
			vertLookup.Add(oldMesh.triangleCount, triangleLookup);
		}
		
		triangleLookup.Push(oldMesh);
		usedMeshes.Remove(oldMesh);
	}

	// Release all the currently used meshes back to the pool.
	public static void releaseUsedMeshes()
	{
		int limiter = 0;
		while (usedMeshes.Count > 0)
		{
			releasePaylineMesh(usedMeshes[0]);
			
			limiter++;
			if (limiter >= 1000)
			{
				Debug.LogError("Infinite loop detected in DrawerCache.releaseUsedMeshese()!");
				break;
			}
		}
	}
}

public class PaylineMesh
{
	public int vertexCount;
	public int triangleCount;
	public Mesh mesh;
}
