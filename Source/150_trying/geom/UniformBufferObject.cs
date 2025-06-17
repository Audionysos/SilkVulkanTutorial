using Silk.NET.Maths;

namespace _150_trying.geom;

public struct UniformBufferObject {
	public Matrix4X4<float> model;
	public Matrix4X4<float> view;
	public Matrix4X4<float> proj;
}
