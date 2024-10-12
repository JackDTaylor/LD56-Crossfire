using UnityEngine;

public class RangeWeapon : MonoBehaviour {
	public float fireRate = 1f;
	public float spread = 2.5f;
	public int damage;
	
	public float maxPassThroughLevel = 33f;
	public float passThroughFalloff = 3f;

	public bool applyAngleConstraints;
	public Vector2 angleConstraints;

	public Transform rotateTransform;

	public AntController owner;

	public Transform[] barrels;

	float fireCooldown;

	Camera playerCamera;

	void Start() {
		playerCamera = Camera.main;
	}

	void Update() {
		fireCooldown -= Time.deltaTime;

		if(owner.isPlayerControlled) {
			UpdatePlayerControls();
		} else {
			UpdateAIControls();
		}
	}

	void UpdatePlayerControls() {
		var target = playerCamera.ScreenToWorldPoint(Input.mousePosition);

		RotateTo(target);

		if(Input.GetMouseButton(0)) {
			Fire();
		}
	}

	void UpdateAIControls() {
		var closestAnt = owner.GetRangedWeaponTarget(this);

		if(closestAnt == null) {
			return;
		}

		RotateTo(closestAnt.transform.position);
		Fire();
	}

	void RotateTo(Vector3 target) {
		Vector3 direction = target - rotateTransform.position;
		direction.z = 0;

		float angle = Mathf.Atan2(direction.y,  direction.x) * Mathf.Rad2Deg - 90f;

		if(applyAngleConstraints) {
			Vector3 playerLookDirection = owner.transform.right;
			float playerLookAngle = Mathf.Atan2(playerLookDirection.y, playerLookDirection.x) * Mathf.Rad2Deg;

			float deltaAngle = Mathf.DeltaAngle(playerLookAngle, angle);
			float clampedAngle = Mathf.Clamp(deltaAngle, angleConstraints.x, angleConstraints.y);
			angle = playerLookAngle + clampedAngle;
		}

		rotateTransform.rotation = Quaternion.Euler(0, 0, angle);
	}

	void Fire() {
		if(fireCooldown > 0) {
			return;
		}

		foreach(var barrel in barrels) {
			var euler = rotateTransform.rotation.eulerAngles;

			euler.z += Random.Range(-spread, +spread);

			ProjectileManager.Get().SpawnBullet(barrel.transform.position, euler, this, owner);
		}

		fireCooldown = fireRate;
	}
}