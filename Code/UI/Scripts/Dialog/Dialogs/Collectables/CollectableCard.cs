using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.IO;

public class CollectableCard : MonoBehaviour 
{
	public UISprite mainImage;
	public UISprite backgroundImage;
	public GameObject newBadgeParent;
	public GameObject sheenParent;

	public GameObject starsParent;
	public UISprite[] starObjects; //Maybe dynamically create these?
	public GameObject[] starShadows;

	public Animator cardAnimator;
	public TextMeshPro descriptionLabel; //Might only need to set this if the card is clicked on?
	public TextMeshPro titleLabel;
	public TextMeshPro cardIDLabel;
	public GameObject titleAnchor;
	public ButtonHandler onClickButton; // In case we want to do something when we click the card. (Used when in album dialog)

	public GameObject newBadgePrefab;
	public GameObject staticNewBadgePrefab;
	public GameObject[] sheenPrefabs;
	public GameObject shadowParent;

	[SerializeField]
	private GameObject powerupContainerPrefab;
	[SerializeField] 
	private GameObject powerupContainerAnchor;

	public GameObject powerupObject;

	private Animator newBadgeAnimator;
	private int rarity = 0;
	public CollectableCardData data;
	public bool isPowerup { get; protected set; }

	public UITexture cardImageTexture;
	public UITexture cardFrameTexture;

	private const float STAR_INTRO_ANIM_WAIT = 0.7f;
	private const float STAR_TWEEN_STAGGER_TIME = 0.15f;
	private const float STAR_TWEEN_TIME = 0.85f;
	private const float STAR_EXPLOSION_LENGTH = 2.0f;
	private const int STARS_OFFSET = 22;
	private readonly Vector3 powerupsStarPosition = new Vector3(88, -177, -5);
	private readonly Vector3 nameLabelPosOverride = new Vector3(0, -134, 0);

	private const string NEW_BADGE_INTRO_ANIM_NAME = "intro";
	private const string NEW_BADGE_BOUNCE_ANIM_NAME = "bounce";
	private const string FRAME_SPRITE_NAME = "Card Frame ";
	private const string POWERUP_FRAME_SPRITE_NAME = "Card Frame 6 PowerUps";

	//Localizations
	private const string CARD_TITLE_LOCALIZATION_POSTFIX = "_title";
	private const string CARD_DESCRIPTION_LOCALIZATION_POSTFIX = "_description";
	
	private readonly Color unCollectedColor = new Color(0.5f, 0.5f ,0.5f);
	private readonly Color collectedColor = new Color(1f, 1f ,1f);

	//Array of special card tints in order of lowest rarity to highest
	public static readonly Color[] rarityColors =  {
										new Color32(3, 0, 55, 255),
										new Color32(35, 3, 3, 255),
										new Color32(0, 4, 40, 255),
										new Color32(45, 18, 0, 255),
										new Color32(41, 0, 77, 255)
									};
	
	public static readonly Color[] powerupStarsRarityColors =  {
		new Color32(51, 153, 255, 255),
		new Color32(204, 102, 51, 255),
		new Color32(173, 213, 213, 255),
		new Color32(255, 255, 153, 255),
		new Color32(255, 153, 255, 255)
	};
		
	public enum CardLocation
	{
		PACK_DROP,
		SET_VIEW,
		DETAILED_VIEW
	}

	public void init(CollectableCardData cardData, CardLocation location, UIAtlas spriteAtlas = null, Dictionary<string, Texture2D> loadedTextures = null)
	{
		data = cardData;
		bool dataContainsPowerup = PowerupBase.collectablesPowerupsMap.ContainsKey(data.keyName);
		if (dataContainsPowerup)
		{
			isPowerup = true;
			string powerupName = PowerupBase.collectablesPowerupsMap[data.keyName]; 
			//this card has dropped a powerup
			backgroundImage.gameObject.SetActive(false);
			mainImage.gameObject.SetActive(false);
			cardIDLabel.gameObject.SetActive(false);
			titleAnchor.transform.localPosition = nameLabelPosOverride;

			Material frameMaterial = new Material(cardFrameTexture.material.shader);
			Texture2D frameTexture;
			if (loadedTextures.TryGetValue(POWERUP_FRAME_SPRITE_NAME, out frameTexture))
			{
				frameMaterial.mainTexture = frameTexture;
				cardFrameTexture.material = frameMaterial;
				cardFrameTexture.gameObject.SetActive(true);
			}
			
			powerupObject = NGUITools.AddChild(powerupContainerAnchor, powerupContainerPrefab);
			if (powerupObject != null)
			{
				PowerupCardItem powerupCard = powerupObject.GetComponent<PowerupCardItem>();
				if (powerupCard != null)
				{
					powerupCard.setupPowerup(cardData, powerupName, location);
				}
			}

		}
		else if (spriteAtlas != null)
		{
			if (backgroundImage != null)
			{
				backgroundImage.atlas = spriteAtlas;
				backgroundImage.spriteName = FRAME_SPRITE_NAME + data.rarity;
				backgroundImage.depth = 1;
			}
			
			mainImage.atlas = spriteAtlas;
			mainImage.spriteName = Path.GetFileName(data.texturePath); //Either the keyName or the cardPath for the spriteName
			mainImage.depth = -1; //Want this layered under the frame/background
		}
		else if (loadedTextures != null)
		{
			backgroundImage.gameObject.SetActive(false);
			mainImage.gameObject.SetActive(false);
			
			Material cardMaterial = new Material(cardImageTexture.material.shader);
			Texture2D cardTexture;
			if (loadedTextures.TryGetValue(Path.GetFileName(data.texturePath), out cardTexture))
			{
				cardMaterial.mainTexture = cardTexture;
				cardImageTexture.material = cardMaterial;
			}
			
			Material frameMaterial = new Material(cardFrameTexture.material.shader);
			Texture2D frameTexture;
			if (loadedTextures.TryGetValue(FRAME_SPRITE_NAME + data.rarity, out frameTexture))
			{
				frameMaterial.mainTexture = frameTexture;
				cardFrameTexture.material = frameMaterial;
			}

			cardImageTexture.gameObject.SetActive(true);
			cardFrameTexture.gameObject.SetActive(true);
		}

		if (titleLabel != null)
		{
			titleLabel.text = Localize.text(data.keyName + CARD_TITLE_LOCALIZATION_POSTFIX);
		}

		//Set stars according to rarity
		rarity = data.rarity;

		//Set the card### label
		if (cardIDLabel != null)
		{
			cardIDLabel.color = rarityColors[rarity-1];
			cardIDLabel.text = string.Format("#{0}", data.id);
		}
		
		setupStars(dataContainsPowerup);

		switch (location)
		{
			case CardLocation.PACK_DROP:
				if (sheenPrefabs.Length >= rarity)
				{
					if (sheenPrefabs[rarity-1] != null && !isPowerup)
					{
						NGUITools.AddChild(sheenParent, sheenPrefabs[rarity-1]);
					}
				}
				break;

			case CardLocation.SET_VIEW:
				onClickButton.gameObject.SetActive(true);
				onClickButton.registerEventDelegate(cardClicked);
				if (!data.isCollected)
				{
					if (spriteAtlas != null)
					{
						backgroundImage.color = unCollectedColor;
						mainImage.color = unCollectedColor;
					}
					else
					{
						cardFrameTexture.color = unCollectedColor;
						cardImageTexture.color = unCollectedColor;
					}
					titleLabel.color = unCollectedColor;
				}
				else
				{
					shadowParent.SetActive(true);

					if (spriteAtlas != null)
					{
						backgroundImage.color = collectedColor;
						mainImage.color = collectedColor;
					}
					else
					{
						cardFrameTexture.color = collectedColor;
						cardImageTexture.color = collectedColor;
					}
					titleLabel.color = collectedColor;
				}
				break;

			case CardLocation.DETAILED_VIEW:
				if (!data.isCollected)
				{
					if (spriteAtlas != null)
					{
						backgroundImage.color = unCollectedColor;
						mainImage.color = unCollectedColor;
					}
					else
					{
						cardFrameTexture.color = unCollectedColor;
						cardImageTexture.color = unCollectedColor;
					}
					titleLabel.color = unCollectedColor;
				}
				break;

			default:
				Debug.LogError("Unsupported Card Location");
				break;
		}
	}

	private void setupStars(bool isPowerup)
	{
		Color[] colors = isPowerup ? powerupStarsRarityColors : rarityColors;
		int activeCount = 0;
		for (int i = 0; i < starObjects.Length; i++)
		{
			if (i < rarity)
			{
				starObjects[i].gameObject.SetActive(true);
				starObjects[i].color = colors[rarity-1];
				starShadows[i].SetActive(isPowerup);
				activeCount++;
			}
			else
			{
				starObjects[i].gameObject.SetActive(false);
			}
		}
		
		if (isPowerup)
		{
			starsParent.transform.localPosition = powerupsStarPosition - new Vector3((activeCount-1)*STARS_OFFSET, 0, 0) ;
		}
	}

	private void cardClicked(Dict args = null)
	{
		Audio.play("ClickMoreInfoCollections");
		newBadgeParent.SetActive(false);
	}

	public IEnumerator startStarCollection(GameObjectCacher starCache, GameObjectCacher bustCache, CollectionsDuplicateMeter starMeter, Transform starParent, int finalStarCount)
	{
		for (int i = 0; i < rarity; i++)
		{
			GameObject star = starCache.getInstance();
			GameObject particle = bustCache.getInstance();
			if(particle != null)
			{
				particle.transform.parent = starParent;
				particle.transform.localPosition = Vector3.zero;
				particle.transform.localScale = Vector3.one;
				if (star != null)
				{
					Transform animatedStarTransform = star.transform;
					animatedStarTransform.parent = starParent;
					animatedStarTransform.position = new Vector3(starObjects[i].transform.position.x, starObjects[i].transform.position.y, starParent.position.z);
					animatedStarTransform.localScale = Vector3.one;
					Audio.play("DupStar" + (rarity + 1) + "AnimateCollections");
					starObjects[i].color = Color.white;
					star.SetActive(true);
					StartCoroutine(tweenStarThenReleaseToPool(star, particle, starCache, bustCache, starMeter, finalStarCount));
					if (i != rarity - 1)
					{
						yield return new WaitForSeconds(STAR_TWEEN_STAGGER_TIME); //Stagger the stars a little bit
					}
				}
			}

		}
	}

	private IEnumerator tweenStarThenReleaseToPool(GameObject star, GameObject particle ,GameObjectCacher starCache, GameObjectCacher bustCache, CollectionsDuplicateMeter starMeter, int finalStarCount)
	{
		Vector3 targetPos = new Vector3 (starMeter.starTarget.position.x, starMeter.starTarget.position.y, starMeter.starParent.position.z);
		yield return new WaitForSeconds(STAR_INTRO_ANIM_WAIT); //Intro animation time
		Audio.play("DupStarTravelCollections");
		iTween.MoveTo(star, iTween.Hash("position", targetPos, "islocal", false, "time", STAR_TWEEN_TIME, "easetype", iTween.EaseType.linear));
		yield return new WaitForSeconds(STAR_TWEEN_TIME);
		Audio.play("DupStarArriveCollections");
		star.SetActive(false);
		starCache.releaseInstance(star);

		particle.SetActive(true);
		starMeter.addToStarMeter(1, finalStarCount);
		StartCoroutine(waitThenReleaseCachedStar(particle, bustCache));
	}

	private IEnumerator waitThenReleaseCachedStar(GameObject particle, GameObjectCacher bustCache)
	{
		yield return new WaitForSeconds(STAR_EXPLOSION_LENGTH);
		particle.SetActive(false);
		bustCache.releaseInstance(particle);
	}

	public void loadAnimatedNewBadge()
	{
		newBadgeParent.SetActive(true);
		GameObject newBadge = NGUITools.AddChild(newBadgeParent, newBadgePrefab);
		newBadgeAnimator = newBadge.GetComponent<Animator>();
	}

	public void loadStaticNewBadge()
	{
		newBadgeParent.SetActive(true);
		NGUITools.AddChild(newBadgeParent, staticNewBadgePrefab);
	}

	public void hideNewBadge()
	{
		SafeSet.gameObjectActive(newBadgeParent, false);
	}

	public void playNewBadgeAnimation(Dict data = null)
	{
		if (newBadgeAnimator != null)
		{
			newBadgeAnimator.Play(NEW_BADGE_INTRO_ANIM_NAME);
		}
	}

	public void playNewCardBounce()
	{
		if (newBadgeAnimator != null)
		{
			newBadgeAnimator.Play(NEW_BADGE_BOUNCE_ANIM_NAME);
		}
	}

	public void reset()
	{
		for (int i = 0; i < starObjects.Length; i++)
		{
			starObjects[i].gameObject.SetActive(false);
		}
	}
}
