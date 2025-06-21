using _150_trying;
using _150_trying.VKComponents;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using static Silk.NET.GLFW.GlfwCallbacks;


var app = new HelloTriangleApplication();
//app.Run();

unsafe class HelloTriangleApplication {
	const int WIDTH = 800;
	const int HEIGHT = 600;

	private IWindow? window;
	private VKSetup setup;

	public HelloTriangleApplication() {
		InitWindow();
		setup = new VKSetup(window!);
		MainLoop();
		setup.clear();
	}

	private void InitWindow() {
		//Create a window.
		var options = WindowOptions.DefaultVulkan with {
			Size = new Vector2D<int>(WIDTH, HEIGHT),
			Title = "Vulkan"
		};

		_ = ShadersCompiler.compile
			(@"shaders/shader.vert", @"shaders/vert.spv");
		_ = ShadersCompiler.compile
			(@"shaders/shader.frag", @"shaders/frag.spv");

		window = Window.Create(options);
		window.Initialize();
		if (window.VkSurface is null)
			throw new Exception("Windowing platform doesn't support Vulkan.");
		window.Resize += FramebufferResizeCallback;
	}

	private void MainLoop() {
		window!.Render += setup.DrawFrame;
		window!.Run();
		setup.vk!.DeviceWaitIdle(setup.device);
	}

	private void FramebufferResizeCallback(Vector2D<int> d) {
		setup.resize();
		//That should eventually cause `RecreateSwapChain()` which is not include currently in the project
	}
}