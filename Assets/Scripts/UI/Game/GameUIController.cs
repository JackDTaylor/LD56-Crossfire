using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GameUIController : MonoBehaviour {
	public Transform deadOverlay;

	public RectTransform xpPanelBar;
	public TextMeshProUGUI xpLevelText;
	
	void Update() {
		var statsManager = PlayerStatsManager.Get();

		var maxWidth = ((RectTransform) xpPanelBar.parent).rect.width;
		var width = (float) statsManager.currentXp / statsManager.requiredXp * maxWidth;

		xpPanelBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

		xpLevelText.text = statsManager.currentLevel.ToString();
	}
}