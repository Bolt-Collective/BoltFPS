using Sandbox;

namespace Seekers;

public sealed class MapVoteManager : SingletonComponent<MapVoteManager>
{
	[Property]
	public bool Voting;
	public void StartVote()
	{
		Voting = true;
	}
	[Property] public Dictionary<SteamId, string> Votes { get; set; } = new();
	public string GetMap()
	{
		var votes = Votes.Values.GroupBy( x => x )
					.Select( g => new { Map = g.Key, Count = g.Count() } )
					.OrderByDescending( x => x.Count )
					.FirstOrDefault();

		Voting = false;

		return votes?.Map ?? null;
	}

	[Rpc.Broadcast]
	public void AddVote( Client client, string map )
	{
		Log.Info( "Vote added for " + map );

		// Remove the old vote if it exists
		if ( client.CurrentVote != null )
		{
			Votes.Remove( client.SteamId );
		}

		// Add the new vote
		Votes[client.SteamId] = map;
		client.CurrentVote = map;
	}
}
