﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using Assimp.Configs;
using SharpDX;
using SharpDX.Direct3D11;
using Animation = Assimp.Animation;
using TextureType = Assimp.TextureType;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    using HxScene = Model.Scene;
    using HxAnimations = Animations;
    using Model;
    namespace Assimp
    {
        public partial class Importer
        {
            protected virtual ErrorCode ProcessNodeAnimation(NodeAnimationChannel channel, double ticksPerSecond, out FastList<HxAnimations.Keyframe1> list)
            {
                var posCount = channel.PositionKeyCount;
                var rotCount = channel.RotationKeyCount;
                var scaleCount = channel.ScalingKeyCount;
                if (posCount != rotCount || rotCount != scaleCount)
                {
                    list = null;
                    ErrorCode |= ErrorCode.NonUniformAnimationKeyDoesNotSupported;
                    return ErrorCode.NonUniformAnimationKeyDoesNotSupported;
                }

                var ret = new FastList<HxAnimations.Keyframe1>(posCount);
                for (var i = 0; i < posCount; ++i)
                    ret.Add(new HxAnimations.Keyframe1
                    {
                        Time = (float)(channel.PositionKeys[i].Time / ticksPerSecond),
                        Position = channel.PositionKeys[i].Value.ToSharpDXVector3(),
                        Rotation = channel.RotationKeys[i].Value.ToSharpDXQuaternion(),
                        Scale = channel.ScalingKeys[i].Value.ToSharpDXVector3()
                    });
                list = ret;
                return ErrorCode.Succeed;
            }

            private ErrorCode LoadAnimations(HelixInternalScene scene)
            {
                var dict = new Dictionary<string, HxScene.SceneNode>(SceneNodes.Count);
                foreach (var node in SceneNodes)
                    if (!dict.ContainsKey(node.Name))
                        dict.Add(node.Name, node);

                foreach (var mesh in scene.Meshes.Where(x => x.Mesh is BoneSkinnedMeshGeometry3D)
                    .Select(x => x.Mesh as BoneSkinnedMeshGeometry3D))
                    if (mesh.Bones != null && mesh.BoneNames != null && mesh.Bones.Count == mesh.BoneNames.Count)
                        for (var i = 0; i < mesh.Bones.Count; ++i)
                            if (dict.TryGetValue(mesh.BoneNames[i], out var s))
                            {
                                var b = mesh.Bones[i];
                                b.ParentNode = s.Parent;
                                b.Node = s;
                                mesh.Bones[i] = b;
                                s.IsAnimationNode = true; // Make sure to set this to true
                            }

                if (scene.AssimpScene.HasAnimations)
                {
                    var animationList = new List<HxAnimations.Animation>(scene.AssimpScene.AnimationCount);
                    if (Configuration.EnableParallelProcessing)
                        Parallel.ForEach(scene.AssimpScene.Animations, ani =>
                        {
                            if (LoadAnimation(ani, dict, out var hxAni) == ErrorCode.Succeed)
                                lock (animationList)
                                {
                                    animationList.Add(hxAni);
                                }
                        });
                    else
                        foreach (var ani in scene.AssimpScene.Animations)
                            if (LoadAnimation(ani, dict, out var hxAni) == ErrorCode.Succeed)
                                animationList.Add(hxAni);
                    scene.Animations = animationList;
                    Animations.AddRange(animationList);
                }
                return ErrorCode.Succeed;
            }

            private ErrorCode LoadAnimation(Animation ani, Dictionary<string, HxScene.SceneNode> dict,
                out HxAnimations.Animation hxAni)
            {
                hxAni = new HxAnimations.Animation(HxAnimations.AnimationType.Node)
                {
                    StartTime = 0,
                    EndTime = (float)(ani.DurationInTicks / ani.TicksPerSecond),
                    Name = ani.Name,
                    NodeAnimationCollection = new List<HxAnimations.NodeAnimation>(ani.NodeAnimationChannelCount)
                };

                if (ani.HasNodeAnimations)
                {
                    var code = ErrorCode.None;
                    foreach (var key in ani.NodeAnimationChannels)
                        if (dict.TryGetValue(key.NodeName, out var node))
                        {
                            var nAni = new HxAnimations.NodeAnimation
                            {
                                Node = node
                            };
                            node.IsAnimationNode = true;// Make sure to set this to true
                            code = ProcessNodeAnimation(key, ani.TicksPerSecond, out var keyframes);
                            if (code == ErrorCode.Succeed)
                            {
                                nAni.KeyFrames = keyframes;
                                hxAni.NodeAnimationCollection.Add(nAni);
                            }
                            else
                            {
                                break;
                            }
                        }

                    return code;
                }

                return ErrorCode.Failed;
            }

        }
    }
}