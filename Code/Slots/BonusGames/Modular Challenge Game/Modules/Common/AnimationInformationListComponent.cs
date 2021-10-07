using UnityEngine;

// all this class does is hold a list of animation information so we can grab the component
// on an instance and animate it. Used by AnimatedParticleEffect.cs
public class AnimationInformationListComponent : MonoBehaviour 
{
	public AnimationListController.AnimationInformationList animationInformationList;
}
