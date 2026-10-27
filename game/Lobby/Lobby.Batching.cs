using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArquipelagoDoCeu;

// No celular cada instância custa uma chamada de desenho: hexágonos, rochas, pontes, margens e árvores somavam umas 900 por quadro.
// As que repetem a mesma malha com os mesmos materiais viram uma instanciação em lote, com uma chamada por grupo e sem copiar vértices.
public partial class Lobby
{
	readonly List<Node3D> treeRoots = [];

	void InstanceStatic()
	{
		var toLocal = GlobalTransform.AffineInverse();
		var groups = new Dictionary<string, List<(MeshInstance3D Node, Material?[] Materials)>>();
		foreach (var root in lote02Roots.Concat(treeRoots))
			foreach (var mesh in root.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>().Where(m => m.Mesh is not null && m.Visible))
			{
				var materials = Enumerable.Range(0, mesh.Mesh.GetSurfaceCount()).Select(mesh.GetActiveMaterial).ToArray();
				var key = $"{mesh.Mesh.GetInstanceId()}|{string.Join(",", materials.Select(m => m?.GetInstanceId() ?? 0UL))}";
				if (!groups.TryGetValue(key, out var group)) groups[key] = group = [];
				group.Add((mesh, materials));
			}

		foreach (var group in groups.Values.Where(g => g.Count > 1))
		{
			var (first, materials) = group[0];
			var shape = first.Mesh;
			// Quando a instância troca o material (o hexágono de cada trilha), a malha do grupo leva esse material.
			if (Enumerable.Range(0, materials.Length).Any(s => materials[s] != shape.SurfaceGetMaterial(s)))
			{
				shape = (Mesh)shape.Duplicate();
				for (var s = 0; s < materials.Length; s++) shape.SurfaceSetMaterial(s, materials[s]);
			}

			var batch = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = shape, InstanceCount = group.Count };
			for (var i = 0; i < group.Count; i++) batch.SetInstanceTransform(i, toLocal * group[i].Node.GlobalTransform);
			AddChild(new MultiMeshInstance3D { Name = "Instanciado", Multimesh = batch });
			foreach (var (node, _) in group) node.Visible = false;
		}
	}
}
