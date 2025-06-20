// Ignore Spelling: Utils Indices Vertices verts khr ubo Mem img

using Silk.NET.Vulkan;
using Image = Silk.NET.Vulkan.Image;

namespace _150_trying.VKComponents;

public unsafe class VKColorResources : VKComponent {
	Image image;
	DeviceMemory imgMem;
	public ImageView imgView;

	public override void init(VKSetup s) {
		var sc = s.require<VKSwapChain>();
		var sce = sc.swapChainExtent;

		VKTextureImage.createImage(s, (int)sce.Width, (int)sce.Height
			, out image, out imgMem
			, format: sc.swapChainImageFormat
			, usage: ImageUsageFlags.TransientAttachmentBit
				| ImageUsageFlags.ColorAttachmentBit
			, properties: MemoryPropertyFlags.DeviceLocalBit
			, samples: s.msaaSamples);

		imgView = VKImageViews.createImageView(s, image, sc.swapChainImageFormat
			, ImageAspectFlags.ColorBit);
	}
	
	public override void clear(VKSetup s) {
		s.vk.DestroyImage(s.device, image, null);
		s.vk.DestroyImageView(s.device, imgView, null);
		s.vk.FreeMemory(s.device, imgMem, null);
	}
}


