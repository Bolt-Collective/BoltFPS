namespace Seekers;

[Library( "tool_rotate_align", Title = "Align Rotation", Description = "Aligns an object's rotation to a specified degree." )]
[Group("construction")]
public partial class AlignRotation : BaseTool
{
	public override bool UseGrid => false;
	[Property] public float RotationSnapDegrees { get; set; } = 90f;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !Input.Pressed( "attack1" ) )
			return false;

		var go = trace.GameObject;

		var rot = go.WorldRotation.Angles();
		var snappedAngles = new Angles(
			MathF.Round( rot.pitch / RotationSnapDegrees ) * RotationSnapDegrees,
			MathF.Round( rot.yaw / RotationSnapDegrees ) * RotationSnapDegrees,
			MathF.Round( rot.roll / RotationSnapDegrees ) * RotationSnapDegrees
		);

		Rotate( trace.GameObject, Rotation.From( snappedAngles ) );

		return true;
	}

	[Rpc.Broadcast]
	public void Rotate( GameObject gameObject, Rotation rot )
	{
		if ( gameObject.IsProxy )
			return;

		gameObject.WorldRotation = rot;
	}

	//public override bool Secondary( SceneTraceResult trace )
	//{
	//	if ( !trace.Hit || !Input.Pressed( "attack2" ) )
	//		return false;

	//	var go = trace.GameObject; 

	//	var rot = go.WorldRotation.Angles();
	//	var snappedAngles = new Angles(
	//		MathF.Round( rot.pitch / RotationSnapDegrees ) * RotationSnapDegrees,
	//		MathF.Round( rot.yaw / RotationSnapDegrees ) * RotationSnapDegrees,
	//		MathF.Round( rot.roll / RotationSnapDegrees ) * RotationSnapDegrees
	//	);
	//	go.WorldRotation = Rotation.From( snappedAngles );

	//	return true;
	//}

}
