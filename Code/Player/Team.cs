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
	[Property] public List<Team> Friends { get; set; }
}
