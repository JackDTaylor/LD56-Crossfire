using System.Globalization;
using TMPro;
using UnityEngine;

public class TextSetter : MonoBehaviour {
	public TextMeshProUGUI text;

	void Start() {
	}

	void Update() {
	}

	public void SetTextFloat(float value) {
		text.text = value.ToString(CultureInfo.InvariantCulture);
	}
}