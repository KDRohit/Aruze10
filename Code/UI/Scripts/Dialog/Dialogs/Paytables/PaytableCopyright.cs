using UnityEngine;
using System.Collections;
using TMPro;

public class PaytableCopyright : TICoroutineMonoBehaviour
{
	[SerializeField] private TextMeshPro titleLabel;
	
	[Header("Text Only")]
	[SerializeField] private GameObject textOnlyObject = null;
	[SerializeField] private TextMeshPro textOnlyBodyLabel;

	[Header("Text and Image")]
	[SerializeField] private GameObject textAndImageObject = null;
	[SerializeField] private TextMeshPro textAndImageBodyLabel;
	[SerializeField] private Transform textAndImageParentTransform;

	[Header("Image Only")]
	[SerializeField] private GameObject imageOnlyObject = null;
	[SerializeField] private Transform imageOnlyTextureParentTransform;

	// Use this for initialization
	void Start ()
	{
		if (PaytablesDialog.instance == null)
		{
			Debug.LogError("Can't find parent PaytableDialog in PaytableCopyright.");
			return;
		}

		// Programatically fill in the text from the game-specific localization keys:
		this.titleLabel.text = Localize.text(PaytablesDialog.instance.dialogIndex.copyrightTitleLoc);

		if (!string.IsNullOrEmpty(PaytablesDialog.instance.dialogIndex.copyrightImage) && !string.IsNullOrEmpty(PaytablesDialog.instance.dialogIndex.copyrightBodyLoc))
		{
			// Text and Image
			textOnlyObject.SetActive(false);
			textAndImageObject.SetActive(true);
			imageOnlyObject.SetActive(false);

			textAndImageBodyLabel.text = Localize.text(PaytablesDialog.instance.dialogIndex.copyrightBodyLoc);
			string imageBaseName = getPaytableLegalImageBasename(PaytablesDialog.instance.dialogIndex.copyrightImage);
			SlotResourceMap.createPaytableLegalImagePrefab(imageBaseName, imageForTextAndImageLoaded, legalImageFailed);
		}
		else if (!string.IsNullOrEmpty(PaytablesDialog.instance.dialogIndex.copyrightImage) && string.IsNullOrEmpty(PaytablesDialog.instance.dialogIndex.copyrightBodyLoc))
		{
			// Image Only
			textOnlyObject.SetActive(false);
			textAndImageObject.SetActive(false);
			imageOnlyObject.SetActive(true);

			string imageBaseName = getPaytableLegalImageBasename(PaytablesDialog.instance.dialogIndex.copyrightImage);
			SlotResourceMap.createPaytableLegalImagePrefab(imageBaseName, imageForImageOnlyLoaded, legalImageFailed);
		}
		else
		{
			// Text Only
			textOnlyObject.SetActive(true);
			textAndImageObject.SetActive(false);
			imageOnlyObject.SetActive(false);

			textOnlyBodyLabel.text = Localize.text(PaytablesDialog.instance.dialogIndex.copyrightBodyLoc);
		}
	}

	// Success callback for the text and image version of the copyright page
	private void imageForTextAndImageLoaded(string filename, Object objectInstance, Dict data)
	{
		if (objectInstance == null)
		{
			return;
		}

		GameObject imageObject = objectInstance as GameObject;
		if (imageObject == null)
		{
			return;
		}

		imageObject.transform.parent = textAndImageParentTransform;
		imageObject.transform.localPosition = Vector3.zero;
		imageObject.transform.localScale = Vector3.one;

		UITexture imageTexture = imageObject.GetComponentInChildren<UITexture>();

		imageTexture.alpha = 0.0f;
		TweenAlpha.Begin(imageTexture.gameObject, 0.5f, 1.0f);
	}

	// Success callback for the image only version of the copyright page
	private void imageForImageOnlyLoaded(string filename, Object objectInstance, Dict data)
	{
		if (objectInstance == null)
		{
			return;
		}

		GameObject imageObject = objectInstance as GameObject;
		if (imageObject == null)
		{
			return;
		}

		imageObject.transform.parent = imageOnlyTextureParentTransform;
		imageObject.transform.localPosition = Vector3.zero;
		imageObject.transform.localScale = Vector3.one;

		UITexture imageTexture = imageObject.GetComponentInChildren<UITexture>();

		imageTexture.alpha = 0.0f;
		TweenAlpha.Begin(imageTexture.gameObject, 0.5f, 1.0f);
	}

	// Fail callback for the legal image loading
	private void legalImageFailed(string filename, Dict data)
	{
		if (data != null)
		{
			Debug.LogWarning("Failed to load: " + filename);
		}
		else
		{
			Debug.LogError("Failed to find paytable legal image prefab. filename = " + filename);
		}
	}

	// Create a basename which removes the file extension from the iamge
	public static string getPaytableLegalImageBasename(string imageFilename)
	{
		string imageBaseName = imageFilename;
		if (imageBaseName.Contains('.'))
		{
			imageBaseName = imageBaseName.Substring(0, imageBaseName.IndexOf('.')); // Remove the .whatever from the end.
		}

		return imageBaseName;
	}
}
