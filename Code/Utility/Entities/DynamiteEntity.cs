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

	public static TimeUntil nextParticle;
	public static int explosionCount;

	public override void OwnerUpdate()
	{
		if ( DetonateBind.Pressed() )
		{
			string particle = "weapons/common/effects/medium_explosion.prefab";

			explosionCount++;

			if ( explosionCount > 5)
				particle = null;

			if ( nextParticle < 0 )
			{
				nextParticle = 0.2f;
				explosionCount = 0;
			}

			if ( RemoveOnExplode )
			{
				Prop.Explosion( "he_grenade_explode", WorldPosition, Radius, Damage, 1, particle );
				GameObject.Destroy();
			}
			else
			{
				Prop.Explosion( "he_grenade_explode", WorldPosition, Radius, Damage, 1	, particle );
			}
		}
	}
}
