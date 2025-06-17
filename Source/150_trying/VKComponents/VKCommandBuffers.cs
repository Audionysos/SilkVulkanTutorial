// Ignore Spelling: Utils Indices Vertices verts khr ubo

using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using _150_trying.utils;

namespace _150_trying.VKComponents;

public unsafe class VKCommandBuffers : VKComponent {
	public CommandBuffer[]? cmdBuffs;

	public override void init(VKSetup s) {
		var (cp, fb, rp, sc, pl, vb, dp) = s.require<VKCommandPool, VKFrameBuffer
			, VKRenderPass, VKSwapChain, VKGraphicsPipeline
			, VKVertexIndexBuffer, VKDescriptorPool>();
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

			Buffer[] indsBuffers = [vb.indBuff];
			s.vk.CmdBindIndexBuffer(cmdBuffs[i], vb.indBuff, 0, IndexType.Uint32);

			s.vk.CmdBindDescriptorSets(cmdBuffs[i]
				, PipelineBindPoint.Graphics
				, pl.pipelineLayout, 0, 1
				, in dp.descriptorSets[i], 0, null);

			s.vk.CmdDrawIndexed(cmdBuffs[i], (uint)vb.inds.Count, 1, 0, 0, 0);

			//s.vk!.CmdDraw(cmdBuffs[i], 
			//	(uint)vb.verts.Count, 1, 0, 0);

			s.vk!.CmdEndRenderPass(cmdBuffs[i]);

			if (s.vk!.EndCommandBuffer(cmdBuffs[i]) != Result.Success) {
				throw new Exception("failed to record command buffer!");
			}

		}
	}

	public override void clear(VKSetup s) {
		//I didn't notice any clean up in original C# clear method.
		//I assume this is all done at once with `vk.DestroyCommandPool`
	}
}


