using Godot;
using System;
using System.Threading.Tasks;

public class Hud : Control
{
	private void OnSaveButtonPressed()
	{
		PersistenceManager.PersistGame(GetTree(), PersistenceManager.TestFileName, false);
	}	

	private async void OnLoadButtonPressed()
	{
		var persistedGame = await PersistenceManager.LoadPersistedGame(GetTree(), PersistenceManager.TestFileName, "res://Scenes/Main.tscn");
		var timeMissed = DateTime.UtcNow - persistedGame.SavedAtUtc;
		GD.Print($"Skipping {timeMissed}");
		TickManager.Instance.FastForward(timeMissed);
	}


	private void OnResetButtonPressed()
	{
		GetTree().ReloadCurrentScene();
	}
}
