// Ignore Spelling: img

using SLImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using _150_trying.utils;
using Sampler = Silk.NET.Vulkan.Sampler;

namespace _150_trying.VKComponents;

public unsafe class VKTextureSampler : VKComponent {
	public Sampler sampler;

	public override void init(VKSetup s) {
		s.vk.GetPhysicalDeviceProperties(s.physicalDevice, out var properties);

		SamplerCreateInfo si = new() {
			SType = StructureType.SamplerCreateInfo,
			MagFilter = Filter.Linear,
			MinFilter = Filter.Linear,
			AddressModeU = SamplerAddressMode.Repeat,
			AddressModeV = SamplerAddressMode.Repeat,
			AddressModeW = SamplerAddressMode.Repeat,
			AnisotropyEnable = true,
			MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
			BorderColor = BorderColor.IntOpaqueBlack,
			UnnormalizedCoordinates = false,
			CompareEnable = false,
			CompareOp = CompareOp.Always,
			MipmapMode = SamplerMipmapMode.Linear,
			MipLodBias = 0f,
			MinLod = 0f,
			MaxLod = 0f,
		};

		s.vk.CreateSampler(s.device, in si, null, out sampler)
			.throwOnFail("Failed to create a sampler.");
		
	}

	public override void clear(VKSetup s) {
		s.vk.DestroySampler(s.device, sampler, null);
	}
}

public unsafe class VKTextureImageView : VKComponent {
	public ImageView view;

	public override void init(VKSetup s) {
		var ti = s.require<VKTextureImage>();
		view = VKImageViews.createImageView(s, ti.textureImage, ti.format);
	}
	
	public override void clear(VKSetup s) {
		s.vk!.DestroyImageView(s.device, view, null);
	}

}

public unsafe class VKTextureImage : VKComponent {
	public Image textureImage;
	DeviceMemory textureImageMemory;
	public Format format { get; } = Format.R8G8B8A8Srgb;

	public override void init(VKSetup s) {
		using var img = SLImage.Load<Rgba32>("textures/texture.jpg");
		var imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);

		DeviceMemory stagingBufferMemory = default;
		s.createBuffer(imageSize, BufferUsageFlags.TransferSrcBit
			, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
			, out var stagingBuffer, out stagingBufferMemory);

		void* data;
		s.vk.MapMemory(s.device, stagingBufferMemory, 0, imageSize, 0, &data);
		img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
		s.vk!.UnmapMemory(s.device, stagingBufferMemory);


		createImage(s, img.Width, img.Height
			, out textureImage, out textureImageMemory);

		
		transitionImageLayout(s, textureImage, format
			, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
		copyBufferToImage(s, stagingBuffer, textureImage
			, (uint)img.Width, (uint)img.Height);
		transitionImageLayout(s, textureImage, format
			, ImageLayout.TransferDstOptimal
			, ImageLayout.ShaderReadOnlyOptimal);

		s.vk.DestroyBuffer(s.device, stagingBuffer, null);
		s.vk.FreeMemory(s.device, stagingBufferMemory, null);
	}

	public static void createImage(VKSetup s, int w, int h
		, out Image image
		, out DeviceMemory imageMemory
		, ImageUsageFlags usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit
		, Format format = Format.R8G8B8A8Srgb
		, ImageTiling tiling = ImageTiling.Optimal
		, ImageLayout layout = ImageLayout.Undefined)
	{
		ImageCreateInfo imageInfo = new() {
			SType = StructureType.ImageCreateInfo,
			ImageType = ImageType.Type2D,
			Extent = {
				Width = (uint)w,
				Height = (uint)h,
				Depth = 1,
			},
			MipLevels = 1,
			ArrayLayers = 1,
			Format = format,
			Tiling = tiling,
			InitialLayout = layout,
			Usage = ImageUsageFlags.TransferDstBit
				| ImageUsageFlags.SampledBit,
			Samples = SampleCountFlags.Count1Bit,
			SharingMode = SharingMode.Exclusive,
		};

		s.vk.CreateImage(s.device
			, in imageInfo, null, out image)
			.throwOnFail("Failed to create image.");


		MemoryRequirements memReq;
		s.vk.GetImageMemoryRequirements(s.device, image, &memReq);

		MemoryAllocateInfo allocInfo = new() {
			SType = StructureType.MemoryAllocateInfo,
			AllocationSize = memReq.Size,
			MemoryTypeIndex = s.FindMemoryType
				(memReq.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
		};

		s.vk.AllocateMemory(s.device, in allocInfo, null, out imageMemory)
			.throwOnFail("failed to allocate image memory!");

		s.vk.BindImageMemory(s.device, image, imageMemory, 0);
	}

	public static void transitionImageLayout(VKSetup s, Image img, Format f
		, ImageLayout oldL, ImageLayout newL)
	{
		var cb = s.beginSingleTimeCommands();

		ImageMemoryBarrier barrier = new (){
			SType = StructureType.ImageMemoryBarrier,
			OldLayout = oldL,
			NewLayout = newL,
			SrcQueueFamilyIndex = 0, //? VK_QUEUE_FAMILY_IGNORED
			DstQueueFamilyIndex = 0, //VK_QUEUE_FAMILY_IGNORED
			Image = img,
			SubresourceRange = {
				AspectMask = ImageAspectFlags.ColorBit,
				BaseMipLevel = 0,
				LevelCount = 1,
				BaseArrayLayer = 0,
				LayerCount = 1,
			},
			SrcAccessMask = 0, // TODO
			DstAccessMask = 0, // TODO
		};

		PipelineStageFlags sourceStage;
		PipelineStageFlags destinationStage;
		if (oldL == ImageLayout.Undefined
			&& newL == ImageLayout.TransferDstOptimal)
		{
			barrier.SrcAccessMask = 0;
			barrier.DstAccessMask = AccessFlags.TransferWriteBit;
			sourceStage = PipelineStageFlags.TopOfPipeBit;
			destinationStage = PipelineStageFlags.TransferBit;
		} else if(oldL == ImageLayout.TransferDstOptimal
			&& newL == ImageLayout.ShaderReadOnlyOptimal)
		{
			barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
			barrier.DstAccessMask = AccessFlags.ShaderReadBit;
			sourceStage = PipelineStageFlags.TransferBit;
			destinationStage = PipelineStageFlags.FragmentShaderBit;
		} else throw new InvalidOperationException
			($@"Can't transition image layout form ""{oldL}"" to ""{newL}"".");


		s.vk.CmdPipelineBarrier(cb
			, sourceStage, destinationStage
			, 0
			, 0, null
			, 0, null
			, 1, in barrier
		);

		//vkCmdPipelineBarrier(
		//	commandBuffer,
		//	0 /* TODO */, 0 /* TODO */,
		//	0,
		//	0, nullptr,
		//	0, nullptr,
		//	1, &barrier
		//);

		s.endSingleTimeCommands(cb);
	}

	//void transitionImageLayout(VkImage image, VkFormat format
	//, VkImageLayout oldLayout, VkImageLayout newLayout)
	//{
	//	VkCommandBuffer commandBuffer = beginSingleTimeCommands();

	//	endSingleTimeCommands(commandBuffer);
	//}

	public static void copyBufferToImage(VKSetup s, Buffer buffer, Image image, uint width, uint height) {
		var cb = s.beginSingleTimeCommands();

		BufferImageCopy region = new() { 
			BufferOffset = 0,
			BufferRowLength = 0,
			BufferImageHeight = 0,
			ImageSubresource = {
				AspectMask = ImageAspectFlags.ColorBit,
				MipLevel = 0,
				BaseArrayLayer = 0,
				LayerCount = 1,
			},
			ImageOffset = new Offset3D(),
			ImageExtent = {
				Width = width,
				Height = height,
				Depth = 1
			}
		};

		s.vk.CmdCopyBufferToImage(cb, buffer, image,
			ImageLayout.TransferDstOptimal, regionCount: 1, in region);

		s.endSingleTimeCommands(cb);
	}


	public override void clear(VKSetup s) {
		s.vk.DestroyImage(s.device, textureImage, null);
		s.vk.FreeMemory(s.device, textureImageMemory, null);
	}

}
