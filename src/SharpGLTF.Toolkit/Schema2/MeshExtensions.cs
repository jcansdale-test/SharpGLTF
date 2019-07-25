﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

using SharpGLTF.Memory;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Schema2
{
    public static partial class Schema2Toolkit
    {
        #region meshes

        public static Mesh CreateMesh(this ModelRoot root, IMeshBuilder<Materials.MaterialBuilder> mesh)
        {
            return root.CreateMeshes(mesh).First();
        }

        public static Mesh CreateMesh<TMaterial>(this ModelRoot root, Func<TMaterial, Material> materialEvaluator, IMeshBuilder<TMaterial> mesh)
        {
            return root.CreateMeshes<TMaterial>(materialEvaluator, mesh).First();
        }

        public static IReadOnlyList<Mesh> CreateMeshes(this ModelRoot root, params IMeshBuilder<Materials.MaterialBuilder>[] meshBuilders)
        {
            return root.CreateMeshes(mb => root.CreateMaterial(mb), meshBuilders);
        }

        public static IReadOnlyList<Mesh> CreateMeshes<TMaterial>(this ModelRoot root, Func<TMaterial, Material> materialEvaluator, params IMeshBuilder<TMaterial>[] meshBuilders)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(materialEvaluator, nameof(materialEvaluator));
            Guard.NotNull(meshBuilders, nameof(meshBuilders));

            foreach (var m in meshBuilders) m.Validate();

            // create a new material for every unique material in the mesh builders.
            var mapMaterials = meshBuilders
                .SelectMany(item => item.Primitives)
                .Select(item => item.Material)
                .Distinct()
                .ToDictionary(m => m, m => materialEvaluator(m));

            // creates meshes and primitives using MemoryAccessors using a single, shared vertex and index buffer
            var srcMeshes = PackedMeshBuilder<TMaterial>
                .PackMeshes(meshBuilders)
                .ToList();

            var dstMeshes = new List<Mesh>();

            foreach (var srcMesh in srcMeshes)
            {
                var dstMesh = srcMesh.CreateSchema2Mesh(root, m => mapMaterials[m]);

                dstMeshes.Add(dstMesh);
            }

            return dstMeshes;
        }

        #endregion

        #region accessors

        public static MeshPrimitive WithIndicesAutomatic(this MeshPrimitive primitive, PrimitiveType primitiveType)
        {
            var root = primitive.LogicalParent.LogicalParent;

            primitive.DrawPrimitiveType = primitiveType;
            primitive.SetIndexAccessor(null);

            return primitive;
        }

        public static MeshPrimitive WithIndicesAccessor(this MeshPrimitive primitive, PrimitiveType primitiveType, IReadOnlyList<Int32> values)
        {
            var root = primitive.LogicalParent.LogicalParent;

            // create an index buffer and fill it
            var view = root.UseBufferView(new Byte[4 * values.Count], 0, null, 0, BufferMode.ELEMENT_ARRAY_BUFFER);
            var array = new IntegerArray(view.Content);
            array.Fill(values);

            var accessor = root.CreateAccessor();

            accessor.SetIndexData(view, 0, values.Count, IndexEncodingType.UNSIGNED_INT);

            primitive.DrawPrimitiveType = primitiveType;
            primitive.SetIndexAccessor(accessor);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Single> values)
        {
            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[4 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new ScalarArray(view.Content);
            array.Fill(values);

            var accessor = root.CreateAccessor();
            primitive.SetVertexAccessor(attribute, accessor);

            accessor.SetVertexData(view, 0, values.Count, DimensionType.SCALAR, EncodingType.FLOAT, false);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Vector2> values)
        {
            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[8 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new Vector2Array(view.Content);
            array.Fill(values);

            var accessor = root.CreateAccessor();
            primitive.SetVertexAccessor(attribute, accessor);

            accessor.SetVertexData(view, 0, values.Count, DimensionType.VEC2, EncodingType.FLOAT, false);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Vector3> values)
        {
            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[12 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new Vector3Array(view.Content);
            array.Fill(values);

            var accessor = root.CreateAccessor();

            accessor.SetVertexData(view, 0, values.Count, DimensionType.VEC3, EncodingType.FLOAT, false);

            primitive.SetVertexAccessor(attribute, accessor);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, string attribute, IReadOnlyList<Vector4> values)
        {
            var root = primitive.LogicalParent.LogicalParent;

            // create a vertex buffer and fill it
            var view = root.UseBufferView(new Byte[16 * values.Count], 0, null, 0, BufferMode.ARRAY_BUFFER);
            var array = new Vector4Array(view.Content);
            array.Fill(values);

            var accessor = root.CreateAccessor();

            accessor.SetVertexData(view, 0, values.Count, DimensionType.VEC4, EncodingType.FLOAT, false);

            primitive.SetVertexAccessor(attribute, accessor);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessors(this MeshPrimitive primitive, IReadOnlyList<VertexPosition> vertices)
        {
            var xvertices = vertices
                .Select(item => new VertexBuilder<VertexPosition, VertexEmpty, VertexEmpty>(item))
                .ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors(this MeshPrimitive primitive, IReadOnlyList<VertexPositionNormal> vertices)
        {
            var xvertices = vertices
                .Select(item => new VertexBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>(item))
                .ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TvP, TvM>(this MeshPrimitive primitive, IReadOnlyList<(TvP, TvM)> vertices)
            where TvP : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            var xvertices = vertices
                .Select(item => new VertexBuilder<TvP, TvM, VertexEmpty>(item.Item1, item.Item2))
                .ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TvP, TvM, TvS>(this MeshPrimitive primitive, IReadOnlyList<(TvP, TvM, TvS)> vertices)
            where TvP : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            var xvertices = vertices
                .Select(item => new VertexBuilder<TvP, TvM, TvS>(item.Item1, item.Item2, item.Item3))
                .ToList();

            return primitive.WithVertexAccessors(xvertices);
        }

        public static MeshPrimitive WithVertexAccessors<TVertex>(this MeshPrimitive primitive, IReadOnlyList<TVertex> vertices)
            where TVertex : IVertexBuilder
        {
            var memAccessors = VertexUtils.CreateVertexMemoryAccessors(new[] { vertices }).First();

            return primitive.WithVertexAccessors(memAccessors);
        }

        public static MeshPrimitive WithVertexAccessors(this MeshPrimitive primitive, IEnumerable<Memory.MemoryAccessor> memAccessors)
        {
            foreach (var va in memAccessors) primitive.WithVertexAccessor(va);

            return primitive;
        }

        public static MeshPrimitive WithVertexAccessor(this MeshPrimitive primitive, Memory.MemoryAccessor memAccessor)
        {
            var root = primitive.LogicalParent.LogicalParent;

            primitive.SetVertexAccessor(memAccessor.Attribute.Name, root.CreateVertexAccessor(memAccessor));

            return primitive;
        }

        public static MeshPrimitive WithIndicesAccessor(this MeshPrimitive primitive, PrimitiveType primitiveType, Memory.MemoryAccessor memAccessor)
        {
            var root = primitive.LogicalParent.LogicalParent;

            var accessor = root.CreateAccessor();

            accessor.SetIndexData(memAccessor);

            primitive.DrawPrimitiveType = primitiveType;
            primitive.SetIndexAccessor(accessor);

            return primitive;
        }

        #endregion

        #region material

        public static MeshPrimitive WithMaterial(this MeshPrimitive primitive, Material material)
        {
            primitive.Material = material;
            return primitive;
        }

        #endregion

        #region evaluation

        public static IEnumerable<(IVertexBuilder, IVertexBuilder, IVertexBuilder, Material)> EvaluateTriangles(this Mesh mesh)
        {
            if (mesh == null) return Enumerable.Empty<(IVertexBuilder, IVertexBuilder, IVertexBuilder, Material)>();

            return mesh.Primitives.SelectMany(item => item.EvaluateTriangles());
        }

        public static IEnumerable<(IVertexBuilder, IVertexBuilder, IVertexBuilder, Material)> EvaluateTriangles(this MeshPrimitive prim)
        {
            if (prim == null) yield break;

            var vertices = prim.GetVertexColumns();
            var triangles = prim.GetTriangleIndices();

            bool hasNormals = vertices.Normals != null;

            var vtype = VertexUtils.GetVertexBuilderType(prim.VertexAccessors.Keys.ToArray());

            foreach (var t in triangles)
            {
                var a = vertices.GetVertex(vtype, t.Item1);
                var b = vertices.GetVertex(vtype, t.Item2);
                var c = vertices.GetVertex(vtype, t.Item3);

                /*
                if (!hasNormals)
                {
                    var n = Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
                    n = Vector3.Normalize(n);
                    a.Geometry.SetNormal(n);
                    b.Geometry.SetNormal(n);
                    c.Geometry.SetNormal(n);
                }*/

                yield return (a, b, c, prim.Material);
            }
        }

        public static IEnumerable<(VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, Material)> EvaluateTriangles<TvG, TvM, TvS>(this Mesh mesh)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            if (mesh == null) return Enumerable.Empty<(VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, Material)>();

            return mesh.Primitives.SelectMany(item => item.EvaluateTriangles<TvG, TvM, TvS>());
        }

        public static IEnumerable<(VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, VertexBuilder<TvG, TvM, TvS>, Material)> EvaluateTriangles<TvG, TvM, TvS>(this MeshPrimitive prim)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            if (prim == null) yield break;

            var vertices = prim.GetVertexColumns();
            var triangles = prim.GetTriangleIndices();

            bool hasNormals = vertices.Normals != null;

            foreach (var t in triangles)
            {
                var a = vertices.GetVertex<TvG, TvM, TvS>(t.Item1);
                var b = vertices.GetVertex<TvG, TvM, TvS>(t.Item2);
                var c = vertices.GetVertex<TvG, TvM, TvS>(t.Item3);

                if (!hasNormals)
                {
                    var n = Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
                    n = Vector3.Normalize(n);
                    a.Geometry.SetNormal(n);
                    b.Geometry.SetNormal(n);
                    c.Geometry.SetNormal(n);
                }

                yield return (a, b, c, prim.Material);
            }
        }

        public static IEnumerable<(VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, Material)> EvaluateTriangles<TvG, TvM>(this Mesh mesh, Transforms.ITransform xform)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            if (mesh == null) return Enumerable.Empty<(VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, Material)>();

            return mesh.Primitives.SelectMany(item => item.EvaluateTriangles<TvG, TvM>(xform));
        }

        public static IEnumerable<(VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, VertexBuilder<TvG, TvM, VertexEmpty>, Material)> EvaluateTriangles<TvG, TvM>(this MeshPrimitive prim, Transforms.ITransform xform)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            if (xform == null || !xform.Visible) yield break;

            var vertices = prim.GetVertexColumns();

            vertices.ApplyTransform(xform);

            var triangles = prim.GetTriangleIndices();

            var jointweights = new (int, float)[8];

            foreach (var t in triangles)
            {
                var a = vertices.GetVertex<TvG, TvM>(t.Item1);
                var b = vertices.GetVertex<TvG, TvM>(xform.FlipFaces ? t.Item3 : t.Item2);
                var c = vertices.GetVertex<TvG, TvM>(xform.FlipFaces ? t.Item2 : t.Item3);

                yield return ((a.Geometry, a.Material), (b.Geometry, b.Material), (c.Geometry, c.Material), prim.Material);
            }
        }

        public static VertexBufferColumns GetVertexColumns(this MeshPrimitive primitive)
        {
            Guard.NotNull(primitive, nameof(primitive));

            var columns = new VertexBufferColumns();

            _Initialize(primitive.VertexAccessors, columns);

            for (int i = 0; i < primitive.MorphTargetsCount; ++i)
            {
                var morphTarget = primitive.GetMorphTargetAccessors(i);
                _Initialize(morphTarget, columns.AddMorphTarget());
            }

            return columns;
        }

        private static void _Initialize(IReadOnlyDictionary<string, Accessor> vertexAccessors, VertexBufferColumns dstColumns)
        {
            if (vertexAccessors.ContainsKey("POSITION")) dstColumns.Positions = vertexAccessors["POSITION"].AsVector3Array();
            if (vertexAccessors.ContainsKey("NORMAL")) dstColumns.Normals = vertexAccessors["NORMAL"].AsVector3Array();
            if (vertexAccessors.ContainsKey("TANGENT")) dstColumns.Tangents = vertexAccessors["TANGENT"].AsVector4Array();

            if (vertexAccessors.ContainsKey("COLOR_0")) dstColumns.Colors0 = vertexAccessors["COLOR_0"].AsColorArray();
            if (vertexAccessors.ContainsKey("COLOR_1")) dstColumns.Colors1 = vertexAccessors["COLOR_1"].AsColorArray();

            if (vertexAccessors.ContainsKey("TEXCOORD_0")) dstColumns.TexCoords0 = vertexAccessors["TEXCOORD_0"].AsVector2Array();
            if (vertexAccessors.ContainsKey("TEXCOORD_1")) dstColumns.TexCoords1 = vertexAccessors["TEXCOORD_1"].AsVector2Array();

            if (vertexAccessors.ContainsKey("JOINTS_0")) dstColumns.Joints0 = vertexAccessors["JOINTS_0"].AsVector4Array();
            if (vertexAccessors.ContainsKey("JOINTS_1")) dstColumns.Joints1 = vertexAccessors["JOINTS_1"].AsVector4Array();

            if (vertexAccessors.ContainsKey("WEIGHTS_0")) dstColumns.Weights0 = vertexAccessors["WEIGHTS_0"].AsVector4Array();
            if (vertexAccessors.ContainsKey("WEIGHTS_1")) dstColumns.Weights1 = vertexAccessors["WEIGHTS_1"].AsVector4Array();
        }

        private static void _Initialize(IReadOnlyDictionary<string, Accessor> vertexAccessors, MorphTargetColumns dstColumns)
        {
            if (vertexAccessors.ContainsKey("POSITION")) dstColumns.Positions = vertexAccessors["POSITION"].AsVector3Array();
            if (vertexAccessors.ContainsKey("NORMAL")) dstColumns.Normals = vertexAccessors["NORMAL"].AsVector3Array();
            if (vertexAccessors.ContainsKey("TANGENT")) dstColumns.Tangents = vertexAccessors["TANGENT"].AsVector3Array();

            if (vertexAccessors.ContainsKey("COLOR_0")) dstColumns.Colors0 = vertexAccessors["COLOR_0"].AsVector4Array();
        }

        public static IEnumerable<(int, int, int)> GetTriangleIndices(this MeshPrimitive primitive)
        {
            if (primitive == null) return Enumerable.Empty<(int, int, int)>();

            if (primitive.IndexAccessor == null) return primitive.DrawPrimitiveType.GetTrianglesIndices(primitive.GetVertexAccessor("POSITION").Count);

            return primitive.DrawPrimitiveType.GetTrianglesIndices(primitive.IndexAccessor.AsIndicesArray());
        }

        /// <summary>
        /// Calculates a default set of normals for the given mesh.
        /// </summary>
        /// <param name="mesh">A <see cref="Mesh"/> instance.</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> where the keys represent positions and the values represent Normals.</returns>
        public static Dictionary<Vector3, Vector3> GetComputedNormals(this Mesh mesh)
        {
            var posnrm = new Dictionary<Vector3, Vector3>();

            void addDirection(Dictionary<Vector3, Vector3> dict, Vector3 pos, Vector3 dir)
            {
                if (!dir._IsReal()) return;
                if (!dict.TryGetValue(pos, out Vector3 n)) n = Vector3.Zero;
                dict[pos] = n + dir;
            }

            foreach (var p in mesh.Primitives)
            {
                var positions = p.GetVertexAccessor("POSITION").AsVector3Array();

                foreach (var t in p.GetTriangleIndices())
                {
                    var p1 = positions[t.Item1];
                    var p2 = positions[t.Item2];
                    var p3 = positions[t.Item3];
                    var d = Vector3.Cross(p2 - p1, p3 - p1);
                    addDirection(posnrm, p1, d);
                    addDirection(posnrm, p2, d);
                    addDirection(posnrm, p3, d);
                }
            }

            foreach (var pos in posnrm.Keys.ToList())
            {
                posnrm[pos] = Vector3.Normalize(posnrm[pos]);
            }

            return posnrm;
        }

        public static void AddMesh<TMaterial, TvG, TvM, TvS>(this MeshBuilder<TMaterial, TvG, TvM, TvS> meshBuilder, Mesh srcMesh, Func<Material, TMaterial> materialFunc)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
            where TvS : struct, IVertexSkinning
        {
            Guard.NotNull(meshBuilder, nameof(meshBuilder));

            if (srcMesh == null) return;

            Guard.NotNull(materialFunc, nameof(materialFunc));

            foreach (var srcPrim in srcMesh.Primitives)
            {
                var dstPrim = meshBuilder.UsePrimitive(materialFunc(srcPrim.Material));

                foreach (var tri in srcPrim.EvaluateTriangles<TvG, TvM, TvS>())
                {
                    dstPrim.AddTriangle(tri.Item1, tri.Item2, tri.Item3);
                }
            }
        }

        /// <summary>
        /// Evaluates the current <paramref name="srcScene"/> at a given <paramref name="animation"/> and <paramref name="time"/>
        /// and creates a static <see cref="MeshBuilder{TMaterial, TvG, TvM, TvS}"/>
        /// </summary>
        /// <typeparam name="TMaterial">Any material type</typeparam>
        /// <typeparam name="TvG">A subtype of <see cref="IVertexGeometry"/></typeparam>
        /// <typeparam name="TvM">A subtype of <see cref="IVertexMaterial"/></typeparam>
        /// <param name="srcScene">The source <see cref="Scene"/> to evaluate.</param>
        /// <param name="animation">The source <see cref="Animation"/> to evaluate.</param>
        /// <param name="time">A time point, in seconds, within <paramref name="animation"/>.</param>
        /// <param name="materialFunc">A function to convert <see cref="Material"/> into <typeparamref name="TMaterial"/>.</param>
        /// <returns>A new <see cref="MeshBuilder{TMaterial, TvG, TvM, TvS}"/> containing the evaluated geometry.</returns>
        public static MeshBuilder<TMaterial, TvG, TvM, VertexEmpty> ToStaticMeshBuilder<TMaterial, TvG, TvM>(this Scene srcScene, Animation animation, float time, Func<Material, TMaterial> materialFunc)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            var mesh = new MeshBuilder<TMaterial, TvG, TvM, VertexEmpty>();

            if (srcScene == null) return mesh;

            if (animation != null) Guard.MustShareLogicalParent(srcScene, animation, nameof(animation));

            Guard.NotNull(materialFunc, nameof(materialFunc));

            foreach (var tri in srcScene.EvaluateTriangles<VertexPositionNormal, VertexColor1Texture1>(animation, time))
            {
                var material = materialFunc(tri.Item4);

                mesh.UsePrimitive(material).AddTriangle(tri.Item1, tri.Item2, tri.Item3);
            }

            return mesh;
        }

        public static MeshBuilder<Materials.MaterialBuilder, TvG, TvM, VertexEmpty> ToStaticMeshBuilder<TvG, TvM>(this Scene srcScene, Animation animation, float time)
            where TvG : struct, IVertexGeometry
            where TvM : struct, IVertexMaterial
        {
            var materials = new Dictionary<Material, Materials.MaterialBuilder>();

            Materials.MaterialBuilder convertMaterial(Material srcMaterial)
            {
                if (materials.TryGetValue(srcMaterial, out Materials.MaterialBuilder dstMaterial)) return dstMaterial;

                dstMaterial = new Materials.MaterialBuilder();
                srcMaterial.CopyTo(dstMaterial);

                // if we find an exiting match, we will use it instead.
                var oldMaterial = materials.Values.FirstOrDefault(item => Materials.MaterialBuilder.AreEqual(dstMaterial, item));
                if (oldMaterial != null) dstMaterial = oldMaterial;

                return materials[srcMaterial] = dstMaterial;
            }

            return srcScene.ToStaticMeshBuilder<Materials.MaterialBuilder, TvG, TvM>(animation, time, convertMaterial);
        }

        public static IMeshBuilder<Materials.MaterialBuilder> ToMeshBuilder(this Mesh srcMesh)
        {
            if (srcMesh == null) return null;

            var vertexAttributes = srcMesh.Primitives
                .SelectMany(item => item.VertexAccessors.Keys)
                .Distinct()
                .ToArray();

            Materials.MaterialBuilder defMat = null;

            var dstMaterials = new Dictionary<Material, Materials.MaterialBuilder>();

            var dstMesh = MeshBuilderToolkit.CreateMeshBuilderFromVertexAttributes<Materials.MaterialBuilder>(vertexAttributes);

            foreach (var srcTri in srcMesh.EvaluateTriangles())
            {
                IPrimitiveBuilder dstPrim = null;

                if (srcTri.Item4 == null)
                {
                    if (defMat == null) defMat = Materials.MaterialBuilder.CreateDefault();
                    dstPrim = dstMesh.UsePrimitive(defMat);
                }
                else
                {
                    if (!dstMaterials.TryGetValue(srcTri.Item4, out Materials.MaterialBuilder dstMat))
                    {
                        dstMat = new Materials.MaterialBuilder();
                        srcTri.Item4.CopyTo(dstMat);
                        dstMaterials[srcTri.Item4] = dstMat;
                    }

                    dstPrim = dstMesh.UsePrimitive(dstMat);
                }

                dstPrim.AddTriangle(srcTri.Item1, srcTri.Item2, srcTri.Item3);
            }

            return dstMesh;
        }

        public static void SaveAsWavefront(this ModelRoot model, string filePath)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNullOrEmpty(filePath, nameof(filePath));
            Guard.IsFalse(filePath.Any(c => char.IsWhiteSpace(c)), nameof(filePath), "Whitespace characters not allowed in filename");

            var wf = new IO.WavefrontWriter();
            wf.AddModel(model);
            wf.WriteFiles(filePath);
        }

        public static void SaveAsWavefront(this ModelRoot model, string filePath, Animation animation, float time)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNullOrEmpty(filePath, nameof(filePath));
            Guard.IsFalse(filePath.Any(c => char.IsWhiteSpace(c)), nameof(filePath), "Whitespace characters not allowed in filename");

            var wf = new IO.WavefrontWriter();
            wf.AddModel(model, animation, time);
            wf.WriteFiles(filePath);
        }

        #endregion
    }
}
