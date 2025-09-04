namespace Seekers;

public static class Explosion
{
	[Rpc.Host]
	public static void AtPoint( Vector3 point, float radius, float baseDamage, Component attacker = null,
		bool external = true, Component inflictor = null, Curve falloff = default )
	{
		if ( falloff.Frames.Count() == 0 )
		{
			falloff = new Curve( new Curve.Frame( 1.0f, 1.0f ), new Curve.Frame( 0.0f, 0.0f ) );
		}

		var scene = Game.ActiveScene;
		if ( !scene.IsValid() ) return;

		var objectsInArea = scene.FindInPhysics( new Sphere( point, radius ) );
		var inflictorRoot = inflictor?.GameObject?.Root;

		var trace = scene.Trace
			.WithoutTags( "trigger", "ragdoll", "movement" );

		if ( inflictorRoot.IsValid() )
			trace = trace.IgnoreGameObjectHierarchy( inflictorRoot );

		foreach ( var obj in objectsInArea )
		{
			// If the object isn't in line of sight, fuck it off
			var tr = trace.Ray( point, obj.WorldPosition )
				.WithoutTags( new TagSet() { "solid", "player", "npc", "glass" }.Append( "solid" ).ToArray() )
				.Run();

			var distance = obj.WorldPosition.Distance( point );
			var direction = (obj.WorldPosition - point).Normal;

			var damage = baseDamage * falloff.Evaluate( distance / radius );

			var force = direction * damage * 2500f;

			if ( tr.Hit && tr.GameObject.IsValid() )
			{
				if ( !obj.Root.IsDescendant( tr.GameObject ) )
					continue;

				if ( tr.GameObject.Components.TryGet<Rigidbody>( out var prop ) )
				{
					prop.ApplyForce( force );
				}
			}

			BaseWeapon.DoDamage( obj.Root, damage, force, obj.WorldPosition );

			if ( obj.Root.GetComponentInChildren<HealthComponent>() is not { } hc )
				continue;

			hc.TakeDamage( attacker, damage, inflictor, point, force, flags: DamageFlags.Explosion,
				external: external );
		}
	}
}
