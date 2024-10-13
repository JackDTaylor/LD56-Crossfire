using UnityEngine;
using Random = UnityEngine.Random;

public class AntSpawner : MonoBehaviour {
	public GameObject antPrefab;
	public Transform spawnParent;

	public int botSpawnLevel = 1;

	public AIStrategy[] strategies;

	public bool autospawnEnabled;
	public float autospawnTimeout = 1f;

	float spawnTimeout;

	public AntController Spawn(Player player = null, int level = -1) {
		if(gameObject.activeInHierarchy == false && player == null) {
			return null;
		}

		var ant = Instantiate(antPrefab, spawnParent);

		ant.transform.position = transform.position;
		ant.transform.rotation = Quaternion.Euler(Vector3.forward * Random.Range(0f, 360f));

		var antController = ant.GetComponent<AntController>();

		if(player) {
			antController.player = player;
			ant.name = "Ant (Player)";
		} else {
			antController.aiStrategy = strategies[Random.Range(0, strategies.Length)];
			ant.name = "Ant (" + antController.aiStrategy.name + ")";
		}

		antController.level = level <= 0 ? botSpawnLevel : level;
		antController.Init();

		return antController;
	}

	void Update() {
		if(autospawnEnabled) {
			spawnTimeout -= Time.deltaTime;

			if(spawnTimeout < 0) {
				Spawn();
				spawnTimeout = autospawnTimeout;
			}
		}
	}
}