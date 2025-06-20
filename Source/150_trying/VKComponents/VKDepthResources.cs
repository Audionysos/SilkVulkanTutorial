// Ignore Spelling: Utils Indices Vertices verts khr ubo Mem

using Silk.NET.Vulkan;
using System.Diagnostics;
using Image = Silk.NET.Vulkan.Image;

namespace _150_trying.VKComponents;

/// <summary>https://vulkan-tutorial.com/Depth_buffering</summary>
public unsafe class VKDepthResources : VKComponent {
	public Image image;
	public DeviceMemory imageMem;
	public ImageView imageView;
	Format _format;
	public Format format {
		[DebuggerHidden]
		get => initialized ? _format : throw new InvalidOperationException("The component was not yet initialized");
		private set => _format = value;
	}

	public override void init(VKSetup s) {
		var sc = s.require<VKSwapChain>();
		format = findDepthFormat(s);
		VKTextureImage.createImage(s
			, (int)sc.swapChainExtent.Width, (int)sc.swapChainExtent.Height
			, out image, out imageMem
			, ImageUsageFlags.DepthStencilAttachmentBit
			, _format, samples:s.msaaSamples);
		imageView = VKImageViews.createImageView(s, image, _format
			, ImageAspectFlags.DepthBit);

		//createImage(swapChainExtent.width, swapChainExtent.height
		//	, 1, msaaSamples, depthFormat, VK_IMAGE_TILING_OPTIMAL
		//	, VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT
		//	, VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT
		//	, depthImage, depthImageMemory);
	}

	public Format findDepthFormat(VKSetup s) {
		return findSupportedFormat(s, [Format.D32Sfloat
			, Format.D32SfloatS8Uint, Format.D24UnormS8Uint]
			, ImageTiling.Optimal
			, FormatFeatureFlags.DepthStencilAttachmentBit);

	}

	public override void clear(VKSetup s) {
		s.vk.DestroyImage(s.device, image, null);
		s.vk.DestroyImageView(s.device, imageView, null);
		s.vk.FreeMemory(s.device, imageMem, null);
	}

	private bool hasStencilComponent(Format format) {
		return format == Format.D32SfloatS8Uint
			|| format == Format.D24UnormS8Uint;
	}

	public static Format findSupportedFormat(VKSetup s, Format[] candidates
		, ImageTiling tiling = ImageTiling.Optimal
		, FormatFeatureFlags features = FormatFeatureFlags.DepthStencilAttachmentBit)
	{
		foreach (Format format in candidates) {
			s.vk.GetPhysicalDeviceFormatProperties(s.physicalDevice
				, format, out var props);

			if (tiling ==  ImageTiling.Linear 
				&& (props.LinearTilingFeatures & features) == features) {
				return format;
			} else if (tiling == ImageTiling.Optimal
				&& (props.OptimalTilingFeatures & features) == features) {
				return format;
			}
		}
		return Format.Undefined;
	}

	
}


