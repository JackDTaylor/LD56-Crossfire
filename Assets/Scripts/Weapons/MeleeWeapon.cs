using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour {
	public Vector2 rotateDegrees;
	public float rotateSpeed = 1f;
	public int damage = 10;

	public AntController owner;

	public Collider2D bladeCollider;

	class HitHistory {
		public AntController ant;
		public float time;
	}

	readonly List<HitHistory> hitHistory = new List<HitHistory>();

	bool IsAntAlreadyHit(AntController ant) {
		if(ant == null) {
			return true;
		}

		foreach(var entry in hitHistory) {
			if(entry.ant != null && entry.ant == ant && Time.time - entry.time < GetAttackCooldown())
				return true;
		}

		return false;
	}

	float GetAttackCooldown() {
		return 1f / rotateSpeed;
	}

	void Update() {
		var state = 0.5f + 0.5f * Mathf.Sin(Time.time * rotateSpeed * Mathf.PI);
		transform.localRotation = Quaternion.Euler(Vector3.forward * Mathf.Lerp(rotateDegrees.x, rotateDegrees.y, state));

		var colliders = new List<Collider2D>();

		bladeCollider.GetContacts(colliders);

		foreach(var col in colliders) {
			var ant = col.GetComponentInParent<AntController>();

			if(ant == null || ant.isDead || ant.isInvincible || IsAntAlreadyHit(ant)) {
				continue;
			}

			ant.ReceiveDamage(damage, owner, ant.transform.position);

			hitHistory.Add(new HitHistory() { ant = ant, time = Time.time });
		}
	}
}