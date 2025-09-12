namespace Seekers;

public abstract class StatusEffect : Component
{
	[Property]
	public TimeUntil Duration { get; set; }

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

		if ( target.Root.Components.TryGet<T>(out var effect) )
		{
			var statusEffect = effect as StatusEffect;
			if (statusEffect.IsValid())
			{
				statusEffect.InitialDuration = duration;
				statusEffect.Duration = duration;
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

		return null;
	}
}

public abstract class StatusTrigger : Component, Component.ITriggerListener
{

	[Property] public float RequiredDuration { get; set; }

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
			if ( target.Value < RequiredDuration )
				continue;

			Apply( target.Key );
		}
	}

	void ITriggerListener.OnTriggerEnter( GameObject other )
	{
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
