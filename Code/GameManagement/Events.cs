using Sandbox.Events;

namespace Seekers;

/// <summary>
/// Called on the host when a new player joins, before NetworkSpawn is called.
/// </summary>
public record PlayerConnectedEvent( Client Client ) : IGameEvent;

/// <summary>
/// Called on the host when a new player joins, after NetworkSpawn is called.
/// </summary>
public record PlayerJoinedEvent( Client Player ) : IGameEvent;

/// <summary>
/// Called on the host when a client leaves
/// </summary>
public record PlayerDisconnectedEvent( Client Player ) : IGameEvent;

/// <summary>
/// Called on the host when a player (re)spawns.
/// </summary>
public record PlayerSpawnedEvent( Pawn Player ) : IGameEvent;
