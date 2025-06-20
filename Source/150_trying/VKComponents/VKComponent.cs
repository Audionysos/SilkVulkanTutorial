// Ignore Spelling: Utils Indices Vertices verts khr ubo Mem

using _150_trying.geom;
using _150_trying.utils;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.OpenAL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using SixLabors.ImageSharp;
using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace _150_trying.VKComponents;

public abstract class VKComponent {
	protected bool _i;
	public bool initialized { get => _i; set => _i = value || _i; }
	public abstract void init(VKSetup s);
	public abstract void clear(VKSetup s);

}

/// <summary>https://vulkan-tutorial.com/en/Uniform_buffers/Descriptor_pool_and_sets</summary>
public unsafe class VKDescriptorPool : VKComponent {
	DescriptorPool descriptorPool;
	public DescriptorSet[] descriptorSets = new DescriptorSet[VKSetup.MAX_FRAMES_IN_FLIGHT];

	public override void init(VKSetup s) {
		var poolSizes = new[] {
			 new DescriptorPoolSize {
				Type = DescriptorType.UniformBuffer,
				DescriptorCount = VKSetup.MAX_FRAMES_IN_FLIGHT
			 },
			 new DescriptorPoolSize {
				Type = DescriptorType.CombinedImageSampler,
				DescriptorCount = VKSetup.MAX_FRAMES_IN_FLIGHT
			 },
		};

		fixed (DescriptorPoolSize* sizes = poolSizes) {
			DescriptorPoolCreateInfo poolInfo = new() {
				SType = StructureType.DescriptorPoolCreateInfo,
				PoolSizeCount = (uint)poolSizes.Length,
				PPoolSizes = sizes,
				MaxSets = VKSetup.MAX_FRAMES_IN_FLIGHT,
			};
			s.vk.CreateDescriptorPool(s.device
				, in poolInfo, null, out descriptorPool)
				.throwOnFail("Failed to create descriptor pool.");
		}


		createDescriptorSets(s);
	}

	private void createDescriptorSets(VKSetup s) {
		var (sl, ub) = s.require<VKDescriptorSetLayout, VKUniformBuffers>();
		var layouts = new DescriptorSetLayout[VKSetup.MAX_FRAMES_IN_FLIGHT];
		Array.Fill(layouts, sl.descriptorSetLayout);


		fixed (DescriptorSet* descriptorSetsPtr = descriptorSets)
		fixed (DescriptorSetLayout* layoutsPtr = layouts) {
			DescriptorSetAllocateInfo allocInfo = new() {
				SType = StructureType.DescriptorSetAllocateInfo,
				DescriptorPool = descriptorPool,
				DescriptorSetCount = VKSetup.MAX_FRAMES_IN_FLIGHT,
				PSetLayouts = layoutsPtr,
			};

			s.vk.AllocateDescriptorSets(s.device,
				in allocInfo, descriptorSetsPtr)
				.throwOnFail("Failed to allocate descriptor sets.");

		}

		for (var i = 0; i < VKSetup.MAX_FRAMES_IN_FLIGHT; i++) {
			DescriptorBufferInfo bufferInfo = new() {
				Buffer = ub.all[i].buffer,
				Offset = 0,
				Range = (ulong)Marshal.SizeOf<UniformBufferObject>(),
			};

			DescriptorImageInfo ii = new() {
				ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
				ImageView = s.require<VKTextureImageView>().view,
				Sampler = s.require<VKTextureSampler>().sampler,
			};

			var descWrites = new []{
				new WriteDescriptorSet{
					SType = StructureType.WriteDescriptorSet,
					DstSet = descriptorSets[i],
					DstBinding = 0,
					DstArrayElement = 0,
					DescriptorType = DescriptorType.UniformBuffer,
					DescriptorCount = 1,
					PBufferInfo = &bufferInfo,
					PTexelBufferView = null,
				},
				new WriteDescriptorSet{
					SType = StructureType.WriteDescriptorSet,
					DstSet = descriptorSets[i],
					DstBinding = 1,
					DstArrayElement = 0,
					DescriptorType = DescriptorType.CombinedImageSampler,
					DescriptorCount = 1,
					PImageInfo = &ii,
				},
			};

			fixed(WriteDescriptorSet* dwp = descWrites) {
				s.vk.UpdateDescriptorSets(s.device,
					(uint)descWrites.Length, dwp, 0, null);
			}
		}
	}

	public override void clear(VKSetup s) {
		s.vk.DestroyDescriptorPool(s.device, descriptorPool, null);
	}

}

//--------^^^^^^^^^^^^^^^^^^^^^^^^^^
//--------vvvvvvvvvvvvvvvvvvvvvvvvv

public unsafe class VKDebugMessenger : VKComponent {
	public ExtDebugUtils? debugUtils;
	public DebugUtilsMessengerEXT debugMessenger;
	public List<string> errors = new ();

	public override void init(VKSetup s) {
		if (!s.EnableValidationLayers) return;

		//TryGetInstanceExtension equivalent to method CreateDebugUtilsMessengerEXT from original tutorial.
		if (!s.vk!.TryGetInstanceExtension(s.instance, out debugUtils)) return;

		DebugUtilsMessengerCreateInfoEXT createInfo = new();
		PopulateDebugMessengerCreateInfo(ref createInfo);

		debugUtils!.CreateDebugUtilsMessenger(s.instance, in createInfo
			, null, out debugMessenger)
			.throwOnFail("failed to set up debug messenger!");
	}

	private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo) {
		createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
		createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
									 DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
									 DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
		createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
								 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
								 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
		createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)
			DebugCallback;
	}

	private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity
		, DebugUtilsMessageTypeFlagsEXT messageTypes
		, DebugUtilsMessengerCallbackDataEXT* pCallbackData
		, void* pUserData) {
		var e = Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage);
		Debug.WriteLine($"ERROR VK:" + e);
		errors.Add(e!);
		return Vk.False;
	}

	public override void clear(VKSetup s) {
		if (s.EnableValidationLayers) {
			//DestroyDebugUtilsMessenger equivalent to method DestroyDebugUtilsMessengerEXT from original tutorial.
			debugUtils!.DestroyDebugUtilsMessenger(s.instance, debugMessenger, null);
		}
	}

}

public unsafe class VKInstance : VKComponent {

	//this is also called in Debug messenger - mistake?
	private void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo) {
		createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
		createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
									 DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
									 DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
		createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
								 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
								 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
		createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)
			DebugCallback;
	}
	private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData) {
		Debug.WriteLine($"Vulkan:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));
		return Vk.False;
	}

	public override void init(VKSetup s) {
		s.vk = Vk.GetApi();

		if (s.EnableValidationLayers && !CheckValidationLayerSupport(s)) {
			throw new Exception("validation layers requested, but not available!");
		}

		ApplicationInfo appInfo = new() {
			SType = StructureType.ApplicationInfo,
			PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
			ApplicationVersion = new Version32(1, 0, 0),
			PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
			EngineVersion = new Version32(1, 0, 0),
			ApiVersion = Vk.Version12
		};

		InstanceCreateInfo createInfo = new() {
			SType = StructureType.InstanceCreateInfo,
			PApplicationInfo = &appInfo
		};

		var extensions = GetRequiredExtensions(s);
		createInfo.EnabledExtensionCount = (uint)extensions.Length;
		createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions); ;

		if (s.EnableValidationLayers) {
			createInfo.EnabledLayerCount = (uint)s.validationLayers.Length;
			createInfo.PpEnabledLayerNames = (byte**)SilkMarshal
				.StringArrayToPtr(s.validationLayers);

			DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
			PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
			createInfo.PNext = &debugCreateInfo;
		} else {
			createInfo.EnabledLayerCount = 0;
			createInfo.PNext = null;
		}

		if (s.vk.CreateInstance(in createInfo, null, out var instance) != Result.Success) {
			throw new Exception("failed to create instance!");
		} else {
			s.instance = instance;
		}

		Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
		Marshal.FreeHGlobal((nint)appInfo.PEngineName);
		SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

		if (s.EnableValidationLayers) {
			SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
		}
	}

	private bool CheckValidationLayerSupport(VKSetup s) {
		uint layerCount = 0;
		s.vk!.EnumerateInstanceLayerProperties(ref layerCount, null);
		var availableLayers = new LayerProperties[layerCount];
		fixed (LayerProperties* availableLayersPtr = availableLayers) {
			s.vk!.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
		}

		var availableLayerNames = availableLayers
			.Select(layer => Marshal.PtrToStringAnsi((nint)layer.LayerName))
			.ToHashSet();

		return s.validationLayers.All(availableLayerNames.Contains);
	}

	private string[] GetRequiredExtensions(VKSetup s) {
		var glfwExtensions = s.window!.VkSurface!
			.GetRequiredExtensions(out var glfwExtensionCount);
		var extensions = SilkMarshal.PtrToStringArray
			((nint)glfwExtensions, (int)glfwExtensionCount);

		if (s.EnableValidationLayers) {
			return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
		}

		return extensions;
	}

	public override void clear(VKSetup s) {
		s.vk!.DestroyInstance(s.instance, null);
	}
}

public unsafe class VKSurface : VKComponent {
	public SurfaceKHR surface;
	public KhrSurface? khrSurface;

	public override void init(VKSetup s) {
		if (!s.vk!.TryGetInstanceExtension
			<KhrSurface>(s.instance, out khrSurface)) {
			throw new NotSupportedException("KHR_surface extension not found.");
		}

		surface = s.window!.VkSurface!
			.Create<AllocationCallbacks>(s.instance.ToHandle(), null).ToSurface();
	}

	public override void clear(VKSetup s) {
		khrSurface!.DestroySurface(s.instance, surface, null);
	}
}

public unsafe class VKDevicePicker : VKComponent {
	public override void init(VKSetup s) {
		uint devicedCount = 0;
		s.vk!.EnumeratePhysicalDevices(s.instance, ref devicedCount, null);

		if (devicedCount == 0) {
			throw new Exception("failed to find GPUs with Vulkan support!");
		}

		var devices = new PhysicalDevice[devicedCount];
		fixed (PhysicalDevice* devicesPtr = devices) {
			s.vk!.EnumeratePhysicalDevices(s.instance, ref devicedCount, devicesPtr);
		}

		foreach (var d in devices) {
			if (IsDeviceSuitable(s, d)) {
				s.physicalDevice = d;
				s.msaaSamples = s.getMaxUsableSampleCount();
				break;
			}
		}

		if (s.physicalDevice.Handle == 0) {
			throw new Exception("failed to find a suitable GPU!");
		}
	}

	private bool IsDeviceSuitable(VKSetup s, PhysicalDevice device) {
		var indices = FindQueueFamilies(s, device);

		bool extensionsSupported = CheckDeviceExtensionsSupport(s, device);

		bool swapChainAdequate = false;
		if (extensionsSupported) {
			var swapChainSupport = QuerySwapChainSupport(s, device);
			swapChainAdequate = swapChainSupport.Formats.Any()
				&& swapChainSupport.PresentModes.Any();
		}

		s.vk.GetPhysicalDeviceFeatures(device, out var supportedFeatures);

		return indices.IsComplete() && extensionsSupported && swapChainAdequate
			&& supportedFeatures.SamplerAnisotropy;
	}

	private bool CheckDeviceExtensionsSupport(VKSetup s, PhysicalDevice device) {
		uint extentionsCount = 0;
		s.vk!.EnumerateDeviceExtensionProperties
			(device, (byte*)null, ref extentionsCount, null);

		var availableExtensions = new ExtensionProperties[extentionsCount];
		fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions) {
			s.vk!.EnumerateDeviceExtensionProperties
				(device, (byte*)null, ref extentionsCount, availableExtensionsPtr);
		}

		var availableExtensionNames = availableExtensions
			.Select(extension => Marshal.PtrToStringAnsi
			((nint)extension.ExtensionName)).ToHashSet();

		return s.deviceExtensions.All(availableExtensionNames.Contains);
	}

	public SwapChainSupportDetails QuerySwapChainSupport(VKSetup st, PhysicalDevice physicalDevice) {
		var s = st.require<VKSurface>();
		var details = new SwapChainSupportDetails();

		s.khrSurface!.GetPhysicalDeviceSurfaceCapabilities
			(physicalDevice, s.surface, out details.Capabilities);

		uint formatCount = 0;
		s.khrSurface.GetPhysicalDeviceSurfaceFormats
			(physicalDevice, s.surface, ref formatCount, null);

		if (formatCount != 0) {
			details.Formats = new SurfaceFormatKHR[formatCount];
			fixed (SurfaceFormatKHR* formatsPtr = details.Formats) {
				s.khrSurface.GetPhysicalDeviceSurfaceFormats
					(physicalDevice, s.surface, ref formatCount, formatsPtr);
			}
		} else {
			details.Formats = Array.Empty<SurfaceFormatKHR>();
		}

		uint presentModeCount = 0;
		s.khrSurface.GetPhysicalDeviceSurfacePresentModes
			(physicalDevice, s.surface, ref presentModeCount, null);

		if (presentModeCount != 0) {
			details.PresentModes = new PresentModeKHR[presentModeCount];
			fixed (PresentModeKHR* formatsPtr = details.PresentModes) {
				s.khrSurface.GetPhysicalDeviceSurfacePresentModes
					(physicalDevice, s.surface, ref presentModeCount, formatsPtr);
			}

		} else {
			details.PresentModes = Array.Empty<PresentModeKHR>();
		}

		return details;
	}

	public QueueFamilyIndices FindQueueFamilies(VKSetup s, PhysicalDevice device) {
		var sf = s.require<VKSurface>();
		var indices = new QueueFamilyIndices();

		uint queueFamilityCount = 0;
		s.vk!.GetPhysicalDeviceQueueFamilyProperties(device
			, ref queueFamilityCount, null);

		var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
		fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies) {
			s.vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
		}


		uint i = 0;
		foreach (var queueFamily in queueFamilies) {
			if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit)) {
				indices.GraphicsFamily = i;
			}

			sf.khrSurface!.GetPhysicalDeviceSurfaceSupport
				(device, i, sf.surface, out var presentSupport);

			if (presentSupport) {
				indices.PresentFamily = i;
			}

			if (indices.IsComplete()) {
				break;
			}

			i++;
		}

		return indices;
	}

	public override void clear(VKSetup s) {
		s.vk!.DestroyDevice(s.device, null);
	}

}

public unsafe class VKLogicalDevice : VKComponent {
	public override void init(VKSetup s) {
		var dp = s.require<VKDevicePicker>();
		var indices = dp.FindQueueFamilies(s, s.physicalDevice);

		var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value
			, indices.PresentFamily!.Value };
		uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

		using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
		var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

		float queuePriority = 1.0f;
		for (int i = 0; i < uniqueQueueFamilies.Length; i++) {
			queueCreateInfos[i] = new() {
				SType = StructureType.DeviceQueueCreateInfo,
				QueueFamilyIndex = uniqueQueueFamilies[i],
				QueueCount = 1
			};


			queueCreateInfos[i].PQueuePriorities = &queuePriority;
		}

		PhysicalDeviceFeatures deviceFeatures = new() {
			SamplerAnisotropy = true,
			SampleRateShading = true,
		};

		DeviceCreateInfo createInfo = new() {
			SType = StructureType.DeviceCreateInfo,
			QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
			PQueueCreateInfos = queueCreateInfos,

			PEnabledFeatures = &deviceFeatures,

			EnabledExtensionCount = (uint)s.deviceExtensions.Length,
			PpEnabledExtensionNames =
				(byte**)SilkMarshal.StringArrayToPtr(s.deviceExtensions)
		};

		if (s.EnableValidationLayers) {
			createInfo.EnabledLayerCount = (uint)s.validationLayers.Length;
			createInfo.PpEnabledLayerNames =
				(byte**)SilkMarshal.StringArrayToPtr(s.validationLayers);
		} else {
			createInfo.EnabledLayerCount = 0;
		}

		if (s.vk!.CreateDevice(s.physicalDevice, in createInfo, null, out s.device)
				!= Result.Success) {
			throw new Exception("failed to create logical device!");
		}

		s.vk!.GetDeviceQueue(s.device
			, indices.GraphicsFamily!.Value, 0, out s.graphicsQueue);
		s.vk!.GetDeviceQueue(s.device
			, indices.PresentFamily!.Value, 0, out s.presentQueue);

		if (s.EnableValidationLayers) {
			SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
		}

		SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
	}

	public override void clear(VKSetup s) {

	}
}

public unsafe class VKSwapChain : VKComponent {
	public KhrSwapchain? khrSwapChain;
	public SwapchainKHR swapChain;
	public Image[]? swapChainImages;
	public Format swapChainImageFormat;
	public Extent2D swapChainExtent;

	public override void init(VKSetup s) {
		var (dp, sf) = s.require<VKDevicePicker, VKSurface>();
		var swapChainSupport = dp.QuerySwapChainSupport(s, s.physicalDevice);

		var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
		var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
		var extent = ChooseSwapExtent(s, swapChainSupport.Capabilities);

		var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
		if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount) {
			imageCount = swapChainSupport.Capabilities.MaxImageCount;
		}

		SwapchainCreateInfoKHR creatInfo = new() {
			SType = StructureType.SwapchainCreateInfoKhr,
			Surface = sf.surface,

			MinImageCount = imageCount,
			ImageFormat = surfaceFormat.Format,
			ImageColorSpace = surfaceFormat.ColorSpace,
			ImageExtent = extent,
			ImageArrayLayers = 1,
			ImageUsage = ImageUsageFlags.ColorAttachmentBit,
		};

		var indices = dp.FindQueueFamilies(s, s.physicalDevice);
		var queueFamilyIndices = stackalloc[] {
			  indices.GraphicsFamily!.Value
			, indices.PresentFamily!.Value };

		if (indices.GraphicsFamily != indices.PresentFamily) {
			creatInfo = creatInfo with {
				ImageSharingMode = SharingMode.Concurrent,
				QueueFamilyIndexCount = 2,
				PQueueFamilyIndices = queueFamilyIndices,
			};
		} else {
			creatInfo.ImageSharingMode = SharingMode.Exclusive;
		}

		creatInfo = creatInfo with {
			PreTransform = swapChainSupport.Capabilities.CurrentTransform,
			CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
			PresentMode = presentMode,
			Clipped = true,

			OldSwapchain = default
		};

		if (!s.vk!.TryGetDeviceExtension(s.instance, s.device
			, out khrSwapChain)) {
			throw new NotSupportedException("VK_KHR_swapchain extension not found.");
		}

		if (khrSwapChain!.CreateSwapchain(s.device, in creatInfo, null
			, out swapChain) != Result.Success) {
			throw new Exception("failed to create swap chain!");
		}

		khrSwapChain.GetSwapchainImages(s.device, swapChain, ref imageCount, null);
		swapChainImages = new Image[imageCount];
		fixed (Image* swapChainImagesPtr = swapChainImages) {
			khrSwapChain.GetSwapchainImages
				(s.device, swapChain, ref imageCount, swapChainImagesPtr);
		}

		swapChainImageFormat = surfaceFormat.Format;
		swapChainExtent = extent;
	}

	private Extent2D ChooseSwapExtent(VKSetup s, SurfaceCapabilitiesKHR capabilities) {
		if (capabilities.CurrentExtent.Width != uint.MaxValue) {
			return capabilities.CurrentExtent;
		} else {
			var framebufferSize = s.window!.FramebufferSize;

			Extent2D actualExtent = new() {
				Width = (uint)framebufferSize.X,
				Height = (uint)framebufferSize.Y
			};

			actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
			actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

			return actualExtent;
		}
	}

	private SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats) {
		foreach (var availableFormat in availableFormats) {
			if (availableFormat.Format == Format.B8G8R8A8Srgb
				&& availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr) {
				return availableFormat;
			}
		}
		return availableFormats[0];
	}

	private PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes) {
		foreach (var availablePresentMode in availablePresentModes) {
			if (availablePresentMode == PresentModeKHR.MailboxKhr) {
				return availablePresentMode;
			}
		}
		return PresentModeKHR.FifoKhr;
	}

	public override void clear(VKSetup s) {
		khrSwapChain!.DestroySwapchain(s.device, swapChain, null);
	}
}

public unsafe class VKFrameBuffer : VKComponent {
	public Framebuffer[]? swapChainFrameBuffers;

	public override void init(VKSetup s) {
		var (sc, rp, iv, dr, cr) = s.require<VKSwapChain, VKRenderPass
			, VKImageViews, VKDepthResources, VKColorResources>();
		swapChainFrameBuffers = new Framebuffer[iv.swapChainImageViews!.Length];

		for (int i = 0; i < iv.swapChainImageViews.Length; i++) {
			var attachments = new[] {
				cr.imgView,
				dr.imageView,
				iv.swapChainImageViews[i],
			};
			fixed(ImageView* ap = attachments) {
				FramebufferCreateInfo framebufferInfo = new() {
					SType = StructureType.FramebufferCreateInfo,
					RenderPass = rp.renderPass,
					AttachmentCount = (uint)attachments.Length,
					PAttachments = ap,
					Width = sc.swapChainExtent.Width,
					Height = sc.swapChainExtent.Height,
					Layers = 1,
				};

				s.vk!.CreateFramebuffer(s.device, in framebufferInfo
					, null, out swapChainFrameBuffers[i])
					.throwOnFail("failed to create frame buffer!");
			}
		}
	}

	public override void clear(VKSetup s) {
		foreach (var framebuffer in swapChainFrameBuffers!) {
			s.vk!.DestroyFramebuffer(s.device, framebuffer, null);
		}
	}
}

public unsafe class VKCommandPool : VKComponent {
	public CommandPool commandPool;

	public override void init(VKSetup s) {
		var (fb, dp) = s.require<VKFrameBuffer, VKDevicePicker>();
		QueueFamilyIndices queueFamilyIndices =
			dp.FindQueueFamilies(s, s.physicalDevice);

		CommandPoolCreateInfo poolInfo = new() {
			SType = StructureType.CommandPoolCreateInfo,
			Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
			QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
		};

		if (s.vk!.CreateCommandPool(s.device, in poolInfo, null, out commandPool)
			!= Result.Success) {
			throw new Exception("failed to create command pool!");
		}
	}

	public override void clear(VKSetup s) {
		s.vk!.DestroyCommandPool(s.device, commandPool, null);
	}
}

public unsafe class VKSyncObjects : VKComponent {
	public Semaphore[]? imageAvailableSemaphores;
	public Semaphore[]? renderFinishedSemaphores;
	public Fence[]? inFlightFences;
	public Fence[]? imagesInFlight;

	public override void init(VKSetup s) {
		var sc = s.require<VKSwapChain>();
		imageAvailableSemaphores = new Semaphore[VKSetup.MAX_FRAMES_IN_FLIGHT];
		renderFinishedSemaphores = new Semaphore[VKSetup.MAX_FRAMES_IN_FLIGHT];
		inFlightFences = new Fence[VKSetup.MAX_FRAMES_IN_FLIGHT];
		imagesInFlight = new Fence[sc.swapChainImages!.Length];

		SemaphoreCreateInfo semaphoreInfo = new() {
			SType = StructureType.SemaphoreCreateInfo,
		};

		FenceCreateInfo fenceInfo = new() {
			SType = StructureType.FenceCreateInfo,
			Flags = FenceCreateFlags.SignaledBit,
		};

		for (var i = 0; i < VKSetup.MAX_FRAMES_IN_FLIGHT; i++) {
			if (s.vk!.CreateSemaphore(s.device, in semaphoreInfo
				, null, out imageAvailableSemaphores[i]) != Result.Success
				||
				s.vk!.CreateSemaphore(s.device, in semaphoreInfo
				, null, out renderFinishedSemaphores[i]) != Result.Success
				||
				s.vk!.CreateFence(s.device, in fenceInfo
				, null, out inFlightFences[i]) != Result.Success) {
				throw new Exception("failed to create synchronization objects for a frame!");
			}
		}
	}

	public override void clear(VKSetup s) {
		for (int i = 0; i < VKSetup.MAX_FRAMES_IN_FLIGHT; i++) {
			s.vk!.DestroySemaphore(s.device, renderFinishedSemaphores![i], null);
			s.vk!.DestroySemaphore(s.device, imageAvailableSemaphores![i], null);
			s.vk!.DestroyFence(s.device, inFlightFences![i], null);
		}
	}
}


public struct QueueFamilyIndices {
	public uint? GraphicsFamily { get; set; }
	public uint? PresentFamily { get; set; }

	public bool IsComplete() {
		return GraphicsFamily.HasValue && PresentFamily.HasValue;
	}
}

public struct SwapChainSupportDetails {
	public SurfaceCapabilitiesKHR Capabilities;
	public SurfaceFormatKHR[] Formats;
	public PresentModeKHR[] PresentModes;
}


