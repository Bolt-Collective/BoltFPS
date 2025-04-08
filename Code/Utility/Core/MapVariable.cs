namespace Seekers;

public sealed class MapVariable : Component
{
	[Property] private int MaxPlayers { get; set; } = 16;
	[Property] private int TestPlayerCount { get; set; } = 4;
	[Button] private void SetMapTest() => SetMap( TestPlayerCount );

	protected override void OnStart()
	{
		SetMapTest();
	}
	public void SetMap( int playerCount )
	{
		var spawnGroup = SetSpawnGroup();
		SetMapDoors( playerCount, spawnGroup );
	}

	private void SetMapDoors( int playerCount, List<GameObject> spawnGroup )
	{
		var doors = GameObject.Children.Where( child => child.Tags.Has( "closeddoor" ) ).ToList();
		var spawnMidPoint = GetMiddlePoint( spawnGroup );

		// Order doors by distance to the spawn group to prioritize opening closer doors.
		doors = [.. doors.OrderBy( x => Vector3.DistanceBetween( spawnMidPoint, x.WorldPosition + Vector3.Random ) )];

		float playerPercent = MathX.Clamp( playerCount / (float)MaxPlayers, 0, 1 );

		// Use the player percentage relative to the maximum number of players to determine how many doors should be open.
		int openDoorCount = (int)MathF.Round( doors.Count * playerPercent );

		// Prioritize opening doors closer to the spawn group by removing the farthest ones.
		if ( doors.Count > openDoorCount * 2 )
		{
			doors.RemoveRange( openDoorCount * 2, doors.Count - openDoorCount * 2 );
		}

		var openDoors = GameObject.Children.Where( child => child.Tags.Has( "opendoor" ) ).ToList();
		foreach ( var door in openDoors )
			door.Enabled = false;

		// Pair doors correctly.
		var doorPairs = GetAllDoorPairs( doors, openDoors );

		// Select half of the open doors randomly, excluding the closest ones.
		var randomDoors = doors.OrderBy( x => doors.IndexOf( x ) > openDoorCount / 2 ? Game.Random.Next( 0, doors.Count * 2 ) : doors.Count * 3 ).Take( openDoorCount / 2 ).ToList();

		foreach ( var (closed, open) in doorPairs )
		{
			// Based on the number of open doors, open the closest doors (half of the open doors are selected this way) 
			// and also open randomly selected doors.
			var openDoor = randomDoors.Contains( closed ) || doors.IndexOf( closed ) <= openDoorCount / 2;
			closed.Enabled = !openDoor;
			open.Enabled = openDoor;
		}
	}

	private List<GameObject> SetSpawnGroup()
	{
		var spawnGroupTags = new List<string>();
		foreach ( var child in GameObject.Children )
		{
			string spawnTag = null;
			foreach ( var tag in child.Tags )
			{
				if ( !tag.StartsWith( "spawngroup" ) )
					continue;

				spawnTag = tag;
				break;
			}

			if ( spawnTag == null )
				continue;

			if ( spawnGroupTags.Contains( spawnTag ) )
				continue;

			spawnGroupTags.Add( spawnTag );
		}

		var spawnGroups = new List<List<GameObject>>();

		foreach ( var tag in spawnGroupTags )
		{
			spawnGroups.Add( GameObject.Children.Where( child => child.Tags.Has( tag ) ).ToList() );
		}

		var activeGroup = Game.Random.Next( spawnGroups.Count );

		for ( int i = 0; i < spawnGroups.Count; i++ )
		{
			foreach ( var spawn in spawnGroups[i] )
			{
				spawn.Enabled = i == activeGroup;
			}
		}

		return spawnGroups[activeGroup];
	}

	public static Vector3 GetMiddlePoint( List<GameObject> objects )
	{
		if ( objects == null || objects.Count == 0 )
			return Vector3.Zero;

		Vector3 sum = Vector3.Zero;
		foreach ( GameObject obj in objects )
		{
			sum += obj.WorldPosition;
		}

		return sum / objects.Count;
	}

	public List<(GameObject closed, GameObject open)> GetAllDoorPairs( List<GameObject> closedDoors, List<GameObject> openDoors )
	{
		var pairs = new List<(GameObject closed, GameObject open)>();

		foreach ( var closedDoor in closedDoors )
		{
			GameObject matchedDoor = null;

			float closestDistance = 120;

			foreach ( var openDoor in openDoors )
			{
				var distance = Vector3.DistanceBetween( openDoor.WorldPosition, closedDoor.WorldPosition );

				if ( distance > closestDistance )
					continue;

				closestDistance = distance;
				matchedDoor = openDoor;
			}

			if ( !matchedDoor.IsValid() )
				continue;

			pairs.Add( (closedDoor, matchedDoor) );
		}

		return pairs;
	}
}
