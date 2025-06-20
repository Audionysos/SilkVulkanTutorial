// Ignore Spelling: img mip Mipmaps

using _150_trying.utils;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Threading;
using Buffer = Silk.NET.Vulkan.Buffer;
using Sampler = Silk.NET.Vulkan.Sampler;
using SLImage = SixLabors.ImageSharp.Image;

namespace _150_trying.VKComponents;

public unsafe class VKTextureSampler : VKComponent {
	public Sampler sampler;

	public override void init(VKSetup s) {
		var ti = s.require<VKTextureImage>();
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
			MinLod = 0,
			//MinLod = ti.mipLevels / 2,
			MaxLod = ti.mipLevels,
			MipLodBias = 0f,
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
		view = VKImageViews.createImageView(s, ti.textureImage, ti.format
			, mipLevels:ti.mipLevels);
	}
	
	public override void clear(VKSetup s) {
		s.vk!.DestroyImageView(s.device, view, null);
	}

}

public unsafe class VKTextureImage : VKComponent {
	public Image textureImage;
	DeviceMemory textureImageMemory;
	public Format format { get; } = Format.R8G8B8A8Srgb;
	public uint mipLevels;

	public override void init(VKSetup s) {
		var tf = s.require<VKModelLoading>().TEXTURE_PATH;
		using var img = SLImage.Load<Rgba32>(tf);
		//using var img = SLImage.Load<Rgba32>("textures/texture.jpg");
		var imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);
		mipLevels = (uint)Math.Floor(Math.Log2(Math.Max(img.Width, img.Height))) + 1;

		DeviceMemory stagingBufferMemory = default;
		s.createBuffer(imageSize, BufferUsageFlags.TransferSrcBit
			, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
			, out var stagingBuffer, out stagingBufferMemory);

		void* data;
		s.vk.MapMemory(s.device, stagingBufferMemory, 0, imageSize, 0, &data);
		img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
		s.vk!.UnmapMemory(s.device, stagingBufferMemory);


		createImage(s, img.Width, img.Height
			, out textureImage, out textureImageMemory
			, mipLevels:mipLevels
			, usage: ImageUsageFlags.TransferDstBit
				| ImageUsageFlags.TransferSrcBit
				| ImageUsageFlags.SampledBit
		);


		transitionImageLayout(s, textureImage, format
			, ImageLayout.Undefined, ImageLayout.TransferDstOptimal
			, mipLevels: mipLevels);
		copyBufferToImage(s, stagingBuffer, textureImage
			, (uint)img.Width, (uint)img.Height);
		generateMipmaps(s, textureImage, img.Width, img.Height, mipLevels);

		s.vk.DestroyBuffer(s.device, stagingBuffer, null);
		s.vk.FreeMemory(s.device, stagingBufferMemory, null);
	}

	public static void generateMipmaps(VKSetup s, Image image, int w, int h, uint mipLevels
		, Format format = Format.R8G8B8A8Srgb)
	{
		s.vk.GetPhysicalDeviceFormatProperties(s.physicalDevice, format, out var prs);
		if ((prs.OptimalTilingFeatures & FormatFeatureFlags.SampledImageFilterLinearBit) == 0)
			throw new NotSupportedException("Texture image format does not support linear blitting.");

		var cb = s.beginSingleTimeCommands();
		ImageMemoryBarrier br = new() {
			SType = StructureType.ImageMemoryBarrier,
			Image = image,
			SrcQueueFamilyIndex = 0, //VK_QUEUE_FAMILY_IGNORED
			DstQueueFamilyIndex = 0, //VK_QUEUE_FAMILY_IGNORED
			SubresourceRange = {
				AspectMask = ImageAspectFlags.ColorBit,
				BaseArrayLayer = 0,
				LayerCount = 1,
				LevelCount = 1
			},
		};

		int mipWidth = w;
		int mipHeight = h;

		for (uint i = 1; i < mipLevels; i++) {
			br.SubresourceRange.BaseMipLevel = i - 1;
			br.OldLayout = ImageLayout.TransferDstOptimal;
			br.NewLayout = ImageLayout.TransferSrcOptimal;
			br.SrcAccessMask = AccessFlags.TransferWriteBit;
			br.DstAccessMask = AccessFlags.TransferReadBit;

			s.vk.CmdPipelineBarrier(cb, PipelineStageFlags.TransferBit
				, PipelineStageFlags.TransferBit, 0
				, 0, null
				, 0, null
				, 1, &br);


			ImageBlit blit = new() {
				SrcSubresource = {
					AspectMask = ImageAspectFlags.ColorBit,
					MipLevel = i - 1,
					BaseArrayLayer = 0,
					LayerCount = 1
				},
				SrcOffsets = {
					Element0 = new Offset3D(0,0,0),
					Element1 = new Offset3D(mipWidth, mipHeight, 1),
				},
				DstOffsets = {
					Element0 = new Offset3D(0,0,0),
					Element1 = new Offset3D(mipWidth > 1 ? mipWidth /2 : 1,
						mipHeight > 1 ? mipHeight / 2 : 1, 1),
				},
				DstSubresource = {
					AspectMask = ImageAspectFlags.ColorBit,
					MipLevel = i,
					BaseArrayLayer = 0,
					LayerCount = 1
				}
			};

			s.vk.CmdBlitImage(cb
				, image, ImageLayout.TransferSrcOptimal
				, image, ImageLayout.TransferDstOptimal
				, 1, &blit, Filter.Linear);

			br.OldLayout = ImageLayout.TransferSrcOptimal;
			br.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
			br.SrcAccessMask = AccessFlags.TransferReadBit;
			br.DstAccessMask = AccessFlags.ShaderReadBit;

			s.vk.CmdPipelineBarrier(cb, PipelineStageFlags.TransferBit
				, PipelineStageFlags.FragmentShaderBit, 0
				, 0, null
				, 0, null
				, 1, &br);

			if (mipWidth > 1) mipWidth /= 2;
			if (mipHeight > 1) mipHeight /= 2;

		}

		br.SubresourceRange.BaseMipLevel = mipLevels - 1;
		br.OldLayout = ImageLayout.TransferDstOptimal;// VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;
		br.NewLayout = ImageLayout.ShaderReadOnlyOptimal;// VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
		br.SrcAccessMask = AccessFlags.TransferWriteBit;// VK_ACCESS_TRANSFER_WRITE_BIT;
		br.DstAccessMask = AccessFlags.ShaderReadBit;// VK_ACCESS_SHADER_READ_BIT;

		s.vk.CmdPipelineBarrier(cb, PipelineStageFlags.TransferBit
			, PipelineStageFlags.FragmentShaderBit,0
			, 0, null
			, 0, null
			, 1, &br);

		s.endSingleTimeCommands(cb);
	}

	public static void createImage(VKSetup s, int w, int h
		, out Image image
		, out DeviceMemory imageMemory
		, ImageUsageFlags usage = ImageUsageFlags.TransferDstBit
		| ImageUsageFlags.SampledBit
		, Format format = Format.R8G8B8A8Srgb
		, ImageTiling tiling = ImageTiling.Optimal
		, ImageLayout layout = ImageLayout.Undefined
		, MemoryPropertyFlags properties = MemoryPropertyFlags.DeviceLocalBit
		, uint mipLevels = 1
		, SampleCountFlags samples = SampleCountFlags.Count1Bit)
	{
		ImageCreateInfo imageInfo = new() {
			SType = StructureType.ImageCreateInfo,
			ImageType = ImageType.Type2D,
			Extent = {
				Width = (uint)w,
				Height = (uint)h,
				Depth = 1,
			},
			MipLevels = mipLevels,
			ArrayLayers = 1,
			Format = format,
			Tiling = tiling,
			InitialLayout = layout,
			Usage = usage,
			Samples = samples,
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
				(memReq.MemoryTypeBits, properties)
		};

		s.vk.AllocateMemory(s.device, in allocInfo, null, out imageMemory)
			.throwOnFail("failed to allocate image memory!");

		s.vk.BindImageMemory(s.device, image, imageMemory, 0);
	}

	public static void transitionImageLayout(VKSetup s, Image img, Format f
		, ImageLayout oldL, ImageLayout newL
		, uint mipLevels = 1)
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
				LevelCount = mipLevels,
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

		s.endSingleTimeCommands(cb);
	}


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
