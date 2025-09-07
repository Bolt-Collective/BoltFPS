namespace Seekers;

[Library( "tool_dynamite", Title = "Dynamite", Description = "An explosive device that explodes." )]
[Group( "construction" )]
public partial class Dynamite : BaseEntitySpawner<DynamiteEntity>
{
	[Property] public InputBind DetonateBind { get; set; } = new("use", true);

	[Property] public float Damage { get; set; } = 100;

	[Property] public float Radius { get; set; } = 5;

	[Property] public bool RemoveOnExplode { get; set; } = true;

	protected override string PreviewModelPath => "models/dynamite/dyn.vmdl";
	protected override Rotation PreviewRotationOffset => Rotation.From( new Angles( 0, 0, 0 ) );
	protected override float PreviewNormalOffset => 8f;

	public override IEnumerable<ToolHint> GetHints()
	{
		yield return new ToolHint( "attack1", "Place" );
		yield return new ToolHint( "attack2", "Apply changes on existing entity" );
		yield return ToolHint.ForBind( "Detonate", DetonateBind );
	}

	protected override void ApplyChanges( GameObject target )
	{
		ChangeValues( target, DetonateBind.GetBroadcast(), Damage, Radius, RemoveOnExplode );
	}

	protected override void CreateEntity( SelectionPoint sp )
	{
		if ( !sp.GameObject.IsValid() )
			return;

		var detonate = DetonateBind?.GetBroadcast() ?? default;

		CreateDynamite( sp, detonate, Damage, Radius, RemoveOnExplode, Network.OwnerId );
	}

	[Rpc.Broadcast]
	public void ChangeValues( GameObject tnt, BroadcastBind detonateBind, float damage, float radius,
		bool removeOnExplode )
	{
		if ( tnt.IsProxy )
			return;

		var dynamiteEntity = tnt.GetComponent<DynamiteEntity>();

		if ( !dynamiteEntity.IsValid() )
			return;

		dynamiteEntity.Damage = damage;
		dynamiteEntity.Radius = radius;
		dynamiteEntity.RemoveOnExplode = removeOnExplode;
		dynamiteEntity.DetonateBind = new InputBind( detonateBind );
	}

	[Rpc.Host]
	public static void CreateDynamite( SelectionPoint selectionPoint, BroadcastBind detonateBind, float damage,
		float radius,
		bool removeOnExplode, Guid owner )
	{
		var tnt = new GameObject();
		tnt.WorldPosition = selectionPoint.WorldPosition + selectionPoint.WorldNormal * 8;
		tnt.WorldRotation = Rotation.LookAt( selectionPoint.WorldNormal ) * Rotation.From( new Angles( 0, 0, 0 ) );

		var modelProp = tnt.Components.Create<Prop>();
		modelProp.Model = Model.Load( "models/dynamite/dyn.vmdl" );

		var propHelper = tnt.AddComponent<PropHelper>();
		propHelper.Invincible = true;

		var dynamiteEntity = tnt.AddComponent<DynamiteEntity>();
		dynamiteEntity.DetonateBind = new InputBind( detonateBind );
		dynamiteEntity.Damage = damage;
		dynamiteEntity.Radius = radius;
		dynamiteEntity.RemoveOnExplode = removeOnExplode;
		dynamiteEntity.EntityOwner = owner;

		tnt.NetworkSpawn();

		UndoSystem.Add( creator: owner, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Dynamite", tnt );
		}, prop: tnt );
	}
}
