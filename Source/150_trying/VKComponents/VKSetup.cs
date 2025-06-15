// Ignore Spelling: Utils Indices Vertices verts khr

using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Device = Silk.NET.Vulkan.Device;
using Queue = Silk.NET.Vulkan.Queue;
using Silk.NET.Windowing;
using System.Diagnostics.CodeAnalysis;

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


