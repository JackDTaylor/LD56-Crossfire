using UnityEngine;
using UnityEngine.Assertions;

public class Level : MonoBehaviour {
	public Transform groundContainer;
	public Transform entitiesContainer;
	public Transform spawnersContainer;
	public Transform pickupsContainer;
	public Transform projectilesContainer;

	void Start() {
		Assert.IsNotNull(groundContainer);
		Assert.IsNotNull(entitiesContainer);
		Assert.IsNotNull(spawnersContainer);
		Assert.IsNotNull(pickupsContainer);
		Assert.IsNotNull(projectilesContainer);
	}
}