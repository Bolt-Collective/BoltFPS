namespace Seekers;

public sealed class BasicLoadoutManager : Component
{
	protected override void OnFixedUpdate()
	{
		if ( !WeaponPicker.Current.IsValid() )
			return;

		var loadout = new List<string>();

		foreach(var catagory in WeaponPicker.Current.WeaponCatagories)
		{
			if ( catagory.SelectedWeapon == "" || catagory.SelectedWeapon == null )
				continue;
			loadout.Add( catagory.SelectedWeapon );
		}
		HostSetLoadout( Connection.Local.Id, loadout );
	}

	[Rpc.Host]
	public void HostSetLoadout( Guid id, List<string> loadout )
	{
		EnsuredLoadout.Instance?.SetLoadout( id, loadout );
	}
}
