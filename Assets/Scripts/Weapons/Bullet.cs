using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Bullet : MonoBehaviour {
	public AntController owner;
	public RangeWeapon weapon;
	public Transform ownerBarrel;
	
	float damage;
	
	public float speed = 55f;
	public float maxDistance = 250f;
	
	public float trailLength = 1f;
	
	public float delay;
	
	float distanceTravelled;
	
	float destinationDistance;
	bool destinationReached;

	LineRenderer lineRenderer;
	
	readonly List<AntController> alreadyHitAnts = new List<AntController>();

	void Start() {
		speed       = weapon.bulletSpeed;
		trailLength = weapon.bulletTrailLength;
		maxDistance = weapon.range;
		damage      = weapon.damage;
		
		lineRenderer = GetComponent<LineRenderer>();
		lineRenderer.SetPosition(1, Vector3.zero);
	}
	
	bool IsPassThrough(AntController ant) {
		// Penalty for distance (0% at dist `speed/5`, so after 100ms bullet will never pass through)
		var distancePenalty = Mathf.Clamp01(distanceTravelled / (speed / 5f));
		
		// Penalty for level between weapon `passThroughLevels` (1..33 by default)
		var levelPenalty = Mathf.Clamp01((ant.level - weapon.passThroughLevel.x) / (weapon.passThroughLevel.y - weapon.passThroughLevel.x));
		
		var penalty = Mathf.Pow(distancePenalty + levelPenalty, 2);
		
		// https://www.desmos.com/calculator/ig7fdl2xa7 - falls 1..0 - L50..L300 (additional multiplier to force bullet stopping by big ants) 
		var sizeDiceMultiplier = -0.4f * ant.CalcScale() + 1.4f;
		
		var dice = Random.Range(0f, 1f) * sizeDiceMultiplier;
		
		if(ant.isDead) {
			// If ant is dead, PassThrough will count as a miss, that's what we need for dead bodies
			// 1 out of 8 bullets will stop at dead body on average
			dice *= 8f; 
		}
		
		// Debug.Log($"Bullet passthrough: {dice} > {penalty} = {dice > penalty} ({distancePenalty} + {levelPenalty})² = {Mathf.Pow(distancePenalty + levelPenalty, 2)}");
		
		return dice > penalty;
	}
	
	bool ProcessAntCollision(AntController ant, RaycastHit2D hit) {
		if(ant == owner || ant.isInvincible || alreadyHitAnts.Contains(ant)) {
			return false;
		}

		alreadyHitAnts.Add(ant);
		
		var passThrough = IsPassThrough(ant);
		var isDead = ant.isDead;
		
		if(!isDead) {
			ant.ReceiveDamage(Mathf.RoundToInt(damage), owner, hit.point);
			ant.PlayHitAnimation();
		}
		
		if(!isDead || !passThrough) {
			SpawnBloodSprays(hit, ant, passThrough);
		}
		
		if(passThrough) {
			damage = Mathf.Max(damage / weapon.passThroughFalloff, 1f);
			return false; // Returns false if bullet should proceed
		}
		
		if(isDead) {
			ant.level -= 1;

			if(ant.level <= 0) {
				ant.DestroyBody();
			} else {
				ant.PlayHitAnimation();
			}
		}

		SetEndPoint(hit.point);
		return true; // Returns true if bullet should stop
	}
	
	public void SpawnBloodSprays(RaycastHit2D hit, AntController ant, bool passThrough) {
		var size = ant.CalcScale() * ant.CalcScale();
		var intensity = weapon.bloodIntensity;
		
		if(ant.isDead) {
			size *= 0.5f;
			intensity *= 0.5f;
		}

		ProjectileManager.Get().SpawnBloodSpray(hit.point, transform.rotation.eulerAngles.z - 180f, intensity * 0.33f, size);

		if(passThrough) {
			ProjectileManager.Get().SpawnBloodSpray(hit.point, transform.rotation.eulerAngles.z, intensity, size);
		}
	}
	
	bool CheckCollision(Vector2 from, Vector2 to) {
		int[] blockingLayers = {
			LayerMask.NameToLayer("Level"),
		};
	
		int[] ignoredLayers = {
			LayerMask.NameToLayer("AntMouth"),
		};

		Vector2 direction = (to - from).normalized;
        
		float distance = Vector2.Distance(from, to);

		// Выполняем RaycastAll с начальной точки, направлением и расстоянием
		var hits = Physics2D.RaycastAll(from, direction, distance);
		
		foreach(var hit in hits) {
			if(ignoredLayers.Contains(hit.collider.gameObject.layer)) {
				continue;
			}
			
			if(blockingLayers.Contains(hit.collider.gameObject.layer)) {
				SetEndPoint(hit.point);
				return true;
			}
			
			var ant = hit.collider.gameObject.GetComponentInParent<AntController>();

			if(ant != null && ProcessAntCollision(ant, hit)) {
				return true;
			}
		}
		
		return false;
	}
	
	void SetEndPoint(Vector2 endPoint) {
		var distance = ((Vector2)transform.position - endPoint).magnitude;

		lineRenderer.SetPosition(1, Vector3.up * distance);
	}
	
	Vector2 GetPathPosition(float distance) {
		return transform.position + transform.up * distance;
	}
	
	void Update() {
		if(delay > 0) {
			delay -= Time.deltaTime;
			transform.position = ownerBarrel.position;
			return;
		}

		var prevDistance = distanceTravelled;
		distanceTravelled += speed * Time.deltaTime;
		
		var toDistance = destinationReached ? destinationDistance : distanceTravelled;
		var fromDistance =  Mathf.Clamp(distanceTravelled - trailLength, 0, toDistance);
		
		if(!destinationReached) {
			if(CheckCollision(GetPathPosition(prevDistance), GetPathPosition(distanceTravelled))) {
				destinationReached = true;
				destinationDistance = distanceTravelled;
			}
		}

		lineRenderer.SetPosition(0, Vector3.up * fromDistance);
		lineRenderer.SetPosition(1, Vector3.up * toDistance);

		if(distanceTravelled > maxDistance) {
			DestroyObject();
		}
		
		if(destinationReached && toDistance - fromDistance < Mathf.Epsilon) {
			DestroyObject();
		}
	}
	
	void DestroyObject() {
		transform.SetParent(null);
		Destroy(gameObject);
	}
}