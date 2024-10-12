using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Bullet : MonoBehaviour {
	public AntController owner;
	public RangeWeapon weapon;
	
	float damage;
	float lifespan = 0.25f;
	
	bool IsPassThrough(AntController ant) {
		// Chance goes from 100% (lvl 1) to 0% (lvl 33+)
		return Random.Range(0f, 1f) > (ant.level - 1) / weapon.maxPassThroughLevel;
	}

	void Start() {
		damage = weapon.damage;
		
		var hits = Physics2D.RaycastAll(transform.position, transform.up, 1000f);
		
		foreach(var hit in hits) {
			var ant = hit.collider.gameObject.GetComponentInParent<AntController>();
			
			if(hit.collider.gameObject.layer == LayerMask.NameToLayer("Level")) {
				SetEndPoint(hit.point);
				return;
			}
			
			if(ant != null && ant != owner && !ant.isInvincible) {
				var passThrough = IsPassThrough(ant);
				
				// We've hit other player
				if(!ant.isDead) {
					// We hit an ant
					ant.ReceiveDamage(Mathf.RoundToInt(damage), owner, hit.point);
					ant.PlayHitAnimation();
				} 

				if(passThrough) {
					damage = Mathf.Max(damage / weapon.passThroughFalloff, 1f);
				} else {
					if(ant.isDead) {
						ant.level -= 1;
						
						if(ant.level <= 0) {
							ant.DestroyBody();
						} else {
							ant.PlayHitAnimation();
						}
					}
					
					SetEndPoint(hit.point);
					return;
				}
			}
		}
	}
	
	void SetEndPoint(Vector2 endPoint) {
		var lineRenderer = GetComponent<LineRenderer>();
		var distance = ((Vector2)transform.position - endPoint).magnitude;

		lineRenderer.SetPosition(1, Vector3.up * distance);
	}
	
	void Update() {
		lifespan -= Time.deltaTime;

		var color = GetComponent<LineRenderer>().endColor;

		GetComponent<LineRenderer>().endColor = new Color(
			color.r,
			color.g,
			color.b,
			lifespan * 4f
		);

		if(lifespan < 0) {
			transform.SetParent(null);
			Destroy(gameObject);
		}
	}
}