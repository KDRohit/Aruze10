using UnityEngine;
using System.Collections;
using UnityEditor;

/*
 * Editor class to help with assigning large numbers of ParticleTrailController values, just allows multi edit for now
 */
[CustomEditor(typeof(ParticleTrailController), true), CanEditMultipleObjects]
public class ParticleTrailControllerEditor : Editor 
{}
