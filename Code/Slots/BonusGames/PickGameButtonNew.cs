using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Use Label Wrappers to support NGUI labels and Text Mesh Pro. */

public class PickGameButtonNew : TICoroutineMonoBehaviour
{
	public GameObject button;
	public Animator animator;
	
	public LabelWrapper revealNumberLabel;
	public LabelWrapper multiplierLabel;
	public LabelWrapper extraLabel;
	
	public UISprite imageReveal;
	public UISprite[] multipleImageReveals;
	public Material material;
	public string pickMeSoundName;
	
	public MeshRenderer[] glowList;
	public MeshRenderer[] glowShadowList;
}
