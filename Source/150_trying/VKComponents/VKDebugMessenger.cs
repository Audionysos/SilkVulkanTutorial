// Ignore Spelling: Utils Indices Vertices verts khr ubo Mem

using _150_trying.utils;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace _150_trying.VKComponents;

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


