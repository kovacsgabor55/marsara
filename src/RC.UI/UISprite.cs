﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Common;

namespace RC.UI
{
    /// <summary>
    /// Represents a 2D image that can be rendered to an IUIRenderContext.
    /// </summary>
    public abstract class UISprite
    {
        /// <summary>
        /// Constructs a UISprite object.
        /// </summary>
        /// <param name="width">The with of this UISprite in logical pixels.</param>
        /// <param name="height">The height of this UISprite in logical pixels.</param>
        /// <param name="pixelSize">The pixel size of the UISprite.</param>
        public UISprite(int width, int height, RCIntVector pixelSize)
        {
            this.size = new RCIntVector(width, height);
            this.pixelSize = pixelSize;
            this.transparentColor = UIColor.Undefined;
        }

        /// <summary>
        /// Gets the ID of this UISprite resource.
        /// </summary>
        public abstract int ResourceId { get; }

        /// <summary>
        /// Gets the pixel size of this UISprite. The X coordinate of the returned RCIntVector is
        /// the horizontal pixel size, the Y coordinate is the vertical pixel size.
        /// </summary>
        public RCIntVector PixelSize { get { return this.pixelSize; } }

        /// <summary>
        /// Gets or sets the color that is used to mark transparent pixels of this UISprite.
        /// UIColor.Undefined means that there is no transparency defined.
        /// </summary>
        public UIColor TransparentColor
        {
            get { return this.transparentColor; }
            set
            {
                if (this.transparentColor != value)
                {
                    this.transparentColor = value;
                    TransparentColor_set(this.transparentColor);
                }
            }
        }

        /// <summary>
        /// Gets the size of this UISprite in logical pixels.
        /// </summary>
        public RCIntVector Size { get { return this.size; } }

        /// <summary>
        /// Saves this UISprite to the given file.
        /// </summary>
        /// <param name="fileName">The name of the target file.</param>
        /// <remarks>For debugging.</remarks>
        public abstract void Save(string fileName);

        /// <summary>
        /// Internal method to setting a new transparent color.
        /// </summary>
        /// <param name="newColor">The new transparent color to set.</param>
        /// <remarks>This method is only called when the transparent color has been changed.</remarks>
        protected abstract void TransparentColor_set(UIColor newColor);

        /// <summary>
        /// The color that is used to mark transparent pixels of this UISprite or UIColor.Undefined if
        /// there is no transparency defined.
        /// </summary>
        private UIColor transparentColor;

        /// <summary>
        /// The pixel size of this UISprite.
        /// </summary>
        private RCIntVector pixelSize;

        /// <summary>
        /// The size of this UISprite in logical pixels.
        /// </summary>
        private RCIntVector size;
    }
}