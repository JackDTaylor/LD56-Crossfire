using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(LayoutElement))]
public class ConditionalVisibility : MonoBehaviour {
	public float minWidth;
	public bool invertBehavior;

	RectTransform rectTransform;
	CanvasGroup canvasGroup;
	LayoutElement layoutElement;

	void Awake() {
		rectTransform = GetComponent<RectTransform>();
		canvasGroup = GetComponent<CanvasGroup>();
		layoutElement = GetComponent<LayoutElement>();

		UpdateVisibility();
	}

	void Update() {
		UpdateVisibility();
	}

	void UpdateVisibility() {
		if(rectTransform.rect.width < minWidth) {
			SetVisibility(invertBehavior);
		} else {
			SetVisibility(!invertBehavior);
		}
	}

	void SetVisibility(bool state) {
		canvasGroup.alpha = state ? 1.0f : 0.0f;
		layoutElement.ignoreLayout = !state;
	}
}