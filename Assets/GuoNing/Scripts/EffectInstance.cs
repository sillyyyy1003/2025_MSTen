using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectInstance : MonoBehaviour
{
	private EffectType effectType;
	private ParticleSystem[] particles;
	public void SetUp(EffectType effectType)
	{
		this.effectType = effectType;
		particles = GetComponentsInChildren<ParticleSystem>();

		foreach (var p in particles)
		{
			var main = p.main;
			main.stopAction = ParticleSystemStopAction.Callback;
			p.Play(true);
		}
	}

	private void Update()
	{
		foreach (var p in particles)
		{
			if (p.IsAlive())
				return;
		}
	
		EffectManager.Instance.Recycle(effectType, gameObject);
	}
	void OnParticleSystemStopped()
	{
		EffectManager.Instance.Recycle(effectType, gameObject);
	}



}
