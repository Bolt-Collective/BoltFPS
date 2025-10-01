namespace Seekers;

public class Dismemberment : Component
{
	[RequireComponent, Property] public HealthComponent HealthComponent { get; set; }
	[Property] public SkinnedModelRenderer Body { get; set; }
	[Property] public GameObject Particle { get; set; }
	[Property] public SoundEvent DisSound { get; set; }

	[Property, InlineEditor] public List<Dismemberable> Dismemberables { get; set; }

	public Dismemberable GetDismemberable(string Name)
	{
		foreach(var dismemberable in Dismemberables)
		{
			if ( dismemberable.Name == Name )
				return dismemberable;
		}

		return null;
	}

	public class Dismemberable
	{
		public string Name { get; set; }
		public List<string> HitboxNames { get; set; }
		public GameObject Bone { get; set; }
		public string BodyGroup { get; set; }
		public int dismemberedValue { get; set; } = 1;
		public float Health { get; set; } = 100;
		public float HealthCost { get; set; } = 100;
		public bool invinsible { get; set; }

		[Hide] public Component LastAttaker;
	}

	protected override void OnStart()
	{
		HealthComponent.OnDamaged += OnDamaged;
	}

	protected override void OnFixedUpdate()
	{
		foreach(var dismemberable in Dismemberables)
		{
			if ( dismemberable.Health > 0 )
				continue;
			HealthComponent.TakeDamage( dismemberable?.LastAttaker ?? this, (dismemberable.HealthCost * HealthComponent.MaxHealth / 100) * Time.Delta );
		}
	}

	public void OnDamaged(DamageInfo damageInfo)
	{
		if ( IsProxy )
			return;

		foreach(var dismemberable in Dismemberables)
		{
			if ( dismemberable.invinsible )
				return;

			if ( dismemberable.Health <= 0 )
				continue;

			if ( !dismemberable.HitboxNames.Contains( damageInfo.hitbox ) )
				continue;

			dismemberable.Health -= damageInfo.Damage * 100 / HealthComponent.MaxHealth;

			dismemberable.LastAttaker = damageInfo.Attacker;

			if ( dismemberable.Health > 0 )
				continue;

			Dismember( dismemberable );
		}
	}

	[Rpc.Broadcast]
	public void Dismember(Dismemberable dismemberable)
	{
		if (Body.IsValid())
			Body.SetBodyGroup( dismemberable.BodyGroup, dismemberable.dismemberedValue );

		if (dismemberable.Bone.IsValid())
			dismemberable.Bone.Enabled = false;

		if ( DisSound.IsValid() )
			Sound.Play( DisSound, dismemberable.Bone.WorldPosition );

		if ( Particle.IsValid() )
			Particles.MakeParticleSystem( Particle, dismemberable.Bone.WorldTransform, 2 );
	}
}
