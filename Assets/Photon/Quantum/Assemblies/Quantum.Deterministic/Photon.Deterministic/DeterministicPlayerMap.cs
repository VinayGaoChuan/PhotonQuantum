using System.Collections.Generic;
using Quantum;

namespace Photon.Deterministic
{
	/// <summary>
	/// A utility class to collect a list of players and player slots.
	/// A client can maintain multiple players with individual player slots.
	/// The player always refers to a global player index that is the same on all clients.
	/// The player slot is a zero based index counting the number of players that one client controls.
	/// E.g. a client has two players: PlayerRef 5 and PlayerRef 2, the player slots are 0 and 1.
	/// </summary>
	public class DeterministicPlayerMap
	{
		/// <summary>
		/// List of all players.
		/// </summary>
		public readonly List<PlayerRef> Players = new List<PlayerRef>();

		/// <summary>
		/// List of all player slots to iterate over.
		/// </summary>
		public readonly List<int> PlayerSlots = new List<int>();

		/// <summary>
		/// Maps each slot to a player to iterate over.
		/// </summary>
		public readonly Dictionary<int, PlayerRef> PlayerSlotToPlayer = new Dictionary<int, PlayerRef>();

		/// <summary>
		/// Maps each player to a slot.
		/// </summary>
		public readonly Dictionary<PlayerRef, int> PlayerToPlayerSlot = new Dictionary<PlayerRef, int>();

		private int _max;

		/// <summary>
		/// The number of players in this collection.
		/// </summary>
		public int Count => PlayerSlots.Count;

		/// <summary>
		/// The number of player that can be added to this collection.
		/// </summary>
		public bool Available => Count < _max;

		/// <summary>
		/// Set or get the max count.
		/// </summary>
		public int MaxCount
		{
			get
			{
				return _max;
			}
			set
			{
				_max = value;
			}
		}

		/// <summary>
		/// Create a player map for a given maximum number of players.
		/// </summary>
		/// <param name="max">Max players this collection or a client can have.</param>
		public DeterministicPlayerMap(int max = 128)
		{
			_max = max;
		}

		/// <summary>
		/// Check if the given player slot is assigned.
		/// </summary>
		/// <param name="slot">Player slot</param>
		/// <returns><see langword="true" /> if the player slot has been added</returns>
		public bool Has(int slot)
		{
			return PlayerSlotToPlayer.ContainsKey(slot);
		}

		/// <summary>
		/// Check is the given player is assigned.
		/// </summary>
		/// <param name="player">Player ref</param>
		/// <returns><see langword="true" /> if the player has been added</returns>
		public bool Has(PlayerRef player)
		{
			return PlayerSlotToPlayer.ContainsValue(player);
		}

		/// <summary>
		/// Return the player slot that a player ref was assigned to.
		/// </summary>
		/// <param name="player">Player ref</param>
		/// <returns>The player slot for the player or -1</returns>
		public int SearchSlot(PlayerRef player)
		{
			if (PlayerToPlayerSlot.TryGetValue(player, out var value))
			{
				return value;
			}
			return -1;
		}

		/// <summary>
		/// Return the player that a player slot was assigned to.
		/// </summary>
		/// <param name="slot">Player slot</param>
		/// <returns>The player or -1</returns>
		public int SearchPlayer(int slot)
		{
			if (PlayerSlotToPlayer.TryGetValue(slot, out var value))
			{
				return value;
			}
			return -1;
		}

		/// <summary>
		/// Add a player and player slot to the collection.
		/// Will check if the local player slot is already assigned.
		/// Will check if max capacity is reached.
		/// </summary>
		/// <param name="local">Player slot</param>
		/// <param name="global">Player ref</param>
		/// <returns><see langword="true" /> when the player was added</returns>
		public bool Add(int local, PlayerRef global)
		{
			if (Count >= _max)
			{
				return false;
			}
			if (Has(local))
			{
				return false;
			}
			PlayerSlots.Add(local);
			Players.Add(global);
			PlayerSlotToPlayer.Add(local, global);
			PlayerToPlayerSlot.Add(global, local);
			return true;
		}

		/// <summary>
		/// Remove a player slot and its player from the collection.
		/// </summary>
		/// <param name="slot">Player slot</param>
		public void Remove(int slot)
		{
			if (Has(slot))
			{
				PlayerRef playerRef = PlayerSlotToPlayer[slot];
				PlayerSlots.Remove(slot);
				Players.Remove(playerRef);
				PlayerSlotToPlayer.Remove(slot);
				PlayerToPlayerSlot.Remove(playerRef);
			}
		}

		/// <summary>
		/// Remove a player ref and its player slot from the collection.
		/// </summary>
		/// <param name="player">Player ref</param>
		public void Remove(PlayerRef player)
		{
			if (Has(player))
			{
				int num = PlayerToPlayerSlot[player];
				PlayerSlots.Remove(num);
				Players.Remove(player);
				PlayerSlotToPlayer.Remove(num);
				PlayerToPlayerSlot.Remove(player);
			}
		}
	}
}

