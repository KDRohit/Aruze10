using UnityEditor;
using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class MaterialMakerOption : object
{
    private string[] symbols = { "M1", "M2", "M3", "M4", "F5", "F6", "F7", "F8", "F9", "WD", "BN" };
    private string[] banners = { "clickme", "challenge", "freespins", "bonus", "winamount"};
    private string[] backgrounds = { "fsbackground", "fsbkg", "background", "bkg", "reelbackground", "reelbkg" };

    public string name;
    public string matName = "";
    public Texture texture;
    public Texture alphaTexture;
    public int type = 0;
    public Material material;
    public bool wasChecked;

    public MaterialMakerOption(string p_name, Texture p_texture)
    {
        name = p_name;
        texture = p_texture;
        tryToNameMat();
    }

    //Name suggestion, this is quick and dirty most will just get defaulted to the texture name, 
    //I will fix this my next push, difficult since the textures from the artists have no solid naming conventions
    private void tryToNameMat()
    {
        if (!name.ToUpper().Contains("OVERLAY") && !name.ToUpper().Contains("ALPHA"))
        {
            foreach (string s in symbols)
            {
                if (name.ToUpper().Contains(s.ToUpper()))
                {
                    matName = s;
                    return;
                }
            }

            foreach (string s in banners)
            {
                if (name.ToUpper().Contains(s.ToUpper()))
                {
                    matName = s;
                    return;
                }
            }

            foreach (string s in backgrounds)
            {
                if (name.ToUpper().Contains(s.ToUpper()))
                {
                    matName = s;
                    return;
                }
            }
        }
        matName = name;
        wasChecked = false;
    }
}