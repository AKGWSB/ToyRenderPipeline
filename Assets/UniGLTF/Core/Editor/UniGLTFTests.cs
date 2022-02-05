﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UniGLTF
{
    public class UniGLTFTests
    {
        static GameObject CreateSimpelScene()
        {
            var root = new GameObject("gltfRoot").transform;

            var scene = new GameObject("scene0").transform;
            scene.SetParent(root, false);
            scene.localPosition = new Vector3(1, 2, 3);

            return root.gameObject;
        }

        void AssertAreEqual(Transform go, Transform other)
        {
            var lt = go.Traverse().GetEnumerator();
            var rt = go.Traverse().GetEnumerator();

            while (lt.MoveNext())
            {
                if (!rt.MoveNext())
                {
                    throw new Exception("rt shorter");
                }

                MonoBehaviourComparator.AssertAreEquals(lt.Current.gameObject, rt.Current.gameObject);
            }

            if (rt.MoveNext())
            {
                throw new Exception("rt longer");
            }
        }

        [Test]
        public void UniGLTFSimpleSceneTest()
        {
            var go = CreateSimpelScene();
            var context = new ImporterContext();

            try
            {
                // export
                var gltf = new glTF();
                using (var exporter = new gltfExporter(gltf))
                {
                    exporter.Prepare(go);
                    exporter.Export();

                    // import
                    context.ParseJson(gltf.ToJson(), new SimpleStorage(new ArraySegment<byte>()));
                    //Debug.LogFormat("{0}", context.Json);
                    context.Load();

                    AssertAreEqual(go.transform, context.Root.transform);
                }
            }
            finally
            {
                //Debug.LogFormat("Destory, {0}", go.name);
                GameObject.DestroyImmediate(go);
                context.Destroy(true);
            }
        }

        void BufferTest(int init, params int[] size)
        {
            var initBytes = init == 0 ? null : new byte[init];
            var storage = new ArrayByteBuffer(initBytes);
            var buffer = new glTFBuffer(storage);

            var values = new List<byte>();
            int offset = 0;
            foreach (var x in size)
            {
                var nums = Enumerable.Range(offset, x).Select(y => (Byte)y).ToArray();
                values.AddRange(nums);
                var bytes = new ArraySegment<Byte>(nums);
                offset += x;
                buffer.Append(bytes, glBufferTarget.NONE);
            }

            Assert.AreEqual(values.Count, buffer.byteLength);
            Assert.True(Enumerable.SequenceEqual(values, buffer.GetBytes().ToArray()));
        }

        [Test]
        public void BufferTest()
        {
            BufferTest(0, 0, 100, 200);
            BufferTest(0, 128);
            BufferTest(0, 256);

            BufferTest(1024, 0);
            BufferTest(1024, 128);
            BufferTest(1024, 2048);
            BufferTest(1024, 900, 900);
        }

        [Test]
        public void UnityPathTest()
        {
            var root = UnityPath.FromUnityPath(".");
            Assert.IsFalse(root.IsNull);
            Assert.IsFalse(root.IsUnderAssetsFolder);
            Assert.AreEqual(UnityPath.FromUnityPath("."), root);

            var assets = UnityPath.FromUnityPath("Assets");
            Assert.IsFalse(assets.IsNull);
            Assert.IsTrue(assets.IsUnderAssetsFolder);

            var rootChild = root.Child("Assets");
            Assert.AreEqual(assets, rootChild);

            var assetsChild = assets.Child("Hoge");
            var hoge = UnityPath.FromUnityPath("Assets/Hoge");
            Assert.AreEqual(assetsChild, hoge);

            //var children = root.TravserseDir().ToArray();
        }

        [Test]
        public void VersionChecker()
        {
            Assert.False(ImporterContext.IsGeneratedUniGLTFAndOlderThan("hoge", 1, 16));
            Assert.False(ImporterContext.IsGeneratedUniGLTFAndOlderThan("UniGLTF-1.16", 1, 16));
            Assert.True(ImporterContext.IsGeneratedUniGLTFAndOlderThan("UniGLTF-1.15", 1, 16));
            Assert.False(ImporterContext.IsGeneratedUniGLTFAndOlderThan("UniGLTF-11.16", 1, 16));
            Assert.True(ImporterContext.IsGeneratedUniGLTFAndOlderThan("UniGLTF-0.16", 1, 16));
            Assert.True(ImporterContext.IsGeneratedUniGLTFAndOlderThan("UniGLTF", 1, 16));
        }
    }
}
