using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace WinterSled
{
    /// <summary>
    /// Loads the embedded undercarriage OBJ (written by model/build_model.py) into a mesh
    /// with one submesh per material, in the order the materials first appear.
    /// </summary>
    internal static class ObjMesh
    {
        public static Mesh Load(string resourceName, out List<string> materials)
        {
            var asm = typeof(ObjMesh).Assembly;
            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("embedded resource missing", resourceName);
                }
                using (var reader = new StreamReader(stream))
                {
                    return Parse(reader, resourceName, out materials);
                }
            }
        }

        private static Mesh Parse(TextReader reader, string name, out List<string> materials)
        {
            var positions = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var vertPos = new List<Vector3>();
            var vertUv = new List<Vector2>();
            var vertNormal = new List<Vector3>();
            var lookup = new Dictionary<string, int>();
            var submeshes = new List<List<int>>();
            materials = new List<string>();
            List<int> current = null;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }
                switch (parts[0])
                {
                    // the OBJ is mirrored on x like Unity's own importer expects; mirror it back
                    case "v":
                        positions.Add(new Vector3(-F(parts[1]), F(parts[2]), F(parts[3])));
                        break;
                    case "vt":
                        uvs.Add(new Vector2(F(parts[1]), F(parts[2])));
                        break;
                    case "vn":
                        normals.Add(new Vector3(-F(parts[1]), F(parts[2]), F(parts[3])));
                        break;
                    case "usemtl":
                        int index = materials.IndexOf(parts[1]);
                        if (index < 0)
                        {
                            materials.Add(parts[1]);
                            submeshes.Add(new List<int>());
                            index = materials.Count - 1;
                        }
                        current = submeshes[index];
                        break;
                    case "f":
                        if (current == null)
                        {
                            materials.Add("default");
                            current = new List<int>();
                            submeshes.Add(current);
                        }
                        var face = new int[parts.Length - 1];
                        for (int i = 1; i < parts.Length; i++)
                        {
                            if (!lookup.TryGetValue(parts[i], out int vi))
                            {
                                var idx = parts[i].Split('/');
                                vi = vertPos.Count;
                                vertPos.Add(positions[int.Parse(idx[0]) - 1]);
                                vertUv.Add(idx.Length > 1 && idx[1].Length > 0 ? uvs[int.Parse(idx[1]) - 1] : Vector2.zero);
                                vertNormal.Add(idx.Length > 2 && idx[2].Length > 0 ? normals[int.Parse(idx[2]) - 1] : Vector3.up);
                                lookup[parts[i]] = vi;
                            }
                            face[i - 1] = vi;
                        }
                        // mirroring flipped the handedness: reverse the winding back
                        for (int i = 1; i + 1 < face.Length; i++)
                        {
                            current.Add(face[0]);
                            current.Add(face[i + 1]);
                            current.Add(face[i]);
                        }
                        break;
                }
            }

            var mesh = new Mesh { name = name };
            if (vertPos.Count > 65000)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }
            mesh.SetVertices(vertPos);
            mesh.SetUVs(0, vertUv);
            mesh.SetNormals(vertNormal);
            mesh.subMeshCount = submeshes.Count;
            for (int i = 0; i < submeshes.Count; i++)
            {
                mesh.SetTriangles(submeshes[i], i);
            }
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static float F(string s)
        {
            return float.Parse(s, CultureInfo.InvariantCulture);
        }
    }
}
