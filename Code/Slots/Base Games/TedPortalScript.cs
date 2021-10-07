using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BannerTypeEnum = SlotBaseGame.BannerInfo.BannerTypeEnum;
using TextDirectionEnum =  SlotBaseGame.BannerTextInfo.TextDirectionEnum;
using TextLocationEnum =  SlotBaseGame.BannerTextInfo.TextLocationEnum;

public class TedPortalScript : PortalScript {

	public UICamera extraCamera;

	protected override IEnumerator doBeginPortalReveals(GameObject objClicked, RevealDelegate revealDelegate)
	{
		GameObject cameraRoot = GameObject.Find("0 Camera");
		extraCamera.transform.parent = cameraRoot.transform;
		extraCamera.transform.localPosition = Vector3.zero;
		extraCamera.gameObject.SetActive(true);
		if (_bannerMap[BannerTypeEnum.CLICKME].pickMePrefab != null)
		{
			stopPickMeAnimation(true);
			StopCoroutine("playPickMeAnimation");		
		}
		GameObject revealObject;
		revealedObjects = new List<GameObject>();
		//It is possible to send the text object clicked on since clicking on the object itself may be impossible from a 
		//perspective camera, so we will check to see if we got a text object, and if so, grab the associated revealObject.
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			GameObject bannerText = CommonGameObject.findDirectChild(bannerTextObjects[i], "Text");
			if (objClicked.transform.parent.gameObject == bannerObjects[i] || objClicked == bannerText)
			{
				_revealedIndex = i;
				
				LabelWrapper label;
				LabelWrapper footerLabel;
				LabelWrapper headerLabel;
				getBannerLabels(bannerTextObjects[i], out label, out footerLabel, out headerLabel);

				CommonGameObject.setLayerRecursively(bannerTextObjects[i], LayerMask.NameToLayer("NGUI_UNDERLAY"));
				resetBannerPositions(i, headerLabel.gameObject, label.gameObject, footerLabel.gameObject);

				label.text = "";
				footerLabel.text = "";
				headerLabel.text = "";
				if (_outcome.isChallenge)
				{
					revealObject = createRevealBanner(BannerTypeEnum.CHALLENGE, bannerObjects[i]);
					setAllBannerTextInfo(BannerTypeEnum.CHALLENGE, headerLabel, label, footerLabel);
				}
				else if(_outcome.isGifting)
				{
					revealObject = createRevealBanner(BannerTypeEnum.GIFTING, bannerObjects[i]);
					setAllBannerTextInfo(BannerTypeEnum.GIFTING, headerLabel, label, footerLabel);
				}
				else
				{
					revealObject = createRevealBanner(BannerTypeEnum.OTHER, bannerObjects[i]);
					setAllBannerTextInfo(BannerTypeEnum.OTHER, headerLabel, label, footerLabel);
				}
				
				VisualEffectComponent.Create(_revealVfx, revealObject);
				yield return this.StartCoroutine(doAnimateBannerOnly(bannerObjects[i]));
				bannerObjects[i] = null;
			}
		}
		StartCoroutine(revealDisabledBanners());
	}


	protected IEnumerator revealDisabledBanners()
	{
		Color disabledColor = new Color(0.25f, 0.25f, 0.25f);
		GameObject revealObject = null;
		for (int i = 0; i < bannerObjects.Count; i++)
		{
			if (bannerObjects[i] != null && i != _revealedIndex)
			{
				LabelWrapper label;
				LabelWrapper footerLabel;
				LabelWrapper headerLabel;
				getBannerLabels(bannerTextObjects[i], out label, out footerLabel, out headerLabel);

				CommonGameObject.setLayerRecursively(bannerTextObjects[i], LayerMask.NameToLayer("NGUI_UNDERLAY"));
				resetBannerPositions(i, headerLabel.gameObject, label.gameObject, footerLabel.gameObject);
				
				label.text = "";
				footerLabel.text = "";
				headerLabel.text = "";
				
				if (_outcome.isChallenge)
				{
					if (!spinsAdded)
					{
						revealObject = createRevealBanner(BannerTypeEnum.GIFTING, bannerObjects[i]);
						setAllBannerTextInfo(BannerTypeEnum.GIFTING, headerLabel, label, footerLabel);
						spinsAdded = true;
					}
					else
					{
						revealObject = createRevealBanner(BannerTypeEnum.OTHER, bannerObjects[i]);
						setAllBannerTextInfo(BannerTypeEnum.OTHER, headerLabel, label, footerLabel);
						label.color = disabledColor;
					}
				}
				else if (_outcome.isGifting)
				{
					if (!bonusAdded)
					{
						revealObject = createRevealBanner(BannerTypeEnum.CHALLENGE, bannerObjects[i]);
						bonusAdded = true;
						setAllBannerTextInfo(BannerTypeEnum.CHALLENGE, headerLabel, label, footerLabel);
					}
					else
					{
						revealObject = createRevealBanner(BannerTypeEnum.OTHER, bannerObjects[i]);
						setAllBannerTextInfo(BannerTypeEnum.OTHER, headerLabel, label, footerLabel);
						label.color = disabledColor;
					}
				}
				else if (_outcome.isCredit)
				{
					if (!bonusAdded)
					{
						revealObject = createRevealBanner(BannerTypeEnum.CHALLENGE, bannerObjects[i]);
						bonusAdded = true;
						setAllBannerTextInfo(BannerTypeEnum.CHALLENGE, headerLabel, label, footerLabel);
					}
					else
					{
						revealObject = createRevealBanner(BannerTypeEnum.GIFTING, bannerObjects[i]);
						setAllBannerTextInfo(BannerTypeEnum.GIFTING, headerLabel, label, footerLabel);
					}
				}
				
				// Since these are definitely not the active panels, let's disable all text flourishes and set the color to disabled	.
				label.color = disabledColor;
				footerLabel.color = disabledColor;
				headerLabel.color = disabledColor;
				
				label.effectColor = Color.black;
				footerLabel.effectColor = Color.black;
				headerLabel.effectColor = Color.black;
				
				label.isGradient = false;
				footerLabel.isGradient = false;
				headerLabel.isGradient = false;
				
				// Get the child banner where the material is stored.
				GameObject banner = CommonGameObject.findDirectChild(revealObject, "Banner");
				if (banner != null && banner.GetComponent<Renderer>().material.HasProperty("_Color"))
				{
					// Set the Color to Disabled
					banner.GetComponent<Renderer>().material.SetColor("_Color", disabledColor);
				}

				VisualEffectComponent.Create(_revealVfx, revealObject);
				AnimateBannerFlyout(bannerObjects[i]);
				yield return new WaitForSeconds(.5f);
				bannerObjects[i] = null;
			}
		}	
		

		
		StartCoroutine(reParentCamera());
		if (revealedObjects.Count == 3)
		{
			Invoke("beginBonus", 1.5f);
		}
		else
		{
			Invoke("revealBanners", 0.75f);
		}

		Audio.play(Audio.soundMap("bonus_portal_reveal_others"));

	}

	IEnumerator reParentCamera()
	{
		yield return new WaitForSeconds(1.45f);
		extraCamera.transform.parent = this.gameObject.transform;
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		extraCamera.gameObject.SetActive(false);
	}

}
