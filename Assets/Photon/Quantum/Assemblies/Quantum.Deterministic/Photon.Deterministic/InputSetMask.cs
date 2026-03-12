namespace Photon.Deterministic

{

	/// <summary>

	/// The input mask is a utility struct to keep track of which players are included in a set of inputs.

	/// Max player count supported is 128.

	/// Internally the mask is split into two ulongs.

	/// </summary>

	public struct InputSetMask

	{

		private ulong _maskA;



		private ulong _maskB;



		/// <summary>

		/// Create a mask that includes all players.

		/// </summary>

		/// <returns>Resulting mask</returns>

		public static InputSetMask AllPlayersMask()

		{

			return new InputSetMask(ulong.MaxValue, ulong.MaxValue);

		}



		private InputSetMask(ulong maskA, ulong maskB)

		{

			_maskA = maskA;

			_maskB = maskB;

		}



		/// <summary>

		/// Constructor using a single player index.

		/// </summary>

		/// <param name="index">Player index</param>

		public InputSetMask(int index)

		{

			_maskA = 0uL;

			_maskB = 0uL;

			if (index < 64)

			{

				_maskA = (ulong)(1L << index);

			}

			else

			{

				_maskB = (ulong)(1L << index - 64);

			}

		}
    


		/// <summary>

		/// Check if a player is included in the mask.

		/// </summary>

		/// <param name="index">Player index</param>

		/// <returns><see langword="true" /> if the player is included</returns>

		public readonly bool Contains(int index)

		{

			if (index < 64)

			{

				return (_maskA & (ulong)(1L << index)) != 0;

			}

			return (_maskB & (ulong)(1L << index - 64)) != 0;

		}



		/// <summary>

		/// Combine two masks. An enabled player flag takes precedence.

		/// </summary>

		/// <param name="other">Other mask</param>

		/// <returns>A new mask all players flagged.</returns>

		public readonly InputSetMask Combine(InputSetMask other)

		{

			return new InputSetMask(_maskA | other._maskA, _maskB | other._maskB);

		}



		/// <summary>

		/// Intersect two masks and return a new mask with the players that are included in both masks.

		/// </summary>

		/// <param name="other">Other mask</param>

		/// <returns>A new mask with players included in both mask</returns>

		public readonly InputSetMask Intersects(InputSetMask other)

		{

			return new InputSetMask(_maskA & other._maskA, _maskB & other._maskB);

		}



		/// <summary>

		/// Inverse a mask.

		/// </summary>

		/// <returns>A new mask with inverted player flags</returns>

		public readonly InputSetMask Inverse()

		{

			return new InputSetMask(~_maskA, ~_maskB);

		}



		/// <summary>

		/// Add a player index to the mask.

		/// </summary>

		/// <param name="index">Player index</param>

		public void Add(int index)

		{

			if (index < 64)

			{

				_maskA |= (ulong)(1L << index);

			}

			else

			{

				_maskB |= (ulong)(1L << index - 64);

			}

		}



		/// <summary>

		/// Remove a player index from the mask.

		/// </summary>

		/// <param name="index">Player index</param>

		public void Remove(int index)

		{

			if (index < 64)

			{

				_maskA &= (ulong)(~(1L << index));

			}

			else

			{

				_maskB &= (ulong)(~(1L << index - 64));

			}

		}



		/// <summary>

		/// Serialize the mask to a bitstream.

		/// </summary>

		/// <param name="stream">Stream to write or read from</param>

		/// <param name="playerLength">The player length to optimize the amount of memory used.</param>

		public void Serialize(BitStream stream, int playerLength)

		{

			if (playerLength <= 64)

			{

				stream.Serialize(ref _maskA, playerLength);

				return;

			}

			stream.Serialize(ref _maskA, 64);

			stream.Serialize(ref _maskB, playerLength - 64);

		}



		/// <summary>

		/// Equals operator.

		/// </summary>

		/// <param name="mask1">Mask 1</param>

		/// <param name="mask2">Mask 2</param>

		/// <returns><see langword="true" /> if both mask are equal</returns>

		public static bool operator ==(InputSetMask mask1, InputSetMask mask2)

		{

			if (mask1._maskA == mask2._maskA)

			{

				return mask1._maskB == mask2._maskB;

			}

			return false;

		}



		/// <summary>

		/// Un-equals operator.

		/// </summary>

		/// <param name="mask1">Mask 1</param>

		/// <param name="mask2">Mask 2</param>

		/// <returns><see langword="true" /> if the masks are different</returns>

		public static bool operator !=(InputSetMask mask1, InputSetMask mask2)

		{

			if (mask1._maskA == mask2._maskA)

			{

				return mask1._maskB != mask2._maskB;

			}

			return true;

		}



		/// <summary>

		/// Equals method.

		/// </summary>

		/// <param name="other">Other mask</param>

		/// <returns><see langword="true" /> if both masks are equal</returns>

		public override readonly bool Equals(object other)

		{

			if (other is InputSetMask && _maskA == ((InputSetMask)other)._maskA)

			{

				return _maskB == ((InputSetMask)other)._maskB;

			}

			return false;

		}



		/// <summary>

		/// Returns a unique hashcode for the mask.

		/// </summary>

		/// <returns>Hashcode</returns>

		public override readonly int GetHashCode()

		{

			return 31 * (int)_maskA + (int)_maskB;

		}

	}

}


