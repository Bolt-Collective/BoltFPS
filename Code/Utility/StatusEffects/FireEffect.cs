using Sandbox;
using System.Security.Cryptography;
using static Sandbox.PhysicsContact;

namespace Seekers;

public class FireEffect : StatusEffect, Component.ICollisionListener
{

	[Property]
	public float Damage { get; set; }

	Vector3 lastPos;
	public override void Apply()
	{
		var vel = (WorldPosition - lastPos) / Time.Delta;
		lastPos = WorldPosition;

		Duration -= vel.Length * Time.Delta * 0.05f;

		Visuals();

		var fireRes = 0f;

		foreach ( var tag in GameObject.Root.Tags )
		{
			if ( !tag.StartsWith( "fireres-" ) )
				continue;

			var split = tag.Split( "-" );

			if ( split.Count() < 1 )
				continue;

			var stringValue = tag.Split( "-" )[1];

			float.TryParse( stringValue, out fireRes );

			break;
		}

		if ( HealthComponent.IsValid() )
			HealthComponent.TakeDamage( Inflictor, Damage * Time.Delta * (1 - fireRes) );
		else if ( PropHelper.IsValid() )
			PropHelper.Damage( Damage * Time.Delta );
	}

	[Rpc.Host]
	public static void ApplyFireTo( GameObject target, Component inflictor, float duration, float damage )
	{
		var effect = ApplyTo<FireEffect>( target, inflictor, duration ) as FireEffect;

		if ( !effect.IsValid() )
			return;
		effect.Damage = damage;
	}

	SkinnedModelRenderer SkinnedModelRenderer;
	ModelRenderer ModelRenderer;

	TimeUntil nextFire;

	static GameObject FireParticle => GameObject.GetPrefab( "particles/fire/fire.prefab" );

	float nextDuration => InitialDuration * 0.5f;

	public void Visuals()
	{
		if ( !SkinnedModelRenderer.IsValid() && GameObject.Root.Components.TryGet<SkinnedModelRenderer>( out var skinnedMeshRenderer, FindMode.EnabledInSelfAndChildren ) )
		{
			SkinnedModelRenderer = skinnedMeshRenderer;
		}

		if ( !ModelRenderer.IsValid() && GameObject.Root.Components.TryGet<ModelRenderer>( out var modelRenderer, FindMode.EnabledInSelfAndChildren ) )
		{
			ModelRenderer = modelRenderer;
		}

		if ( nextFire > 0 )
			return;

		nextFire = 1.5f;

		if ( SkinnedModelRenderer.IsValid() )
			SkinnedVisuals( SkinnedModelRenderer );
		else if ( ModelRenderer.IsValid() )
			ModelVisuals( ModelRenderer );
	}

	public void SkinnedVisuals( SkinnedModelRenderer skinnedModelRenderer )
	{
		if ( !skinnedModelRenderer.IsValid() )
			return;

		var scale = MathF.Pow( skinnedModelRenderer.LocalBounds.Size.Length, 1f / 3f ) * 0.2f;

		var pelvis = skinnedModelRenderer.GetBoneObject( "pelvis" );
		if ( pelvis.IsValid() )
		{
			AddFireParticle( pelvis.WorldPosition, pelvis, scale * 2, nextDuration, Damage );
		}

		var target = skinnedModelRenderer.GetBoneObject( Game.Random.Next( skinnedModelRenderer.GetBoneVelocities().Count() ) );

		AddFireParticle( target.WorldPosition, target, scale, nextDuration, Damage );
	}

	[Rpc.Broadcast]
	public static void AddFireParticle( Vector3 pos, GameObject target, float scale, float duration, float damage )
	{
		if ( !target.IsValid() )
			return;

		var particle = Particles.CreateParticleSystem( FireParticle, new Transform( pos ), 5, target );
		particle.WorldScale = Vector3.One * scale;

		if ( particle.Components.TryGet<FireTrigger>( out var trigger ) )
		{
			trigger.Duration = duration;
			trigger.Damage = damage;
		}
	}

	public void ModelVisuals( ModelRenderer modelRenderer )
	{
		if ( !modelRenderer.IsValid() )
			return;

		var scale = MathF.Pow( modelRenderer.LocalBounds.Size.Length, 1f / 3f ) * 0.2f;

		var point = RandomPointOnFace( modelRenderer.LocalBounds );

		AddFireParticle( modelRenderer.WorldTransform.PointToWorld( point ), modelRenderer.GameObject, scale, nextDuration, Damage );
	}

	public static Vector3 RandomPointOnFace( BBox bbox )
	{
		var size = bbox.Size;
		var min = bbox.Mins;
		var max = bbox.Maxs;

		int face = Game.Random.Int( 0, 5 );

		switch ( face )
		{
			case 0: return new Vector3( min.x, Game.Random.Float( min.y, max.y ), Game.Random.Float( min.z, max.z ) );

			case 1: return new Vector3( max.x, Game.Random.Float( min.y, max.y ), Game.Random.Float( min.z, max.z ) );

			case 2: return new Vector3( Game.Random.Float( min.x, max.x ), min.y, Game.Random.Float( min.z, max.z ) );

			case 3: return new Vector3( Game.Random.Float( min.x, max.x ), max.y, Game.Random.Float( min.z, max.z ) );

			case 4: return new Vector3( Game.Random.Float( min.x, max.x ), Game.Random.Float( min.y, max.y ), min.z );

			case 5: return new Vector3( Game.Random.Float( min.x, max.x ), Game.Random.Float( min.y, max.y ), max.z );

			default: return bbox.RandomPointOnEdge;
		}
	}

}

public class FireTrigger : StatusTrigger
{
	[Property] public float Damage { get; set; } = 5;

	[Property] public float Duration { get; set; } = 5;

	public override void Apply( GameObject target )
	{
		FireEffect.ApplyFireTo( target, this, Duration, Damage );
	}

	public override bool AddRequirement( KeyValuePair<GameObject, RealTimeSince> target )
	{
		var requiredDuration = RequiredDuration;
		if ( target.Key.Components.TryGet<PropHelper>( out var ph ) )
		{
			var tagMaterial = "";

			foreach ( var tag in target.Key.Tags )
			{
				if ( tag.StartsWith( "m-" ) || tag.StartsWith( "m_" ) )
				{
					tagMaterial = tag.Remove( 0, 2 );
					break;
				}
			}

			SurfaceImpacts surface = null;
			try
			{
				surface = tagMaterial == ""
					? ph.Surface.ReplaceSurface() as SurfaceImpacts
					: (Surface.FindByName( tagMaterial ) ?? ph.Surface.ReplaceSurface()) as SurfaceImpacts;
			}
			catch
			{
				surface = ph.Surface as SurfaceImpacts;
			}

			if ( surface.IsValid() )
			{
				requiredDuration /= surface.Flammability;
			}
		}

		if ( target.Value < requiredDuration )
			return false;

		return true;
	}
}
