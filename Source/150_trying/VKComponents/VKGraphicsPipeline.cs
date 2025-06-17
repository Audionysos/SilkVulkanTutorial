// Ignore Spelling: Utils Indices Vertices verts khr

using Silk.NET.Vulkan;
using Silk.NET.Core.Native;
using _150_trying.geom;

namespace _150_trying.VKComponents;

public unsafe class VKGraphicsPipeline : VKComponent {
	public PipelineLayout pipelineLayout;
	public Pipeline graphicsPipeline;

	public override void init(VKSetup s) {
		var (sc, rp, dl) = s.require<VKSwapChain, VKRenderPass, VKDescriptorSetLayout>();
		var vertShaderCode = File.ReadAllBytes("shaders/vert.spv");
		var fragShaderCode = File.ReadAllBytes("shaders/frag.spv");

		var vertShaderModule = CreateShaderModule(s, vertShaderCode);
		var fragShaderModule = CreateShaderModule(s, fragShaderCode);

		PipelineShaderStageCreateInfo vertShaderStageInfo = new() {
			SType = StructureType.PipelineShaderStageCreateInfo,
			Stage = ShaderStageFlags.VertexBit,
			Module = vertShaderModule,
			PName = (byte*)SilkMarshal.StringToPtr("main")
		};

		PipelineShaderStageCreateInfo fragShaderStageInfo = new() {
			SType = StructureType.PipelineShaderStageCreateInfo,
			Stage = ShaderStageFlags.FragmentBit,
			Module = fragShaderModule,
			PName = (byte*)SilkMarshal.StringToPtr("main")
		};

		var shaderStages = stackalloc[] {
			vertShaderStageInfo,
			fragShaderStageInfo
		};

		var bindingDesc = Vertex.getBindingDescription();
		var attributeDesc = Vertex.getAttributeDescriptions();
		fixed (DescriptorSetLayout* descriptorSetLayout = &dl.descriptorSetLayout)
		fixed (VertexInputAttributeDescription* attDescP = attributeDesc) {

			PipelineVertexInputStateCreateInfo vertexInputInfo = new() {
				SType = StructureType.PipelineVertexInputStateCreateInfo,
				VertexBindingDescriptionCount = 1,
				VertexAttributeDescriptionCount = (uint)attributeDesc.Length,
				PVertexBindingDescriptions = &bindingDesc,
				PVertexAttributeDescriptions = attDescP,
			};

			PipelineInputAssemblyStateCreateInfo inputAssembly = new() {
				SType = StructureType.PipelineInputAssemblyStateCreateInfo,
				Topology = PrimitiveTopology.TriangleList,
				PrimitiveRestartEnable = false,
			};

			Viewport viewport = new() {
				X = 0,
				Y = 0,
				Width = sc.swapChainExtent.Width,
				Height = sc.swapChainExtent.Height,
				MinDepth = 0,
				MaxDepth = 1,
			};

			Rect2D scissor = new() {
				Offset = { X = 0, Y = 0 },
				Extent = sc.swapChainExtent,
			};

			PipelineViewportStateCreateInfo viewportState = new() {
				SType = StructureType.PipelineViewportStateCreateInfo,
				ViewportCount = 1,
				PViewports = &viewport,
				ScissorCount = 1,
				PScissors = &scissor,
			};

			PipelineRasterizationStateCreateInfo rasterizer = new() {
				SType = StructureType.PipelineRasterizationStateCreateInfo,
				DepthClampEnable = false,
				RasterizerDiscardEnable = false,
				PolygonMode = PolygonMode.Fill,
				LineWidth = 1,
				CullMode = CullModeFlags.BackBit,
				FrontFace = FrontFace.CounterClockwise,
				DepthBiasEnable = false,
			};

			PipelineMultisampleStateCreateInfo multisampling = new() {
				SType = StructureType.PipelineMultisampleStateCreateInfo,
				SampleShadingEnable = false,
				RasterizationSamples = SampleCountFlags.Count1Bit,
			};

			PipelineColorBlendAttachmentState colorBlendAttachment = new() {
				ColorWriteMask = ColorComponentFlags.RBit
					| ColorComponentFlags.GBit | ColorComponentFlags.BBit
					| ColorComponentFlags.ABit,
				BlendEnable = false,
			};

			PipelineColorBlendStateCreateInfo colorBlending = new() {
				SType = StructureType.PipelineColorBlendStateCreateInfo,
				LogicOpEnable = false,
				LogicOp = LogicOp.Copy,
				AttachmentCount = 1,
				PAttachments = &colorBlendAttachment,
			};

			colorBlending.BlendConstants[0] = 0;
			colorBlending.BlendConstants[1] = 0;
			colorBlending.BlendConstants[2] = 0;
			colorBlending.BlendConstants[3] = 0;

			PipelineLayoutCreateInfo pipelineLayoutInfo = new() {
				SType = StructureType.PipelineLayoutCreateInfo,
				SetLayoutCount = 1,
				PSetLayouts = descriptorSetLayout,
				PushConstantRangeCount = 0,
			};

			if (s.vk!.CreatePipelineLayout
				(s.device, in pipelineLayoutInfo, null, out pipelineLayout)
				!= Result.Success) {
				throw new Exception("failed to create pipeline layout!");
			}

			GraphicsPipelineCreateInfo pipelineInfo = new() {
				SType = StructureType.GraphicsPipelineCreateInfo,
				StageCount = 2,
				PStages = shaderStages,
				PVertexInputState = &vertexInputInfo,
				PInputAssemblyState = &inputAssembly,
				PViewportState = &viewportState,
				PRasterizationState = &rasterizer,
				PMultisampleState = &multisampling,
				PColorBlendState = &colorBlending,
				Layout = pipelineLayout,
				RenderPass = rp.renderPass,
				Subpass = 0,
				BasePipelineHandle = default
			};

			if (s.vk!.CreateGraphicsPipelines
				(s.device, default, 1, in pipelineInfo, null, out graphicsPipeline)
				!= Result.Success) {
				throw new Exception("failed to create graphics pipeline!");
			}
		}

		s.vk!.DestroyShaderModule(s.device, fragShaderModule, null);
		s.vk!.DestroyShaderModule(s.device, vertShaderModule, null);

		SilkMarshal.Free((nint)vertShaderStageInfo.PName);
		SilkMarshal.Free((nint)fragShaderStageInfo.PName);
	}

	private ShaderModule CreateShaderModule(VKSetup s, byte[] code) {
		ShaderModuleCreateInfo createInfo = new() {
			SType = StructureType.ShaderModuleCreateInfo,
			CodeSize = (nuint)code.Length,
		};

		ShaderModule shaderModule;

		fixed (byte* codePtr = code) {
			createInfo.PCode = (uint*)codePtr;

			if (s.vk!.CreateShaderModule
				(s.device, in createInfo, null, out shaderModule)
				!= Result.Success) {
				throw new Exception("Failed to create shader module");
			}
		}

		return shaderModule;

	}

	public override void clear(VKSetup s) {
		s.vk!.DestroyPipeline(s.device, graphicsPipeline, null);
		s.vk!.DestroyPipelineLayout(s.device, pipelineLayout, null);
	}
}


