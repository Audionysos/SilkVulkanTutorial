// Ignore Spelling: Utils Indices Vertices verts khr

using Silk.NET.Vulkan;
using _150_trying.utils;
using System;

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

		DescriptorSetLayoutBinding smpBind = new() {
			Binding = 1,
			DescriptorCount = 1,
			DescriptorType = DescriptorType.CombinedImageSampler,
			PImmutableSamplers = null,
			StageFlags = ShaderStageFlags.FragmentBit,
		};

		DescriptorSetLayoutBinding[] bindings = [uboBind, smpBind]; 

		fixed(DescriptorSetLayoutBinding* bp = bindings) {
			DescriptorSetLayoutCreateInfo li = new() {
				SType = StructureType.DescriptorSetLayoutCreateInfo,
				BindingCount = (uint)bindings.Length,
				PBindings = bp,
			};

			s.vk.CreateDescriptorSetLayout(s.device, in li, null, out descriptorSetLayout)
				.throwOnFail("Failed to create descriptor set layout.");
		}
	}

	public override void clear(VKSetup s) {
		s.vk.DestroyDescriptorSetLayout
			(s.device, descriptorSetLayout, null);
	}
}


