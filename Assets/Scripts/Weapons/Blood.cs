using System;
using UnityEngine;

public class Blood : MonoBehaviour {
	public float intensity = 1f;
	public float scale;
	
	public ParticleSystemShapeType shapeType = ParticleSystemShapeType.Cone;
	
	public ParticleSystem bloodParticles;

	void Start() {
		var main = bloodParticles.main;
		var shape = bloodParticles.shape;
		
		shape.shapeType = shapeType;
		
		main.startSize = ScaleCurve(main.startSize, intensity * Mathf.Max(scale, 1f), 0.5f);
		main.startSpeed = ScaleCurve(main.startSpeed, Mathf.Lerp(intensity, intensity * Mathf.Max(scale, 1f), 0.25f));
		
		var burst = bloodParticles.emission.GetBurst(0);
		burst.count = ScaleCurve(burst.count, intensity, 0.25f);
		bloodParticles.emission.SetBurst(0, burst);
	}
	
	ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve curve, float multiplier, float effectScale = 1.0f) {
		switch(curve.mode) {
			case ParticleSystemCurveMode.Constant:
				curve.constant = Mathf.Lerp(curve.constant, curve.constant * multiplier, effectScale);
				break;
			
			case ParticleSystemCurveMode.TwoConstants: 
				curve.constantMin = Mathf.Lerp(curve.constantMin, curve.constantMin * multiplier, effectScale);
				curve.constantMax = Mathf.Lerp(curve.constantMax, curve.constantMax * multiplier, effectScale);
				
				break;

			case ParticleSystemCurveMode.Curve: 
			case ParticleSystemCurveMode.TwoCurves:
				throw new NotImplementedException();
			
			default: 
				throw new ArgumentOutOfRangeException();
		}
		
		return curve;
	}

	void Update() {
		if(bloodParticles.isStopped) {
			Destroy(gameObject);
		}
	}
}