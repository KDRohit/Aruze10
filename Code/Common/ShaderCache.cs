using  System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ShaderCache
{
    private static Dictionary<string, Shader> shaderCache = new Dictionary<string, Shader>();

    public static Shader find(string shaderName)
    {
        Shader shaderReference;
        if (shaderCache.TryGetValue(shaderName, out shaderReference))
        {
            return shaderReference;
        }
        
        shaderReference = Shader.Find(shaderName);
        shaderCache.Add(shaderName, shaderReference);
        
        if (shaderReference == null)
        {
            Debug.LogError("ShaderCache::find - Unable to find shader " + shaderName);
        }
        
        return shaderReference;
    }
}
