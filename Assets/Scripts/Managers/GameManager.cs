using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour {
	public static GameManager Get() => Manager.Get<GameManager>();
	void Awake() => Manager.Register(this);

	public int targetBotCount = 64;
	public float spawnInterval = 3f;

	public Level level;

	public GameObject[] levelPrefabs;

	public MenuUIController menuUIController;
	[FormerlySerializedAs("gameUiController")] public GameUIController gameUIController;

	private bool isPlayingInternal;

	public bool isPlaying {
		get => isPlayingInternal;
		set {
			isPlayingInternal = value;

			level.gameObject.SetActive(value);
			gameUIController.gameObject.SetActive(value);
			menuUIController.gameObject.SetActive(!value);
		}
	}

	float timeToNextSpawn;

	bool isPlayerDead;

	public void Start() {

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

		isPlaying = true;

		timeToNextSpawn = spawnInterval;

		RespawnPlayer();
		
		var spawners = GetShuffledSpawnersStack();
		var botsSpawned = 0;
		
		while(botsSpawned++ < targetBotCount && spawners.Count > 0) {
			spawners.Pop().Spawn();
		}
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

	public AntController GetPlayer() {
		return GetAllAnts().FirstOrDefault(ant => ant.isPlayerControlled);
	}

	public void RespawnPlayer() {
		var player = GetPlayer();

		if(player != null) {
			player.isPlayerControlled = false;
		}

		GetShuffledSpawnersStack().Pop().Spawn(true, GetComponent<PlayerStatsManager>().currentLevel);
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

		var player = GetPlayer();

		isPlayerDead = player == null || player.isDead;

		gameUIController.deadOverlay.gameObject.SetActive(isPlayerDead);

		if(isPlayerDead && Input.GetKeyDown(KeyCode.Space)) {
			RespawnPlayer();
			timeToNextSpawn = spawnInterval;
		}

		timeToNextSpawn -= Time.deltaTime;

		var botsAlive = GetAllAnts().Count(ant => !ant.isDead && !ant.isPlayerControlled);

		if(timeToNextSpawn < 0 && botsAlive < targetBotCount) {
			var spawnersStack = GetShuffledSpawnersStack();
			
			var botsToSpawn = (int)Mathf.Clamp(targetBotCount - botsAlive, 0, spawnersStack.Count);
			var botsSpawned = 0;
			
			while(spawnersStack.Count > 0 && botsSpawned++ < botsToSpawn) {
				spawnersStack.Pop().Spawn();
			}

			timeToNextSpawn = spawnInterval;
		}
	}
}