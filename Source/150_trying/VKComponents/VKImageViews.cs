// Ignore Spelling: Utils Indices Vertices verts khr ubo Mem mip

using _150_trying.utils;
using Silk.NET.Vulkan;
using Image = Silk.NET.Vulkan.Image;

namespace _150_trying.VKComponents;

public unsafe class VKImageViews : VKComponent {
	public ImageView[]? swapChainImageViews;

	public override void init(VKSetup s) {
		var sc = s.require<VKSwapChain>();
		swapChainImageViews = new ImageView[sc.swapChainImages!.Length];

		for (int i = 0; i < sc.swapChainImages.Length; i++) {
			swapChainImageViews[i] = createImageView(s, sc.swapChainImages[i]
				, sc.swapChainImageFormat);
		}
	}

	public static ImageView createImageView(VKSetup s, Image image, Format format
		, ImageAspectFlags aspect = ImageAspectFlags.ColorBit
		, uint mipLevels = 1)
	{
		ImageViewCreateInfo ci = new() {
			SType = StructureType.ImageViewCreateInfo,
			Image = image,
			ViewType = ImageViewType.Type2D,
			Format = format,
			SubresourceRange = {
				AspectMask = aspect,
				BaseMipLevel = 0,
				LevelCount = mipLevels,
				BaseArrayLayer = 0,
				LayerCount = 1,
			},
		};
		s.vk.CreateImageView(s.device, in ci, null, out var imageView)
			.throwOnFail("Failed to create image view!");
		return imageView;
	}

	public override void clear(VKSetup s) {
		foreach (var imageView in swapChainImageViews!) {
			s.vk!.DestroyImageView(s.device, imageView, null);
		}
	}
}


