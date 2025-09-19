namespace Seekers;

public class Dismemberment : Component
{
	[RequireComponent, Property] public HealthComponent HealthComponent { get; set; }
	[Property] public SkinnedModelRenderer Body { get; set; }

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
	}

	protected override void OnStart()
	{
		HealthComponent.OnDamaged += OnDamaged;
	}

	public void OnDamaged(DamageInfo damageInfo)
	{
		Log.Info( damageInfo.hitbox );
		if ( IsProxy )
			return;

		foreach(var dismemberable in Dismemberables)
		{
			if ( dismemberable.Health <= 0 )
				continue;

			if ( !dismemberable.HitboxNames.Contains( damageInfo.hitbox ) )
				continue;

			dismemberable.Health -= damageInfo.Damage;

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
	}
}
