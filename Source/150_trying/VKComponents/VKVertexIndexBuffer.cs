// Ignore Spelling: Utils Indices Vertices verts khr Mem inds

using Silk.NET.Vulkan;
using _150_trying.geom;
using Buffer = Silk.NET.Vulkan.Buffer;
using MemProps = Silk.NET.Vulkan.MemoryPropertyFlags;
using BuffUsage = Silk.NET.Vulkan.BufferUsageFlags;
using _150_trying.utils;
using Silk.NET.OpenAL;
using System;

namespace _150_trying.VKComponents;

public unsafe class VKVertexIndexBuffer : VKComponent {
	public Buffer buffer;
	public DeviceMemory bufferMemory;

	public Buffer indBuff;
	public DeviceMemory indMem;

	public Buffer stagingBuff;
	public DeviceMemory stagingMem;

	public Vertices verts = new() {
			{(-0.5f, -0.5f), (1.0f, 0.0f, 0.0f), (1, 0)},
			{(0.5f, -0.5f), (0.0f, 1.0f, 0.0f),  (0, 0)},
			{(0.5f, 0.5f), ( 0.0f, 0.0f, 1.0f),  (0, 1)},
			{(-0.5f, 0.5f), ( 1.0f, 1.0f, 1.0f), (1, 1)},
	};

	public Indices inds = new uint[] {
		0, 1, 2, 2, 3, 0
	};

	public override void init(VKSetup s) {
		createVertexBuffer(s);
		createIndexBuffer(s);
	}

	private void createIndexBuffer(VKSetup s) {
		s.createBuffer(inds.size, BuffUsage.TransferSrcBit
			, MemProps.HostVisibleBit | MemProps.HostCachedBit
			, out stagingBuff, out stagingMem);

		void* data;
		s.vk.MapMemory(s.device, stagingMem, 0, inds.size, 0, &data);
		inds.AsSpan().CopyTo(new Span<uint>(data, inds.Count));
		s.vk.UnmapMemory(s.device, stagingMem);

		s.createBuffer(inds.size
			, BuffUsage.TransferDstBit | BuffUsage.IndexBufferBit
			, MemProps.DeviceLocalBit
			, out indBuff, out indMem);


		s.copyBuffer(stagingBuff, indBuff, inds.size);
		s.vk.DestroyBuffer(s.device, stagingBuff, null);
		s.vk.FreeMemory(s.device, stagingMem, null);
	}

	private void createVertexBuffer(VKSetup s) {
		s.createBuffer(verts.size, BuffUsage.TransferSrcBit
			, MemProps.HostVisibleBit | MemProps.HostCachedBit
			, out stagingBuff, out stagingMem);

		void* data;
		s.vk.MapMemory(s.device, stagingMem, 0, verts.size, 0, &data);
		verts.AsSpan().CopyTo(new Span<Vertex>(data, verts.Count));
		s.vk.UnmapMemory(s.device, stagingMem);

		s.createBuffer(verts.size
			, BuffUsage.TransferDstBit | BuffUsage.VertexBufferBit
			, MemProps.DeviceLocalBit
			, out buffer, out bufferMemory);


		s.copyBuffer(stagingBuff, buffer, verts.size);
		s.vk.DestroyBuffer(s.device, stagingBuff, null);
		s.vk.FreeMemory(s.device, stagingMem, null);
	}

	public override void clear(VKSetup s) {
		s.vk!.DestroyBuffer(s.device, buffer, null);
		s.vk.FreeMemory(s.device, bufferMemory, null);

		s.vk!.DestroyBuffer(s.device, indBuff, null);
		s.vk.FreeMemory(s.device, indMem, null);
	}

}


