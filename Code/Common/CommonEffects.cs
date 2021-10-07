using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
This is a purely static class of generic useful functions that relate to effects.
*/
public static class CommonEffects
{
	
	/// Returns a value between min and max based on the current time
	/// and the speed in a constant growth and decay function (sine)
	public static float pulsateBetween(float min, float max, float speed, float offset = 0f)
	{
		offset = Mathf.Clamp01(offset);
		
		return (Mathf.Sin(Time.time * speed + (offset * 2f * Mathf.PI)) + 1f) * 0.5f * (max - min) + min;
	}
	
	// add a component of type UIPanelReduceAlpha to the panel passed, with default/specified values. 
	public static void addUIPanelReduceAlphaEffect(UIPanel panel, float speedOfDecline = 0.001f, float valueToReduceTo = 0.0f, bool oscillateEffect = false)
	{
		UIPanelReduceAlpha reduceAlpha = panel.gameObject.AddComponent<UIPanelReduceAlpha>() as UIPanelReduceAlpha;
		reduceAlpha.initialize(panel, speedOfDecline, valueToReduceTo, oscillateEffect);
	}
	
	// add a component of type ToGrey to the MeshRenderer passed, with default/specified values. 
	public static void addToGreyEffect(MeshRenderer materialWithHue, float startingValue = 0.0f,  float speedOfDecline = 0.001f) 
	{
		ToGrey toGrey = materialWithHue.gameObject.AddComponent<ToGrey>() as ToGrey;
		toGrey.initialize(materialWithHue, startingValue, speedOfDecline);
	}
	
	
	// add a component of type OscillateTextColor to the UILabel passed, with default/specified values
	public static void addOscillateTextColorEffect(UILabel label, Color[] colors, float speedOfOscillation = 0.001f)
	{
		addOscillateTextColorEffect(new LabelWrapper(label), colors, speedOfOscillation);
	}

	public static void addOscillateTextColorEffect(LabelWrapper label, Color[] colors, float speedOfOscillation = 0.001f)
	{
		OscillateTextColor oscillateColor = label.gameObject.AddComponent<OscillateTextColor>() as OscillateTextColor;
		oscillateColor.initialize(label, colors, speedOfOscillation);
	}
	
	// add a component of type OscillateSpriteColor to the UISprite passed, with default/specified values
	public static void addOscillateSpriteColorEffect(UISprite sprite, Color[] colors, float speedOfOscillation = 0.001f)
	{
		OscillateSpriteColor oscillateSprite = sprite.gameObject.AddComponent<OscillateSpriteColor>() as OscillateSpriteColor;
		oscillateSprite.initialize(sprite, colors, speedOfOscillation);
	}
	
	public static void addOscillateParticleColorEffect( ParticleSystem passedParticles, Color leftColor, Color rightColor, float startingValue = 0.0f, float speedOfOscillation = 0.001f, int initialDirection = 1)
	{
		OscillateParticleColor oscillateParticle = passedParticles.gameObject.AddComponent<OscillateParticleColor>() as OscillateParticleColor;
		oscillateParticle.initialize(passedParticles, leftColor, rightColor, startingValue, speedOfOscillation, initialDirection);
	}

	/**
	 *  Performs two LERPS using a triangle kernel meaning that "val" will take normalized values from [0,1] and 0.5 will represent the max value of the triangle
	 *  and 0 and 1 will be the min value
	*/
	public static float triangleLERP(float val, float min, float max)
	{
		float scale;

		if (val <= 0.5f) 
		{
			//Ramp up (first part of triangle lerp [0,0.5]
			float halfNormalizedPos = val * 2; //Map to [0,1] space Lower half reel position
			scale = Mathf.Lerp(min,max,halfNormalizedPos);
		} 
		else 
		{
			//Ramp down (second part of triangle lerp [0.5,1])
			float halfNormalizedPos = (val - 0.5f) * 2; //Map to [0,1] space Upper half reel position
			scale = Mathf.Lerp(max,min,halfNormalizedPos);
		}

		return scale;
	}

	/**
	 *  Performs two LERPS using an inverse triangle kernel meaning that "val" will take normalized values from [0,1] and 0.5 will represent the min value of the triangle and
	 * 0 and 1 will take the max value
	*/
	public static float inverseTriangleLERP(float val, float min, float max)
	{
		float scale;

		if (val <= 0.5f) 
		{
			//Ramp up (first part of triangle lerp [0,0.5]
			float halfNormalizedPos = val * 2; //Map to [0,1] space Lower half reel position
			scale = Mathf.Lerp(max,min,halfNormalizedPos);
		} 
		else 
		{
			//Ramp down (second part of triangle lerp [0.5,1])
			float halfNormalizedPos = (val - 0.5f) * 2; //Map to [0,1] space Upper half reel position
			scale = Mathf.Lerp(min,max,halfNormalizedPos);
		}

		return scale;
	}

	/**
	Play all particle systems on the passed in object and its children
	*/
	public static void playAllParticleSystemsOnObject(GameObject obj)
	{
		foreach (ParticleSystem particleSys in obj.GetComponentsInChildren<ParticleSystem>(true))
		{
			setEmissionEnable(particleSys, true);
			particleSys.Play();
		}
	}
	
	/**
	Stop all particle systems on the passed in object and its children
	*/
	public static void stopAllParticleSystemsOnObject(GameObject obj)
	{
		foreach (ParticleSystem particleSys in obj.GetComponentsInChildren<ParticleSystem>(true))
		{
			particleSys.Stop();
			particleSys.Clear();
			setEmissionEnable(particleSys, false);
		}
	}
	
	/**
	Stop all animations, animators, and particle systems on an object
	*/
	public static void stopAllVisualEffectsOnObject(GameObject obj)
	{

		CommonAnimation.stopAllAnimationsOnObject(obj);
		CommonAnimation.stopAllAnimatorsOnObject(obj);
	    stopAllParticleSystemsOnObject(obj);
	}

	/// Scales a gameobject to the given scale, then back to the original scale, over the given duration.
	public static IEnumerator throb(GameObject go, float scale, float duration)
	{
		Vector3 origScale = go.transform.localScale;
		duration *= .5f;
		
		iTween.ScaleTo(go, iTween.Hash("scale", Vector3.one * scale, "time", duration, "easetype", iTween.EaseType.easeOutSine));
		yield return new WaitForSeconds(duration);
		
		iTween.ScaleTo(go, iTween.Hash("scale", origScale, "time", duration, "easetype", iTween.EaseType.easeInSine));
		yield return new WaitForSeconds(duration);		
	}
	
	/// Scales a gameobject to the given scale, then back to the original scale, over the given duration.
	public static IEnumerator throb(GameObject go, Vector3 scale, float duration)
	{
		Vector3 origScale = go.transform.localScale;
		duration *= .5f;
		
		iTween.ScaleTo(go, iTween.Hash("scale", scale, "time", duration, "easetype", iTween.EaseType.easeOutSine));
		yield return new WaitForSeconds(duration);
		
		iTween.ScaleTo(go, iTween.Hash("scale", origScale, "time", duration, "easetype", iTween.EaseType.easeInSine));
		yield return new WaitForSeconds(duration);		
	}

	/// Checks to see if a particle system is alive because IsAlive(), doens't always work.
	/// False if the particle system is done emitting particles and all particles are dead.
	public static bool isParticleSystemAlive(GameObject go, bool includeChildren = true)
	{
		if (!includeChildren)
		{
			ParticleSystem ps = go.GetComponent<ParticleSystem>();
			if (ps != null)
			{
				if (ps.emission.enabled || ps.particleCount > 0)
				{
					return true;
				}
			}
		}
		else
		{
			ParticleSystem[] particleSystems = go.GetComponentsInChildren<ParticleSystem>();
			if (particleSystems != null)
			{
				foreach (ParticleSystem ps in particleSystems)
				{
					if (ps.emission.enabled || ps.particleCount > 0)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public static IEnumerator fadeSpritesAndText(GameObject parentObject, float targetAlpha, float duration)
	{
		bool isMaxAlpha = false;

		UISprite[] sprites = parentObject.GetComponentsInChildren<UISprite>();

		TextMeshPro[] textMeshObjects = parentObject.GetComponentsInChildren<TextMeshPro>();

		float currentTime = 0;

		while (currentTime < duration && !isMaxAlpha)
		{
			currentTime += Time.deltaTime;

			if (currentTime > duration)
			{
				currentTime = duration;
			}

			float newAlpha = Mathf.Clamp((currentTime / duration), 0.0f, 1.0f);

			if (newAlpha > targetAlpha)
			{
				newAlpha= targetAlpha;
				isMaxAlpha = true;
			}

			foreach (UISprite sprite in sprites)
			{
				sprite.alpha = newAlpha;
			}

			foreach (TextMeshPro text in textMeshObjects)
			{
				text.alpha = newAlpha;
			}

			yield return null;
		}

		yield return null;
	}

	// Shakes the screen around a bit to give the effect that there is something looming.
	// Loops forever so you will have to handle cleaning it up yourself.
	public static IEnumerator shakeScreen(GameObject[] objectsToShake, float YRot = .05f, float ZRot = .05f)
	{
		float minYRot = -YRot;
		float maxYRot = YRot;
		float minZRot = -ZRot;
		float maxZRot = ZRot;
		while (true)
		{
			// Move each object in objects to shake.
			foreach (GameObject go in objectsToShake)
			{
				Vector3 shakeRotation = go.transform.localEulerAngles;
				//shakeRotation.x = CommonEffects.pulsateBetween(minZRot, maxZRot, 3);
				shakeRotation.y = CommonEffects.pulsateBetween(minYRot, maxYRot, 13);
				shakeRotation.z = CommonEffects.pulsateBetween(minZRot, maxZRot, 19);
				go.transform.localEulerAngles = shakeRotation;
			}
			yield return null;
		}
	}

	// Convenience function to set ParticleSystem.emission.enable in a single line
	// (with unity 5.3, you need to copy the emission to a temporary variable first)
	public static void setEmissionEnable(ParticleSystem ps, bool enable)
	{
		ParticleSystem.EmissionModule em = ps.emission;
		em.enabled = enable;
	}

	// Convenience function to set ParticleSystem.emission.rate in a single line
	// (with unity 5.3, you need to copy the emission to a temporary variable first)
	public static void setEmissionRate(ParticleSystem ps, float rate)
	{
		ParticleSystem.EmissionModule em = ps.emission;
		em.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
	}

	public static void throbLoop(GameObject gameObject, float maxScale = 1.12f, float time = 1.0f)
	{
		Vector3 maxSize = new Vector3(maxScale, maxScale, time);
		iTween.ScaleTo(gameObject, iTween.Hash("scale", maxSize, "islocal", false, "time", 1.5f, "looptype", iTween.LoopType.pingPong, "easetype", iTween.EaseType.easeInOutQuad));
	}
}
