using System.Collections;
using UnityEngine;

/*
 * External plugin file used for stuff like particle coin effects
 */
[RequireComponent(typeof(ParticleSystem))]
public class particleAttractorSpherical : MonoBehaviour 
{
	ParticleSystem ps;
	ParticleSystem.Particle[] m_Particles;
	public Transform target;
	public float speed = 5f;
	int numParticlesAlive;
	
	void Start() 
	{
		ps = GetComponent<ParticleSystem>();
		m_Particles = new ParticleSystem.Particle[ps.main.maxParticles];
	}
	
	void Update() 
	{
		if (target != null)
		{
			numParticlesAlive = ps.GetParticles(m_Particles);
			float step = speed * Time.deltaTime;
			for (int i = 0; i < numParticlesAlive; i++)
			{
				m_Particles[i].position = Vector3.SlerpUnclamped(m_Particles[i].position, target.position, step);
			}

			ps.SetParticles(m_Particles, numParticlesAlive);
		}
		else
		{
			// Perform this check in the Update loop since in some instances target is set after creating the script through code,
			// So we will only destroy it if the target isn't set when it goes to run its update.
#if UNITY_EDITOR
			// Only report this in the editor, since it should only be a visual issue that can't be immediately fixed on prod
			// and might come up a lot if a bunch of people are all interacting with the prefab that contains this broken script
			Debug.LogError("particleAttractorSpherical.Update() - target was not set, destroying this particleAttractorSpherical on GameObject.name = " + gameObject.name);
#endif
			Destroy(this);
		}
	}
}
