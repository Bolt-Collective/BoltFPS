namespace Seekers;

public sealed class BasicLoadoutManager : Component
{
	protected override void OnFixedUpdate()
	{
		if ( WeaponPicker.Current == null || !WeaponPicker.Current.IsValid() )
			return;

		var loadout = new List<string>();

		foreach ( var category in WeaponPicker.Current.WeaponCategories )
		{
			if ( category.SelectedWeapons == null || category.SelectedWeapons.Count == 0 )
				continue;

			foreach ( var weapon in category.Weapons )
			{
				if ( category.SelectedWeapons.Contains( weapon.DisplayName ) )
					loadout.Add( weapon.GetDetails() );
			}
		}

		EnsuredLoadout.Instance?.SetLoadout( Connection.Local.Id, loadout );
	}
}
