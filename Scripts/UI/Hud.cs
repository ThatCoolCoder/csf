using Godot;
using System;
using System.Threading.Tasks;

public class Hud : Control
{
	private void OnSaveButtonPressed()
	{
		PersistenceManager.PersistScene(GetTree(), PersistenceManager.TestFileName);
	}	

	private async void OnLoadButtonPressed()
	{
		await PersistenceManager.LoadPersistedScene(GetTree(), PersistenceManager.TestFileName, "res://Scenes/Main.tscn");
	}
}
