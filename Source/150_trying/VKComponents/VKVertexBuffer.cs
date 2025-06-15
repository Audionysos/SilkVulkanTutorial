// Ignore Spelling: Utils Indices Vertices verts khr Mem

using Silk.NET.Vulkan;
using _150_trying.geom;
using Buffer = Silk.NET.Vulkan.Buffer;
using MemProps = Silk.NET.Vulkan.MemoryPropertyFlags;
using BuffUsage = Silk.NET.Vulkan.BufferUsageFlags;
using _150_trying.utils;
using Silk.NET.OpenAL;

namespace _150_trying.VKComponents;

public unsafe class VKVertexBuffer : VKComponent {
	public Buffer buffer;
	public DeviceMemory bufferMemory;

	public Buffer stagingBuff;
	public DeviceMemory stagingMem;

	public Vertices verts = new() {
			{(0.0f, -0.5f), (1.0f, 1.0f, 1.0f)},
			{(0.5f, 0.5f), ( 0.0f, 1.0f, 0.0f)},
			{(-0.5f, 0.5f), ( 0.0f, 0.0f, 1.0f)},
	};

	public override void init(VKSetup s) {
		createBuffer(s, verts.size, BuffUsage.TransferSrcBit
			, MemProps.HostVisibleBit | MemProps.HostCachedBit
			, out stagingBuff, out stagingMem);

		void* data;
		s.vk.MapMemory(s.device, stagingMem, 0, verts.size, 0, &data);
		verts.AsSpan().CopyTo(new Span<Vertex>(data, verts.Count));
		s.vk.UnmapMemory(s.device, stagingMem);

		createBuffer(s, verts.size
			, BuffUsage.TransferDstBit | BuffUsage.VertexBufferBit
			, MemProps.DeviceLocalBit
			, out buffer, out bufferMemory);

		CommandPool cp = s.require<VKCommandPool>().commandPool;
		copyBuffer(s, cp, stagingBuff, buffer, verts.size);
		s.vk.DestroyBuffer(s.device, stagingBuff, null);
		s.vk.FreeMemory(s.device, stagingMem, null);
	}

	private void copyBuffer(VKSetup s, CommandPool cp, Buffer src, Buffer dst, ulong size) {
		CommandBufferAllocateInfo allocInfo = new() {
			SType = StructureType.CommandBufferAllocateInfo,
			Level = CommandBufferLevel.Primary,
			CommandPool = cp,
			CommandBufferCount = 1,
		};
		s.vk.AllocateCommandBuffers(s.device
			, in allocInfo, out var cmdBuff);

		CommandBufferBeginInfo beginInfo = new (){
			SType = StructureType.CommandBufferBeginInfo,
			Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
		};
		s.vk.BeginCommandBuffer(cmdBuff, in beginInfo);

		BufferCopy copyRegion = new (){
			SrcOffset = 0, DstOffset = 0, //optional
			Size = size
		};
		s.vk.CmdCopyBuffer(cmdBuff, src, dst, 1, in copyRegion);
		s.vk.EndCommandBuffer(cmdBuff);

		SubmitInfo si = new() {
			SType = StructureType.SubmitInfo,
			CommandBufferCount = 1,
			PCommandBuffers = &cmdBuff
		};

		s.vk.QueueSubmit(s.graphicsQueue, 1, in si, new Fence());
		s.vk.QueueWaitIdle(s.graphicsQueue);

		s.vk.FreeCommandBuffers(s.device, cp, 1, in cmdBuff);

	}

	/// <summary>https://vulkan-tutorial.com/Vertex_buffers/Staging_buffer</summary>
	/// <param name="s"></param>
	/// <param name="size"></param>
	/// <param name="usage"></param>
	/// <param name="properties"></param>
	/// <param name="buffer"></param>
	/// <param name="memory"></param>
	private void createBuffer(VKSetup s, ulong size
		, BuffUsage usage, MemProps properties
		, out Buffer buffer, out DeviceMemory memory)
	{
		var bi = new BufferCreateInfo {
			SType = StructureType.BufferCreateInfo,
			Size = size,
			Usage = usage,
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
				memReq.MemoryTypeBits, properties
			)
		};

		s.vk.AllocateMemory(s.device, in allocInfo, null, out memory)
			.throwOnFail("Failed to allocate memory for vertex buffer");
		s.vk.BindBufferMemory(s.device, buffer, memory, 0);
	}

	private uint FindMemoryType(VKSetup s, uint typeFilter, MemProps properties) {
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


