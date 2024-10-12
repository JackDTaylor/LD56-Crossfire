using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class GunSpawner : MonoBehaviour {
	public enum Pickup { NONE, KNIFE, SABRE, PISTOL, TURRET1, TURRET2 }

	public Sprite knifeSprite;
	public Sprite sabreSprite;
	public Sprite pistolSprite;
	public Sprite turret1Sprite;
	public Sprite turret2Sprite;

	public float spawnDelay = 10f;

	public float knifeWeight = 0.5f;
	public float sabreWeight = 0.25f;
	public float pistolWeight = 0.15f;
	public float turret1Weight = 0.08f;
	public float turret2Weight = 0.02f;

	public SpriteRenderer iconRenderer;

	public float rotateSpeed = 3f;

	public Pickup currentPickup;
	public Collider2D collider2d;

	float timeLeft = 0;

	void Start() {
		timeLeft = spawnDelay;
	}

	void Update() {
		iconRenderer.sprite = PickupSprite(currentPickup);
		iconRenderer.transform.rotation = quaternion.Euler(Vector3.forward * Time.time * rotateSpeed);

		if(currentPickup != Pickup.NONE) {
			var colliders = new List<Collider2D>();
			collider2d.GetContacts(colliders);

			foreach(var col in colliders) {
				var ant = col.GetComponentInParent<AntController>();

				if(ant == null) {
					continue;
				}

				ant.PickupWeapon(currentPickup);

				currentPickup = Pickup.NONE;
				timeLeft = spawnDelay;
			}

			return;
		}

		timeLeft -= Time.deltaTime;

		if(timeLeft < 0) {
			currentPickup = PickRandomPickup();
			timeLeft = spawnDelay;
		}
	}

	Sprite PickupSprite(Pickup pickup) {
		return pickup switch {
			Pickup.KNIFE => knifeSprite,
			Pickup.SABRE => sabreSprite,
			Pickup.PISTOL => pistolSprite,
			Pickup.TURRET1 => turret1Sprite,
			Pickup.TURRET2 => turret2Sprite,
			_ => null
		};
	}

	Pickup PickRandomPickup() {
		var value = Random.Range(0f, 1f);

		if(value < turret2Weight) {
			return Pickup.TURRET2;
		}

		if(value < turret1Weight) {
			return Pickup.TURRET1;
		}

		if(value < pistolWeight) {
			return Pickup.PISTOL;
		}

		if(value < sabreWeight) {
			return Pickup.SABRE;
		}

		if(value < knifeWeight) {
			return Pickup.KNIFE;
		}

		return Pickup.NONE;
	}
}