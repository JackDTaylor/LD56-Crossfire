using System.Collections.ObjectModel;
using UnityEngine;

public class AIStrategyRequest
{
	public AntController me;
	public Vector2 position;
	public Vector2 direction;

	public Collection<AntController> visibleAnts;
	public Collection<GunSpawner> visibleGuns;
	public Collection<Vector2>[] explorePoints;
}