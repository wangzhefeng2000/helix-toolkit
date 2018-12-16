﻿using System;
using System.Collections.Generic;
using System.Text;

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
    namespace Animations
    {
        /// <summary>
        /// 
        /// </summary>
        public enum AnimationRepeatMode
        {
            PlayOnce,
            Loop,
            PlayOnceHold,
        }
        /// <summary>
        /// 
        /// </summary>
        public interface IAnimationUpdater
        {
            /// <summary>
            /// Gets or sets the repeat mode.
            /// </summary>
            /// <value>
            /// The repeat mode.
            /// </value>
            AnimationRepeatMode RepeatMode { set; get; }
            /// <summary>
            /// Updates the animation with current time stamp.
            /// </summary>
            /// <param name="timeStamp">The time stamp.</param>
            /// <param name="frequency">The frequency.</param>
            void Update(long timeStamp, long frequency);
            /// <summary>
            /// Resets this animation.
            /// </summary>
            void Reset();
        }
        /// <summary>
        ///
        /// </summary>
        public interface IAnimationNode
        {
            /// <summary>
            /// Gets or sets a value indicating whether this scene node is animation node.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this scene node is animation node; otherwise, <c>false</c>.
            /// </value>
            bool IsAnimationNode { set; get; }
            /// <summary>
            /// Gets a value indicating whether this scene node is animation node root.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this scene node is animation node root; otherwise, <c>false</c>.
            /// </value>
            bool IsAnimationNodeRoot { get; }
        }
    }
}
