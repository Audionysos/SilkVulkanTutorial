// Ignore Spelling: Utils Indices Vertices

using Silk.NET.Vulkan;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using System.Collections;

namespace _150_trying.geom;

public class Vertices : IReadOnlyList<Vertex> {
	private List<Vertex> _all = [];
	public int Count => _all.Count;
	public Vertex this[int index] => _all[index];
	public ulong size => (ulong)(Marshal.SizeOf<Vertex>() * Count);

	public static Vertices test() {
		Vertices vs = new Vertices() {
			{(0.0f, -0.5f), (1.0f, 0.0f, 0.0f)},
			{(0.5f, 0.5f), ( 0.0f, 1.0f, 0.0f)},
			{(-0.5f, 0.5f), ( 0.0f, 0.0f, 1.0f)},
	};
		return vs;
	}

	public void Add((float a, float b) pos, (float r, float g, float b) color) {
		_all.Add(new Vertex() {
			pos = new Vector2D<float>(pos.a, pos.b)
		, color = new Vector3D<float>(color.r, color.g, color.b)
		});
	}

	public Span<Vertex> AsSpan() {
		return _all.ToArray().AsSpan();
	}

	public IEnumerator GetEnumerator() => _all.GetEnumerator();
	IEnumerator<Vertex> IEnumerable<Vertex>.GetEnumerator()
		=> _all.GetEnumerator();
}

public struct Vertex {
	public Vector2D<float> pos;
	public Vector3D<float> color;

	public static VertexInputBindingDescription getBindingDescription() {
		VertexInputBindingDescription bindingDescription = new() {
			Binding = 0,
			Stride = (uint)Unsafe.SizeOf<Vertex>(),
			InputRate = VertexInputRate.Vertex,
		};

		return bindingDescription;
	}

	public static VertexInputAttributeDescription[] getAttributeDescriptions() {
		var attributeDescriptions = new[]
		{
			new VertexInputAttributeDescription()
			{
				Binding = 0,
				Location = 0,
				Format = Format.R32G32Sfloat,
				Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
			},
			new VertexInputAttributeDescription()
			{
				Binding = 0,
				Location = 1,
				Format = Format.R32G32B32Sfloat,
				Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
			}
		};

		return attributeDescriptions;
	}
}

