using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectInstance : MonoBehaviour
{
	private EffectType effectType;
	private ParticleSystem[] particles;
	private bool isLoop = false;
	public void SetUp(EffectType effectType,bool _isLoop)
	{
		this.effectType = effectType;
		particles = GetComponentsInChildren<ParticleSystem>();

		isLoop = _isLoop;
		foreach (var p in particles)
		{
			var main = p.main;
			main.stopAction = isLoop
			? ParticleSystemStopAction.None
			: ParticleSystemStopAction.Callback;
			p.Play(true);
		}
	}

	private void Update()
	{
		if (isLoop)
			return;

		foreach (var p in particles)
		{
			if (p.IsAlive())
				return;
		}
	
		EffectManager.Instance.Recycle(effectType, gameObject);
	}
	void OnParticleSystemStopped()
	{
		if (isLoop) return;

		EffectManager.Instance.Recycle(effectType, gameObject);
	}

}
