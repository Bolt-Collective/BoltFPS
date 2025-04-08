namespace Seekers;

public class LoadedPrefab
{
	public string _path;
	private GameObject _prefab;

	public LoadedPrefab( string prefabPath )
	{
		_path = prefabPath;
		_prefab = GetPrefab( _path );
	}

	public GameObject Prefab
	{
		get
		{
			if ( !_prefab.IsValid() )
			{
				_prefab = GetPrefab( _path );
			}
			return _prefab;
		}
	}

	public GameObject GetPrefab( string path )
	{
		var prefab = GameObject.GetPrefab( path );
		var clone = prefab.Clone( Vector3.One * 5000 );
		clone.DestroyAsync();
		return prefab;
	}
}
