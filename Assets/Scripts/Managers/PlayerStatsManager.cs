using TMPro;
using UnityEngine;

public class PlayerStatsManager : MonoBehaviour {
	public static PlayerStatsManager Get() => Manager.Get<PlayerStatsManager>();
	void Awake() => Manager.Register(this);

	public int currentXp = 0;
	public int currentLevel = 1;
	public int requiredXp = 0;

	public GameObject xpOrbPrefab;
	public Camera playerCamera;

	[ReadOnly]
	public AntController player; 

	void Start() {
		requiredXp = GetLevelXp(currentLevel);
	}

	AntController GetPlayer() {
		return GetComponent<GameManager>().GetPlayer();
	}

	int GetLevelXp(int level) {
		// https://www.desmos.com/calculator/shoc0j7bos
		return Mathf.RoundToInt(10f * Mathf.Pow(level, 1.5f));
	}
	void UpdateLevelXp() {
		while(currentXp >= requiredXp) {
			currentXp -= requiredXp;
			currentLevel += 1;
		}

		requiredXp = GetLevelXp(currentLevel);
	}

	public void AddXp(int count) {
		currentXp += count;
		UpdateLevelXp();
	}

	public void SpawnXpOrb(Vector3 position, int xpAmount) {
		var orb = Instantiate(xpOrbPrefab, GameManager.Get().level.pickupsContainer);

		orb.transform.position = position;
		orb.GetComponent<XPOrb>().xpAmount = xpAmount;
	}

	void Update() {
		player = GetPlayer();

		if(Input.GetKeyDown(KeyCode.F8)) {
			AddXp(100);
		}

		UpdateLevelXp();

		if(player != null) {
			var targetSize = Mathf.Lerp(10f, 20f, Mathf.Clamp01(player.level / 200f));

			playerCamera.orthographicSize = Mathf.Lerp(playerCamera.orthographicSize, targetSize, Time.deltaTime);
		}
	}
}