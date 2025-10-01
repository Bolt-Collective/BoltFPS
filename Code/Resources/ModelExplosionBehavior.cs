#nullable disable
using System.Text.Json.Serialization;

namespace Sandbox.ModelEditor.Nodes;

/// <summary>
/// Defines the model as explosive. Support for this depends on the entity.
/// </summary>
[GameData( "explosion_behavior" )]
[Sphere( "explosive_radius", "" )]
[Description( "Defines the model as explosive. Support for this depends on the entity." )]
public class ModelExplosionBehavior
{
	/// <summary>Sound override for when the prop explodes.</summary>
	[JsonPropertyName( "explosion_custom_sound" )]
	[FGDType( "sound", "", "" )]
	[Description( "Sound override for when the prop explodes." )]
	public string Sound { get; set; }

	/// <summary>
	/// Amount of damage to do at the center on the explosion. It will falloff over distance.
	/// </summary>
	[JsonPropertyName( "explosive_damage" )]
	[DefaultValue( -1 )]
	[Description( "Amount of damage to do at the center on the explosion. It will falloff over distance." )]
	public float Damage { get; set; } = -1f;

	/// <summary>Range of explosion's damage.</summary>
	[JsonPropertyName( "explosive_radius" )]
	[DefaultValue( -1 )]
	[Description( "Range of explosion's damage." )]
	public float Radius { get; set; } = -1f;

	/// <summary>
	/// Scale of the force applied to entities damaged by the explosion and the models break pieces.
	/// </summary>
	[JsonPropertyName( "explosive_force" )]
	[Title( "Force Scale" )]
	[DefaultValue( -1 )]
	[Description( "Scale of the force applied to entities damaged by the explosion and the models break pieces." )]
	public float Force { get; set; } = -1f;
}
