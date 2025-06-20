// Ignore Spelling: Utils Indices Vertices verts khr Mem inds

using System.Collections;

namespace _150_trying.geom;

public class Indices : IReadOnlyList<uint> {
	private uint[] all;
	public uint this[int index] => all[index];
	/// <summary>In bytes.</summary>
	public ulong size { get; private set; }
	public int Count => all.Length;

	public Indices() => all = Array.Empty<uint>();

	public Indices(IReadOnlyList<uint> copied)
		: this([.. copied]) {}
	
	public Indices(uint[] referenced) {
		all = referenced;
		size = sizeof(uint) * (ulong)all.Length;
	}

	public Span<uint> AsSpan() => all.AsSpan();
	
	public static implicit operator Indices(uint[] referenced)
		=> new (referenced);

	public static implicit operator Indices(List<uint> copied)
		=> new (copied);

	public IEnumerator<uint> GetEnumerator()
		=> (IEnumerator<uint>)all.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => all.GetEnumerator();
}


