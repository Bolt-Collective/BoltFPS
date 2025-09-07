namespace Seekers;

public class DynamiteEntity : OwnedEntity
{
	[Property] public float Damage { get; set; } = 100;

	[Property] public float Radius { get; set; } = 5;

	[Property] public InputBind DetonateBind { get; set; }

	[Property] public bool RemoveOnExplode { get; set; } = true;

	private PropHelper Prop { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		Prop = GetComponent<PropHelper>();
	}

	public override void OwnerUpdate()
	{
		if ( DetonateBind.Down() )
		{
			if ( RemoveOnExplode )
			{
				Prop.Explosion( "he_grenade_explode", WorldPosition, Radius, Damage, 1 );
				GameObject.Destroy();
			}
			else
			{
				Prop.Explosion( "he_grenade_explode", WorldPosition, Radius, Damage, 1 );
			}
		}
	}
}
