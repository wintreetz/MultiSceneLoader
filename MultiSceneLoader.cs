using System;
using System.Collections.Generic;
using Bolt;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MultiSceneLoader : Bolt.GlobalEventListener
{
	/// <summary>
	/// Stores the binding between the Action Button and the Scene to be loaded
	/// </summary>
	[Serializable]
	public struct LoadSceneBundle
	{
		public string SceneName;
		public GameObject LocationActions;
	}

	// List of scenes and Action Buttons
	[SerializeField] private LoadSceneBundle[] sceneBundles;

	// List of currently loaded scenes locally
	public static List<string> loadedScenes;

	void Start()
	{
		loadedScenes = new List<string>();

	}

	/// <summary>
	/// On Destroy remove all button callbacks
	/// </summary>
	private void OnDestroy()
	{

	}

	/// <summary>
	/// Loads the Scene locally and request the clients to do the same
	/// </summary>
	/// <param name="sceneName">Target Scene Name</param>
	public static void LoadScene(string sceneName, BoltConnection entityConnection)
	{
		if (BoltNetwork.IsServer)
		{
			var evt = LoadSceneRequest.Create(entityConnection);
			evt.SceneName = sceneName;
			evt.Load = true;
			evt.Send();
			
			if (!loadedScenes.Contains(sceneName))
			{
				Debug.Log("server loading scene");

				SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
				loadedScenes.Add(sceneName);
			}

			if (sceneName == "TreetzHUB")
			{
				BoltEntity entity = BoltNetwork.Instantiate(BoltPrefabs.SpacePlayer, new Vector3(2.75f,-.75f,0f), Quaternion.identity);
				entity.TakeControl();
				entity.AssignControl(entityConnection);
			}
		}
	}

	/// <summary>
	/// Unloads the Scene locally and request the clients to do the same
	/// </summary>
	/// <param name="sceneName">Target Scene Name</param>
	private void UnloadScene(string sceneName)
	{
		if (BoltNetwork.IsServer)
		{
			var evt = LoadSceneRequest.Create(Bolt.GlobalTargets.AllClients, Bolt.ReliabilityModes.ReliableOrdered);
			evt.SceneName = sceneName;
			evt.Load = false;
			evt.Send();

			SceneManager.UnloadSceneAsync(sceneName);
			loadedScenes.Remove(sceneName);
		}
	}

	/// <summary>
	/// Runs only the client side.
	/// The Server requests that a certain scene to be loaded, and the client replies with Response
	/// confirming the scene load.
	/// </summary>
	public override void OnEvent(LoadSceneRequest evnt)
	{
		if (BoltNetwork.IsClient)
		{
			if (evnt.Load)
			{
				Debug.Log("client attempting to load scene");

				SceneManager.LoadSceneAsync(evnt.SceneName, LoadSceneMode.Additive);
				loadedScenes.Add(evnt.SceneName);
			}
			else
			{
				SceneManager.UnloadSceneAsync(evnt.SceneName);
				loadedScenes.Remove(evnt.SceneName);
			}

			var evt = LoadSceneResponse.Create(Bolt.GlobalTargets.OnlyServer);
			evt.SceneName = evnt.SceneName;
			evt.Load = evnt.Load;
			evt.Send();
		}
	}

	/// <summary>
	/// Runs only on the Server, just so signal that a remote client has loaded scene
	/// </summary>
	public override void OnEvent(LoadSceneResponse evnt)
	{
		if (BoltNetwork.IsServer)
		{
			if (evnt.Load)
			{
				BoltLog.Warn("Connection {0} has loaded scene {1}", evnt.RaisedBy, evnt.SceneName);
			}
			else
			{
				BoltLog.Warn("Connection {0} has unloaded scene {1}", evnt.RaisedBy, evnt.SceneName);
			}
		}
	}

	/*public override void SceneLoadRemoteDone(BoltConnection connection, IProtocolToken token)
	{
		if (BoltNetwork.IsServer)
		{
			BoltLog.Warn("Remote Connection {0} has Loaded Scene", connection);

			foreach (var item in loadedScenes)
			{
				var evt = LoadSceneRequest.Create(connection, ReliabilityModes.ReliableOrdered);
				evt.SceneName = item;
				evt.Load = true;
				evt.Send();
			}
		}
	}*/
}
