using Silk.NET.Vulkan;
using System.Diagnostics;
using System.Numerics;

namespace _150_trying.utils;

public static class SilkVulkanExtensions {

	/// <summary>Throws an exception if this result is not <see cref="Result.Success"/>.</summary>
	/// <param name="r"></param>
	/// <param name="message">Main message text displayed for the exception.
	/// This method will add the result name and code at the end of this.</param>
	/// <exception cref="VulkanException"></exception>
	[DebuggerHidden]
	public static void throwOnFail(this Result r, string? message = null) {
		if (r == Result.Success) return;
		var nL = message != null ? "\n" : "";
		throw new VulkanException($"{message}{nL}{r} Code:{(int)r}");
	}

	public static T toRadians<T>(this T degrees) where T : INumber<T> {
		return degrees * T.CreateTruncating(Math.PI)
			/ T.CreateTruncating(180f);
	}
}

[Serializable]
internal class VulkanException : Exception {
	public VulkanException() {
	}

	public VulkanException(string? message) : base(message) {
	}

	public VulkanException(string? message, Exception? innerException) : base(message, innerException) {
	}
}