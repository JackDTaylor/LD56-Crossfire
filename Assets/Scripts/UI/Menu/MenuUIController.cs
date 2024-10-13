using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MenuUIController : MonoBehaviour {
	public enum ScreenType { MENU, NEW_GAME, PAUSE }

	public RectTransform menuScreen;
	public RectTransform newGameScreen;
	public RectTransform pauseScreen;

	// public Toggle multiplayerToggle;

	// public RectTransform singleplayerConfigPanel;
	// public RectTransform multiplayerConfigPanel;

	public TMP_InputField playerNameInput;

	public Image pauseScreenshotImage;

	void Start() {
		ShowScreen(ScreenType.MENU);

		playerNameInput.text = $"Player{Random.Range(100000, 999999)}";
	}

	public void ShowScreen(ScreenType screen) {
		menuScreen.gameObject.SetActive(false);
		newGameScreen.gameObject.SetActive(false);
		pauseScreen.gameObject.SetActive(false);

		switch(screen) {
			case ScreenType.MENU:
				menuScreen.gameObject.SetActive(true);
				return;

			case ScreenType.NEW_GAME:
				newGameScreen.gameObject.SetActive(true);
				return;

			case ScreenType.PAUSE:
				pauseScreen.gameObject.SetActive(true);
				return;

			default:
				throw new ArgumentOutOfRangeException(nameof(screen), screen, null);
		}
	}

	public void ShowNewGameScreen() {
		ShowScreen(ScreenType.NEW_GAME);
	}

	void Update() {
		// multiplayerConfigPanel.gameObject.SetActive(multiplayerToggle.isOn);
		// singleplayerConfigPanel.gameObject.SetActive(!multiplayerToggle.isOn);
	}
}