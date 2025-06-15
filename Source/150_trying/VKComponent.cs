// Ignore Spelling: Utils Indices Vertices verts khr

using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Vulkan.Extensions.EXT;
using Device = Silk.NET.Vulkan.Device;
using Queue = Silk.NET.Vulkan.Queue;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Core;
using Silk.NET.Windowing;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using System.Collections;
using System.Diagnostics;
using _150_trying.geom;
using Silk.NET.OpenAL;
using Buffer = Silk.NET.Vulkan.Buffer;
using _150_trying.utils;
using System.Diagnostics.CodeAnalysis;

namespace _150_trying;

public abstract class VKComponent {
	protected bool _i;
	public bool initialized { get => _i; set => _i = value || _i; }
	public abstract void init(VKSetup s);
	public abstract void clear(VKSetup s);

}

public unsafe class VKSetup {
	public const int MAX_FRAMES_IN_FLIGHT = 2;

	public IWindow window { get; }
	public Instance instance { get; set; }
	public Vk vk { get; set; }

	public readonly string[] validationLayers = [
		"VK_LAYER_KHRONOS_validation"
	];
	public readonly string[] deviceExtensions = [
		KhrSwapchain.ExtensionName
	];

	public bool EnableValidationLayers { get; set; } = true;

	public PhysicalDevice physicalDevice;
	public Device device;
	public Queue graphicsQueue;
	public Queue presentQueue;

	private List<VKComponent> components = [];

	public VKSetup(IWindow window) {
		this.window = window;
		init();
	}

	[MemberNotNull("vk")]
	private VKSetup init() {
		components = [
			new VKInstance(), //this should set `vk`
			new VKDebugMessenger(),
			new VKSurface(),
			new VKDevicePicker(),
			new VKLogicalDevice(),
			new VKSwapChain(),
			new VKImageViews(),
			new VKRenderPass(),
			new VKGraphicsPipeline(),
			new VKFrameBuffer(),
			new VKVertexBuffer(),
			new VKCommandPool(),
			new VKCommandBuffers(),
			new VKSyncObjects(),
		];
		foreach (var c in components) {
			c.init(this);
			if (vk == null) throw new Exception("Failed to initialize.");
			c.initialized = true;
		}
		if (vk == null) throw new Exception("Failed to initialize.");
		return this;
	}

	public void clear() {
		for (int i = components.Count - 1; i >= 0; i--) {
			var c = components[i];
			c.clear(this);
		}
	}
	int currentFrame;

	public void DrawFrame(double delta) {
		var (so, sc, cb) = require<VKSyncObjects, VKSwapChain, VKCommandBuffers>();
		vk!.WaitForFences(device, 1, in so.inFlightFences![currentFrame]
			, true, ulong.MaxValue);

		uint imageIndex = 0;
		sc.khrSwapChain!.AcquireNextImage(device, sc.swapChain, ulong.MaxValue
			, so.imageAvailableSemaphores![currentFrame], default, ref imageIndex);

		if (so.imagesInFlight![imageIndex].Handle != default) {
			vk!.WaitForFences(device, 1, in so.imagesInFlight[imageIndex]
				, true, ulong.MaxValue);
		}
		so.imagesInFlight[imageIndex] = so.inFlightFences[currentFrame];

		SubmitInfo submitInfo = new() {
			SType = StructureType.SubmitInfo,
		};

		var waitSemaphores = stackalloc[] {
			so.imageAvailableSemaphores[currentFrame] };
		var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

		var buffer = cb.cmdBuffs![imageIndex];

		submitInfo = submitInfo with {
			WaitSemaphoreCount = 1,
			PWaitSemaphores = waitSemaphores,
			PWaitDstStageMask = waitStages,

			CommandBufferCount = 1,
			PCommandBuffers = &buffer
		};

		var signalSemaphores = stackalloc[] { so.renderFinishedSemaphores![currentFrame] };
		submitInfo = submitInfo with {
			SignalSemaphoreCount = 1,
			PSignalSemaphores = signalSemaphores,
		};

		vk!.ResetFences(device, 1, in so.inFlightFences[currentFrame]);

		if (vk!.QueueSubmit(graphicsQueue, 1, in submitInfo
			, so.inFlightFences[currentFrame]) != Result.Success) {
			throw new Exception("failed to submit draw command buffer!");
		}

		var swapChains = stackalloc[] { sc.swapChain };
		PresentInfoKHR presentInfo = new() {
			SType = StructureType.PresentInfoKhr,

			WaitSemaphoreCount = 1,
			PWaitSemaphores = signalSemaphores,

			SwapchainCount = 1,
			PSwapchains = swapChains,

			PImageIndices = &imageIndex
		};

		sc.khrSwapChain.QueuePresent(presentQueue, in presentInfo);

		currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;

	}


	//public T? get<T>() { return default; }
	public object require(Type t) {
		var c = components.Find(c => c.GetType() == t);
		if (c == null) throw new Exception($"Component not found exception ({t})");
		if (!c.initialized) throw new Exception($"Component not initialized ({t})");
		return c;
	}
	public T require<T>() => (T)require(typeof(T));
	public (T, T2) require<T, T2>() => (require<T>(), require<T2>());
	public (T, T2, T3) require<T, T2, T3>() => (require<T>(), require<T2>(), require<T3>());
	public (T, T2, T3, T4) require<T, T2, T3, T4>() => (require<T>(), require<T2>(), require<T3>(), require<T4>());
	public (T, T2, T3, T4, T5) require<T, T2, T3, T4, T5>() => (require<T>(), require<T2>(), require<T3>(), require<T4>(), require<T5>());
	public (T, T2, T3, T4, T5, T6) require<T, T2, T3, T4, T5, T6>() => (require<T>(), require<T2>(), require<T3>(), require<T4>(), require<T5>(), require<T6>());


}

public unsafe class VKVertexBuffer : VKComponent {
	public Buffer buffer;
	public DeviceMemory bufferMemory;
	public Vertices verts = new() {
			{(0.0f, -0.5f), (1.0f, 1.0f, 1.0f)},
			{(0.5f, 0.5f), ( 0.0f, 1.0f, 0.0f)},
			{(-0.5f, 0.5f), ( 0.0f, 0.0f, 1.0f)},
	};

	public override void init(VKSetup s) {
		var bi = new BufferCreateInfo {
			SType = StructureType.BufferCreateInfo,
			Size = verts.size,
			Usage = BufferUsageFlags.VertexBufferBit,
			SharingMode = SharingMode.Exclusive,
		};

		s.vk!.CreateBuffer(s.device, in bi, null, out buffer)
			.throwOnFail("Failed to create vertex buffer");

		MemoryRequirements memReq;
		s.vk!.GetBufferMemoryRequirements(s.device, buffer, &memReq);

		MemoryAllocateInfo allocInfo = new() {
			SType = StructureType.MemoryAllocateInfo,
			AllocationSize = memReq.Size,
			MemoryTypeIndex = FindMemoryType(s,
				memReq.MemoryTypeBits, 
				MemoryPropertyFlags.HostVisibleBit
				| MemoryPropertyFlags.HostCoherentBit
			)
		};

		s.vk.AllocateMemory(s.device, in allocInfo, null, out bufferMemory)
			.throwOnFail("Failed to allocate memory for vertex buffer");
		s.vk.BindBufferMemory(s.device, buffer, bufferMemory, 0);

		void* data;
		s.vk.MapMemory(s.device, bufferMemory, 0, bi.Size, 0, &data);
		verts.AsSpan().CopyTo(new Span<Vertex>(data, verts.Count));
		s.vk.UnmapMemory(s.device, bufferMemory);
	}

	private uint FindMemoryType(VKSetup s, uint typeFilter, MemoryPropertyFlags properties) {
		s.vk!.GetPhysicalDeviceMemoryProperties
			(s.physicalDevice, out var props);

		for (int i = 0; i < props.MemoryTypeCount; i++) {
			if ((typeFilter & (1 << i)) != 0
				&& (props.MemoryTypes[i].PropertyFlags & properties) == properties) {
				return (uint)i;
			}
		}

		throw new Exception("failed to find suitable memory type!");
	}

	public override void clear(VKSetup s) {
		s.vk!.DestroyBuffer(s.device, buffer, null);
		s.vk.FreeMemory(s.device, bufferMemory, null);
	}

}

//--------------------------------

public unsafe class VKDebugMessenger : VKComponent {
	public ExtDebugUtils? debugUtils;
	public DebugUtilsMessengerEXT debugMessenger;

	public override void init(VKSetup s) {
		if (!s.EnableValidationLayers) return;

		//TryGetInstanceExtension equivalent to method CreateDebugUtilsMessengerEXT from original tutorial.
		if (!s.vk!.TryGetInstanceExtension(s.instance, out debugUtils)) return;

		DebugUtilsMessengerCreateInfoEXT createInfo = new();
		PopulateDebugMessengerCreateInfo(ref createInfo);

		if (debugUtils!.CreateDebugUtilsMessenger
			(s.instance, in createInfo, null, out debugMessenger) != Result.Success) {
			throw new Exception("failed to set up debug messenger!");
		}
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

	private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData) {
		Debug.WriteLine($"Vulkan:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));
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

		Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
		Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
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
			.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName))
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
			swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
		}

		return indices.IsComplete() && extensionsSupported && swapChainAdequate;
	}

	private bool CheckDeviceExtensionsSupport(VKSetup s, PhysicalDevice device) {
		uint extentionsCount = 0;
		s.vk!.EnumerateDeviceExtensionProperties
			(device, (byte*)null, ref extentionsCount, null);

		var availableExtensions = new ExtensionProperties[extentionsCount];
		fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions) {
			s.vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, availableExtensionsPtr);
		}

		var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();

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

		var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
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

		PhysicalDeviceFeatures deviceFeatures = new();

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

public unsafe class VKImageViews : VKComponent {
	public ImageView[]? swapChainImageViews;

	public override void init(VKSetup s) {
		var sc = s.require<VKSwapChain>();
		swapChainImageViews = new ImageView[sc.swapChainImages!.Length];

		for (int i = 0; i < sc.swapChainImages.Length; i++) {
			ImageViewCreateInfo createInfo = new() {
				SType = StructureType.ImageViewCreateInfo,
				Image = sc.swapChainImages[i],
				ViewType = ImageViewType.Type2D,
				Format = sc.swapChainImageFormat,
				Components =
				{
					R = ComponentSwizzle.Identity,
					G = ComponentSwizzle.Identity,
					B = ComponentSwizzle.Identity,
					A = ComponentSwizzle.Identity,
				},
				SubresourceRange =
				{
					AspectMask = ImageAspectFlags.ColorBit,
					BaseMipLevel = 0,
					LevelCount = 1,
					BaseArrayLayer = 0,
					LayerCount = 1,
				}

			};

			if (s.vk!.CreateImageView(s.device, in createInfo
				, null, out swapChainImageViews[i]) != Result.Success) {
				throw new Exception("failed to create image views!");
			}
		}
	}

	public override void clear(VKSetup s) {
		foreach (var imageView in swapChainImageViews!) {
			s.vk!.DestroyImageView(s.device, imageView, null);
		}
	}
}

public unsafe class VKRenderPass : VKComponent {
	public RenderPass renderPass;

	public override void init(VKSetup s) {
		var sc = s.require<VKSwapChain>();
		AttachmentDescription colorAttachment = new() {
			Format = sc.swapChainImageFormat,
			Samples = SampleCountFlags.Count1Bit,
			LoadOp = AttachmentLoadOp.Clear,
			StoreOp = AttachmentStoreOp.Store,
			StencilLoadOp = AttachmentLoadOp.DontCare,
			InitialLayout = ImageLayout.Undefined,
			FinalLayout = ImageLayout.PresentSrcKhr,
		};

		AttachmentReference colorAttachmentRef = new() {
			Attachment = 0,
			Layout = ImageLayout.ColorAttachmentOptimal,
		};

		SubpassDescription subpass = new() {
			PipelineBindPoint = PipelineBindPoint.Graphics,
			ColorAttachmentCount = 1,
			PColorAttachments = &colorAttachmentRef,
		};

		SubpassDependency dependency = new() {
			SrcSubpass = Vk.SubpassExternal,
			DstSubpass = 0,
			SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
			SrcAccessMask = 0,
			DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
			DstAccessMask = AccessFlags.ColorAttachmentWriteBit
		};

		RenderPassCreateInfo renderPassInfo = new() {
			SType = StructureType.RenderPassCreateInfo,
			AttachmentCount = 1,
			PAttachments = &colorAttachment,
			SubpassCount = 1,
			PSubpasses = &subpass,
			DependencyCount = 1,
			PDependencies = &dependency,
		};

		if (s.vk!.CreateRenderPass
			(s.device, in renderPassInfo, null, out renderPass) != Result.Success) {
			throw new Exception("failed to create render pass!");
		}
	}

	public override void clear(VKSetup s) {
		s.vk!.DestroyRenderPass(s.device, renderPass, null);
	}

}

public unsafe class VKGraphicsPipeline : VKComponent {
	public PipelineLayout pipelineLayout;
	public Pipeline graphicsPipeline;

	public override void init(VKSetup s) {
		var (sc, rp) = s.require<VKSwapChain, VKRenderPass>();
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
				FrontFace = FrontFace.Clockwise,
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
				SetLayoutCount = 0,
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

public unsafe class VKFrameBuffer : VKComponent {
	public Framebuffer[]? swapChainFrameBuffers;

	public override void init(VKSetup s) {
		var (sc, rp, iv) = s.require<VKSwapChain, VKRenderPass, VKImageViews>();
		swapChainFrameBuffers = new Framebuffer[iv.swapChainImageViews!.Length];

		for (int i = 0; i < iv.swapChainImageViews.Length; i++) {
			var attachment = iv.swapChainImageViews[i];

			FramebufferCreateInfo framebufferInfo = new() {
				SType = StructureType.FramebufferCreateInfo,
				RenderPass = rp.renderPass,
				AttachmentCount = 1,
				PAttachments = &attachment,
				Width = sc.swapChainExtent.Width,
				Height = sc.swapChainExtent.Height,
				Layers = 1,
			};

			if (s.vk!.CreateFramebuffer(s.device, in framebufferInfo
				, null, out swapChainFrameBuffers[i]) != Result.Success) {
				throw new Exception("failed to create frame buffer!");
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

public unsafe class VKCommandBuffers : VKComponent {
	public CommandBuffer[]? cmdBuffs;

	public override void init(VKSetup s) {
		var (cp, fb, rp, sc, pl, vb) = s.require<VKCommandPool, VKFrameBuffer
			, VKRenderPass, VKSwapChain, VKGraphicsPipeline, VKVertexBuffer>();
		cmdBuffs = new CommandBuffer[fb.swapChainFrameBuffers!.Length];

		CommandBufferAllocateInfo allocInfo = new() {
			SType = StructureType.CommandBufferAllocateInfo,
			CommandPool = cp.commandPool,
			Level = CommandBufferLevel.Primary,
			CommandBufferCount = (uint)cmdBuffs.Length,
		};

		fixed (CommandBuffer* commandBuffersPtr = cmdBuffs) {
			s.vk.AllocateCommandBuffers(s.device
				, in allocInfo, commandBuffersPtr)
				.throwOnFail("failed to allocate command buffers!");
		}


		for (int i = 0; i < cmdBuffs.Length; i++) {
			CommandBufferBeginInfo beginInfo = new() {
				SType = StructureType.CommandBufferBeginInfo,
			};

			if (s.vk!.BeginCommandBuffer(cmdBuffs[i], in beginInfo)
				!= Result.Success) {
				throw new Exception("failed to begin recording command buffer!");
			}

			RenderPassBeginInfo renderPassInfo = new() {
				SType = StructureType.RenderPassBeginInfo,
				RenderPass = rp.renderPass,
				Framebuffer = fb.swapChainFrameBuffers[i],
				RenderArea =
				{
					Offset = { X = 0, Y = 0 },
					Extent = sc.swapChainExtent,
				}
			};

			ClearValue clearColor = new() {
				Color = new() {
					Float32_0 = 0, Float32_1 = 0
								, Float32_2 = 0, Float32_3 = 1
				},
			};

			renderPassInfo.ClearValueCount = 1;
			renderPassInfo.PClearValues = &clearColor;

			s.vk!.CmdBeginRenderPass(cmdBuffs[i], &renderPassInfo
				, SubpassContents.Inline);

			s.vk!.CmdBindPipeline(cmdBuffs[i]
				, PipelineBindPoint.Graphics, pl.graphicsPipeline);

			
			Buffer[] vertexBuffers = [vb.buffer];
			var offsets = new ulong[] { 0 };
			s.vk.CmdBindVertexBuffers
				(cmdBuffs[i], 0, 1, vertexBuffers, offsets);

			s.vk!.CmdDraw(cmdBuffs[i], 
				(uint)vb.verts.Count, 1, 0, 0);

			s.vk!.CmdEndRenderPass(cmdBuffs[i]);

			if (s.vk!.EndCommandBuffer(cmdBuffs[i]) != Result.Success) {
				throw new Exception("failed to record command buffer!");
			}

		}
	}

	public override void clear(VKSetup s) {

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


