﻿// Ignore Spelling: Utils Indices Vertices verts khr ubo

using Silk.NET.Vulkan;
using System.Runtime.InteropServices;
using Buffer = Silk.NET.Vulkan.Buffer;
using _150_trying.geom;

namespace _150_trying.VKComponents;

/// <summary>https://vulkan-tutorial.com/Uniform_buffers/Descriptor_layout_and_buffer</summary>
public unsafe class VKUniformBuffers : VKComponent {
	public List<(Buffer buffer, DeviceMemory mem)> all = [];
	List<IntPtr> mapped = []; //void

	public override void init(VKSetup s) {
		var buffSize = (ulong)Marshal.SizeOf<UniformBufferObject>();

		for (int i = 0; i < VKSetup.MAX_FRAMES_IN_FLIGHT; i++) {
			s.createBuffer(buffSize, BufferUsageFlags.UniformBufferBit
				, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
				, out var b, out var m);
			all.Add((b, m));

			void* mm = null;
			s.vk.MapMemory(s.device, m, 0, buffSize, 0, &mm);
			mapped.Add((IntPtr)mm);
		}
	}

	public void update(UniformBufferObject ubo, int buffIndex) {
		Marshal.StructureToPtr(ubo, mapped[buffIndex], true);
	}
	
	public override void clear(VKSetup s) {
		for (int i = 0; i < VKSetup.MAX_FRAMES_IN_FLIGHT; i++) {
			s.vk.DestroyBuffer(s.device, all[i].buffer, null);
			s.vk.FreeMemory(s.device, all[i].mem, null);
		}
		all.Clear(); mapped.Clear();
	}
}


