namespace Seekers;

public sealed class BasicLoadoutManager : Component
{
	protected override void OnFixedUpdate()
	{
		if ( !WeaponPicker.Current.IsValid() )
			return;

		var loadout = new List<string>();

		foreach(var catagory in WeaponPicker.Current.WeaponCategories)
		{
			if ( catagory.SelectedWeapon == "" || catagory.SelectedWeapon == null )
				continue;

			foreach(var weapon in catagory.Weapons)
			{
				if(weapon.DisplayName == catagory.SelectedWeapon)
					loadout.Add( weapon.GetDetails() );
			}
		}

		EnsuredLoadout.Instance?.SetLoadout( Connection.Local.Id, loadout );
	}


}
