// Ignore Spelling: Utils Indices Vertices verts khr

using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System.Diagnostics.CodeAnalysis;
using Device = Silk.NET.Vulkan.Device;
using Queue = Silk.NET.Vulkan.Queue;
using Buffer = Silk.NET.Vulkan.Buffer;
using MemProps = Silk.NET.Vulkan.MemoryPropertyFlags;
using BuffUsage = Silk.NET.Vulkan.BufferUsageFlags;
using _150_trying.utils;
using _150_trying.geom;
using Silk.NET.Maths;
using _150_trying.utils;
using System.Diagnostics;

namespace _150_trying.VKComponents;

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
			new VKDescriptorSetLayout(),
			new VKGraphicsPipeline(),
			new VKFrameBuffer(),
			new VKCommandPool(),
			new VKTextureImage(),
			new VKTextureImageView(),
			new VKTextureSampler(),
			new VKVertexIndexBuffer(),
			new VKUniformBuffers(),
			new VKDescriptorPool(),
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
		var (so, sc, cb, ub) = require<VKSyncObjects, VKSwapChain
			, VKCommandBuffers, VKUniformBuffers>();
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

		updateUniformBuffer(currentFrame, sc, ub);

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

	private DateTime startTime = DateTime.Now;

	private void updateUniformBuffer(int currentImage, VKSwapChain sc, VKUniformBuffers ub) {
		var time = (float)(DateTime.Now - startTime).TotalSeconds;
		var ubo = new UniformBufferObject() {
			model = Matrix4X4<float>.Identity
				* Matrix4X4.CreateFromAxisAngle<float>
				(new Vector3D<float>(0, 0, 1), time * 90.0f.toRadians() ),
			view = Matrix4X4.CreateLookAt(
				new Vector3D<float>(2, 2, 2)
				, new Vector3D<float>(0, 0, 0)
				, new Vector3D<float>(0, 0, 1)),
			proj = Matrix4X4.CreatePerspectiveFieldOfView
				(45.0f.toRadians()
				, sc.swapChainExtent.Width / sc.swapChainExtent.Height
				, 0.1f, 10.0f),
		};
		ubo.proj.M22 *= -1;

		ub.update(ubo, currentImage);
	}

	#region Helper methods
	/// <summary>https://vulkan-tutorial.com/Vertex_buffers/Staging_buffer</summary>
	/// <param name="s"></param>
	/// <param name="size"></param>
	/// <param name="usage"></param>
	/// <param name="properties"></param>
	/// <param name="buffer"></param>
	/// <param name="memory"></param>
	public void createBuffer(ulong size
		, BuffUsage usage, MemProps properties
		, out Buffer buffer, out DeviceMemory memory) {
		var bi = new BufferCreateInfo {
			SType = StructureType.BufferCreateInfo,
			Size = size,
			Usage = usage,
			SharingMode = SharingMode.Exclusive,
		};

		vk.CreateBuffer(device, in bi, null, out buffer)
			.throwOnFail("Failed to create vertex buffer");

		MemoryRequirements memReq;
		vk.GetBufferMemoryRequirements(device, buffer, &memReq);

		MemoryAllocateInfo allocInfo = new() {
			SType = StructureType.MemoryAllocateInfo,
			AllocationSize = memReq.Size,
			MemoryTypeIndex = FindMemoryType(
				memReq.MemoryTypeBits, properties
			)
		};

		vk.AllocateMemory(device, in allocInfo, null, out memory)
			.throwOnFail("Failed to allocate memory for vertex buffer");
		vk.BindBufferMemory(device, buffer, memory, 0);
	}

	public void copyBuffer(Buffer src, Buffer dst, ulong size) {
		//CommandPool cp = require<VKCommandPool>().commandPool;
		//CommandBufferAllocateInfo allocInfo = new() {
		//	SType = StructureType.CommandBufferAllocateInfo,
		//	Level = CommandBufferLevel.Primary,
		//	CommandPool = cp,
		//	CommandBufferCount = 1,
		//};
		//vk.AllocateCommandBuffers(device
		//	, in allocInfo, out var cmdBuff);

		//CommandBufferBeginInfo beginInfo = new() {
		//	SType = StructureType.CommandBufferBeginInfo,
		//	Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
		//};
		//vk.BeginCommandBuffer(cmdBuff, in beginInfo);

		var cmdBuff = beginSingleTimeCommands();

		BufferCopy copyRegion = new() {
			SrcOffset = 0, DstOffset = 0, //optional
			Size = size
		};
		vk.CmdCopyBuffer(cmdBuff, src, dst, 1, in copyRegion);

		endSingleTimeCommands(cmdBuff);

		//vk.EndCommandBuffer(cmdBuff);

		//SubmitInfo si = new() {
		//	SType = StructureType.SubmitInfo,
		//	CommandBufferCount = 1,
		//	PCommandBuffers = &cmdBuff
		//};

		//vk.QueueSubmit(graphicsQueue, 1, in si, new Fence());
		//vk.QueueWaitIdle(graphicsQueue);

		//vk.FreeCommandBuffers(device, cp, 1, in cmdBuff);
	}

	public CommandBuffer beginSingleTimeCommands() {
		CommandBufferAllocateInfo ai = new() {
			SType = StructureType.CommandBufferAllocateInfo,
			Level = CommandBufferLevel.Primary,
			CommandPool = require<VKCommandPool>().commandPool,
			CommandBufferCount = 1,
		};

		vk.AllocateCommandBuffers(device, in ai, out var cb);

		CommandBufferBeginInfo bi = new() {
			SType = StructureType.CommandBufferBeginInfo,
			Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
		};
		vk.BeginCommandBuffer(cb, in bi);
		return cb;
	}

	public void endSingleTimeCommands(CommandBuffer cb) {
		vk.EndCommandBuffer(cb);
		SubmitInfo si = new() {
			SType = StructureType.SubmitInfo,
			CommandBufferCount = 1,
			PCommandBuffers = &cb
		};
		vk.QueueSubmit(graphicsQueue, 1, in si, default);
		vk.QueueWaitIdle(graphicsQueue);
		vk.FreeCommandBuffers(device, require<VKCommandPool>().commandPool
			, 1, in cb);
	}

	public uint FindMemoryType(uint typeFilter, MemProps properties) {
		vk.GetPhysicalDeviceMemoryProperties
			(physicalDevice, out var props);

		for (int i = 0; i < props.MemoryTypeCount; i++) {
			if ((typeFilter & 1 << i) != 0
				&& (props.MemoryTypes[i].PropertyFlags & properties) == properties) {
				return (uint)i;
			}
		}

		throw new Exception("failed to find suitable memory type!");
	}
	#endregion


	//public T? get<T>() { return default; }
	[DebuggerHidden]
	public object require(Type t) {
		var c = components.Find(c => c.GetType() == t)
			?? throw new Exception($"Component not found exception ({t})");
		if (!c.initialized) throw new Exception($"Component not initialized ({t})");
		return c;
	}
	[DebuggerHidden]
	public T require<T>() => (T)require(typeof(T));
	[DebuggerHidden]
	public (T, T2) require<T, T2>() => (require<T>(), require<T2>());
	[DebuggerHidden]
	public (T, T2, T3) require<T, T2, T3>() => (require<T>(), require<T2>(), require<T3>());
	[DebuggerHidden]
	public (T, T2, T3, T4) require<T, T2, T3, T4>() => (require<T>(), require<T2>(), require<T3>(), require<T4>());
	[DebuggerHidden]
	public (T, T2, T3, T4, T5) require<T, T2, T3, T4, T5>() => (require<T>(), require<T2>(), require<T3>(), require<T4>(), require<T5>());
	[DebuggerHidden]
	public (T, T2, T3, T4, T5, T6) require<T, T2, T3, T4, T5, T6>() => (require<T>(), require<T2>(), require<T3>(), require<T4>(), require<T5>(), require<T6>());
	[DebuggerHidden]
	public (T, T2, T3, T4, T5, T6, T7) require<T, T2, T3, T4, T5, T6, T7>() => (require<T>(), require<T2>(), require<T3>(), require<T4>(), require<T5>(), require<T6>(), require<T7>());


}


