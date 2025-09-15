using System.Security.Cryptography;

namespace Seekers;

public abstract class StatusEffect : Component
{
	[Property]
	public float Duration { get; set; }

	[Property]
	public float InitialDuration { get; set; }

	public Component Inflictor { get; set; }

	private HealthComponent _healthComponent;
	public HealthComponent HealthComponent
	{
		get
		{
			if ( !_healthComponent.IsValid() )
			{
				_healthComponent = GameObject.Root.GetComponent<HealthComponent>();
			}
			return _healthComponent;
		}
	}

	private PropHelper _propHelper;
	public PropHelper PropHelper
	{
		get
		{
			if ( !_propHelper.IsValid() )
			{
				_propHelper = GameObject.Root.GetComponent<PropHelper>();
			}
			return _propHelper;
		}
	}

	protected override void OnFixedUpdate()
	{
		Duration -= Time.Delta;
		if (Duration < 0)
		{
			Destroy();
			return;
		}

		if (!Networking.IsHost) 
			return;

		Apply();
	}

	public virtual void Apply() { }

	public static StatusEffect ApplyTo<T>( GameObject target, Component inflictor, float duration )
	{
		if ( !target.IsValid() )
			return null;

		if ( target.Tags.Contains( "map" ) )
			return null;

		if ( target.Tags.Contains( "movement" ) )
			return null;

		if ( target.Root.Components.TryGet<T>(out var effect) )
		{
			var statusEffect = effect as StatusEffect;
			if (statusEffect.IsValid())
			{
				var dur = MathF.Max(duration, statusEffect.Duration);
				statusEffect.InitialDuration = dur;
				statusEffect.Duration = dur;
				statusEffect.Inflictor = inflictor;
			}
			return statusEffect;
		}
		else
		{
			var typeDes = TypeLibrary.GetType<T>();
			var newEffect = target.Root.Components.Create(typeDes);

			var statusEffect = newEffect as StatusEffect;
			if ( statusEffect.IsValid() )
			{
				statusEffect.InitialDuration = duration;
				statusEffect.Duration = duration;
				statusEffect.Inflictor = inflictor;
			}
			return statusEffect;
		}
	}
}

public abstract class StatusTrigger : Component, Component.ITriggerListener
{

	[Property] public float RequiredDuration { get; set; }
	[Property] public bool SelfInflicting { get; set; }

	private Dictionary<GameObject, RealTimeSince> Targets = new();

	public virtual void Apply( GameObject target )
	{

	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
			return;

		foreach ( var target in Targets )
		{
			if ( !AddRequirement(target) )
				continue;

			Apply( target.Key );
		}
	}

	public virtual bool AddRequirement(KeyValuePair<GameObject, RealTimeSince> target)
	{
		if ( target.Value < RequiredDuration )
			return false;

		return true;
	}

	void ITriggerListener.OnTriggerEnter( GameObject other )
	{
		if ( other.Tags.Contains( "map" ) )
			return;

		if ( other.Tags.Contains( "movement" ) )
			return;

		if ( other.Root == GameObject.Root && !SelfInflicting )
			return;

		if ( Targets.ContainsKey( other ) )
			return;

		Targets.Add( other, 0 );
	}

	void ITriggerListener.OnTriggerExit( GameObject other )
	{
		if ( !Targets.ContainsKey( other ) )
			return;

		Targets.Remove( other );
	}
}
