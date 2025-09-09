namespace Seekers;

public class SpawnerEntity : OwnedEntity
{
	//[Property] public string SpawnTime { get; set; }

	//[Property] public InputBind DetonateBind { get; set; }

	//[Property] public bool RemoveOnExplode { get; set; } = true;

	//private PropHelper Prop { get; set; }

	//protected override void OnStart()
	//{
	//	base.OnStart();

	//	Prop = GetComponent<PropHelper>();
	//}

	//public static TimeUntil nextParticle;
	//public static int explosionCount;

	//public override void OwnerUpdate()
	//{
	//	if ( DetonateBind.Pressed() )
	//	{
	//		string particle = "weapons/common/effects/medium_explosion.prefab";

	//		explosionCount++;

	//		if ( explosionCount > 5 )
	//			particle = null;

	//		if ( nextParticle < 0 )
	//		{
	//			nextParticle = 0.2f;
	//			explosionCount = 0;
	//		}

	//		if ( RemoveOnExplode )
	//		{
	//			Prop.Explosion( "he_grenade_explode", WorldPosition, Radius, Damage, 1, particle );
	//			GameObject.Destroy();
	//		}
	//		else
	//		{
	//			Prop.Explosion( "he_grenade_explode", WorldPosition, Radius, Damage, 1, particle );
	//		}
	//	}
	//}
}

