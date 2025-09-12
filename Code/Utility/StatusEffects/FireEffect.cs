using Sandbox;
using System.Security.Cryptography;
using static Sandbox.PhysicsContact;

namespace Seekers;

public class FireEffect : StatusEffect, Component.ICollisionListener
{
	public Dictionary<GameObject, SpreadData> SpreadTargets { get; set; } = new();

	public struct SpreadData
	{
		public RealTimeSince TimeSinceSpreadable;
		public RealTimeSince TimeSinceCollided;
		public SpreadData (float timeSinceSpreadable = 0, float timeSinceCollided = 0)
		{
			TimeSinceSpreadable = timeSinceSpreadable;
			TimeSinceCollided = timeSinceCollided;
		}
	}

	//when collision update starts working, spread will work with still shit
	void ICollisionListener.OnCollisionUpdate( Collision collision )
	{
		AddSpread( collision );
	}

	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		AddSpread( collision );
	}

	public void AddSpread( Collision collision )
	{
		if ( SpreadTargets.ContainsKey( collision.Other.GameObject.Root ) )
		{
			var spreadData = SpreadTargets[collision.Other.GameObject.Root];
			SpreadTargets[collision.Other.GameObject.Root] = new SpreadData( spreadData.TimeSinceSpreadable, 0 );
		}
		else
		{
			SpreadTargets.Add( collision.Other.GameObject.Root, new SpreadData() );
		}
	}

	[Property]
	public float Damage { get; set; }

	Vector3 lastPos;
	public override void Apply()
	{
		Spread();

		var vel = (WorldPosition - lastPos) / Time.Delta;
		lastPos = WorldPosition;

		Duration -= vel.Length * Time.Delta * 0.05f;

		Visuals();

		var fireRes = 0f;

		foreach(var tag in GameObject.Root.Tags)
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
			HealthComponent.TakeDamage( Inflictor, Damage * Time.Delta * (1-fireRes) );
		else if ( PropHelper.IsValid() )
			PropHelper.Damage( Damage * Time.Delta );
	}

	public void Spread()
	{

		foreach ( var spreadable in new Dictionary<GameObject, SpreadData>( SpreadTargets ) )
		{
			Log.Info( spreadable );
			if ( spreadable.Value.TimeSinceCollided > 1 )
			{
				SpreadTargets.Remove( spreadable.Key );
				continue;
			}

			if ( spreadable.Value.TimeSinceSpreadable > 1 )
			{
				ApplyFireTo( spreadable.Key, this, InitialDuration, Damage );
			}
		}
	}

	public static void ApplyFireTo(GameObject target, Component inflictor, float duration, float damage)
	{
		var effect = ApplyTo<FireEffect>( target, inflictor, duration ) as FireEffect;

		if ( !effect.IsValid() )
			return;
		effect.Damage = damage;
	}

	SkinnedModelRenderer SkinnedModelRenderer;
	ModelRenderer ModelRenderer;

	TimeUntil nextFire;

	static GameObject FireParticle = GameObject.GetPrefab( "particles/fire/fire1s.prefab" );

	public void Visuals()
	{
		if ( !SkinnedModelRenderer.IsValid() && GameObject.Root.Components.TryGet<SkinnedModelRenderer>(out var skinnedMeshRenderer, FindMode.EnabledInSelfAndChildren) )
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
			SkinnedVisuals(SkinnedModelRenderer);
		else if ( ModelRenderer.IsValid() )
			ModelVisuals(ModelRenderer);
	}

	public void SkinnedVisuals(SkinnedModelRenderer skinnedModelRenderer)
	{
		if ( !skinnedModelRenderer.IsValid() )
			return;

		var scale = MathF.Pow( skinnedModelRenderer.LocalBounds.Size.Length, 1f / 3f ) * 0.2f;

		var pelvis = skinnedModelRenderer.GetBoneObject( "pelvis" );
		if (pelvis.IsValid())
		{
			AddFireParticle( pelvis.WorldPosition, pelvis, scale * 2 );
		}

		var target = skinnedModelRenderer.GetBoneObject( Game.Random.Next(skinnedModelRenderer.GetBoneVelocities().Count()) );

		AddFireParticle( target.WorldPosition, target, scale );
	}

	[Rpc.Broadcast]
	public static void AddFireParticle(Vector3 pos, GameObject target, float scale)
	{
		if ( !target.IsValid() )
			return;

		var particle = Particles.CreateParticleSystem( FireParticle, new Transform( pos ), 2, target );
		particle.WorldScale = Vector3.One * scale;
	}

	public void ModelVisuals(ModelRenderer modelRenderer)
	{
		if ( !modelRenderer.IsValid() )
			return;

		var scale = MathF.Pow( modelRenderer.LocalBounds.Size.Length, 1f / 3f ) * 0.2f;

		var point = modelRenderer.LocalBounds.RandomPointOnEdge;

		AddFireParticle( modelRenderer.WorldTransform.PointToWorld( point ), modelRenderer.GameObject, scale );
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
}
