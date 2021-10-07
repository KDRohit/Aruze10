using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Handles dynamically stacking coin sprites, mainly to save time from manually having to do it.
*/
[ExecuteInEditMode]
public class CoinStacker : TICoroutineMonoBehaviour
{
	private const int STACK_SPACING_X = 100;
	private const int STACK_SPACING_Y = 50;
	private const string COIN_SPRITE_NAME = "Coin Horizontal Small";
	
	public int startingDepth;
	public GameObject coinSpriteTemplate;
	public string[] stackData;
	public int coinHeight = 16;

	private List<CoinStack> stacks = new List<CoinStack>();

#if UNITY_EDITOR
	public bool remakeStacks = false;
	public bool forceClearStacks = false;
	public bool storeNewPosition = false;	// Click this after moving the transform position while there are no stacks showing.
	
	private Vector3 originalPosition = Vector3.zero;
	
	void Update()
	{
		if (Application.isPlaying)
		{
			return;
		}
		
		if (forceClearStacks)
		{
			clearStacks();
			forceClearStacks = false;
		}
		
		if (remakeStacks)
		{
			clearStacks();
			if (originalPosition == Vector3.zero)
			{
				storeNewPositionNow();
			}
			setupStacks();
			showStacks();
			remakeStacks = false;
		}
		
		if (storeNewPosition)
		{
			storeNewPositionNow();
			storeNewPosition = false;
		}
	}
	
	private void storeNewPositionNow()
	{
		originalPosition = transform.localPosition;
	}

	private void clearStacks()
	{
		while (transform.childCount > 0)
		{
			DestroyImmediate(transform.GetChild(0).gameObject);
		}
		stacks.Clear();
		// Restore the original position of the CoinStacker object, since it gets repositioned after making the stacks.
		transform.localPosition = originalPosition;
	}

	private void showStacks()
	{
		foreach (CoinStack stack in stacks)
		{
			foreach (UISprite sprite in stack.sprites)
			{
				sprite.gameObject.SetActive(true);
			}
			stack.setReflectionSprite(stack.sprites.Count);
		}
	}
	
#endif
	
	void Awake()
	{
		if (Application.isPlaying)
		{
			setupStacks();
		}
	}
	
	public void setupStacks()
	{
		for (int row = 0; row < stackData.Length; row++)
		{
			string[] stackCounts = stackData[row].Split(' ');
			
			for (int column = 0; column < stackCounts.Length; column++)
			{
				string countString = stackCounts[column];
				int count = 0;
				if (countString != "")
				{
					try
					{
						count = int.Parse(countString);
					}
					catch
					{
						Debug.LogWarning("Invalid non-int value provided for CoinStacker: " + countString);
					}
				}
				stacks.Add(new CoinStack(row, column, count));
			}
		}
		
		// Create, position, then initially hide all the coin sprites.
		float minX = 99999999;
		float maxX = -minX;
		float minY = 99999999;
		float maxY = -minY;
		
		foreach (CoinStack stack in stacks)
		{
			int x = stack.column;
			int y = stack.row;
			int count = stack.coinCount;
			int baseDepth = startingDepth + y * 20;
			
			float xPos = x * STACK_SPACING_X;
			float baseY = -(y * STACK_SPACING_Y);
			// We use the negative value of y above because we want each successive row to be below the previous one,
			// so it appears in front of the previous one.
			
			if (x % 2 == 1)
			{
				// Every other column, stagger the y position slightly, so the rows don't look
				// so perfectly aligned and non-organic.
				baseY += STACK_SPACING_Y * .2f;
			}
			
			if (y % 2 == 1)
			{
				// Every other row, stagger the x position so stacks aren't exactly in front of each other.
				xPos += STACK_SPACING_X * .5f;
			}
						
			for (int z = 0; z < count; z++)
			{
				GameObject go = CommonGameObject.instantiate(coinSpriteTemplate) as GameObject;
				go.transform.parent = transform;
				UISprite coin = go.GetComponent<UISprite>();
				coin.spriteName = COIN_SPRITE_NAME;
				
				go.SetActive(false);
				stack.sprites.Add(coin);
				
				float yPos = baseY + z * coinHeight;
				
				coin.transform.localPosition = new Vector3(xPos, yPos, coinSpriteTemplate.transform.localPosition.z);
				coin.depth = baseDepth + z + 1;	// Add 1 because the reflection is the same but without the 1 added.
				coin.MakePixelPerfect();
				
				minX = Mathf.Min(minX, xPos);
				maxX = Mathf.Max(maxX, xPos);
				minY = Mathf.Min(minY, yPos);
				maxY = Mathf.Max(maxY, yPos);
			}

			// Create a sprite for the reflection, but don't use it yet.
			GameObject reflectionGo = NGUITools.AddChild(gameObject, coinSpriteTemplate) as GameObject;
			stack.reflectionSprite = reflectionGo.GetComponent<UISprite>();
			reflectionGo.SetActive(false);
			reflectionGo.name = "Reflection";
			
			float yPosReflection = baseY - coinHeight;
			stack.reflectionSprite.transform.localPosition = new Vector3(xPos, yPosReflection, coinSpriteTemplate.transform.localPosition.z);
			stack.reflectionSprite.depth = baseDepth;
		}

		// Automatically center the stacks on the original center point.
		Vector3 pos = transform.localPosition;
		pos.x -= (maxX + minX) * .5f * transform.localScale.x;
		pos.y -= (maxY + minY) * .5f * transform.localScale.y;
		transform.localPosition = pos;
		
		// TODO: Find a good sound for the coins stacking animation, or don't.
//		Audio.play("coin_wave");
	}
	
	// Start animating the stacks into view.
	public void animateStacks()
	{
		foreach (CoinStack stack in stacks)
		{
			StartCoroutine(stack.animate());
		}
	}
		
	private class CoinStack
	{
		private const float COIN_STACK_DELAY = 0.1f;
		private string[] REFLECTION_SPRITE_NAMES = new string[]
		{
			"",		// There is no sprite for 0 reflections.
			"Coin Horizontal Small Reflection 1",
			"Coin Horizontal Small Reflection 2",
			"Coin Horizontal Small Reflection 3"
		};
		
		public int row;
		public int column;
		public int coinCount = 0;				// The number of coins in the stack.
		public List<UISprite> sprites = new List<UISprite>();
		public UISprite reflectionSprite = null;
		public Vector2 position = Vector2.zero;

		public CoinStack(int row, int column, int coinCount)
		{
			this.row = row;
			this.column = column;
			this.coinCount = coinCount;
		}
		
		// Show all the coin sprites in an animated way.
		public IEnumerator animate()
		{
			int coinsShowing = 0;
			
			foreach (UISprite sprite in sprites)
			{
				sprite.gameObject.SetActive(true);
				coinsShowing++;
				
				setReflectionSprite(coinsShowing);
				
				yield return new WaitForSeconds(COIN_STACK_DELAY);
			}
		}
		
		public void setReflectionSprite(int coinsShowing)
		{
			if (coinsShowing == 0)
			{
				return;
			}
			
			// Show the reflection sprite if it hasn't been shown yet.
			if (!reflectionSprite.gameObject.activeSelf)
			{
				reflectionSprite.gameObject.SetActive(true);
			}
			// Determine which reflection to use.
			int count = Mathf.Min(coinsShowing, 3);
			reflectionSprite.spriteName = REFLECTION_SPRITE_NAMES[count];
			reflectionSprite.MakePixelPerfect();
		}
	}
}
