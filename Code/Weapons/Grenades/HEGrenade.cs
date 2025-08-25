namespace Seekers;

[Title( "HE Grenade" )]
public partial class HEGrenade : BaseGrenade
{
	[Property] public float DamageRadius { get; set; } = 512f;
	[Property] public float MaxDamage { get; set; } = 100f;
	[Property] public float ScreenShakeIntensity { get; set; } = 3f;
	[Property] public float ScreenShakeLifeTime { get; set; } = 1.5f;

	[Property]
	public Curve DamageFalloff { get; set; } =
		new Curve( new Curve.Frame( 1.0f, 1.0f ), new Curve.Frame( 0.0f, 0.0f ) );

	protected override void Explode()
	{
		if ( Networking.IsHost )
			Explosion.AtPoint( WorldPosition, DamageRadius, MaxDamage, Player, true, this, DamageFalloff );

		var viewer = Client.Local?.GetPawn<Pawn>();
		var screenShaker = viewer?.Controller?.ScreenShaker;

		Sound.Play( "he_grenade_explode", WorldPosition );

		if ( screenShaker.IsValid() && viewer.IsValid() )
		{
			var distance = viewer.GameObject.WorldPosition.Distance( WorldPosition );
			var falloff = DamageFalloff;

			if ( falloff.Frames.Count() == 0 )
			{
				falloff = new(new Curve.Frame( 1f, 1f ), new Curve.Frame( 0f, 0f ));
			}

			var radiusWithPadding = DamageRadius * 1.2f;

			if ( distance <= radiusWithPadding )
			{
				var scalar = falloff.Evaluate( distance / radiusWithPadding );
				var shake = new ScreenShake.Random( ScreenShakeIntensity * scalar, ScreenShakeLifeTime );
				screenShaker.Add( shake );
			}
		}

		base.Explode();
	}
}
