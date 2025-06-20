- Shaders source code file have to be saved without BOM.

- I simplified some call that used `fix() {}` . Not sure how this works but it looked unnecessary, and the vertex triangle renders without it.

- I moved `DepthResources` up before `RenderPass` instead of `TextureImage` like in original tutorial to reuse `format` queried by the `DepthResources` as it doesn't seem to have other dependencies than `SwapChain`.


