using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour {
	public static PlayerManager Get() => ManagerProvider.Get<PlayerManager>();
	void Awake() => ManagerProvider.Register(this);

	public readonly List<Player> players = new List<Player>();
	
	public Transform playersContainer;
	public GameObject playerPrefab;
	
	public Player AddPlayer(Player.Type type, string playerName) {
		var obj = Instantiate(playerPrefab, playersContainer);
		var player = obj.GetComponent<Player>();
		
		player.type = type;
		player.playerName = playerName;
		
		obj.name = $"[{type}] {playerName}";
		
		players.Add(player);
		
		return player;
	}

	public void RemoveAllPlayers() {
		foreach(var player in players) {
			player.BeforeDestroy();
			
			player.transform.SetParent(null); // Become Batman
			Destroy(player.gameObject);
		}
	}
}