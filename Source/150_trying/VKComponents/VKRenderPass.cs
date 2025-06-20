// Ignore Spelling: Utils Indices Vertices verts khr ubo Mem

using Silk.NET.Vulkan;

namespace _150_trying.VKComponents;

public unsafe class VKRenderPass : VKComponent {
	public RenderPass renderPass;

	public override void init(VKSetup s) {
		var (sc, dr) = s.require<VKSwapChain, VKDepthResources>();
		AttachmentDescription colorAttachment = new() {
			Format = sc.swapChainImageFormat,
			Samples = s.msaaSamples,
			LoadOp = AttachmentLoadOp.Clear,
			StoreOp = AttachmentStoreOp.Store,
			StencilLoadOp = AttachmentLoadOp.DontCare,
			InitialLayout = ImageLayout.Undefined,
			FinalLayout = ImageLayout.ColorAttachmentOptimal, //ImageLayout.PresentSrcKhr,
		};

		AttachmentReference colorAttachmentRef = new() {
			Attachment = 0,
			Layout = ImageLayout.ColorAttachmentOptimal,
		};

		AttachmentDescription depthAttachment = new() {
			Format = dr.format,
			Samples = s.msaaSamples,
			LoadOp = AttachmentLoadOp.Clear,
			StoreOp = AttachmentStoreOp.DontCare,
			StencilLoadOp = AttachmentLoadOp.DontCare,
			InitialLayout = ImageLayout.Undefined,
			FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
		};

		AttachmentReference depthAttachmentRef = new() {
			Attachment = 1,
			Layout = ImageLayout.DepthStencilAttachmentOptimal,
		};

		AttachmentDescription colorAttachmentResolve = new() {
			Format = sc.swapChainImageFormat,
			Samples = SampleCountFlags.Count1Bit,
			LoadOp = AttachmentLoadOp.DontCare,
			StoreOp = AttachmentStoreOp.Store,
			StencilLoadOp = AttachmentLoadOp.DontCare,
			StencilStoreOp = AttachmentStoreOp.DontCare,
			InitialLayout = ImageLayout.Undefined,
			FinalLayout = ImageLayout.PresentSrcKhr,
		};

		AttachmentReference colorAttachmentResolveRef = new() {
			Attachment = 2,
			Layout = ImageLayout.ColorAttachmentOptimal,
		};


		SubpassDescription subPass = new() {
			PipelineBindPoint = PipelineBindPoint.Graphics,
			ColorAttachmentCount = 1,
			PColorAttachments = &colorAttachmentRef,
			PDepthStencilAttachment = &depthAttachmentRef,
			PResolveAttachments = &colorAttachmentResolveRef,
		};

		SubpassDependency dependency = new() {
			SrcSubpass = Vk.SubpassExternal,
			DstSubpass = 0,
			SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit
				| PipelineStageFlags.EarlyFragmentTestsBit,
			SrcAccessMask = 0,
			DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit
				| PipelineStageFlags.EarlyFragmentTestsBit,
			DstAccessMask = AccessFlags.ColorAttachmentWriteBit
				| AccessFlags.DepthStencilAttachmentWriteBit
		};

		var attachments = new[] 
			{ colorAttachment, depthAttachment, colorAttachmentResolve };
		fixed(AttachmentDescription* ap = attachments) {
			RenderPassCreateInfo renderPassInfo = new() {
				SType = StructureType.RenderPassCreateInfo,
				AttachmentCount = (uint)attachments.Length,
				PAttachments = ap,
				SubpassCount = 1,
				PSubpasses = &subPass,
				DependencyCount = 1,
				PDependencies = &dependency,
			};
			if (s.vk!.CreateRenderPass
				(s.device, in renderPassInfo, null, out renderPass) != Result.Success) {
				throw new Exception("failed to create render pass!");
			}
		}
	}

	public override void clear(VKSetup s) {
		s.vk!.DestroyRenderPass(s.device, renderPass, null);
	}

}


