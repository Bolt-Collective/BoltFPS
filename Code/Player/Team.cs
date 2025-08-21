namespace Seekers;

public class TeamManager : SingletonComponent<TeamManager>
{
	[Property] public Team DefaultTeam { get; set; } = BasicTeam;
	public static Team BasicTeam => ResourceLibrary.GetAll<Team>().FirstOrDefault( x => x.ResourceName == "basic" );
	public static Team SpectatorsTeam => ResourceLibrary.GetAll<Team>().FirstOrDefault( x => x.ResourceName == "spectators" );
	public static Team GetTeam(string team) => ResourceLibrary.GetAll<Team>().FirstOrDefault(x => x.ResourceName == team);
}

[GameResource( "Team", "team", "A team", Icon = "People" )]
public sealed class Team : GameResource
{
	[KeyProperty] public string DisplayName { get; set; }
	[KeyProperty] public float RespawnTime { get; set; } = 3f;
	[KeyProperty] public Color Color { get; set; }
	[KeyProperty, ImageAssetPath] public string Image { get; set; }
	[KeyProperty] public string Objective { get; set; }
	[Property] public PrefabFile PawnPrefab { get; set; }
	[Property] public bool FriendlyFire { get; set; } = true;

	[Property] public bool UseEnemies { get; set; } = true;
	[Property, ShowIf("UseEnemies", false)] public List<Team> Friends { get; set; }
	[Property, ShowIf( "UseEnemies", true )] public List<Team> Enemies { get; set; }

	public bool IsEnemy(Team team)
	{
		if ( team == this )
			return false;
		if (UseEnemies)
			return Enemies.Contains(team);
		else
			return !Friends.Contains(team);
	}

	public bool IsEnemy(GameObject gameObject)
	{
		if ( gameObject.Root.Components.TryGet<Pawn>( out var pawn ) )
			return IsEnemy( pawn.Owner.Team );

		if ( gameObject.Root.Components.TryGet<NPC>( out var npc ) )
			return IsEnemy( npc.TeamRef );

		return false;
	}
}
