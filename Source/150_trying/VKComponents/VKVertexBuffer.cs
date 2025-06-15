// Ignore Spelling: Utils Indices Vertices verts khr

using Silk.NET.Vulkan;
using _150_trying.geom;
using Buffer = Silk.NET.Vulkan.Buffer;
using _150_trying.utils;

namespace _150_trying.VKComponents;

public unsafe class VKVertexBuffer : VKComponent {
	public Buffer buffer;
	public DeviceMemory bufferMemory;
	public Vertices verts = new() {
			{(0.0f, -0.5f), (1.0f, 1.0f, 1.0f)},
			{(0.5f, 0.5f), ( 0.0f, 1.0f, 0.0f)},
			{(-0.5f, 0.5f), ( 0.0f, 0.0f, 1.0f)},
	};

	public override void init(VKSetup s) {
		var bi = new BufferCreateInfo {
			SType = StructureType.BufferCreateInfo,
			Size = verts.size,
			Usage = BufferUsageFlags.VertexBufferBit,
			SharingMode = SharingMode.Exclusive,
		};

		s.vk!.CreateBuffer(s.device, in bi, null, out buffer)
			.throwOnFail("Failed to create vertex buffer");

		MemoryRequirements memReq;
		s.vk!.GetBufferMemoryRequirements(s.device, buffer, &memReq);

		MemoryAllocateInfo allocInfo = new() {
			SType = StructureType.MemoryAllocateInfo,
			AllocationSize = memReq.Size,
			MemoryTypeIndex = FindMemoryType(s,
				memReq.MemoryTypeBits, 
				MemoryPropertyFlags.HostVisibleBit
				| MemoryPropertyFlags.HostCoherentBit
			)
		};

		s.vk.AllocateMemory(s.device, in allocInfo, null, out bufferMemory)
			.throwOnFail("Failed to allocate memory for vertex buffer");
		s.vk.BindBufferMemory(s.device, buffer, bufferMemory, 0);

		void* data;
		s.vk.MapMemory(s.device, bufferMemory, 0, bi.Size, 0, &data);
		verts.AsSpan().CopyTo(new Span<Vertex>(data, verts.Count));
		s.vk.UnmapMemory(s.device, bufferMemory);
	}

	private uint FindMemoryType(VKSetup s, uint typeFilter, MemoryPropertyFlags properties) {
		s.vk!.GetPhysicalDeviceMemoryProperties
			(s.physicalDevice, out var props);

		for (int i = 0; i < props.MemoryTypeCount; i++) {
			if ((typeFilter & 1 << i) != 0
				&& (props.MemoryTypes[i].PropertyFlags & properties) == properties) {
				return (uint)i;
			}
		}

		throw new Exception("failed to find suitable memory type!");
	}

	public override void clear(VKSetup s) {
		s.vk!.DestroyBuffer(s.device, buffer, null);
		s.vk.FreeMemory(s.device, bufferMemory, null);
	}

}


