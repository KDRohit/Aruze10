using UnityEngine;
using System.Collections.Generic;

public class VisualEffectComponent : TICoroutineMonoBehaviour
{
	public enum EffectDuration
	{
		Auto,	// calculates play time based on particle systems and
					// animations, and destroys itself after completion
		Timed,		// destroys itself after a user-specified duration
		ScriptControlled,	// destruction is left up to the user
	};
	
	private List<ParticleSystem> particleSystems;
	private List<Animation> animations;
	private bool isPlaying = false;
	private bool isPaused = false;
	private bool isFinishing = false;
	private float duration = -1.0f;
	private float timeLeft;
	//private float finishTime;
	private Vector3 lastPosition;
	private bool destroyedThisFrame = false;
	
	public EffectDuration durationType = EffectDuration.Auto;
	public float editorSpecifiedDuration = -1;
	public bool playOnAwake = false;
	public bool fadesOutOnFinish = false;
	public float finishTime = 0;
	public Vector3 offset = Vector3.zero;
	public Vector3 scale = Vector3.zero;	// this param is ignored if it's the zero vector
	public int renderQueue = -1;	// -1 means don't touch the render queue, just use whatever it's already using, anything >= 0 sets the render queue value
	
	public bool IsPlaying { get { return isPlaying; } }
	public bool IsPaused { get { return isPaused; } }

	public float Duration { get { return duration; } }

	public static VisualEffectComponent Create(GameObject prefab, GameObject parent = null)
	{
		if(prefab == null) return null;
		
		GameObject obj = CommonGameObject.instantiate(prefab) as GameObject;
		VisualEffectComponent vfx = null;
		if(obj != null)
		{
			if(parent != null)
				obj.transform.parent = parent.transform;
			
			vfx = obj.GetComponent<VisualEffectComponent>();
			if(vfx != null)
			{
				obj.transform.localPosition = vfx.offset;
				if(vfx.scale != Vector3.zero)
				{
					obj.transform.localScale = vfx.scale;
				}
				vfx.Play();
			}
		}
		
		return vfx;
	}
	
	void Awake()
	{
		ParticleSystem[] particleSystemsArray = GetComponentsInChildren<ParticleSystem>();
		particleSystems = new List<ParticleSystem>(particleSystemsArray);
		
		Animation[] animationsArray = GetComponentsInChildren<Animation>();
		animations = new List<Animation>(animationsArray);
		
		Initialize();
		
		if(playOnAwake)
		{
			Play();
		}
	}
	
	void Initialize()
	{
		if (this.durationType == EffectDuration.Auto)
		{
			// compute the duration based on our parent object's longest animation or particle system
			this.duration = -1.0f;
			if (this.particleSystems != null)
			{
				foreach(ParticleSystem particleSystem in this.particleSystems)
				{
					this.duration = Mathf.Max(this.duration, particleSystem.main.duration);
				}
			}
			/*
			if (this.particleEmitters != null)
			{
				foreach (ParticleEmitter emitter in this.particleEmitters)
				{
					this.duration = Mathf.Max(this.duration, emitter.maxEnergy);
				}
			}
			*/
			if (this.animations != null)
			{
				foreach(Animation animation in this.animations)
				{
					if (animation.clip != null)
					{
						this.duration = Mathf.Max(this.duration, animation.clip.length);
					}
				}
			}
		}
		else if (this.durationType == EffectDuration.Timed)
		{
			this.duration = this.editorSpecifiedDuration;
		}

		this.timeLeft = duration;
		//this.finishTime = this.finishTime_u / (float)(Stat.units);
		this.isFinishing = false;
		this.destroyedThisFrame = false;
		
		if(renderQueue >= 0)
		{
			foreach(ParticleSystem ps in particleSystems)
			{
				ps.GetComponent<Renderer>().material.renderQueue = renderQueue;
			}
		}
	}
	
	public void Play()
	{
		foreach(ParticleSystem ps in particleSystems)
		{
			ps.Play();
		}
		
		foreach(Animation anim in animations)
		{
			anim.Play();
		}
		
		isPlaying = true;
	}
	
	public void Pause()
	{
		foreach(ParticleSystem ps in particleSystems)
		{
			ps.Stop();
		}
		
		foreach(Animation anim in animations)
		{
			foreach(AnimationState animState in anim)
				animState.speed = 0;
		}
		
		isPaused = true;
	}
	
	public void Resume()
	{
		if(!isPlaying) Play();
		
		foreach(ParticleSystem ps in particleSystems)
		{
			ps.Play();
		}
		
		foreach(Animation anim in animations)
		{
			foreach(AnimationState animState in anim)
				animState.speed = 1;
		}
		
		isPaused = false;
	}
	
	public void Reset()
	{
		foreach(ParticleSystem ps in particleSystems)
		{
			ps.Stop();
		}
		
		foreach(Animation anim in animations)
		{
			anim.Stop();
		}
		
		isPlaying = false;
		isPaused = false;
	}
	
	void Update ()
	{
		if (this.durationType == EffectDuration.Auto || this.durationType == EffectDuration.Timed)
		{
			this.timeLeft -= Time.deltaTime;
			if (this.timeLeft <= 0)
			{
				this.Finish();
			}
		}

		if (this.isFinishing)
		{
			this.finishTime -= Time.deltaTime;
			if (this.finishTime <= 0)
			{
				DestroyMe();
			}
		}
	}
	
	public virtual void Finish(bool forceDestroyMe = false)
	{
		isPlaying = false;
		
		if(!this.isFinishing)
		{
			this.isFinishing = true;
			
			if(fadesOutOnFinish)
			{
				FadeOutVfx(false);
			}

			if (this.finishTime <= 0 || forceDestroyMe)
			{
				DestroyMe();
			}
		}
	}
	
	protected void OnDestroy()
	{
		if (!this.isFinishing)
		{
			this.Finish(true);
		}
	}
	
	public virtual void FadeOutVfx(bool killParticles)
	{
		isPlaying = false;
		
		if (this.particleSystems != null)
		{
			for (int i = 0; i < this.particleSystems.Count; ++i)
			{
				if (this.particleSystems[i] == null) continue;

				this.particleSystems[i].Stop();
				CommonEffects.setEmissionEnable(this.particleSystems[i], false);

				if (killParticles)
				{
					this.particleSystems[i].Clear();
				}
			}
		}
		if (this.animations != null)
		{
			for (int i = 0; i < this.animations.Count; ++i)
			{
				if (this.animations[i] == null) continue;
				
				this.animations[i].Stop();
			}
		}
	}
	
	private void DestroyMe()
	{
		if (!this.destroyedThisFrame)
		{
			GameObject.Destroy(this.gameObject);
			
			this.destroyedThisFrame = true;
		}
	}
}
