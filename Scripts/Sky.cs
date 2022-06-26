using Godot;
using System;

public class Sky : Spatial
{
	// 0 == midnight. 0.5 == noon. 1 == midnight again
	[Export] public float TimeOfDay
	{
		get
		{
			return _timeOfDay;
		}
		set
		{
			if (value != _timeOfDay)
			{
				_timeOfDay = value;
				UpdateEnvironment();
			}
		}
	}
	private float _timeOfDay = 0.5f;

	// (Longitude = left/right)
	[Export] public float SunLongitude
	{
		get { return _sunLongitude; }
		set
		{
			_sunLongitude = value;
			if (sky != null) sky.SunLongitude = value;
			if (directionalLight != null) directionalLight.RotationDegrees = new Vector3(directionalLight.RotationDegrees.x, value, directionalLight.RotationDegrees.z);
		}
	}
	private float _sunLongitude = 0;
	[Export] public Curve SkyBrightnessCurve;
	[Export] public float SkyBrightnessMultiplier = 1; // Because stupid godot curves always are 0 to 1 no matter what the max and min values are set to, we need to manually multiply
	[Export] public Curve GroundBrightnessCurve;
	[Export] public float GroundBrightnessMultiplier = 1;
	[Export] public Curve AmbientLightCurve;
	[Export] public float AmbientLightMultiplier = 1;
	[Export] public Curve DirectionalLightEnergyCurve;
	[Export] public float DirectionalLightEnergyMultiplier = 0.812f;

	private DirectionalLight directionalLight;
	private Godot.Environment environment;
	private ProceduralSky sky;

	public override void _Ready()
	{
		environment = GetNode<WorldEnvironment>("WorldEnvironment").Environment;
		sky = environment.BackgroundSky as ProceduralSky;
		directionalLight = GetNode<DirectionalLight>("DirectionalLight");
		sky.SunLongitude = SunLongitude;
		UpdateEnvironment();
	}

	private void UpdateEnvironment()
	{
		var sunLatitude = Utils.MapNumber(_timeOfDay, 0, 1, -90, 270);
		if (sunLatitude > 180) sunLatitude -= 360;

		var lightRotation = sunLatitude + 180;
		if (lightRotation > 180) lightRotation -= 360;
		if (sky != null)
		{
			sky.SunLatitude = sunLatitude;
			sky.SkyEnergy = SkyBrightnessCurve.Interpolate(_timeOfDay) * SkyBrightnessMultiplier;
			sky.GroundEnergy = GroundBrightnessCurve.Interpolate(_timeOfDay) * GroundBrightnessMultiplier;
			environment.AmbientLightEnergy = GroundBrightnessCurve.Interpolate(_timeOfDay) * AmbientLightMultiplier;
		}
		if (directionalLight != null)
		{
			directionalLight.RotationDegrees = new Vector3(lightRotation, directionalLight.RotationDegrees.y, directionalLight.RotationDegrees.z);
			directionalLight.LightEnergy = DirectionalLightEnergyCurve.Interpolate(_timeOfDay) * DirectionalLightEnergyMultiplier;
			directionalLight.LightIndirectEnergy = DirectionalLightEnergyCurve.Interpolate(_timeOfDay);
		}
	}
}
