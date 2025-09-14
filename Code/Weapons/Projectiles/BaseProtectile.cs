using Sandbox.Services;

namespace Seekers;

public abstract class BaseProjectile : Component
{
	[Property]
	public GameObject Origin { get; set; }

	[Property]
	public Vector3 OriginLocalPos { get; set; }

	[Property] public float Duration { get; set; } = 5;
	[Property] public float Damage { get; set; } = 10;

	[Property] public Vector3 Speed { get; set; }

	public TimeUntil TimeUntilDestroy { get; set; }
	protected override void OnStart()
	{
		TimeUntilDestroy = Duration;
		prevPos = WorldPosition;
	}

	public Vector3 prevPos;
	protected override void OnFixedUpdate()
	{
		if ( TimeUntilDestroy < 0 )
		{
			GameObject.Destroy();
			return;
		}

		if ( IsProxy )
			return;

		Update();

		prevPos = WorldPosition;
	}

	public virtual void Update()
	{

	}

	public SceneTraceResult Trace(float radius)
	{
		var trace = Game.ActiveScene.Trace.Ray( prevPos, WorldPosition )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "npc", "glass" )
			.WithoutTags( "playercontroller", "debris", "movement", "ignorebullets" )
			.IgnoreGameObjectHierarchy( Origin.Root )
			.Size( radius );

		return trace.Run();
	}
}
