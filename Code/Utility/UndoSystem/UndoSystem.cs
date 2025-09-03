using BoltFPS;
using Sandbox;
using Sandbox.Diagnostics;
using Seekers;
using System;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;

public sealed class UndoSystem : GameObjectSystem<UndoSystem>
{
	public UndoSystem( Scene scene ) : base( scene )
	{
	}

	private static Dictionary<Guid, List<Undo>> Undos = new();

	private static List<Undo> Get( Guid id )
	{
		if ( !Undos.ContainsKey( id ) )
			Undos.Add( id, new List<Undo>() );

		return Undos[id];
	}

	public static string UndoObjects( string message, params GameObject[] objects )
	{
		bool skip = true;
		foreach ( var obj in objects )
		{
			if ( obj?.IsValid() == true )
				skip = false;
		}

		if ( skip )
			return "skip";

		foreach ( var obj in objects )
		{
			obj?.BroadcastDestroy();
		}

		return message;
	}

	private static Undo GetFirstAndRemove( Guid id )
	{
		if ( !Undos.ContainsKey( id ) )
			Undos.Add( id, new List<Undo>() );

		if ( Undos[id].Count == 0 ) return null;

		Undo undo = Undos[id][0];
		Undos[id].RemoveAt( 0 );

		return undo;
	}

	private static void AddUndo( Guid id, Undo undo )
	{
		if ( !Undos.ContainsKey( id ) )
		{
			Undos.Add( id, new List<Undo>() );
		}

		Undos[id].Insert( 0, undo );
	}

	public static bool Remove( Guid id, Undo undo )
	{
		if ( !Undos.ContainsKey( id ) )
			Undos.Add( id, new List<Undo>() );

		return Undos[id].Remove( undo );
	}

	public static bool RemoveAll( Guid id )
	{
		if ( !Undos.ContainsKey( id ) )
			Undos.Add( id, new List<Undo>() );

		int count = Undos[id].Count;
		Undos[id].Clear();
		return count > 0;
	}

	[ConCmd( "undo" )]
	public static void PlayerUndo()
	{
		var player = Client.Local;
		if ( !player.IsValid() )
			return;

		BroadcastUndo( player.Network.Owner.Id );
	}

	[Rpc.Host]
	public static void BroadcastUndo( Guid id )
	{
		Undo undo = GetFirstAndRemove( id );

		if ( undo != null )
		{
			if ( undo.UndoCallback != null )
			{
				var undoMessage = undo.UndoCallback();
				if ( undoMessage == "skip" )
				{
					PlayerUndo();
					return;
				}

				if ( undoMessage != "" )
				{
					ToastNotification.Current.BroadcastToast( undoMessage, 3, id );

					if ( undo.Prop != null ) CreateUndoParticles( undo.Prop.WorldPosition );
				}
			}
		}
	}

	public static void Add( Guid creator, Func<string> callback, GameObject prop = null )
	{
		if ( creator == default ) return;

		var undo = new Undo( creator: creator, callback: callback, prop: prop );

		AddUndo( creator, undo );
	}


	[Rpc.Broadcast]
	public static void CreateUndoParticles( Vector3 pos )
	{
		if ( pos != Vector3.Zero )
		{
			//Particles.MakeParticleSystem( "particles/physgun_freeze.vpcf", new Transform( pos ), 4 );
		}
	}
}
