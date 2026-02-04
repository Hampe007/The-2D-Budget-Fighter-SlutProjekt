using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Responsible for saving local match result for the current player after the fight ends.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class GameManager : NetworkBehaviour
{
	[Header("Match Outcome")]
	[SerializeField]
	private bool playerWon;
	[SerializeField]
	private int killsThisMatch;
	[SerializeField]
	private int deathsThisMatch;
	[SerializeField]
	private bool returnToMenuOnMatchEnd;
	[SerializeField]
	private bool rematchOnMatchEnd;

	public bool matchEnded;
	private bool hasSaved = false;

	private void Awake()
	{
		Debug.Log($"[GameManager] IsServer = {IsServer}, IsClient = {IsClient}, IsOwner = {IsOwner}");
	}

	/// <summary>
	/// Call this at the end of the match when the player dies or wins.
	/// </summary>
	public void FinalizeMatch(bool won, int kills, int deaths)
	{
		Debug.Log($"GameManagers FinalizeMatch() has been activated");

		Debug.Log($"[GameManager] IsServer = {IsServer}, IsClient = {IsClient}");

		if (matchEnded) return;
		matchEnded = true;

		var stats = SaveManager.LoadPlayerData();
		stats.RecordMatch(kills, deaths, won);
		SaveManager.SavePlayerData(stats);

		Debug.Log("[GameManager] Match stats saved:");
		Debug.Log(stats.ToString());

		if (IsServer && returnToMenuOnMatchEnd)
		{
			Debug.Log("[GameManager] RequestReturnToMenuClientRpc() is trying to be called inside FinalizeMatch()");
			if (rematchOnMatchEnd)
			{
				StartCoroutine(RematchRoutine());
			}
			else
			{
				StartCoroutine(ReturnToMenuRoutine());
			}
		}
		else if (IsServer)
		{
			Debug.Log("[GameManager] Match ended; staying in arena for capture.");
		}
	}

	/// <summary>
	/// Server -> ALL clients (including itself):
	/// shut networking down locally and reload the StartMenu scene.
	/// </summary>
	private IEnumerator ReturnToMenuRoutine()
	{
		Debug.Log("[GameManager] ReturnToMenuRoutine() is running");

		yield return new WaitForSeconds(0.25f); // let late RPCs finish

		// Clean up the transport on this machine
		if (NetworkManager.Singleton.IsListening)
			NetworkManager.Singleton.Shutdown();

		// Go back to the main menu locally and alone
		SceneManager.LoadScene("StartMenu", LoadSceneMode.Single);
	}
	private IEnumerator RematchRoutine()
	{
		Debug.Log("[GameManager] RematchRoutine() is running");

		yield return new WaitForSeconds(0.25f); // let late RPCs finish

		// Clean up the transport on this machine
		if (NetworkManager.Singleton.IsListening)
			NetworkManager.Singleton.Shutdown();

		// Reload the fight scene for an immediate rematch
		SceneManager.LoadScene("FightScene", LoadSceneMode.Single);
	}
}