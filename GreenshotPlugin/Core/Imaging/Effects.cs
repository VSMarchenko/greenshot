﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using Greenshot.Plugin.Drawing;
using System.IO;
using System.Collections.Generic;
using GreenshotPlugin.Core;

namespace Greenshot.Core {
	/// <summary>
	/// Interface to describe an effect
	/// </summary>
	public interface IEffect {
		Image Apply(Image sourceImage, out Point offsetChange);
	}

	/// <summary>
	/// DropShadowEffect
	/// </summary>
	public class DropShadowEffect : IEffect {
		public DropShadowEffect() {
			Darkness = 0.6f;
			ShadowSize = 7;
			ShadowOffset = new Point(-1, -1);
		}
		public float Darkness {
			get;
			set;
		}
		public int ShadowSize {
			get;
			set;
		}
		public Point ShadowOffset {
			get;
			set;
		}
		public virtual Image Apply(Image sourceImage, out Point offsetChange) {
			return ImageHelper.CreateShadow(sourceImage, Darkness, ShadowSize, ShadowOffset, out offsetChange, PixelFormat.Format32bppArgb);
		}
	}

	/// <summary>
	/// TornEdgeEffect extends on DropShadowEffect
	/// </summary>
	public class TornEdgeEffect : DropShadowEffect {
		public TornEdgeEffect() : base() {
			ShadowSize = 7;
			ToothHeight = 12;
			HorizontalToothRange = 20;
			VerticalToothRange = 20;
		}
		public int ToothHeight {
			get;
			set;
		}
		public int HorizontalToothRange {
			get;
			set;
		}
		public int VerticalToothRange {
			get;
			set;
		}
		public override Image Apply(Image sourceImage, out Point offsetChange) {
			using (Image tmpTornImage = ImageHelper.CreateTornEdge(sourceImage, ToothHeight, HorizontalToothRange, VerticalToothRange)) {
				return ImageHelper.CreateShadow(tmpTornImage, Darkness, ShadowSize, ShadowOffset, out offsetChange, PixelFormat.Format32bppArgb);
			}
		}
	}

	/// <summary>
	/// GrayscaleEffect
	/// </summary>
	public class GrayscaleEffect : IEffect {
		public Image Apply(Image sourceImage, out Point offsetChange) {
			offsetChange = Point.Empty;
			return ImageHelper.CreateGrayscale(sourceImage);
		}
	}

	/// <summary>
	/// MonochromeEffect
	/// </summary>
	public class MonochromeEffect : IEffect {
        private byte threshold;
        /// <param name="threshold">Threshold for monochrome filter (0 - 255), lower value means less black</param>
        public MonochromeEffect(byte threshold) {
            this.threshold = threshold;
        }
		public Image Apply(Image sourceImage, out Point offsetChange) {
			offsetChange = Point.Empty;
			return ImageHelper.CreateMonochrome(sourceImage, threshold);
		}
	}

	/// <summary>
	/// AdjustEffect
	/// </summary>
	public class AdjustEffect : IEffect {
		public AdjustEffect() : base() {
			Contrast = 1f;
			Brightness = 1f;
			Gamma = 1f;
		}
		public float Contrast {
			get;
			set;
		}
		public float Brightness {
			get;
			set;
		}
		public float Gamma {
			get;
			set;
		}
		public Image Apply(Image sourceImage, out Point offsetChange) {
			offsetChange = Point.Empty;
			return ImageHelper.Adjust(sourceImage, Brightness, Contrast, Gamma);
		}
	}

	/// <summary>
	/// ReduceColorsEffect
	/// </summary>
	public class ReduceColorsEffect : IEffect {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ReduceColorsEffect));
		public ReduceColorsEffect() : base() {
			Colors = 256;
		}
		public int Colors {
			get;
			set;
		}
		public Image Apply(Image sourceImage, out Point offsetChange) {
			offsetChange = Point.Empty;
			using (WuQuantizer quantizer = new WuQuantizer((Bitmap)sourceImage)) {
				int colorCount = quantizer.GetColorCount();
				if (colorCount > Colors) {
					try {
						return quantizer.GetQuantizedImage(Colors);
					} catch (Exception e) {
						LOG.Warn("Error occurred while Quantizing the image, ignoring and using original. Error: ", e);
					}
				}
			}
			return null;
		}
	}

	/// <summary>
	/// InvertEffect
	/// </summary>
	public class InvertEffect : IEffect {
		public Image Apply(Image sourceImage, out Point offsetChange) {
			offsetChange = Point.Empty;
			return ImageHelper.CreateNegative(sourceImage);
		}
	}

	/// <summary>
	/// BorderEffect
	/// </summary>
	public class BorderEffect : IEffect {
		public BorderEffect() {
			Width = 2;
			Color = Color.Black;
		}
		public Color Color {
			get;
			set;
		}
		public int Width {
			get;
			set;
		}
		public Image Apply(Image sourceImage, out Point offsetChange) {
			return ImageHelper.CreateBorder(sourceImage, Width, Color, sourceImage.PixelFormat, out offsetChange);
		}
	}

	/// <summary>
	/// RotateEffect
	/// </summary>
	public class RotateEffect : IEffect {
		public RotateEffect(int angle) {
			Angle = angle;
		}
		public int Angle {
			get;
			set;
		}
		public Image Apply(Image sourceImage, out Point offsetChange) {
			offsetChange = Point.Empty;
			RotateFlipType flipType;
			if (Angle == 90) {
				flipType = RotateFlipType.Rotate90FlipNone;
			} else if (Angle == -90 || Angle == 270) {
				flipType = RotateFlipType.Rotate270FlipNone;
			} else {
				throw new NotSupportedException("Currently only an angle of 90 or -90 (270) is supported.");
			}
			return ImageHelper.RotateFlip(sourceImage, flipType);
		}
	}

	/// <summary>
	/// ResizeEffect
	/// </summary>
	public class ResizeEffect : IEffect {
		public ResizeEffect(int width, int height, bool maintainAspectRatio) {
			Width = width;
			Height = height;
			MaintainAspectRatio = maintainAspectRatio;
		}
		public int Width {
			get;
			set;
		}
		public int Height {
			get;
			set;
		}
		public bool MaintainAspectRatio {
			get;
			set;
		}
		public Image Apply(Image sourceImage, out Point offsetChange) {
			offsetChange = Point.Empty;
			return ImageHelper.ResizeImage(sourceImage, MaintainAspectRatio, Width, Height);
		}
	}

	/// <summary>
	/// ResizeCanvasEffect
	/// </summary>
	public class ResizeCanvasEffect : IEffect {
		public ResizeCanvasEffect(int left, int right, int top, int bottom) {
			Left = left;
			Right = right;
			Top = top;
			Bottom = bottom;
			BackgroundColor = Color.Empty;	// Uses the default background color depending on the format
		}
		public int Left {
			get;
			set;
		}
		public int Right {
			get;
			set;
		}
		public int Top {
			get;
			set;
		}
		public int Bottom {
			get;
			set;
		}
		public Color BackgroundColor {
			get;
			set;
		}
		public Image Apply(Image sourceImage, out Point offsetChange) {
			// Make sure the elements move according to the offset the effect made the bitmap move
			offsetChange = new Point(Left, Top);
			return ImageHelper.ResizeCanvas(sourceImage, BackgroundColor, Left, Right, Top, Bottom);
		}
	}
}