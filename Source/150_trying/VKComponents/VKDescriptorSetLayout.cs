// Ignore Spelling: Utils Indices Vertices verts khr

using Silk.NET.Vulkan;
using _150_trying.utils;

namespace _150_trying.VKComponents;

/// <summary>https://vulkan-tutorial.com/Uniform_buffers/Descriptor_layout_and_buffer</summary>
public unsafe class VKDescriptorSetLayout : VKComponent {
	public DescriptorSetLayout descriptorSetLayout;

	public override void init(VKSetup s) {
		DescriptorSetLayoutBinding uboBind = new() {
			Binding = 0,
			DescriptorType = DescriptorType.UniformBuffer,
			DescriptorCount = 1,
			StageFlags = ShaderStageFlags.VertexBit,
			PImmutableSamplers = null,
		};

		DescriptorSetLayoutCreateInfo li = new() {
			SType = StructureType.DescriptorSetLayoutCreateInfo,
			BindingCount = 1,
			PBindings = &uboBind,
		};

		s.vk.CreateDescriptorSetLayout(s.device, in li, null, out descriptorSetLayout)
			.throwOnFail("Failed to create descriptor set layout.");
	}

	public override void clear(VKSetup s) {
		s.vk.DestroyDescriptorSetLayout
			(s.device, descriptorSetLayout, null);
	}
}


