using Sandbox.ModelEditor.Nodes;
namespace Seekers;

/// <summary>
/// A component to help deal with props.
/// </summary>
public sealed class PropHelper : Component, Component.ICollisionListener
{
	[Property, Sync] public float Health { get; set; } = 1f;
	[Property, Sync] public Vector3 Velocity { get; set; } = 0f;
	[Property, Sync] public bool Invincible { get; set; } = false;

	[Sync] public Prop Prop { get; set; }
	[Sync] public ModelPhysics ModelPhysics { get; set; }
	[Sync] public Rigidbody Rigidbody { get; set; }

	public List<FixedJoint> Welds { get; set; } = [];
	public List<SpringJoint> Ropes { get; set; } = [];
	public List<(GameObject to, Vector3 toPoint, Vector3 frompoint)> RopePoints { get; set; } = [];
	public List<Joint> Joints { get; set; } = [];

	private Vector3 lastPosition = Vector3.Zero;

	protected override void OnStart()
	{
		Prop ??= GetComponent<Prop>();
		if ( !Prop.IsValid() ) return;
		Prop.OnPropBreak += OnBreak;

		ModelPhysics ??= GetComponent<ModelPhysics>();
		Rigidbody ??= GetComponent<Rigidbody>();

		Health = Prop?.Health ?? 0f;
		Velocity = 0f;

		lastPosition = Prop?.WorldPosition ?? WorldPosition;
	}

	[Rpc.Broadcast]
	public void Damage( float amount )
	{
		if ( IsProxy ) return;
		if ( !Prop.IsValid() ) return;
		if ( Health <= 0f ) return;

		Health -= amount;

		if ( Health <= 0f && !Invincible )
			Prop.Kill();
	}

	public void OnBreak()
	{
		var gibs = Prop.CreateGibs();

		if ( gibs.Count > 0 )
		{
			foreach ( var gib in gibs )
			{
				if ( !gib.IsValid() )
					continue;

				gib.Tint = Prop.Tint;
				gib.Tags.Add( "debris" );

				gib.AddComponent<PropHelper>();

				gib.GameObject.NetworkSpawn();
				gib.Network.SetOrphanedMode( NetworkOrphaned.Host );
			}
		}

		if ( Prop.Model.TryGetData<ModelExplosionBehavior>( out var data ) )
		{
			// Rest in peace data.Effect
			Explosion( "particles/medium_explosion.prefab", data.Sound, WorldPosition, data.Radius, data.Damage, data.Force );
		}

		Prop.Model = null; // Prevents prop from spawning more gibs.
	}

	public async void AddDamage( float damage )
	{
		await Task.DelaySeconds( 1f / ProjectSettings.Physics.FixedUpdateFrequency + 0.05f );

		Damage( damage );
	}

	[Rpc.Broadcast]
	public void BroadcastAddDamage( float damage )
	{
		AddDamage( damage );
	}

	protected override void OnFixedUpdate()
	{
		if ( Prop.IsValid() )
		{
			Velocity = (Prop.WorldPosition - lastPosition) / Time.Delta;
			lastPosition = Prop.WorldPosition;
		}
	}

	private ModelPropData GetModelPropData()
	{
		if ( Prop.Model.IsValid() && !Prop.Model.IsError && Prop.Model.TryGetData( out ModelPropData propData ) )
		{
			return propData;
		}

		ModelPropData defaultData = new()
		{
			Health = -1,
		};

		return defaultData;
	}

	public float Mass => GetMass();
	private float GetMass()
	{
		if ( Rigidbody.IsValid() )
			return Rigidbody.Mass;

		float mass = 0;
		foreach(var body in ModelPhysics.Bodies)
		{
			mass += body.Component.Mass;
		}

		return mass;
	}

	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		if ( IsProxy ) return;

		var propData = GetModelPropData();
		if ( propData == null ) return;

		var minImpactSpeed = 500;
		if ( minImpactSpeed <= 0.0f ) minImpactSpeed = 500;

		float impactDmg = Mass / 10;
		if ( impactDmg <= 0.0f ) impactDmg = 10;

		float speed = collision.Contact.Speed.Length;

		if ( speed > minImpactSpeed )
		{
			// I take damage from high speed impacts
			if ( Health > 0 )
			{
				var damage = speed / minImpactSpeed * impactDmg;
				Damage( damage );
			}

			var other = collision.Other;

			// Whatever I hit takes more damage
			if ( other.GameObject.IsValid() && other.GameObject != GameObject )
			{
				var damage = speed / minImpactSpeed * impactDmg * 1.2f;

				if ( other.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
				{
					propHelper.Damage( damage );
				}
				else if ( other.GameObject.Root.Components.TryGet<Pawn>( out var player ) )
				{
					player.TakeDamage( damage );
				}
			}
		}
	}

	public async void Explosion( string particle, string sound, Vector3 position, float radius, float damage, float forceScale )
	{
		await GameTask.Delay( Game.Random.Next( 50, 250 ) );

		BroadcastExplosion( sound, position );

		var particleGameObject = GameObject.GetPrefab( particle );

		Particles.CreateParticleSystem( particleGameObject, new Transform( position, Rotation.Identity ), 10 );

		// Damage, etc
		var overlaps = Game.ActiveScene.FindInPhysics( new Sphere( position, radius ) );

		foreach ( var obj in overlaps )
		{
			if ( !obj.Tags.Intersect( new TagSet() { "solid", "player", "npc", "glass" } ).Any() && obj.Tags.Intersect( new TagSet() { "playercontroller" } ).Any() )
			{
				continue;
			}

			// If the object isn't in line of sight, fuck it off
			var tr = Game.ActiveScene.Trace.Ray( position, obj.WorldPosition )
				.WithoutTags( new TagSet() { "solid", "player", "npc", "glass" }.Append( "solid" ).ToArray() )
				.Run();

			if ( tr.Hit && tr.GameObject.IsValid() )
			{
				if ( !obj.Root.IsDescendant( tr.GameObject ) )
					continue;
			}

			var distance = tr.Hit ? tr.Distance : obj.WorldPosition.Distance( position );
			var distanceMul = 1.0f - Math.Clamp( distance / radius, 0.0f, 1.0f );

			var dmg = damage * distanceMul;

			var force = (obj.WorldPosition - position).Normal * distanceMul * forceScale * 10000f;

			BaseWeapon.DoDamage( obj.Root, damage, force, obj.WorldPosition );
		}
	}

	[Rpc.Broadcast( NetFlags.Unreliable )]
	public static void BroadcastExplosion( string path, Vector3 position )
	{
		if ( string.IsNullOrEmpty( path ) )
		{
			Sound.Play( "rust_pumpshotgun.shootdouble", position );
			return;
		}

		if ( path.StartsWith( "sound/" ) || path.StartsWith( "sounds/" ) )
		{
			var soundEvent = ResourceLibrary.Get<SoundEvent>( path );
			if ( !soundEvent.IsValid() )
			{
				Sound.Play( "rust_pumpshotgun.shootdouble", position );
				return;
			}

			Sound.Play( soundEvent, position );
			return;
		}

		Sound.Play( path, position );
	}

	[Rpc.Broadcast]
	public void Weld( GameObject to )
	{
		if ( IsProxy )
			return;

		PropHelper propHelper = to.Components.Get<PropHelper>();

		var fixedJoint = Components.Create<FixedJoint>();
		fixedJoint.Body = to;
		fixedJoint.LinearDamping = 0;
		fixedJoint.LinearFrequency = 0;
		fixedJoint.AngularDamping = 0;
		fixedJoint.AngularFrequency = 0;

		Welds.Add( fixedJoint );
		Joints.Add( fixedJoint );
		propHelper?.Welds.Add( fixedJoint );
		propHelper?.Joints.Add( fixedJoint );
	}

	[Rpc.Broadcast]
	public void Unweld()
	{
		if ( IsProxy )
			return;

		foreach ( var weld in Welds )
		{
			weld?.Destroy();
		}

		Welds.RemoveAll( item => !item.IsValid() );
		Joints.RemoveAll( item => !item.IsValid() );
	}

	[Rpc.Broadcast]
	public void Rope( GameObject to, Vector3 fromPos, Vector3 toPos, float minLength = 0, float maxLength = 100, bool collision = true )
	{
		if ( IsProxy )
			return;

		PropHelper propHelper = to.Components.Get<PropHelper>();

		var point1Go = new GameObject();
		point1Go.SetParent( GameObject );
		point1Go.LocalPosition = fromPos;
		point1Go.LocalRotation = Rotation.Identity;

		var point2Go = new GameObject();
		if ( !to.Tags.Contains( "map" ) )
		{
			point2Go.SetParent( to );
		}
		else
		{
			point2Go.AddComponent<Rigidbody>().MotionEnabled = false;
		}
		point2Go.LocalPosition = toPos;
		point2Go.LocalRotation = Rotation.Identity;

		var springJoint = point1Go.Components.Create<SpringJoint>();
		springJoint.EnableCollision = collision;
		springJoint.Body = point2Go;
		springJoint.Attachment = Joint.AttachmentMode.LocalFrames;
		springJoint.MinLength = minLength;
		springJoint.MaxLength = maxLength;

		Ropes?.Add( springJoint );
		Joints?.Add( springJoint );
		propHelper?.Ropes?.Add( springJoint );
		propHelper?.Joints?.Add( springJoint );
	}

	[Rpc.Broadcast]
	public void SetRopePoints( List<GameObject> tos, List<Vector3> fromPoints, List<Vector3> toPoints )
	{
		RopePoints = [];

		for ( int i = 0; i < MathF.Min( tos.Count, fromPoints.Count ) && i < toPoints.Count; i++ )
		{
			RopePoints?.Add( (
				tos[i],
				fromPoints[i],
				toPoints[i]
			) );
		}
		;
	}

	[Rpc.Broadcast]
	public void Hinge( GameObject to, Vector3 position, Vector3 normal )
	{
		if ( IsProxy )
			return;

		if ( !to.IsValid() ) return;

		PropHelper propHelper = to.Components.Get<PropHelper>();

		var go = new GameObject
		{
			WorldPosition = position,
			WorldRotation = Rotation.LookAt( Rotation.LookAt( normal ).Up )
		};

		go.SetParent( to );

		var hingeJoint = go.Components.Create<HingeJoint>();
		hingeJoint.Body = GameObject;

		Joints?.Add( hingeJoint );

		propHelper?.Joints?.Add( hingeJoint );
	}
}
