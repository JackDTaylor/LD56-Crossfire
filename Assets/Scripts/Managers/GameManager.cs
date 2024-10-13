using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour {
	public static GameManager Get() => ManagerProvider.Get<GameManager>();
	void Awake() => ManagerProvider.Register(this);

	public float spawnInvincibilityTime = 3f;
	
	public int targetBotCount = 64;
	public float spawnInterval = 3f;

	public Level level;

	public GameObject[] levelPrefabs;

	public MenuUIController menuUIController;
	[FormerlySerializedAs("gameUiController")] public GameUIController gameUIController;
	
	public Transform playersContainer;

	private bool isPlayingInternal;

	public bool isPlaying {
		get => isPlayingInternal;
		set {
			isPlayingInternal = value;

			if(level) {
				level.gameObject.SetActive(value);
			}
			
			gameUIController.gameObject.SetActive(value);
			menuUIController.gameObject.SetActive(!value);
		}
	}

	float timeToNextSpawn;

	bool isPlayerDead;

	public void Start() {
		isPlaying = false;
	}

	#region # Game management functions
	public void LoadLevel(int levelIndex) {
		var newGameState = Instantiate(levelPrefabs[levelIndex], transform);

		level = newGameState.GetComponent<Level>();
	}

	public void UnloadLevel() {
		if(level == null) {
			return;
		}

		level.transform.SetParent(null); // Become batman
		Destroy(level.gameObject);
	}

	public void StartGame(int levelIndex) {
		LoadLevel(levelIndex);

		InitGame();
	}
	
	public void InitGame() {
		isPlaying = true;
		
		PlayerManager.Get().AddPlayer(Player.Type.HUMAN, menuUIController.playerNameInput.text);

		for(int i = 0; i < targetBotCount; i++) {
			PlayerManager.Get().AddPlayer(Player.Type.AI, $"Soulless Machine #{i}");
		}
		
		// TODO: Rework with players system
		// timeToNextSpawn = spawnInterval;
		//
		// RespawnPlayer();
		//
		// var spawners = GetShuffledSpawnersStack();
		// var botsSpawned = 0;
		//
		// while(botsSpawned++ < targetBotCount && spawners.Count > 0) {
		// 	spawners.Pop().Spawn();
		// }
	}

	public void ResumeGame() {
		isPlaying = true;
	}

	public async Task PauseGame() {
		// Disable it a bit earlier so it does not appear on pause screenshot
		gameUIController.gameObject.SetActive(false);

		await UniTask.WaitForEndOfFrame(this);
		TakePauseScreenshot();

		isPlaying = false;
		menuUIController.ShowScreen(MenuUIController.ScreenType.PAUSE);
	}

	public void EndGame() {
		isPlaying = false;
		UnloadLevel();
		
		PlayerManager.Get().RemoveAllPlayers();

		menuUIController.ShowScreen(MenuUIController.ScreenType.MENU);
	}

	public void QuitGame() {
		Application.Quit();
	}

	#endregion
	
	#region # Misc

	void TakePauseScreenshot() {
		Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture();

		Texture2D screenshot = new Texture2D(capture.width, capture.height, TextureFormat.RGB24, false);
		screenshot.SetPixels(capture.GetPixels());
		screenshot.Apply();

		Destroy(capture);

		Sprite screenshotSprite = Sprite.Create(screenshot, new Rect(0, 0, screenshot.width, screenshot.height), new Vector2(0.5f, 0.5f));

		menuUIController.pauseScreenshotImage.sprite = screenshotSprite;
	}

	public void SetTargetAntCountFloat(float value) {
		targetBotCount = Mathf.FloorToInt(value);
	}

	List<AntController> GetAllAnts() {
		return GetComponentsInChildren<AntController>().ToList();
	}

	#endregion

	#region # Player functions

	public AntController GetPlayerAnt() {
		return GetAllAnts().FirstOrDefault(ant => ant.isPlayerControlled);
	}

	public void RespawnPlayer(Player player) {
		var playerAnt = GetPlayerAnt();

		if(playerAnt != null) {
			playerAnt.player = null;
		}

		GetShuffledSpawnersStack().Pop().Spawn(player, PlayerStatsManager.Get().currentLevel);
	}

	#endregion

	void Update() {
		if(isPlaying == false) {
			return;
		}

		UpdateActiveGame();
	}
	
	Stack<AntSpawner> GetShuffledSpawnersStack() {
		var spawners = level.spawnersContainer.GetComponentsInChildren<AntSpawner>();

		return new Stack<AntSpawner>(spawners.OrderBy(x => Random.value));
	}

	void UpdateActiveGame() {
		if(Input.GetKeyDown(KeyCode.Escape)) {
			_ = PauseGame();
			return;
		}

		var player = GetPlayerAnt();

		isPlayerDead = player == null || player.isDead;

		gameUIController.deadOverlay.gameObject.SetActive(isPlayerDead);

		
		// TODO: Rework with players system
		// if(isPlayerDead && Input.GetKeyDown(KeyCode.Space)) {
		// 	RespawnPlayer();
		// 	timeToNextSpawn = spawnInterval;
		// }
		//
		// timeToNextSpawn -= Time.deltaTime;
		//
		// var botsAlive = GetAllAnts().Count(ant => !ant.isDead && !ant.isPlayerControlled);
		//
		// if(timeToNextSpawn < 0 && botsAlive < targetBotCount) {
		// 	var spawnersStack = GetShuffledSpawnersStack();
		// 	
		// 	var botsToSpawn = (int)Mathf.Clamp(targetBotCount - botsAlive, 0, spawnersStack.Count);
		// 	var botsSpawned = 0;
		// 	
		// 	while(spawnersStack.Count > 0 && botsSpawned++ < botsToSpawn) {
		// 		spawnersStack.Pop().Spawn();
		// 	}
		//
		// 	timeToNextSpawn = spawnInterval;
		// }
	}
}