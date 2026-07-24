using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Estragonia;

internal static class VkExtensions
{
	public static void VerifySuccess(this VkInterop.VkResult result, string functionName)
	{
		if (result != VkInterop.VkResult.VK_SUCCESS)
			ThrowError(result, functionName);
		return;

		[DoesNotReturn]
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void ThrowError(VkInterop.VkResult vkResult, string functionName)
		{
			throw new InvalidOperationException($"{functionName} returned Vulkan error {vkResult}");
		}
	}
}
