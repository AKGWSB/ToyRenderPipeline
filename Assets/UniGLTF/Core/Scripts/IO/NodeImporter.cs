﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UniGLTF
{
    public static class NodeImporter
    {
        public static GameObject ImportNode(glTFNode node)
        {
            var nodeName = node.name;
            if (!string.IsNullOrEmpty(nodeName) && nodeName.Contains("/"))
            {
                Debug.LogWarningFormat("node {0} contains /. replace _", node.name);
                nodeName = nodeName.Replace("/", "_");
            }
            var go = new GameObject(nodeName);

            //
            // transform
            //
            if (node.translation != null && node.translation.Length > 0)
            {
                go.transform.localPosition = new Vector3(
                    node.translation[0],
                    node.translation[1],
                    node.translation[2]);
            }
            if (node.rotation != null && node.rotation.Length > 0)
            {
                go.transform.localRotation = new Quaternion(
                    node.rotation[0],
                    node.rotation[1],
                    node.rotation[2],
                    node.rotation[3]);
            }
            if (node.scale != null && node.scale.Length > 0)
            {
                go.transform.localScale = new Vector3(
                    node.scale[0],
                    node.scale[1],
                    node.scale[2]);
            }
            if (node.matrix != null && node.matrix.Length > 0)
            {
                var m = UnityExtensions.MatrixFromArray(node.matrix);
                go.transform.localRotation = m.ExtractRotation();
                go.transform.localPosition = m.ExtractPosition();
                go.transform.localScale = m.ExtractScale();
            }
            return go;
        }

        public class TransformWithSkin
        {
            public Transform Transform;
            public GameObject GameObject { get { return Transform.gameObject; } }
            public int? SkinIndex;
        }

        public static TransformWithSkin BuildHierarchy(ImporterContext context, int i)
        {
            var go = context.Nodes[i].gameObject;
            if (string.IsNullOrEmpty(go.name))
            {
                go.name = string.Format("node{0:000}", i);
            }

            var nodeWithSkin = new TransformWithSkin
            {
                Transform = go.transform,
            };

            //
            // build hierachy
            //
            var node = context.GLTF.nodes[i];
            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    context.Nodes[child].transform.SetParent(context.Nodes[i].transform,
                        false // node has local transform
                        );
                }
            }

            //
            // attach mesh
            //
            if (node.mesh != -1)
            {
                var mesh = context.Meshes[node.mesh];
                if (mesh.Mesh.blendShapeCount == 0 && node.skin == -1)
                {
                    // without blendshape and bone skinning
                    var filter = go.AddComponent<MeshFilter>();
                    filter.sharedMesh = mesh.Mesh;
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = mesh.Materials;
                    // invisible in loading
                    renderer.enabled = false;
                    mesh.Renderers.Add(renderer);
                }
                else
                {
                    var renderer = go.AddComponent<SkinnedMeshRenderer>();

                    if (node.skin != -1)
                    {
                        nodeWithSkin.SkinIndex = node.skin;
                    }

                    renderer.sharedMesh = mesh.Mesh;
                    renderer.sharedMaterials = mesh.Materials;
                    // invisible in loading
                    renderer.enabled = false;
                    mesh.Renderers.Add(renderer);
                }
            }

            return nodeWithSkin;
        }

        //
        // fix node's coordinate. z-back to z-forward
        //
        public static void FixCoordinate(ImporterContext context, List<TransformWithSkin> nodes)
        {
            var globalTransformMap = nodes.ToDictionary(x => x.Transform, x => new PosRot
            {
                Position = x.Transform.position,
                Rotation = x.Transform.rotation,
            });
            foreach (var x in context.GLTF.rootnodes)
            {
                // fix nodes coordinate
                // reverse Z in global
                var t = nodes[x].Transform;
                //t.SetParent(root.transform, false);

                foreach (var transform in t.Traverse())
                {
                    var g = globalTransformMap[transform];
                    transform.position = g.Position.ReverseZ();
                    transform.rotation = g.Rotation.ReverseZ();
                }
            }
        }

        public static void SetupSkinning(ImporterContext context, List<TransformWithSkin> nodes, int i)
        {
            var x = nodes[i];
            var skinnedMeshRenderer = x.Transform.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                var mesh = skinnedMeshRenderer.sharedMesh;
                if (x.SkinIndex.HasValue)
                {
                    if (mesh == null) throw new Exception();
                    if (skinnedMeshRenderer == null) throw new Exception();

                    if (x.SkinIndex.Value < context.GLTF.skins.Count)
                    {
                        var skin = context.GLTF.skins[x.SkinIndex.Value];

                        skinnedMeshRenderer.sharedMesh = null;

                        var joints = skin.joints.Select(y => nodes[y].Transform).ToArray();
                        skinnedMeshRenderer.bones = joints;

                        if (skin.skeleton >= 0 && skin.skeleton < nodes.Count)
                        {
                            skinnedMeshRenderer.rootBone = nodes[skin.skeleton].Transform;
                        }

                        if (skin.inverseBindMatrices != -1)
                        {
                            // BlendShape only ?
#if false
                            // https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html
                            var hipsParent = nodes[0].Transform;
                            var calculatedBindPoses = joints.Select(y => y.worldToLocalMatrix * hipsParent.localToWorldMatrix).ToArray();
                            mesh.bindposes = calculatedBindPoses;
#else
                            var bindPoses = context.GLTF.GetArrayFromAccessor<Matrix4x4>(skin.inverseBindMatrices)
                                .Select(y => y.ReverseZ())
                                .ToArray()
                                ;
                            mesh.bindposes = bindPoses;
#endif
                        }

                        skinnedMeshRenderer.sharedMesh = mesh;
                    }
                }
            }
        }

    }
}
