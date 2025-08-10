namespace Seekers;

public partial class PhysGun
{
	GameObject beam;
	GameObject endNoHit;

	GameObject lastGrabbedObject;

	private LineRenderer line1 => beam.GetComponentsInChildren<LineRenderer>().FirstOrDefault( x => x.GameObject.Name == "Line1" );
	private LineRenderer line2 => beam.GetComponentsInChildren<LineRenderer>().FirstOrDefault( x => x.GameObject.Name == "Line2" );

	[Rpc.Broadcast]
	protected virtual void KillEffects()
	{
		if ( beam.IsValid() )
		{
			beam?.Destroy();
			beam = null;
		}

		if ( endNoHit.IsValid() )
		{
			endNoHit?.Destroy();
			endNoHit = null;
		}

		DisableHighlights( lastGrabbedObject );
		lastGrabbedObject = null;
	}

	private void DisableHighlights( GameObject gameObject )
	{
		if ( gameObject.IsValid() )
		{
			foreach ( var child in gameObject.Root.Children )
			{
				if ( !child.Components.Get<ModelRenderer>().IsValid() )
					continue;

				if ( child.Components.TryGet<HighlightOutline>( out var childglow ) )
				{
					childglow.Destroy();
				}
			}

			if ( gameObject.Root.Components.TryGet<HighlightOutline>( out var glow ) )
			{
				glow.Destroy();
			}
		}
	}

	Vector3 lastBeamPos;

	protected virtual void UpdateEffects()
	{
		if ( !Owner.IsValid() || !Beaming )
		{
			KillEffects();
			return;
		}

		if ( grabbed && !GrabbedObject.IsValid() )
		{
			DisableHighlights( lastGrabbedObject );
		}

		var startPos = Owner.AimRay.Position;
		var dir = Owner.AimRay.Forward;

		var tr = Scene.Trace.Ray( startPos, startPos + dir * MaxTargetDistance )
			.UseHitboxes()
			.IgnoreGameObject( Owner.GameObject )
			.WithAllTags( "solid" )
			.WithoutTags( "player", "movement" )
			.Run();

		beam ??= CreateBeam( tr.EndPosition );

		if ( beam.IsValid() )
		{
			beam.WorldPosition = Attachment( "muzzle" ).Position;
			beam.WorldRotation = Attachment( "muzzle" ).Rotation;
			line1.VectorPoints[0] = beam.WorldPosition;
			line2.VectorPoints[0] = beam.WorldPosition;
		}

		if ( GrabbedObject.IsValid() && !GrabbedObject.Tags.Contains( "world" ) && HeldBody.IsValid() )
		{
			var physGroup = HeldBody.PhysicsGroup;

			line1.VectorPoints[2] = HeldBody.Transform.PointToWorld( GrabbedPos );
			line2.VectorPoints[2] = HeldBody.Transform.PointToWorld( GrabbedPos );

			lastBeamPos = HeldBody.Position + HeldBody.Rotation * GrabbedPos;

			endNoHit?.Destroy();
			endNoHit = null;

			if ( GrabbedObject.Root.GetComponent<ModelRenderer>().IsValid() )
			{
				lastGrabbedObject = GrabbedObject;

				var glow = GrabbedObject.Root.GetOrAddComponent<HighlightOutline>();
				glow.Width = 0.25f;
				glow.Color = new Color( 4f, 50.0f, 70.0f, 1.0f );
				glow.ObscuredColor = new Color( 4f, 50.0f, 70.0f, 0.0005f );

				foreach ( var child in lastGrabbedObject.Root.Children )
				{
					if ( !child.GetComponent<ModelRenderer>().IsValid() )
						continue;

					glow = child.GetOrAddComponent<HighlightOutline>();
					glow.Color = new Color( 0.1f, 1.0f, 1.0f, 1.0f );
				}
			}
		}
		else
		{
			lastBeamPos = tr.EndPosition;

			Vector3.Lerp( lastBeamPos, tr.EndPosition, Time.Delta * 10 );

			if ( beam.IsValid() )
			{
				line1.VectorPoints[2] = lastBeamPos;
				line2.VectorPoints[2] = lastBeamPos;
			}

			endNoHit ??= Particles.MakeParticleSystem( noHitPrefab, new Transform( lastBeamPos ), 0 );
			endNoHit.WorldPosition = lastBeamPos;
		}

		var distance = line1.VectorPoints[0].Distance( line1.VectorPoints[2] );

		line1.VectorPoints[1] = beam.WorldPosition + beam.WorldTransform.Forward * distance * 0.5f;
		line2.VectorPoints[1] = beam.WorldPosition + beam.WorldTransform.Forward * distance * 0.5f;
	}

	private GameObject CreateBeam( Vector3 endPos ) =>
		Particles.MakeParticleSystem( beamPrefab, new Transform( endPos ), 0 );

	private void FreezeEffects() =>
		Particles.MakeParticleSystem( freezePrefab, new Transform( lastBeamPos ), 4 );

	private GameObject noHitPrefab => GameObject.GetPrefab( "particles/physgun_end_nohit.prefab" );
	private GameObject beamPrefab => GameObject.GetPrefab( "particles/physgun_beam.prefab" );
	private GameObject freezePrefab => null;

	protected override void OnDisabled()
	{
		base.OnDisabled();
		KillEffects();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		KillEffects();
	}

	void INetworkListener.OnDisconnected( Connection channel )
	{
		if ( channel == Owner.Network.Owner )
			KillEffects();
	}
}
