using Sandbox.Audio;
using Sandbox.Rendering;

namespace Seekers;

public enum CrosshairType
{
	/// <summary>
	/// Traditional crosshair, 4 lines, 1 dot. Configurable.
	/// </summary>
	Default,

	/// <summary>
	/// Three lines - the default without the top line.
	/// </summary>
	ThreeLines,

	/// <summary>
	/// An arc and a dot
	/// </summary>
	Shotgun,

	/// <summary>
	/// Just the dot, even if it's disabled
	/// </summary>
	Dot,
	None
}

[Flags]
public enum HitmarkerType
{
	Default = 1,
	Kill = 2,
	Headshot = 4
}

public partial class Crosshair : Component
{
	public static Crosshair Instance { get; set; }

	float mainAlpha = 1f;
	float linesAlpha = 1f;

	public float CrosshairGap
	{
		get
		{
			return GameSettingsSystem.Current.CrosshairDistance;
		}
	}

	public bool UseCrosshairDot
	{
		get
		{
			return GameSettingsSystem.Current.ShowCrosshairDot;
		}
	}

	public Color CrosshairColor
	{
		get
		{
			return GameSettingsSystem.Current.CrosshairColor;
		}
	}

	public bool UseDynamicCrosshair
	{
		get
		{
			return GameSettingsSystem.Current.DynamicCrosshair;
		}
	}

	public float CrosshairLength
	{
		get
		{
			return GameSettingsSystem.Current.CrosshairLength;
		}
	}

	public float CrosshairWidth
	{
		get
		{
			return GameSettingsSystem.Current.CrosshairWidth;
		}
	}

	public HitmarkerType Hitmarker { get; set; }
	public TimeSince TimeSinceAttacked { get; set; } = 1000;

	private float HitmarkerTime => Hitmarker.HasFlag( HitmarkerType.Kill ) ? 0.5f : 0.2f;

	protected override void OnStart()
	{
		Instance = this;
	}

	protected override void OnUpdate()
	{
		var center = Screen.Size * 0.5f;

		var player = Pawn.Local;

		if ( !player.IsValid() )
			return;

		if ( !player.Controller.IsValid() )
			return;

		float alphaTarget = 1f;
		float linesTarget = 1f;

		var hud = player.Controller.Camera.Hud;
		var scale = Screen.Height / 1080.0f;

		var gap = CrosshairGap * scale * 0.5f;
		var len = CrosshairLength * scale * 0.5f;
		var w = CrosshairWidth * scale;

		Color color = CrosshairColor;
		CrosshairType type = CrosshairType.Default;
		bool hasAmmo = true;
		bool lowAmmo = false;

		if ( player.IsValid() && player.Controller.Camera.IsValid() )
		{
			var equipment = player.Inventory?.ActiveWeapon;

			if ( equipment.IsValid() )
			{
				type = equipment.CrosshairType;
				if ( equipment.CrosshairType == CrosshairType.None ) return;

				float spread = equipment.Spread + equipment.SpreadIncrease;

				gap += CalculateCrosshairWidth( spread,
					Screen.CreateVerticalFieldOfView( player.Controller.Camera.FieldOfView ), 720 ) * 175f * scale;

				hasAmmo = equipment.Ammo > 0;
				lowAmmo = equipment.Ammo <= equipment.MaxAmmo / 4;

				if ( equipment.IsReloading )
					linesTarget = 0.25f;
			}
			else
			{
				type = CrosshairType.Dot;
			}


			//if ( UseDynamicCrosshair )
			//	gap += player.Spread * 150f * scale;
		}

		hud.SetBlendMode( BlendMode.Lighten );

		color = color.WithAlpha( mainAlpha );

		var linesCol = color;
		if ( !hasAmmo )
		{
			linesCol = Color.Red;
			linesTarget *= 0.25f;
		}
		else if ( lowAmmo )
		{
			linesCol = Color.Orange;
		}

		if ( player.IsValid() && player.Controller.IsSprinting )
		{
			linesTarget = 0;
		}

		linesCol = linesCol.WithAlpha( linesAlpha );

		if ( type == CrosshairType.Default || type == CrosshairType.ThreeLines )
		{
			hud.DrawLine( center + Vector2.Left * (len + gap), center + Vector2.Left * gap, w, linesCol );
			hud.DrawLine( center - Vector2.Left * (len + gap), center - Vector2.Left * gap, w, linesCol );
			hud.DrawLine( center + Vector2.Up * (len + gap), center + Vector2.Up * gap, w, linesCol );

			if ( type != CrosshairType.ThreeLines )
			{
				hud.DrawLine( center - Vector2.Up * (len + gap), center - Vector2.Up * gap, w, linesCol );
			}

			if ( UseCrosshairDot )
			{
				hud.DrawCircle( center, w, color );
			}
		}

		if ( type == CrosshairType.Shotgun )
		{
			float scaleFactor = (1.0f - (TimeSinceAttacked / HitmarkerTime)).Clamp( 0.0f, 1.0f );

			var size = 0f;
			size += gap;

			hud.DrawCircle( center, w, color );
			hud.DrawRect( new Rect( center - ((size / 2f)), size ), Color.Transparent,
				new(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue), new(w, w, w, w), linesCol );
		}

		if ( type == CrosshairType.Dot )
		{
			hud.DrawCircle( center, w, color );
		}

		if ( TimeSinceAttacked < HitmarkerTime )
		{
			// Check if the hitmarker type includes a Kill, which keeps the lines from scaling down
			bool isKillHitmarker = Hitmarker.HasFlag( HitmarkerType.Kill );
			bool isHeadshotHitmarker = Hitmarker.HasFlag( HitmarkerType.Headshot ); // Check for Headshot flag

			float initialHitmarkerLength = 10f * scale; // Starting length of the hitmarker lines
			float initialDiagonalOffset = CrosshairGap * scale / 4; // Initial offset distance from the center
			float translationFactor = isKillHitmarker ? 1.0f : 1.0f - (TimeSinceAttacked / HitmarkerTime);
			translationFactor = translationFactor.Clamp( 0.0f, 1.0f );

			float opacityFactor =
				isKillHitmarker ? 1.0f : (1.0f - (TimeSinceAttacked / HitmarkerTime)).Clamp( 0.0f, 1.0f );
			// Apply scaling factor to line length
			float hitmarkerLength = initialHitmarkerLength;

			// Apply translation factor to offset distance from center
			float diagonalOffset = initialDiagonalOffset;

			// Calculate the start points for the four diagonal lines with translation applied
			var topLeft = center + new Vector2( -diagonalOffset, -diagonalOffset );
			var topRight = center + new Vector2( diagonalOffset, -diagonalOffset );
			var bottomLeft = center + new Vector2( -diagonalOffset, diagonalOffset );
			var bottomRight = center + new Vector2( diagonalOffset, diagonalOffset );

			var hitColor = isHeadshotHitmarker || isKillHitmarker
				? Color.Red.WithAlpha( opacityFactor )
				: Color.White.WithAlpha( 0.5f * opacityFactor );

			// Draw the four diagonal lines with adjusted length and position
			hud.DrawLine( topLeft, topLeft + new Vector2( -hitmarkerLength, -hitmarkerLength ), w, hitColor );
			hud.DrawLine( topRight, topRight + new Vector2( hitmarkerLength, -hitmarkerLength ), w, hitColor );
			hud.DrawLine( bottomLeft, bottomLeft + new Vector2( -hitmarkerLength, hitmarkerLength ), w, hitColor );
			hud.DrawLine( bottomRight, bottomRight + new Vector2( hitmarkerLength, hitmarkerLength ), w, hitColor );
		}

		mainAlpha = mainAlpha.LerpTo( alphaTarget, Time.Delta * 30f );
		linesAlpha = linesAlpha.LerpTo( linesTarget, Time.Delta * 30f );
	}

	public void Trigger( HealthComponent hc, float damage, HitboxTags hitboxTags )
	{
		if ( hc.IsValid() )
		{
			Hitmarker = HitmarkerType.Default;

			var isKill = hc.Health - damage <= 0f;

			TimeSinceAttacked = 0;


			if ( isKill )
				Hitmarker = HitmarkerType.Kill;

			if ( hitboxTags.HasFlag( HitboxTags.Head ) )
			{
				Hitmarker = HitmarkerType.Headshot;
				Sound.Play( "hitmarkerheadshot", Mixer.FindMixerByName( "UI" ) );
			}
			else
			{
				Sound.Play( "hitmarker", Mixer.FindMixerByName( "UI" ) );
			}
		}
	}

	public static float CalculateCrosshairWidth( float spreadAngle, float verticalFov, float screenHeight )
	{
		float halfSpreadRad = MathX.DegreeToRadian( spreadAngle / 2 );
		float halfFovRad = MathX.DegreeToRadian( verticalFov / 2 );

		// Distance from camera to crosshair plane (based on screen height and FOV)
		float screenDistance = (screenHeight / 2) / MathF.Tan( halfFovRad );

		// Base crosshair width (assuming first-person)
		float crosshairWidth = 2 * (MathF.Tan( halfSpreadRad ) * screenDistance);

		var pawn = Pawn.Local;

		if ( pawn.IsValid() && pawn.Controller.ThirdPerson )
		{
			float thirdPersonOffsetX = pawn.Controller.ThirdPersonCameraOffset.x;

			float screenOffset = MathF.Abs( thirdPersonOffsetX ) * screenHeight / (screenHeight * 2 * screenDistance);

			crosshairWidth = MathF.Max( 0, crosshairWidth - screenOffset );
		}

		return crosshairWidth;
	}
}
