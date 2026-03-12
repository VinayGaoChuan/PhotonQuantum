using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using Quantum;

namespace Photon.Deterministic
{
	/// <summary>
	/// Native is a collections of tools that interact with the platform's native code directly
	/// like memory allocation, copy operations, etc.
	/// </summary>
	public static class Native
	{
		public struct AllocatorPerformanceStats
		{
			public long AllocCount;

			public long AllocSize;

			public long AllocTime;

			public long FreeCount;

			public long FreeTime;

			public AllocatorPerformanceStats Collect()
			{
				AllocatorPerformanceStats result = new AllocatorPerformanceStats
				{
					AllocCount = AllocCount,
					AllocSize = AllocSize,
					AllocTime = AllocTime,
					FreeCount = FreeCount,
					FreeTime = FreeTime
				};
				AllocCount = 0L;
				AllocSize = 0L;
				AllocTime = 0L;
				FreeCount = 0L;
				FreeTime = 0L;
				return result;
			}
		}

		/// <summary>
		/// Represents native memory allocations and is used in many places across the engine.
		/// </summary>
		public abstract class Allocator
		{
			public AllocatorPerformanceStats Stats;

			public bool StatsEnabled;

			[Conditional("DEBUG")]
			protected unsafe void TrackAlloc(void* ptr)
			{
			}

			[Conditional("DEBUG")]
			protected unsafe void TrackFree(void* ptr)
			{
			}

			/// <summary>
			/// Disposes the allocator and logs memory leaks in debug mode.
			/// </summary>
			public void Dispose()
			{
			}

			/// <summary>
			/// Frees memory previously allocated by this allocator.
			/// </summary>
			/// <param name="ptr">Pointer to memory allocated with Alloc.</param>
			public unsafe abstract void Free(void* ptr);

			/// <summary>
			/// Allocates memory from the unmanaged memory of the process.
			/// </summary>
			/// <param name="count">The required number of bytes in memory.</param>
			/// <returns>A pointer to the newly allocated memory. This memory must be released using the <see cref="M:Photon.Deterministic.Native.Allocator.Free(System.Void*)" /> method.</returns>
			public unsafe abstract void* Alloc(int count);

			/// <summary>
			/// Allocates memory from the unmanaged memory of the process with a desired alignment.
			/// </summary>
			/// <param name="count">The required number of bytes in memory.</param>
			/// <param name="alignment">The byte alignment.</param>
			/// <returns>A pointer to the newly allocated memory. This memory must be released using the <see cref="M:Photon.Deterministic.Native.Allocator.Free(System.Void*)" /> method.</returns>
			public unsafe abstract void* Alloc(int count, int alignment);

			/// <summary>
			/// Writes the desired number zeroed bytes to the specified memory location.
			/// </summary>
			/// <param name="dest">The destination memory.</param>
			/// <param name="count">The byte count.</param>
			protected unsafe abstract void Clear(void* dest, int count);

			/// <summary>
			/// Allocate and clear native memory.
			/// </summary>
			/// <param name="count">The required number of bytes in memory.</param>
			/// <returns>A pointer to the newly allocated memory. This memory must be released using the <see cref="M:Photon.Deterministic.Native.Allocator.Free(System.Void*)" /> method.</returns>
			public unsafe void* AllocAndClear(int count)
			{
				void* ptr = Alloc(count);
				Clear(ptr, count);
				return ptr;
			}

			/// <summary>
			/// Allocate and clear native memory.
			/// </summary>
			/// <param name="count">The required number of bytes in memory.</param>
			/// <param name="alignment">The byte alignment.</param>
			/// <returns>A pointer to the newly allocated memory. This memory must be released using the <see cref="M:Photon.Deterministic.Native.Allocator.Free(System.Void*)" /> method.</returns>
			public unsafe void* AllocAndClear(int count, int alignment)
			{
				void* ptr = Alloc(count, alignment);
				Clear(ptr, count);
				return ptr;
			}

			/// <summary>
			/// Allocate and clear native memory for a specific type.
			/// </summary>
			/// <returns>A pointer to the newly allocated memory. This memory must be released using the <see cref="M:Photon.Deterministic.Native.Allocator.Free(System.Void*)" /> method.</returns>
			public unsafe T* AllocAndClear<T>() where T : unmanaged
			{
				void* ptr = Alloc(sizeof(T));
				Clear(ptr, sizeof(T));
				return (T*)ptr;
			}

			/// <summary>
			/// Allocate native memory for a specific type.
			/// </summary>
			/// <returns>A pointer to the newly allocated memory. This memory must be released using the <see cref="M:Photon.Deterministic.Native.Allocator.Free(System.Void*)" /> method.</returns>
			public unsafe T* Alloc<T>() where T : unmanaged
			{
				return (T*)Alloc(sizeof(T));
			}

			/// <summary>
			/// Expands the allocated memory to a new size by copying the old contents and freeing the old memory.
			/// </summary>
			/// <param name="buffer">The allocated memory.</param>
			/// <param name="currentSize">The current size.</param>
			/// <param name="newSize">The new size.</param>
			/// <returns>A pointer to the newly allocated memory. This memory must be released using the <see cref="M:Photon.Deterministic.Native.Allocator.Free(System.Void*)" /> method.</returns>
			public unsafe void* Expand(void* buffer, int currentSize, int newSize)
			{
				void* ptr = AllocAndClear(newSize);
				Utils.Copy(ptr, buffer, currentSize);
				Free(buffer);
				return ptr;
			}

			/// <summary>
			/// Expands the allocated memory of the array of the type to a new size by copying the old contents and freeing the old memory.
			/// </summary>
			/// <typeparam name="T">The data type.</typeparam>
			/// <param name="buffer">The allocated memory.</param>
			/// <param name="currentSize">The current size.</param>
			/// <param name="newSize">The new size.</param>
			/// <returns>A pointer to the newly allocated memory. This memory must be released using the <see cref="M:Photon.Deterministic.Native.Allocator.Free(System.Void*)" /> method.</returns>
			public unsafe T* ExpandArray<T>(T* buffer, int currentSize, int newSize) where T : unmanaged
			{
				T* ptr = (T*)AllocAndClear(sizeof(T) * newSize);
				Utils.Copy(ptr, buffer, sizeof(T) * currentSize);
				Free(buffer);
				return ptr;
			}

			/// <summary>
			/// Create a allocation operation table to be passed into native parts of the engine.
			/// </summary>
			/// <returns>Allocator table.</returns>
			public abstract AllocatorVTableManaged GetManagedVTable();
		}

		/// <summary>
		/// Represents a collection of memory utility functions that interact with the platform's native code directly.
		/// </summary>
		public abstract class Utility
		{
			/// <summary>
			/// Copies count bytes from the object pointed to by src to the object pointed to by dest.
			/// </summary>
			/// <param name="dest">Pointer to the memory location to copy to.</param>
			/// <param name="src">Pointer to the memory location to copy from.</param>
			/// <param name="count">Number of bytes to copy.</param>
			public unsafe abstract void Copy(void* dest, void* src, int count);

			/// <summary>
			/// Writes the desired number zeroed bytes to the specified memory location.
			/// </summary>
			/// <param name="dest">Pointer to the object to fill.</param>
			/// <param name="count">Number of bytes to write.</param>
			public unsafe abstract void Clear(void* dest, int count);

			/// <summary>
			/// Copies count characters from the object pointed to by src to the object pointed to by dest.
			/// </summary>
			/// <param name="dest">Pointer to the memory location to copy to.</param>
			/// <param name="src">Pointer to the memory location to copy from.</param>
			/// <param name="count">Number of bytes to copy.</param>
			public unsafe abstract void Move(void* dest, void* src, int count);

			/// <summary>
			/// Copies the value into each of the first count characters of the object pointed to by dest.
			/// </summary>
			/// <param name="dest">Pinter to the object to fill.</param>
			/// <param name="value">The byte to write.</param>
			/// <param name="count">Number of bytes to write.</param>
			public unsafe abstract void Set(void* dest, byte value, int count);

			/// <summary>
			/// Compares the first count bytes of these arrays. The comparison is done lexicographically. 
			/// </summary>
			/// <param name="ptr1">Pointers to the left hand side memory buffers to compare.</param>
			/// <param name="ptr2"> Pointers to the right hand side memory buffers to compare.</param>
			/// <param name="count">Number of bytes to examine.</param>
			/// <returns>Negative value if the first differing byte in <paramref name="ptr1" /> is less than the corresponding byte in <paramref name="ptr2" />. 
			/// 0​ if all count bytes of <paramref name="ptr1" /> and <paramref name="ptr2" /> are equal.
			/// Positive value if the first differing byte in <paramref name="ptr1" /> is greater than the corresponding byte in <paramref name="ptr2" />.</returns>
			public unsafe abstract int Compare(void* ptr1, void* ptr2, int count);

			/// <summary>
			/// Copies count bytes from the array pointed to by src to the array pointed to by dest.
			/// <para>Requires the stride of the copy which represents the size of the array type.</para>
			/// </summary>
			/// <param name="source">Pointer to the memory location to copy from.</param>
			/// <param name="sourceIndex">The offset to the source as in number <paramref name="stride" />.</param>
			/// <param name="destination">Pointer to the memory location to copy to.</param>
			/// <param name="destinationIndex">The offset to the destination as in number of <paramref name="stride" />.</param>
			/// <param name="count">Number of times to copy <paramref name="stride" /> bytes.</param>
			/// <param name="stride">The size of the type inside the array in bytes.</param>
			public unsafe void CopyArrayWithStride(void* source, int sourceIndex, void* destination, int destinationIndex, int count, int stride)
			{
				Copy((byte*)destination + destinationIndex * stride, (byte*)source + sourceIndex * stride, count * stride);
			}

			/// <summary>
			/// Copies count bytes from the array pointed to by src to the array pointed to by dest.
			/// </summary>
			/// <typeparam name="T">The type of the array.</typeparam>
			/// <param name="source">Pointer to the memory location to copy from.</param>
			/// <param name="sourceIndex">The source array offset.</param>
			/// <param name="destination">Pointer to the memory location to copy to.</param>
			/// <param name="destinationIndex">The destination array offset.</param>
			/// <param name="count">The number of array elements to copy.</param>
			public unsafe void CopyArray<T>(T* source, int sourceIndex, T* destination, int destinationIndex, int count) where T : unmanaged
			{
				CopyArrayWithStride(source, sourceIndex, destination, destinationIndex, count, sizeof(T));
			}

			/// <inheritdoc cref="M:Photon.Deterministic.Native.Utility.Clear(System.Void*,System.Int32)" />
			public unsafe static void ClearFast(void* dest, int count)
			{
				Utils.Clear(dest, count);
			}

			/// <inheritdoc cref="M:Photon.Deterministic.Native.Utility.Copy(System.Void*,System.Void*,System.Int32)" />
			public unsafe static void CopyFast(void* dest, void* src, int count)
			{
				Utils.Copy(dest, src, count);
			}
		}

		/// <summary>
		/// An memory delegate used by <see cref="T:Photon.Deterministic.Native.AllocatorVTableManaged" />.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr AllocateDelegate(UIntPtr size);

		/// <summary>
		/// An memory delegate used by <see cref="T:Photon.Deterministic.Native.AllocatorVTableManaged" />.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void FreeDelegate(IntPtr ptr);

		/// <summary>
		/// An memory delegate used by <see cref="T:Photon.Deterministic.Native.AllocatorVTableManaged" />.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void CopyDelegate(IntPtr dst, IntPtr src, UIntPtr size);

		/// <summary>
		/// An memory delegate used by <see cref="T:Photon.Deterministic.Native.AllocatorVTableManaged" />.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void MoveDelegate(IntPtr dst, IntPtr src, UIntPtr size);

		/// <summary>
		/// An memory delegate used by <see cref="T:Photon.Deterministic.Native.AllocatorVTableManaged" />.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SetDelegate(IntPtr ptr, byte value, UIntPtr size);

		/// <summary>
		/// An memory delegate used by <see cref="T:Photon.Deterministic.Native.AllocatorVTableManaged" />.
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int CompareDelegate(IntPtr ptr1, IntPtr ptr2, UIntPtr size);

		/// <summary>
		/// A class that wraps memory allocation methods to be used in native code parts of the engine.
		/// </summary>
		public class AllocatorVTableManaged
		{
			private readonly AllocateDelegate _malloc;

			private readonly FreeDelegate _free;

			private readonly CopyDelegate _memcpy;

			private readonly MoveDelegate _memmove;

			private readonly SetDelegate _memset;

			private readonly CompareDelegate _memcmp;

			/// <summary>
			/// Returns the <see cref="M:Photon.Deterministic.Native.Allocator.Alloc(System.Int32)" /> method.
			/// </summary>
			public AllocateDelegate Allocate => _malloc;

			/// <summary>
			/// Returns the <see cref="M:Photon.Deterministic.Native.Allocator.Free(System.Void*)" /> method.
			/// </summary>
			public FreeDelegate Free => _free;

			/// <summary>
			/// Returns the <see cref="M:Photon.Deterministic.Native.Utility.Copy(System.Void*,System.Void*,System.Int32)" /> method.
			/// </summary>
			public CopyDelegate Copy => _memcpy;

			/// <summary>
			/// Returns the <see cref="M:Photon.Deterministic.Native.Utility.Move(System.Void*,System.Void*,System.Int32)" /> method.
			/// </summary>
			public MoveDelegate Move => _memmove;

			/// <summary>
			/// Returns the <see cref="M:Photon.Deterministic.Native.Utility.Set(System.Void*,System.Byte,System.Int32)" /> method.
			/// </summary>
			public SetDelegate Set => _memset;

			/// <summary>
			/// Returns the <see cref="M:Photon.Deterministic.Native.Utility.Compare(System.Void*,System.Void*,System.Int32)" /> method.
			/// </summary>
			public CompareDelegate Compare => _memcmp;

			/// <summary>
			/// Create am allocator operations table.
			/// </summary>
			/// <param name="alloc">The allocator to use.</param>
			/// <param name="util">The util methods to use.</param>
			public AllocatorVTableManaged(Allocator alloc, Utility util)
			{
				_malloc = malloc;
				_free = free;
				_memcpy = memcpy;
				_memmove = memmove;
				_memset = memset;
				_memcmp = memcmp;
				unsafe void free(IntPtr ptr)
				{
					alloc.Free((void*)ptr);
				}
				unsafe IntPtr malloc(UIntPtr size)
				{
					return (IntPtr)alloc.Alloc((int)(uint)size);
				}
				unsafe int memcmp(IntPtr ptr1, IntPtr ptr2, UIntPtr size)
				{
					return util.Compare((void*)ptr1, (void*)ptr2, (int)(uint)size);
				}
				unsafe void memcpy(IntPtr dst, IntPtr src, UIntPtr size)
				{
					util.Copy((void*)dst, (void*)src, (int)(uint)size);
				}
				unsafe void memmove(IntPtr dst, IntPtr src, UIntPtr size)
				{
					util.Move((void*)dst, (void*)src, (int)(uint)size);
				}
				unsafe void memset(IntPtr ptr, byte value, UIntPtr size)
				{
					util.Set((void*)ptr, value, (int)(uint)size);
				}
			}

			/// <summary>
			/// Create an allocator operations table.
			/// </summary>
			/// <param name="malloc">The explicit Allocate method.</param>
			/// <param name="free">The explicit Free method.</param>
			/// <param name="memcpy">The explicit Copy method</param>
			/// <param name="memmove">The explicit Move method.</param>
			/// <param name="memset">The explicit Set method.</param>
			/// <param name="memcmp">The explicit Compare method.</param>
			public AllocatorVTableManaged(AllocateDelegate malloc, FreeDelegate free, CopyDelegate memcpy, MoveDelegate memmove, SetDelegate memset, CompareDelegate memcmp)
			{
				_malloc = malloc;
				_free = free;
				_memcpy = memcpy;
				_memmove = memmove;
				_memset = memset;
				_memcmp = memcmp;
			}

			/// <summary>
			/// Wrap the allocator operations table into a struct to be marshaled to native code.
			/// </summary>
			/// <returns></returns>
			public AllocatorVTable Marshal()
			{
				return new AllocatorVTable(this);
			}
		}

		/// <summary>
		/// Equal to the <see cref="T:Photon.Deterministic.Native.AllocatorVTableManaged" /> but with marshaled function pointers.
		/// </summary>
		public readonly struct AllocatorVTable
		{
			private readonly IntPtr malloc;

			private readonly IntPtr free;

			private readonly IntPtr memcpy;

			private readonly IntPtr memmove;

			private readonly IntPtr memset;

			private readonly IntPtr memcmp;

			/// <summary>
			/// Create a marshaled allocator operations table.
			/// </summary>
			/// <param name="vtable">The source allocation methods.</param>
			public AllocatorVTable(AllocatorVTableManaged vtable)
			{
				malloc = Marshal.GetFunctionPointerForDelegate(vtable.Allocate);
				free = Marshal.GetFunctionPointerForDelegate(vtable.Free);
				memcpy = Marshal.GetFunctionPointerForDelegate(vtable.Copy);
				memmove = Marshal.GetFunctionPointerForDelegate(vtable.Move);
				memset = Marshal.GetFunctionPointerForDelegate(vtable.Set);
				memcmp = Marshal.GetFunctionPointerForDelegate(vtable.Compare);
			}
		}

		/// <summary>
		/// The libc native allocator implementation.
		/// </summary>
		[SuppressUnmanagedCodeSecurity]
		public class LIBCAllocator : PInvokeAllocator
		{
			[DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
			[SuppressUnmanagedCodeSecurity]
			private static extern IntPtr memset(IntPtr dest, int c, UIntPtr byteCount);

			/// <inheritdoc />
			protected unsafe sealed override void Clear(void* dest, int count)
			{
				memset((IntPtr)dest, 0, (UIntPtr)(ulong)count);
			}

			/// <inheritdoc />
			public sealed override AllocatorVTableManaged GetManagedVTable()
			{
				return new AllocatorVTableManaged(this, Utils);
			}
		}

		/// <summary>
		/// The libc native memory utility implementation.
		/// </summary>
		[SuppressUnmanagedCodeSecurity]
		public class LIBCUtility : Utility
		{
			[DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
			[SuppressUnmanagedCodeSecurity]
			private static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

			[DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
			[SuppressUnmanagedCodeSecurity]
			private static extern IntPtr memmove(IntPtr dest, IntPtr src, UIntPtr count);

			[DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
			[SuppressUnmanagedCodeSecurity]
			private static extern IntPtr memset(IntPtr dest, int c, UIntPtr byteCount);

			[DllImport("libc", CallingConvention = CallingConvention.Cdecl)]
			[SuppressUnmanagedCodeSecurity]
			private static extern int memcmp(IntPtr ptr1, IntPtr ptr2, UIntPtr byteCount);

			/// <inheritdoc />
			public unsafe sealed override void Clear(void* dest, int count)
			{
				memset((IntPtr)dest, 0, (UIntPtr)(ulong)count);
			}

			/// <inheritdoc />
			public unsafe sealed override void Copy(void* dest, void* src, int count)
			{
				memcpy((IntPtr)dest, (IntPtr)src, (UIntPtr)(ulong)count);
			}

			/// <inheritdoc />
			public unsafe sealed override void Move(void* dest, void* src, int count)
			{
				memmove((IntPtr)dest, (IntPtr)src, (UIntPtr)(ulong)count);
			}

			/// <inheritdoc />
			public unsafe sealed override void Set(void* dest, byte value, int count)
			{
				memset((IntPtr)dest, value, (UIntPtr)(ulong)count);
			}

			/// <inheritdoc />
			public unsafe sealed override int Compare(void* ptr1, void* ptr2, int count)
			{
				return memcmp((IntPtr)ptr1, (IntPtr)ptr2, (UIntPtr)(ulong)count);
			}
		}

		/// <summary>
		/// The MSVCRT native allocator implementation.
		/// </summary>
		[SuppressUnmanagedCodeSecurity]
		public sealed class MSVCRTAllocator : PInvokeAllocator
		{
			[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
			[SuppressUnmanagedCodeSecurity]
			private static extern IntPtr memset(IntPtr dest, int c, UIntPtr byteCount);

			/// <inheritdoc />
			protected unsafe sealed override void Clear(void* dest, int count)
			{
				memset((IntPtr)dest, 0, (UIntPtr)(ulong)count);
			}

			/// <inheritdoc />
			public sealed override AllocatorVTableManaged GetManagedVTable()
			{
				return new AllocatorVTableManaged(this, Utils);
			}
		}

		/// <summary>
		/// The MSVCRT native memory utility implementation.
		/// </summary>
		[SuppressUnmanagedCodeSecurity]
		public sealed class MSVCRTUtility : Utility
		{
			[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
			[SuppressUnmanagedCodeSecurity]
			private static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

			[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
			[SuppressUnmanagedCodeSecurity]
			private static extern IntPtr memmove(IntPtr dest, IntPtr src, UIntPtr count);

			[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
			[SuppressUnmanagedCodeSecurity]
			private static extern IntPtr memset(IntPtr dest, int c, UIntPtr byteCount);

			[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
			[SuppressUnmanagedCodeSecurity]
			private static extern int memcmp(IntPtr ptr1, IntPtr ptr2, UIntPtr count);

			/// <inheritdoc />
			public unsafe sealed override void Clear(void* dest, int count)
			{
				memset((IntPtr)dest, 0, (UIntPtr)(ulong)count);
			}

			/// <inheritdoc />
			public unsafe sealed override void Copy(void* dest, void* src, int count)
			{
				memcpy((IntPtr)dest, (IntPtr)src, (UIntPtr)(ulong)count);
			}

			/// <inheritdoc />
			public unsafe sealed override void Move(void* dest, void* src, int count)
			{
				memmove((IntPtr)dest, (IntPtr)src, (UIntPtr)(ulong)count);
			}

			/// <inheritdoc />
			public unsafe sealed override void Set(void* dest, byte value, int count)
			{
				memset((IntPtr)dest, value, (UIntPtr)(ulong)count);
			}

			/// <inheritdoc />
			public unsafe sealed override int Compare(void* ptr1, void* ptr2, int count)
			{
				return memcmp((IntPtr)ptr1, (IntPtr)ptr2, (UIntPtr)(ulong)count);
			}
		}

		private enum AllocatorState
		{
			NotLoaded,
			PhotonAlloc,
			MarshalAlloc
		}

		/// <summary>
		/// A specific allocator that tracks allocations in Debug mode.
		/// </summary>
		[SuppressUnmanagedCodeSecurity]
		public abstract class PInvokeAllocator : Allocator
		{
			private Dictionary<IntPtr, IntPtr> _alignmentMapping = new Dictionary<IntPtr, IntPtr>();

			public PInvokeAllocator()
			{
			}

			/// <inheritdoc />
			public unsafe sealed override void* Alloc(int count)
			{
				if (count == 0)
				{
					return null;
				}
				LoadAllocator();
				return ((_loaded == AllocatorState.PhotonAlloc) ? quantum_malloc((IntPtr)count) : Marshal.AllocHGlobal(count)).ToPointer();
			}

			/// <inheritdoc />
			public unsafe sealed override void* Alloc(int count, int alignment)
			{
				LoadAllocator();
				byte* ptr = (byte*)(void*)((_loaded == AllocatorState.PhotonAlloc) ? quantum_malloc((IntPtr)count + alignment) : Marshal.AllocHGlobal(count + alignment));
				long num = (long)ptr;
				long num2 = num % alignment;
				long num3 = alignment - num2;
				if (num2 != 0L)
				{
					lock (_alignmentMapping)
					{
						_alignmentMapping.Add(new IntPtr(ptr + num3), new IntPtr(ptr));
					}
					return ptr + num3;
				}
				return ptr;
			}

			/// <inheritdoc />
			public unsafe sealed override void Free(void* ptr)
			{
				LoadAllocator();
				if (ptr == null)
				{
					return;
				}
				if (_alignmentMapping.Count > 0)
				{
					lock (_alignmentMapping)
					{
						if (_alignmentMapping.Count > 0 && _alignmentMapping.TryGetValue(new IntPtr(ptr), out var value))
						{
							ptr = value.ToPointer();
						}
					}
				}
				if (_loaded == AllocatorState.PhotonAlloc)
				{
					quantum_free((IntPtr)ptr);
				}
				else
				{
					Marshal.FreeHGlobal((IntPtr)ptr);
				}
			}
		}

		/// <summary>
		/// A static fields that is expected to be set before the engine is initialized.
		/// </summary>
		public static Utility Utils;

		private static volatile AllocatorState _loaded;

		private const string DLL_NO_SUFFIX = "PhotonPluginAlloc";

		private const string DLL = "PhotonPluginAlloc.dll";

		private static string AssemblyDirectory
		{
			get
			{
				string codeBase = Assembly.GetExecutingAssembly().CodeBase;
				UriBuilder uriBuilder = new UriBuilder(codeBase);
				string path = Uri.UnescapeDataString(uriBuilder.Path);
				return Path.GetDirectoryName(path);
			}
		}

		/// <summary>
		/// Rounds the size up to the nearest multiple of the alignment.
		/// <para>Supports up to alignment 64.</para>
		/// </summary>
		/// <param name="size">The size to round.</param>
		/// <param name="alignment">The alignment to use.</param>
		/// <returns>The rounded size.</returns>
		/// <exception cref="T:System.InvalidOperationException">Is raised when the alignment is not supported.</exception>
		public static int RoundUpToAlignment(int size, int alignment)
		{
			return alignment switch
			{
				1 => size, 
				2 => (size + 1 >> 1) * 2, 
				4 => (size + 3 >> 2) * 4, 
				8 => (size + 7 >> 3) * 8, 
				16 => (size + 15 >> 4) * 16, 
				32 => (size + 31 >> 5) * 32, 
				64 => (size + 63 >> 6) * 64, 
				_ => throw new InvalidOperationException($"Invalid Alignment: {alignment}"), 
			};
		}

		/// <summary>
		/// Calculates the alignment for arrays.
		/// </summary>
		/// <param name="elementSize">The array element count.</param>
		/// <returns>8 is the count is larger than 0 and a multiple of 8, otherwise 4.</returns>
		public static int GetAlignmentForArrayElement(int elementSize)
		{
			if (elementSize > 0 && elementSize % 8 == 0)
			{
				return 8;
			}
			return 4;
		}

		[DllImport("kernel32.dll")]
		private static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("PhotonPluginAlloc.dll", CallingConvention = CallingConvention.Cdecl)]
		[SuppressUnmanagedCodeSecurity]
		private static extern void quantum_free(IntPtr ptr);

		[DllImport("PhotonPluginAlloc.dll", CallingConvention = CallingConvention.Cdecl)]
		[SuppressUnmanagedCodeSecurity]
		private static extern IntPtr quantum_malloc(IntPtr size);

		[DllImport("PhotonPluginAlloc.dll", CallingConvention = CallingConvention.Cdecl)]
		[SuppressUnmanagedCodeSecurity]
		private static extern IntPtr quantum_secure_mode();

		/// <summary>
		/// Gets the size of the specified type.
		/// </summary>
		/// <param name="t">The type to get the size of.</param>
		/// <returns>The size of the specified type in bytes.</returns>
		public static int SizeOf(Type t)
		{
			return Marshal.SizeOf(t);
		}

		private static void LoadAllocator()
		{
			if (_loaded != AllocatorState.NotLoaded)
			{
				return;
			}
			lock (typeof(Native))
			{
				if (_loaded != AllocatorState.NotLoaded)
				{
					return;
				}
				try
				{
					_loaded = ((LoadLibrary(Path.Combine(AssemblyDirectory, "PhotonPluginAlloc.dll")) != (IntPtr)0) ? AllocatorState.PhotonAlloc : AllocatorState.NotLoaded);
					if (_loaded == AllocatorState.NotLoaded)
					{
						_loaded = ((LoadLibrary("PhotonPluginAlloc.dll") != (IntPtr)0) ? AllocatorState.PhotonAlloc : AllocatorState.NotLoaded);
					}
					if (_loaded == AllocatorState.NotLoaded)
					{
						_loaded = ((LoadLibrary("PhotonPluginAlloc") != (IntPtr)0) ? AllocatorState.PhotonAlloc : AllocatorState.MarshalAlloc);
					}
					IntPtr intPtr = default(IntPtr);
					if (_loaded == AllocatorState.PhotonAlloc)
					{
						try
						{
							intPtr = quantum_secure_mode();
						}
						catch (Exception ex)
						{
							LogStream logWarn = InternalLogStreams.LogWarn;
							if (logWarn != null)
							{
								logWarn.Log("Please update plugin allocator: " + ex.Message);
							}
							intPtr = (IntPtr)255;
						}
					}
					switch (_loaded)
					{
					case AllocatorState.NotLoaded:
					{
						LogStream logError = InternalLogStreams.LogError;
						if (logError != null)
						{
							logError.Log("Memory Allocator: Unknown");
						}
						break;
					}
					case AllocatorState.MarshalAlloc:
					{
						LogStream logInfo2 = InternalLogStreams.LogInfo;
						if (logInfo2 != null)
						{
							logInfo2.Log("Memory Allocator: System.Runtime.InteropServices.Marshal");
						}
						break;
					}
					case AllocatorState.PhotonAlloc:
					{
						LogStream logInfo = InternalLogStreams.LogInfo;
						if (logInfo != null)
						{
							logInfo.Log($"Memory Allocator: DLL (secure: {(long)intPtr})");
						}
						break;
					}
					}
				}
				catch (Exception ex2)
				{
					LogStream logException = InternalLogStreams.LogException;
					if (logException != null)
					{
						logException.Log(ex2);
					}
					_loaded = AllocatorState.MarshalAlloc;
				}
			}
		}
	}
}

