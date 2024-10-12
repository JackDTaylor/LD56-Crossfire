using UnityEngine;

public class ProjectileManager : MonoBehaviour {
	public static ProjectileManager Get() => Manager.Get<ProjectileManager>();
	void Awake() => Manager.Register(this);
	
	public GameObject bulletPrefab;

	public void SpawnBullet(Vector2 position, Vector3 rotation, RangeWeapon weapon, AntController owner) {
		var obj = Instantiate(bulletPrefab, GameManager.Get().level.projectilesContainer);

		obj.transform.position = position;
		obj.transform.rotation = Quaternion.Euler(rotation);

		var bullet = obj.GetComponent<Bullet>();
		bullet.owner = owner;
		bullet.weapon = weapon;
	}
}