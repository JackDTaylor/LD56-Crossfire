using UnityEngine;

public class ProjectileManager : MonoBehaviour {
	public static ProjectileManager Get() => ManagerProvider.Get<ProjectileManager>();
	void Awake() => ManagerProvider.Register(this);
	
	public GameObject bulletPrefab;
	
	public GameObject bloodSprayPrefab;
	public GameObject bloodSlashPrefab;
	public GameObject bloodSplashPrefab;
	
	public float bulletIndexDelay = 0.1f;

	public void SpawnBullet(Vector2 position, Vector3 rotation, RangeWeapon weapon, AntController owner, int index, Transform ownerBarrel) {
		var obj = Instantiate(bulletPrefab, GameManager.Get().level.projectilesContainer);

		obj.transform.position = position;
		obj.transform.rotation = Quaternion.Euler(rotation);

		var bullet = obj.GetComponent<Bullet>();
		bullet.owner = owner;
		bullet.ownerBarrel = ownerBarrel;
		bullet.weapon = weapon;
		bullet.delay = index * bulletIndexDelay;
	}
	
	public void SpawnBloodSpray(Vector2 position, float normalAngle, float intensity, float scale) {
		SpawnBloodPrefab(bloodSprayPrefab, position, normalAngle, intensity, scale);
	}
	
	public void SpawnBloodSlash(Vector2 position, float normalAngle, float intensity, float scale) {
		SpawnBloodPrefab(bloodSlashPrefab, position, normalAngle, intensity, scale);
	}
	
	public void SpawnBloodSplash(Vector2 position, float normalAngle, float intensity, float scale) {
		SpawnBloodPrefab(bloodSlashPrefab, position, normalAngle, intensity, scale);
	}
	
	void SpawnBloodPrefab(GameObject prefab, Vector2 position, float normalAngle, float intensity, float scale) {
		var obj = Instantiate(prefab, GameManager.Get().level.projectilesContainer);
		
		obj.transform.position = (Vector3)position + Vector3.forward * prefab.transform.position.z;
		obj.transform.rotation = Quaternion.Euler(Vector3.forward * normalAngle);
		
		obj.GetComponent<Blood>().intensity = intensity;
		obj.GetComponent<Blood>().scale = scale;
	}
}