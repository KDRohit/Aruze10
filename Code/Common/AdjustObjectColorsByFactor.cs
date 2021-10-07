using System.Collections.Generic;
using TMPro;
using UnityEngine;

/*
 * Used to adjust the color of a list of UITextures and/or UISprites
 */

[ExecuteInEditMode]
public class AdjustObjectColorsByFactor : MonoBehaviour
{
    public UITexture[] textures;
    public UISprite[] sprites;
    public TextMeshPro[] tmpros;
    
    [SerializeField] private float adjustAmount;
    
    private Dictionary<Object, Color> originalColorMap = new Dictionary<Object, Color>();
    private Dictionary<Object, Color> adjustedColorMap = new Dictionary<Object, Color>();

    private Dictionary<TextMeshPro, string> tmProOriginalColorTextMap = new Dictionary<TextMeshPro, string>();
    private Dictionary<TextMeshPro, string> tmProAdjustedColorTextMap = new Dictionary<TextMeshPro, string>();

    private Color adjustmentColor;

    private void Awake()
    {
        if (textures == null || sprites == null || tmpros == null)
        {
            Debug.LogError("Invalid color adjust configuration on object " + gameObject.name);
            return;
        }

        if (originalColorMap == null)
        {
            originalColorMap = new Dictionary<Object, Color>();
        }
        
        if (originalColorMap.Count == 0)
        {
            buildColorMaps();
        }
    }

    private void buildColorMaps()
    {
        adjustmentColor = new Color(adjustAmount, adjustAmount, adjustAmount, 1);
        //Cache starting colors & converted colors
        foreach (UITexture texture in textures)
        {
            if (texture == null)
            {
                continue;
            }
            originalColorMap.Add(texture, texture.color);
            adjustedColorMap.Add(texture, texture.color * adjustmentColor);
        }
        
        foreach (UISprite sprite in sprites)
        {
            if (sprite != null)
            {
                originalColorMap.Add(sprite, sprite.color);
                adjustedColorMap.Add(sprite, sprite.color * adjustmentColor);
            }
            else if (Application.isEditor)
            {
                Debug.LogError("AdjustObjectColorsByFactor sprite was null");
            }
        }
        
        foreach (TextMeshPro tmpro in tmpros)
        {
            if (tmpro != null)
            {
                originalColorMap.Add(tmpro, tmpro.color);
                adjustedColorMap.Add(tmpro, tmpro.color * adjustmentColor);

                cacheTextColors(tmpro);
            }
            else if (Application.isEditor)
            {
                Debug.LogError("AdjustObjectColorsByFactor TextMeshPro was null");
            }
        }
    }

    //Allowed the cached text values to be updated Post-Awake, in case localized text happens afterwards
    //TODO:There is an issue here that requires the default label values to be kept blank. Need to find a way to fix this
    public void cacheTextColors(TextMeshPro textToCache)
    {
        if (textToCache.text.Contains("</color>"))
        {
            tmProOriginalColorTextMap[textToCache] = textToCache.text;

            string colorString = CommonText.getColorHexStringFromString(textToCache.text);
            Color oldTextColor;
            //Try to convert the hex string to a color
            if (ColorUtility.TryParseHtmlString(colorString, out oldTextColor))
            {
                //Adjust old color and cache if it doesn't exist in the dictionary
                Color newTextColor = oldTextColor * adjustmentColor;
                        
                //Get new hex string for adjusted color & store it so we don't need to bother with converting later
                string newColorString = "#" + ColorUtility.ToHtmlStringRGB(newTextColor);
                string textWithAdjustedColor = textToCache.text.Replace(colorString, newColorString);

                tmProAdjustedColorTextMap[textToCache] = textWithAdjustedColor;
            } 
        }
    }

    public void multiplyColors()
    {
        if (originalColorMap.Count == 0)
        {
            buildColorMaps();
        }
        
        foreach (UITexture texture in textures)
        {
            Color adjustedColor;
            if (texture != null && adjustedColorMap.TryGetValue(texture, out adjustedColor))
            {
                texture.color = adjustedColor;
            }
        }
        
        foreach (UISprite sprite in sprites)
        {
            Color adjustedColor;
            if (sprite != null && adjustedColorMap.TryGetValue(sprite, out adjustedColor))
            {
                sprite.color = adjustedColor;
            }
        }
        
        foreach (TextMeshPro tmpro in tmpros)
        {
            if (tmpro == null)
            {
                if (Data.debugMode)
                {
                    Debug.LogError("AdjustObjectColorsByFactor TextMeshPro was null");
                }

                continue;
            }

            Color adjustedColor;
            if (adjustedColorMap.TryGetValue(tmpro, out adjustedColor))
            {
                tmpro.color = adjustedColor;
            }

            string adjustedColorText;
            if (tmProAdjustedColorTextMap.TryGetValue(tmpro, out adjustedColorText))
            {
                tmpro.SetText(adjustedColorText);
            }
        }
    }

    public void restoreColors()
    {
        foreach (UITexture texture in textures)
        {
            Color originalColor;
            if (texture != null && originalColorMap.TryGetValue(texture, out originalColor))
            {
                texture.color = originalColor;
            }
        }
        
        foreach (UISprite sprite in sprites)
        {
            Color originalColor;
            if (sprite != null && originalColorMap.TryGetValue(sprite, out originalColor))
            {
                sprite.color = originalColor;
            }
        }
        
        foreach (TextMeshPro tmpro in tmpros)
        {
            if (tmpro == null)
            {
                if (Data.debugMode)
                {
                    Debug.LogError("AdjustObjectColorsByFactor TextMeshPro was null");
                }

                continue;
            }

            Color originalColor;
            if (originalColorMap.TryGetValue(tmpro, out originalColor))
            {
                tmpro.color = originalColor;
            }
            
            string originalColorText;
            if (tmProOriginalColorTextMap.TryGetValue(tmpro, out originalColorText))
            {
                tmpro.SetText(originalColorText);
            }
        }
    }
}
