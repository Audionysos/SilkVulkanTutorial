// Ignore Spelling: Utils Indices Vertices Coord

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
			{(0.0f, -0.5f, 0), (1.0f, 0.0f, 0.0f)},
			{(0.5f, 0.5f, 0), ( 0.0f, 1.0f, 0.0f)},
			{(-0.5f, 0.5f, 0), ( 0.0f, 0.0f, 1.0f)},
	};
		return vs;
	}

	public Vertices() { }
	public Vertices(List<Vertex> referenced) {
		_all = referenced;
	}

	public void Add(Vertex v) => _all.Add(v);

	public void Add((float x, float y, float z) pos, (float r, float g, float b) color, (float u, float v) texCoord = default) {
		_all.Add(new Vertex() {
			pos = new Vector3D<float>(pos.x, pos.y, pos.z),
			color = new Vector3D<float>(color.r, color.g, color.b),
			texCoord = new Vector2D<float>(texCoord.u, texCoord.v),
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
	public Vector3D<float> pos;
	public Vector3D<float> color;
	public Vector2D<float> texCoord;

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
				Format = Format.R32G32B32Sfloat,
				Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
			},
			new VertexInputAttributeDescription()
			{
				Binding = 0,
				Location = 1,
				Format = Format.R32G32B32Sfloat,
				Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
			},
			new VertexInputAttributeDescription()
			{
				Binding = 0,
				Location = 2,
				Format = Format.R32G32Sfloat,
				Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(texCoord)),
			}
		};

		return attributeDescriptions;
	}
}

