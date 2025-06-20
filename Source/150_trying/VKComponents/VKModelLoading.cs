// Ignore Spelling: Utils Indices Vertices verts khr ubo Mem

using _150_trying.geom;
using Silk.NET.Maths;
using static _150_trying.utils.SilkVulkanExtensions;

using System.Xml.Linq;
using Silk.NET.Assimp;

namespace _150_trying.VKComponents;

public unsafe class VKModelLoading : VKComponent {
	public string MODEL_PATH = @"Assets\viking_room.obj";
	public string TEXTURE_PATH = @"Assets\viking_room.png";

	public Vertices vertices = [];
	private List<uint> _indices = [];
	public Indices indices = [];
	private Dictionary<Vertex, uint>  map = [];


	public override void init(VKSetup s) {
		using var assimp = Assimp.GetApi();
		var scene = assimp.ImportFile(MODEL_PATH
			, (uint)PostProcessPreset.TargetRealTimeMaximumQuality);

		var vertices = new List<Vertex>();
		var indices = new List<uint>();

		visitNode(scene->MRootNode, scene);

		assimp.ReleaseImport(scene);

		//this.vertices = vertices.ToArray();
		this.indices = _indices;
		
	}

	private void visitNode(Node* node, Scene* scene) {
		for (int m = 0; m < node->MNumMeshes; m++) {
			var mesh = scene->MMeshes[node->MMeshes[m]];

			for (int f = 0; f < mesh->MNumFaces; f++) {
				var face = mesh->MFaces[f];

				for (int i = 0; i < face.MNumIndices; i++) {
					uint index = face.MIndices[i];

					var position = mesh->MVertices[index];
					var texture = mesh->MTextureCoords[0][(int)index];

					Vertex vertex = new Vertex {
						pos = float3(position.X, position.Y, position.Z),
						color = float3(1, 1, 1),
						texCoord = float2(texture.X, 1.0f - texture.Y) //Flip Y for OBJ in Vulkan
					};

					if (map.TryGetValue(vertex, out var meshIndex)) {
						_indices.Add(meshIndex);
					} else {
						_indices.Add((uint)vertices.Count);
						map[vertex] = (uint)vertices.Count;
						vertices.Add(vertex);
					}
				}
			}
		}

		for (int c = 0; c < node->MNumChildren; c++) {
			visitNode(node->MChildren[c], scene);
		}
	}

	public override void clear(VKSetup s) {

	}
}


