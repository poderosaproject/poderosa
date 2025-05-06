// Copyright 2025 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Poderosa.Document;
using Poderosa.Terminal.EscapeSequence;
using Poderosa.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Poderosa.Terminal.Sixel {

    /// <summary>
    /// SIXEL constant values.
    /// </summary>
    internal static class SixelConstants {
        public const int MAX_IMAGE_WIDTH = 4096;
        public const int MAX_IMAGE_HEIGHT = 4096;
        public const int MAX_IMAGES = 32;
    }

    /// <summary>
    /// SIXEL color palette.
    /// </summary>
    internal class SixelPalette {
        private readonly Color[] _colors = new Color[256];

        public SixelPalette() {
            // VT340 default color map
            _colors[0] = Color.FromArgb(ConvertLevel(0), ConvertLevel(0), ConvertLevel(0)); // Black
            _colors[1] = Color.FromArgb(ConvertLevel(20), ConvertLevel(20), ConvertLevel(80)); // Blue
            _colors[2] = Color.FromArgb(ConvertLevel(80), ConvertLevel(13), ConvertLevel(13)); // Red
            _colors[3] = Color.FromArgb(ConvertLevel(20), ConvertLevel(80), ConvertLevel(20)); // Green
            _colors[4] = Color.FromArgb(ConvertLevel(80), ConvertLevel(20), ConvertLevel(80)); // Magenta
            _colors[5] = Color.FromArgb(ConvertLevel(20), ConvertLevel(80), ConvertLevel(80)); // Cyan
            _colors[6] = Color.FromArgb(ConvertLevel(80), ConvertLevel(80), ConvertLevel(20)); // Yellow
            _colors[7] = Color.FromArgb(ConvertLevel(53), ConvertLevel(53), ConvertLevel(53)); // Gray 50%
            _colors[8] = Color.FromArgb(ConvertLevel(26), ConvertLevel(26), ConvertLevel(26)); // Gray 25%
            _colors[9] = Color.FromArgb(ConvertLevel(33), ConvertLevel(33), ConvertLevel(60)); // Pale Blue
            _colors[10] = Color.FromArgb(ConvertLevel(60), ConvertLevel(26), ConvertLevel(26)); // Pale Red
            _colors[11] = Color.FromArgb(ConvertLevel(33), ConvertLevel(60), ConvertLevel(33)); // Pale Green
            _colors[12] = Color.FromArgb(ConvertLevel(60), ConvertLevel(33), ConvertLevel(60)); // Pale Magenta
            _colors[13] = Color.FromArgb(ConvertLevel(33), ConvertLevel(60), ConvertLevel(60)); // Pale Cyan
            _colors[14] = Color.FromArgb(ConvertLevel(60), ConvertLevel(60), ConvertLevel(33)); // Pale Yellow
            _colors[15] = Color.FromArgb(ConvertLevel(80), ConvertLevel(80), ConvertLevel(80)); // Gray 75%

            for (int i = 16; i < _colors.Length; i++) {
                _colors[i] = Color.Black;
            }
        }

        private static int ConvertLevel(int level) {
            return Math.Min(Math.Max(level * 255 / 100, 0), 255);
        }

        /// <summary>
        /// Set color by component level (0-100)
        /// </summary>
        /// <param name="n">color number</param>
        /// <param name="r">red level (0-100)</param>
        /// <param name="g">green level (0-100)</param>
        /// <param name="b">blue level (0-100)</param>
        public void SetColorByLevel(int n, int r, int g, int b) {
            _colors[n] = Color.FromArgb(ConvertLevel(r), ConvertLevel(g), ConvertLevel(b));
        }

        /// <summary>
        /// Accessor to the color of the specified color number.
        /// </summary>
        /// <param name="n">color number (0 - 255)</param>
        /// <returns>color</returns>
        public Color this[int n] {
            get {
                return _colors[n];
            }
            set {
                _colors[n] = value;
            }
        }
    }

    /// <summary>
    /// Temporal buffer to build a single sixel row (6 pixels height)
    /// </summary>
    /// <remarks>
    /// Each pixel color will be determined immediately with the palette at the time.
    /// This is a design tradeoff to improve performance.
    /// </remarks>
    internal class SixelTemporalRowBuffer : IDisposable {

        // each int has premultiplied ARGB
        private readonly List<int> _sixelColors0 = new List<int>();
        private readonly List<int> _sixelColors1 = new List<int>();
        private readonly List<int> _sixelColors2 = new List<int>();
        private readonly List<int> _sixelColors3 = new List<int>();
        private readonly List<int> _sixelColors4 = new List<int>();
        private readonly List<int> _sixelColors5 = new List<int>();

        private int _index = 0;

        private const PixelFormat PIXELFORMAT = PixelFormat.Format32bppPArgb;
        private Bitmap _rowBitmap = new Bitmap(1, 6, PIXELFORMAT);

        /// <summary>
        /// Row width in pixels.
        /// </summary>
        public int Width {
            get {
                return _sixelColors0.Count;
            }
        }

        /// <summary>
        /// Current horizontal position.
        /// </summary>
        public int CurrentX {
            get {
                return _index;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose() {
            _rowBitmap.Dispose();
            _rowBitmap = null;
        }

        /// <summary>
        /// Put vertical 6 pixels and advance the current position.
        /// </summary>
        /// <param name="pattern">sixel pattern (0 - 64)</param>
        /// <param name="color">color code (0 - 255)</param>
        /// <param name="palette">palette</param>
        public void Put(int pattern, int color, SixelPalette palette) {
            if (_index >= SixelConstants.MAX_IMAGE_WIDTH) {
                return;
            }

            int argb = palette[color].ToArgb();

            int sixelBits = pattern & 0x3f;

            int m0 = (sixelBits & 1) - 1;
            int d0 = ~m0 & argb;
            sixelBits >>= 1;
            int m1 = (sixelBits & 1) - 1;
            int d1 = ~m1 & argb;
            sixelBits >>= 1;
            int m2 = (sixelBits & 1) - 1;
            int d2 = ~m2 & argb;
            sixelBits >>= 1;
            int m3 = (sixelBits & 1) - 1;
            int d3 = ~m3 & argb;
            sixelBits >>= 1;
            int m4 = (sixelBits & 1) - 1;
            int d4 = ~m4 & argb;
            sixelBits >>= 1;
            int m5 = (sixelBits & 1) - 1;
            int d5 = ~m5 & argb;

            if (_index < _sixelColors0.Count) {
                _sixelColors0[_index] = _sixelColors0[_index] & m0 | d0;
                _sixelColors1[_index] = _sixelColors1[_index] & m1 | d1;
                _sixelColors2[_index] = _sixelColors2[_index] & m2 | d2;
                _sixelColors3[_index] = _sixelColors3[_index] & m3 | d3;
                _sixelColors4[_index] = _sixelColors4[_index] & m4 | d4;
                _sixelColors5[_index] = _sixelColors5[_index] & m5 | d5;
            }
            else {
                _sixelColors0.Add(d0);
                _sixelColors1.Add(d1);
                _sixelColors2.Add(d2);
                _sixelColors3.Add(d3);
                _sixelColors4.Add(d4);
                _sixelColors5.Add(d5);
            }
            _index++;
        }

        /// <summary>
        /// Reset current position to the left end.
        /// </summary>
        public void CarriageReturn() {
            _index = 0;
        }

        /// <summary>
        /// Clear buffer.
        /// </summary>
        public void Clear() {
            _index = 0;
            _sixelColors0.Clear();
            _sixelColors1.Clear();
            _sixelColors2.Clear();
            _sixelColors3.Clear();
            _sixelColors4.Clear();
            _sixelColors5.Clear();
        }

        /// <summary>
        /// Get bitmap of the sixel row.
        /// </summary>
        /// <returns>bitmap. Do not dispose this bitmap as it will be reused later.</returns>
        public Bitmap GetSixelBitmap() {
            int rowWidth = this.Width;

            if (rowWidth > 0) {
                if (_rowBitmap.Width < rowWidth) {
                    _rowBitmap.Dispose();
                    _rowBitmap = new Bitmap(rowWidth, 6, PIXELFORMAT);
                }

                BitmapData bmpData = _rowBitmap.LockBits(new Rectangle(0, 0, rowWidth, 6), ImageLockMode.WriteOnly, PIXELFORMAT);
                IntPtr dest = bmpData.Scan0;
                int stride = bmpData.Stride;
                Marshal.Copy(_sixelColors0.ToArray(), 0, dest, rowWidth);
                dest += stride;
                Marshal.Copy(_sixelColors1.ToArray(), 0, dest, rowWidth);
                dest += stride;
                Marshal.Copy(_sixelColors2.ToArray(), 0, dest, rowWidth);
                dest += stride;
                Marshal.Copy(_sixelColors3.ToArray(), 0, dest, rowWidth);
                dest += stride;
                Marshal.Copy(_sixelColors4.ToArray(), 0, dest, rowWidth);
                dest += stride;
                Marshal.Copy(_sixelColors5.ToArray(), 0, dest, rowWidth);
                _rowBitmap.UnlockBits(bmpData);
            }

            return _rowBitmap;
        }
    }

    /// <summary>
    /// Bitmap for SIXEL image.
    /// </summary>
    /// <remarks>
    /// Although SIXEL image should be the indexed-color bitmap, this bitmap uses 32bit ARGB.
    /// After put a pixel, its color is not affected by the palette color change.
    /// This is a design tradeoff.
    /// With GDI or GDI+, it is not easy to display indexed-color bitmap which has transparent pixels.
    /// </remarks>
    internal class SixelBitmap : IDisposable {
        private readonly object _sync = new object();
        private Bitmap _bitmap = null;

        private int _x = 0;
        private int _y = 0;
        private int _bottom = 0;

        // trimmed range
        private int? _visibleTop = null;
        private int? _visibleBottom = null;

        public SixelBitmap() {
        }

        /// <summary>
        /// Size of bitmap in pixels.
        /// </summary>
        public Size Size {
            get {
                lock (_sync) {
                    return (_bitmap != null) ? _bitmap.Size : Size.Empty;
                }
            }
        }

        /// <summary>
        /// Current horizontal position of the sixel data.
        /// </summary>
        public int CurrentSixelDataX {
            get {
                return _x;
            }
        }

        /// <summary>
        /// Current vertical position of the sixel data.
        /// </summary>
        public int CurrentSixelDataY {
            get {
                return _y;
            }
        }

        /// <summary>
        /// Right-bottom corner of the last sixel data
        /// </summary>
        public Point LastSixelDataRightBottom {
            get {
                lock (_sync) {
                    return new Point(_x, _bottom);
                }
            }
        }

        /// <summary>
        /// Dispose bitmap.
        /// </summary>
        public void Dispose() {
            lock (_sync) {
                if (_bitmap != null) {
                    _bitmap.Dispose();
                    _bitmap = null;
                }
            }
        }

        /// <summary>
        /// Draw bitmap.
        /// </summary>
        /// <param name="g">graphics object</param>
        /// <param name="p">position to draw bitmap</param>
        public void Draw(Graphics g, Point p) {
            lock (_sync) {
                if (_bitmap != null) {
                    g.DrawImage(_bitmap, p);
                }
            }
        }

        /// <summary>
        /// Fill background in this bitmap.
        /// </summary>
        /// <param name="width">width in pixels</param>
        /// <param name="height">height in pixels</param>
        /// <param name="color">fill color</param>
        public void FillBackground(int width, int height, Color color) {
            int w = Math.Min(Math.Max(width, 1), SixelConstants.MAX_IMAGE_WIDTH);
            int h = Math.Min(Math.Max(height, 1), SixelConstants.MAX_IMAGE_HEIGHT);
            lock (_sync) {
                ExpandBitmap(w, h, color);
            }
        }

        /// <summary>
        /// Expand bitmap to satisfy specified size.
        /// </summary>
        /// <param name="minWidth">minimum width</param>
        /// <param name="minHeight">minimum height</param>
        public void Expand(int minWidth, int minHeight) {
            int w = Math.Min(Math.Max(minWidth, 1), SixelConstants.MAX_IMAGE_WIDTH);
            int h = Math.Min(Math.Max(minHeight, 1), SixelConstants.MAX_IMAGE_HEIGHT);
            lock (_sync) {
                ExpandBitmap(w, h, null);
            }
        }

        /// <summary>
        /// Place a single sixel row (6 pixels height) from the current position.
        /// </summary>
        /// <param name="rowBuffer">sixel row buffer</param>
        /// <param name="pixelSize">pixel size</param>
        public void Put(SixelTemporalRowBuffer rowBuffer, Size pixelSize) {
            lock (_sync) {
                if (_y >= SixelConstants.MAX_IMAGE_HEIGHT) {
                    return;
                }

                int rowBuffWidth = rowBuffer.Width;
                int rowWidth = rowBuffWidth * pixelSize.Width;
                int rowHeight = 6 * pixelSize.Height;
                int newBitmapWidth = Math.Min(Math.Max(_x + rowWidth, 1), SixelConstants.MAX_IMAGE_WIDTH);
                int newBitmapHeight = _y + rowHeight;
                ExpandBitmap(newBitmapWidth, newBitmapHeight, null);

                if (rowWidth > 0) {
                    Bitmap tmpBitmap = rowBuffer.GetSixelBitmap(); // bitmap will be reused. do not dispose it.
                    Debug.Assert(tmpBitmap.Width >= rowBuffWidth);
                    Debug.Assert(tmpBitmap.Height >= 6);
                    using (Graphics g = Graphics.FromImage(_bitmap)) {
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.DrawImage(tmpBitmap, new Rectangle(_x, _y, rowWidth, rowHeight), new Rectangle(0, 0, rowBuffWidth, 6), GraphicsUnit.Pixel);
                    }
                }
                _x += rowBuffer.CurrentX * pixelSize.Width;
                _bottom = newBitmapHeight;
            }
        }

        /// <summary>
        /// Reset sixel horizontal position.
        /// </summary>
        public void CarriageReturn() {
            _x = 0;
        }

        /// <summary>
        /// Reset sixel horizontal position and advance vertial position to the next sixel row.
        /// </summary>
        /// <param name="pixelSize">pixel size</param>
        public void NextRow(Size pixelSize) {
            _x = 0;
            int rowHeight = pixelSize.Height * 6;
            _y += rowHeight;
            _bottom = _y + rowHeight;
        }

        /// <summary>
        /// Clear specified rectangles.
        /// </summary>
        /// <param name="rectangles">rectangles</param>
        public void ClearRectangles(Rectangle[] rectangles) {
            lock (_sync) {
                if (_bitmap != null) {
                    ClearRectanglesInternal(rectangles);
                }
            }
        }

        private void ClearRectanglesInternal(Rectangle[] rectangles) {
            using (Brush brush = new SolidBrush(Color.Transparent)) {
                using (Graphics g = Graphics.FromImage(_bitmap)) {
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.FillRectangles(brush, rectangles);
                }
            }
        }

        /// <summary>
        /// Clear vertical range.
        /// </summary>
        /// <param name="top">top of the vertical range to clear (inclusive)</param>
        /// <param name="bottom">bottom of the vertical range to clear (exclusive)</param>
        /// <param name="empty">returned true if this bitmap becomes empty</param>
        public void ClearVerticalRange(int top, int bottom, out bool empty) {
            lock (_sync) {
                if (_bitmap == null) {
                    // constructing
                    empty = false;
                    return;
                }

                Size imageSize = this.Size;

                if (top < bottom && bottom > 0 && top < imageSize.Height) {
                    ClearRectanglesInternal(
                        new Rectangle[] { new Rectangle(0, top, imageSize.Width, bottom - top) }
                    );

                    int tTop = _visibleTop ?? 0;
                    if (top <= tTop) {
                        _visibleTop = Math.Max(tTop, bottom);
                    }
                    int tBottom = _visibleBottom ?? imageSize.Height;
                    if (bottom >= tBottom) {
                        _visibleBottom = Math.Min(tBottom, top);
                    }
                }

                empty = (_visibleTop ?? 0) >= (_visibleBottom ?? imageSize.Height);
            }
        }

        /// <summary>
        /// Expand or rebuild bitmap if neccessary.
        /// </summary>
        /// <param name="minWidth">minimum width</param>
        /// <param name="minHeight">minimum height</param>
        /// <param name="backColor">color to fill background, or null if background is to be retained.</param>
        private void ExpandBitmap(int minWidth, int minHeight, Color? backColor) {
            Bitmap oldBitmap = _bitmap;
            if (backColor.HasValue || oldBitmap == null || oldBitmap.Width < minWidth || oldBitmap.Height < minHeight) {
                int newWidth;
                int newHeight;
                if (oldBitmap == null) {
                    newWidth = minWidth;
                    newHeight = minHeight;
                }
                else {
                    newWidth = Math.Max(minWidth, oldBitmap.Width);
                    newHeight = Math.Max(minHeight, oldBitmap.Height);
                }

                if (backColor.HasValue) {
                    _bitmap = CreateNewBitmap(newWidth, newHeight, null, new Size(minWidth, minHeight), backColor, oldBitmap);
                }
                else {
                    _bitmap = CreateNewBitmap(newWidth, newHeight, null, null, null, oldBitmap);
                }

                if (oldBitmap != null) {
                    oldBitmap.Dispose();
                }
            }
        }

        /// <summary>
        /// Create a new bitmap.
        /// </summary>
        /// <param name="width">bitmap width</param>
        /// <param name="height">bitmap height</param>
        /// <param name="underBitmap">another bitmap to draw in the bottom layer, or null.</param>
        /// <param name="fillSize">size to fill background, or null if no background fill.</param>
        /// <param name="fillColor">color to fill background, or null if no background fill.</param>
        /// <param name="oldBitmap">previous bitmap, or null if it does not exist.</param>
        /// <returns>new bitmap</returns>
        private static Bitmap CreateNewBitmap(int width, int height, Bitmap underBitmap, Size? fillSize, Color? fillColor, Bitmap oldBitmap) {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bmp)) {
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;

                g.Clear(Color.Transparent);

                if (underBitmap != null) {
                    g.DrawImageUnscaled(underBitmap, 0, 0);
                }

                if (fillSize.HasValue && fillColor.HasValue) {
                    using (SolidBrush brush = new SolidBrush(fillColor.Value)) {
                        g.FillRectangle(brush, 0, 0, fillSize.Value.Width, fillSize.Value.Height);
                    }
                }

                if (oldBitmap != null) {
                    g.DrawImageUnscaled(oldBitmap, 0, 0);
                }
            }
            return bmp;
        }
    }

    /// <summary>
    /// SIXEL image which has bitmap and position.
    /// </summary>
    internal class SixelImage : IDisposable {
        private GLineZOrder? _z;
        private int _lineId;
        private readonly int _columnIndex;
        private readonly SixelBitmap _bitmap = new SixelBitmap();
        private bool _constructing;

        /// <summary>
        /// ID (GLine.ID) of the text line where the top of this image starts.
        /// </summary>
        public int LineId {
            get {
                return _lineId;
            }
        }

        /// <summary>
        /// Text column index where the left side of this image starts.
        /// </summary>
        public int ColumnIndex {
            get {
                return _columnIndex;
            }
        }

        /// <summary>
        /// Image size.
        /// </summary>
        public Size Size {
            get {
                return _bitmap.Size;
            }
        }

        /// <summary>
        /// Current horizontal position of the sixel data on the bitmap.
        /// </summary>
        public int CurrentSixelDataX {
            get {
                return _bitmap.CurrentSixelDataX;
            }
        }

        /// <summary>
        /// Current vertical position of the sixel data on the bitmap.
        /// </summary>
        public int CurrentSixelDataY {
            get {
                return _bitmap.CurrentSixelDataY;
            }
        }

        /// <summary>
        /// ight-bottom corner of the last sixel data on the bitmap.
        /// </summary>
        public Point LastSixelDataRightBottom {
            get {
                return _bitmap.LastSixelDataRightBottom;
            }
        }

        /// <summary>
        /// Whether this image is being constructed.
        /// </summary>
        public bool IsConstructing {
            get {
                return _constructing;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lineId">ID (GLine.ID) of the text line where the top of this image starts.</param>
        /// <param name="columnIndex">Text column index where the left side of this image starts.</param>
        public SixelImage(int lineId, int columnIndex) {
            _z = null;
            _lineId = lineId;
            _columnIndex = columnIndex;
            _constructing = true;
        }

        /// <summary>
        /// Dispose image.
        /// </summary>
        public void Dispose() {
            _bitmap.Dispose();
        }

        /// <summary>
        /// Finish construction.
        /// </summary>
        public void Finish() {
            _constructing = false;
        }

        /// <summary>
        /// Set Z-Order.
        /// </summary>
        /// <remarks>
        /// Subsequent text updates affect this image to make the new text visible.
        /// </remarks>
        /// <param name="z">Z-order</param>
        public void SetZOrder(GLineZOrder z) {
            _z = z;
        }

        /// <summary>
        /// Determine whether this image has overlapping area within the specified range. 
        /// </summary>
        /// <param name="lineIdFrom">start ID of the line range. (inclusive)</param>
        /// <param name="lineIdTo">end ID of the line range. (inclusive)</param>
        /// <param name="linePitch">line pitch</param>
        /// <returns>true if overlapping area exists</returns>
        public bool IsOverlappingWithLineRange(int lineIdFrom, int lineIdTo, float linePitch) {
            if (_lineId > lineIdTo) {
                return false;
            }

            Size bitmapSize = _bitmap.Size;
            int rows = (int)Math.Ceiling(bitmapSize.Height / linePitch);
            if (_lineId + rows - 1 < lineIdFrom) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Clear area of the column spans.
        /// </summary>
        /// <param name="spans">column spans</param>
        /// <param name="topLineId">ID of the top line of the screen</param>
        /// <param name="linePitch">line pitch</param>
        /// <param name="columnPitch">column pitch</param>
        public void ClearSpans(IList<ColumnSpanRectangle> spans, int topLineId, float linePitch, float columnPitch) {
            // The update spans before the image creation and the update spans after the image creation may be mixed.
            // Update spans with Z-order prior to this image are ignored.
            if (!_constructing && _z.HasValue) {
                int imageX = (int)(_columnIndex * columnPitch);
                int imageY = (int)((_lineId - topLineId) * linePitch);

                GLineZOrder z = _z.Value;
                List<Rectangle> converted = new List<Rectangle>(spans.Count);
                foreach (ColumnSpanRectangle span in spans) {
                    if (span.Z.CompareTo(z) >= 0) {
                        converted.Add(
                            new Rectangle(span.Rect.X - imageX, span.Rect.Y - imageY, span.Rect.Width, span.Rect.Height)
                        );
                    }
                }

                if (converted.Count > 0) {
                    _bitmap.ClearRectangles(converted.ToArray());
                }
            }
        }

        /// <summary>
        /// Place a single sixel row (6 pixels height) from the current position.
        /// </summary>
        /// <remarks>
        /// This method works only during image construction.
        /// </remarks>
        /// <param name="rowBuffer">row buffer</param>
        /// <param name="pixelSize">pixel size</param>
        public void Put(SixelTemporalRowBuffer rowBuffer, Size pixelSize) {
            if (_constructing) {
                _bitmap.Put(rowBuffer, pixelSize);
            }
        }

        /// <summary>
        /// Reset sixel horizontal position.
        /// </summary>
        /// <remarks>
        /// This method works only during image construction.
        /// </remarks>
        public void CarriageReturn() {
            if (_constructing) {
                _bitmap.CarriageReturn();
            }
        }

        /// <summary>
        /// Reset sixel horizontal position and advance vertial position to the next sixel row.
        /// </summary>
        /// <remarks>
        /// This method works only during image construction.
        /// </remarks>
        /// <param name="pixelSize">pixel size</param>
        public void NextRow(Size pixelSize) {
            if (_constructing) {
                _bitmap.NextRow(pixelSize);
            }
        }

        /// <summary>
        /// Fill background in this bitmap.
        /// </summary>
        /// <remarks>
        /// This method works only during image construction.
        /// </remarks>
        /// <param name="width">width in pixels</param>
        /// <param name="height">height in pixels</param>
        /// <param name="color">fill color</param>
        public void FillBackground(int width, int height, Color color) {
            if (_constructing) {
                _bitmap.FillBackground(width, height, color);
            }
        }

        /// <summary>
        /// Expand bitmap to satisfy specified size.
        /// </summary>
        /// <remarks>
        /// This method works only during image construction.
        /// </remarks>
        /// <param name="minWidth">minimum width</param>
        /// <param name="minHeight">minimum height</param>
        public void Expand(int minWidth, int minHeight) {
            if (_constructing) {
                _bitmap.Expand(minWidth, minHeight);
            }
        }

        /// <summary>
        /// Draw image.
        /// </summary>
        /// <param name="g">graphics object</param>
        /// <param name="topLineId">ID of the top line of the screen</param>
        /// <param name="linePitch">line pitch</param>
        /// <param name="columnPitch">column pitch</param>
        /// <param name="origin">top-left corner of the first row / column of the screen</param>
        public void Draw(Graphics g, int topLineId, float linePitch, float columnPitch, Point origin) {
            _bitmap.Draw(
                g,
                new Point(
                    origin.X + (int)(_columnIndex * columnPitch),
                    origin.Y + (int)((_lineId - topLineId) * linePitch)
                )
            );
        }

        /// <summary>
        /// Move this image to the line above in the scroll region.
        /// </summary>
        /// <remarks>
        /// This method works only after the image has been constructed.
        /// </remarks>
        /// <param name="scrollRangeLineIdFrom">ID of the top line of the scroll region</param>
        /// <param name="linesToMove">line count to move</param>
        /// <param name="linePitch">line pitch</param>
        /// <param name="empty">return true if this image has been completely cleared</param>
        public void MoveUp(int scrollRangeLineIdFrom, int linesToMove, float linePitch, out bool empty) {
            if (_constructing) {
                empty = false;
                return;
            }

            _lineId -= linesToMove; // may be negative value. but it is no problem...
            int trimTop = (int)Math.Floor((scrollRangeLineIdFrom - _lineId) * linePitch);
            _bitmap.ClearVerticalRange(0, trimTop, out empty);
        }

        /// <summary>
        /// Move this image to the line below in the scroll region.
        /// </summary>
        /// <remarks>
        /// This method works only after the image has been constructed.
        /// </remarks>
        /// <param name="scrollRangeLineIdTo">ID of the bottom line of the scroll region</param>
        /// <param name="linesToMove">line count to move</param>
        /// <param name="linePitch">line pitch</param>
        /// <param name="empty">return true if this image has been completely cleared</param>
        public void MoveDown(int scrollRangeLineIdTo, int linesToMove, float linePitch, out bool empty) {
            if (_constructing) {
                empty = false;
                return;
            }

            _lineId += linesToMove;
            int trimBottom = (int)((scrollRangeLineIdTo + 1 - _lineId) * linePitch);
            _bitmap.ClearVerticalRange(trimBottom, _bitmap.Size.Height, out empty);
        }

        /// <summary>
        /// Clear line range.
        /// </summary>
        /// <remarks>
        /// This method works only after the image has been constructed.
        /// </remarks>
        /// <param name="lineIdFrom">start ID of the line range. (inclusive)</param>
        /// <param name="lineIdTo">end ID of the line range. (inclusive)</param>
        /// <param name="linePitch">line pitch</param>
        /// <param name="empty">return true if this image has been completely cleared</param>
        public void ClearLineRange(int lineIdFrom, int lineIdTo, float linePitch, out bool empty) {
            if (_constructing) {
                empty = false;
                return;
            }

            int clearRangeTop = (int)((lineIdFrom - _lineId) * linePitch);
            int clearRangeBottom = (int)((lineIdTo + 1 - _lineId) * linePitch);
            _bitmap.ClearVerticalRange(clearRangeTop, clearRangeBottom, out empty);
        }
    }

    internal struct LineIdAndColumnSpan {
        public readonly int LineId;
        public readonly GLineColumnSpan ColumnSpan;

        public LineIdAndColumnSpan(int lineId, GLineColumnSpan columnSpan) {
            this.LineId = lineId;
            this.ColumnSpan = columnSpan;
        }
    }

    internal struct ColumnSpanRectangle {
        public readonly GLineZOrder Z;
        public readonly Rectangle Rect;

        public ColumnSpanRectangle(GLineZOrder z, Rectangle rect) {
            this.Z = z;
            this.Rect = rect;
        }
    }

    /// <summary>
    /// SIXEL image manager.
    /// </summary>
    internal class SixelImageManager {
        private readonly object _sync = new object();
        private readonly LinkedList<SixelImage> _sixelImages = new LinkedList<SixelImage>();
        private SixelPalette _sharedPalette = null;

        // SixelImageManager belongs to TerminalDocument, and does not have direct access to the RenderProfile maintaining line-pitch and column-pitch.
        // To manipulate document regardless of rendering, line-pitch and column-pitch passed in ClearSpans() or Draw() are used.
        private float _linePitch = 10;
        private float _columnPitch = 10;


        public SixelImageManager() {
        }

        /// <summary>
        /// Get shared palette.
        /// </summary>
        /// <returns>shared palette</returns>
        public SixelPalette GetSharedPalette() {
            if (_sharedPalette == null) {
                _sharedPalette = new SixelPalette();
            }
            return _sharedPalette;
        }

        /// <summary>
        /// Reset shared palette.
        /// </summary>
        public void ResetSharedPalette() {
            _sharedPalette = null;
        }

        /// <summary>
        /// Add sixel image.
        /// </summary>
        /// <param name="image">sixel image</param>
        public void Add(SixelImage image) {
            lock (_sync) {
                _sixelImages.AddLast(image);

                while (_sixelImages.Count > SixelConstants.MAX_IMAGES) {
                    RemoveSixelImage(_sixelImages.First);
                }

                Debug.Print("sixel images: {0}", _sixelImages.Count);
            }
        }

        /// <summary>
        /// Reduce overlapping images.
        /// </summary>
        /// <param name="image">upper sixel image</param>
        public void ReduceImages(SixelImage image) {
            lock (_sync) {
                LinkedListNode<SixelImage> node = _sixelImages.Last;

                while (node != null && !Object.ReferenceEquals(node.Value, image)) {
                    node = node.Previous;
                }

                if (node == null) {
                    return;
                }

                node = node.Previous;

                while (node != null) {
                    LinkedListNode<SixelImage> prevNode = node.Previous;

                    SixelImage underlay = node.Value;

                    if (underlay.LineId == image.LineId && underlay.ColumnIndex == image.ColumnIndex) {
                        Size underlaySize = underlay.Size;
                        Size imageSize = image.Size;
                        if (underlaySize.Width <= imageSize.Width && underlaySize.Height <= imageSize.Height) {
                            RemoveSixelImage(node);
                        }
                    }

                    node = prevNode;
                }
            }
        }

        private void RemoveSixelImage(LinkedListNode<SixelImage> node) {
            node.Value.Dispose();
            _sixelImages.Remove(node);
            Debug.WriteLine("removed sixel image");
        }

        /// <summary>
        /// Clear area of the column spans.
        /// </summary>
        /// <param name="spans">column spans</param>
        /// <param name="topLineId">ID of the top line of the screen</param>
        /// <param name="lineIdFrom">start ID of the line range</param>
        /// <param name="lineIdTo">end ID of the line range</param>
        /// <param name="linePitch">line pitch</param>
        /// <param name="columnPitch">column pitch</param>
        public void ClearSpans(IList<LineIdAndColumnSpan> spans, int topLineId, int lineIdFrom, int lineIdTo, float linePitch, float columnPitch) {
            if (spans.Count == 0) {
                return;
            }

            // In many cases, the specified line range does not overlap with any image.
            // Rectangles are calculated when the first overlapping image was found.
            ColumnSpanRectangle[] rects = null;

            lock (_sync) {
                _linePitch = linePitch;
                _columnPitch = columnPitch;

                foreach (SixelImage image in _sixelImages) {
                    if (!image.IsConstructing && image.IsOverlappingWithLineRange(lineIdFrom, lineIdTo, linePitch)) {
                        if (rects == null) {
                            rects = new ColumnSpanRectangle[spans.Count];
                            int i = 0;
                            foreach (LineIdAndColumnSpan s in spans) {
                                float fy = (s.LineId - topLineId) * linePitch;
                                int y1 = (int)fy;
                                int y2 = (int)(fy + linePitch);
                                int x1 = (int)(s.ColumnSpan.Start * columnPitch);
                                int x2 = (int)(s.ColumnSpan.End * columnPitch);
                                rects[i] = new ColumnSpanRectangle(s.ColumnSpan.Z, new Rectangle(x1, y1, x2 - x1, y2 - y1));
                                i++;
                            }
                        }

                        image.ClearSpans(rects, topLineId, linePitch, columnPitch);
                    }
                }
            }
        }

        /// <summary>
        /// Draw sixel images.
        /// </summary>
        /// <param name="topLineId">ID of the top line of the screen</param>
        /// <param name="lineIdFrom">start ID of the line range</param>
        /// <param name="lineIdTo">end ID of the line range</param>
        /// <param name="linePitch">line pitch</param>
        /// <param name="columnPitch">column pitch</param>
        /// <param name="g">graphics object</param>
        /// <param name="origin">top-left corner of the first row / column of the screen</param>
        /// <param name="excludedRect"></param>
        public void Draw(int topLineId, int lineIdFrom, int lineIdTo, float linePitch, float columnPitch, Graphics g, Point origin, Rectangle? excludedRect) {
            // In many cases, the specified line range does not overlap with any image.
            // New clipping region is set when the first overlapping image was found.
            Region origRegion = null;

            PixelOffsetMode origPixelOffsetMode = g.PixelOffsetMode;
            SmoothingMode origSmoothingMode = g.SmoothingMode;
            InterpolationMode origInterpolationMode = g.InterpolationMode;

            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            lock (_sync) {
                _linePitch = linePitch;
                _columnPitch = columnPitch;

                foreach (SixelImage image in _sixelImages) {
                    if (image.IsOverlappingWithLineRange(lineIdFrom, lineIdTo, linePitch)) {
                        if (excludedRect.HasValue && origRegion == null) {
                            origRegion = g.Clip;
                            Region newRegion = origRegion.Clone();
                            newRegion.Exclude(excludedRect.Value);
                            g.Clip = newRegion;
                        }

                        image.Draw(g, topLineId, linePitch, columnPitch, origin);
                    }
                }
            }

            if (origRegion != null) {
                g.Clip = origRegion;
            }

            g.PixelOffsetMode = origPixelOffsetMode;
            g.SmoothingMode = origSmoothingMode;
            g.InterpolationMode = origInterpolationMode;
        }

        public bool MoveUp(int scrollRangeLineIdFrom, int scrollRangeLineIdTo, int linesToMove) {
            bool moved = false;
            lock (_sync) {
                LinkedListNode<SixelImage> node = _sixelImages.First;
                while (node != null) {
                    LinkedListNode<SixelImage> nextNode = node.Next;
                    if (node.Value.IsOverlappingWithLineRange(scrollRangeLineIdFrom, scrollRangeLineIdTo, _linePitch)) {
                        moved = true;
                        bool empty;
                        node.Value.MoveUp(scrollRangeLineIdFrom, linesToMove, _linePitch, out empty);
                        if (empty) {
                            RemoveSixelImage(node);
                        }
                    }
                    node = nextNode;
                }
            }
            return moved;
        }

        public bool MoveDown(int scrollRangeLineIdFrom, int scrollRangeLineIdTo, int linesToMove) {
            bool moved = false;
            lock (_sync) {
                LinkedListNode<SixelImage> node = _sixelImages.First;
                while (node != null) {
                    LinkedListNode<SixelImage> nextNode = node.Next;
                    if (node.Value.IsOverlappingWithLineRange(scrollRangeLineIdFrom, scrollRangeLineIdTo, _linePitch)) {
                        moved = true;
                        bool empty;
                        node.Value.MoveDown(scrollRangeLineIdTo, linesToMove, _linePitch, out empty);
                        if (empty) {
                            RemoveSixelImage(node);
                        }
                    }
                    node = nextNode;
                }
            }
            return moved;
        }

        /// <summary>
        /// Clear line range.
        /// </summary>
        /// <remarks>
        /// This method works only after the image has been constructed.
        /// </remarks>
        /// <param name="lineIdFrom">start ID of the line range. (inclusive)</param>
        /// <param name="lineIdTo">end ID of the line range. (inclusive)</param>
        public bool ClearLineRange(int lineIdFrom, int lineIdTo) {
            bool cleared = false;
            lock (_sync) {
                LinkedListNode<SixelImage> node = _sixelImages.First;
                while (node != null) {
                    LinkedListNode<SixelImage> nextNode = node.Next;
                    if (node.Value.IsOverlappingWithLineRange(lineIdFrom, lineIdTo, _linePitch)) {
                        cleared = true;
                        bool empty;
                        node.Value.ClearLineRange(lineIdFrom, lineIdTo, _linePitch, out empty);
                        if (empty) {
                            RemoveSixelImage(node);
                        }
                    }
                    node = nextNode;
                }
            }
            return cleared;
        }

        /// <summary>
        /// Dlete all sixel images.
        /// </summary>
        public void DeleteAll() {
            lock (_sync) {
                while (_sixelImages.Count > 0) {
                    RemoveSixelImage(_sixelImages.First);
                }
            }            
        }
    }

    /// <summary>
    /// SIXEL DCS processor.
    /// </summary>
    internal class SixelDCSProcessor : DCSProcessorBase {
        private const int MAX_REPEAT = 32767;
        private const int MAX_COLOR = 255;

        public delegate GLineZOrder CompletedCallback(int lineId, int columnIndex, Size imageSize, Point lastSixelDataPosition);

        private readonly TerminalDocument _document;
        private readonly SixelImageManager _manager;
        private readonly CompletedCallback _completed;
        private readonly SixelPalette _palette;
        private readonly SixelTemporalRowBuffer _rowBuffer = new SixelTemporalRowBuffer();
        private readonly SixelImage _image;
        private readonly bool _fillBackground;
        private bool _imageAdded = false;
        private int _lastUpdateTime;

        private enum State {
            SIXEL_DATA_WAIT,
            RASTER_ATTR_PARSE,
            REPEAT_COUNT_PARSE,
            COLOR_SPECIFIER_PARSE,
            COLOR_SPECIFIER_PARSE_LONG,
        }

        private State _state = State.SIXEL_DATA_WAIT;
        private int _color = 1;
        private int _repeat = 0; // 0 or 1 means no repeat
        private readonly List<char> _paramBuffer = new List<char>(32);
        private int _paramValue = 0;
        private Size _pixelSize;

        public SixelDCSProcessor(TerminalDocument document, RenderProfile renderProfile, SixelImageManager manager, int lineId, int columnIndex, NumericParams para, bool usePrivateColorRegisters, CompletedCallback completed) {
            int p1 = para.Get(0, -1); // pixel aspect ratio
            int p2 = para.Get(1, 0); // put background color at zero pixel

            switch (p1) {
                case 2:
                    _pixelSize = new Size(1, 5);
                    break;
                case 3:
                case 4:
                    _pixelSize = new Size(1, 3);
                    break;
                case 7:
                case 8:
                case 9:
                    _pixelSize = new Size(1, 1);
                    break;
                case 0:
                case 1:
                case 5:
                case 6:
                default:
                    _pixelSize = new Size(1, 2);
                    break;
            }

            _fillBackground = (p2 == 0 || p2 == 2);

            _document = document;
            _manager = manager;
            _completed = completed;
            _palette = usePrivateColorRegisters ? new SixelPalette() : manager.GetSharedPalette();
            ColorSpec backColorSpec = _document.CurrentDecoration.GetBackColorSpec();
            _image = new SixelImage(lineId, columnIndex);
            _lastUpdateTime = Environment.TickCount;
        }

        protected override void Input(char ch) {
            switch (_state) {
                case State.SIXEL_DATA_WAIT:
                SIXEL_DATA_WAIT:
                    if (ch >= 0x3f && ch <= 0x7e) {
                        PutSixelData(ch - 0x3f, _repeat);
                        _repeat = 0;
                        return;
                    }
                    _repeat = 0;

                    switch (ch) {
                        case '!':
                            _paramValue = 0;
                            _state = State.REPEAT_COUNT_PARSE;
                            return;
                        case '"':
                            _paramBuffer.Clear();
                            _state = State.RASTER_ATTR_PARSE;
                            return;
                        case '#':
                            _paramValue = 0;
                            _paramBuffer.Clear();
                            _state = State.COLOR_SPECIFIER_PARSE;
                            return;
                        case '$':
                            CarriageReturn();
                            return;
                        case '-':
                            NewLine();
                            return;
                        default:
                            // ignore
                            break;
                    }
                    break;

                case State.RASTER_ATTR_PARSE:
                    switch (ch) {
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case ';':
                            _paramBuffer.Add(ch);
                            return;
                    }
                    HandleRasterAttributes(new String(_paramBuffer.ToArray()));
                    _state = State.SIXEL_DATA_WAIT;
                    goto SIXEL_DATA_WAIT;

                case State.REPEAT_COUNT_PARSE:
                    switch (ch) {
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            if (_paramValue > MAX_REPEAT) {
                                return; // ignore trailing digits
                            }
                            _paramValue = _paramValue * 10 + (ch - '0');
                            return;
                    }
                    // if the repeat count is not specified, the repeat count will be zero.
                    // (it is the same as the repeat count = 1)
                    if (_paramValue >= 0 && _paramValue <= MAX_REPEAT) {
                        _repeat = _paramValue;
                    }
                    _state = State.SIXEL_DATA_WAIT;
                    goto SIXEL_DATA_WAIT;

                case State.COLOR_SPECIFIER_PARSE:
                    switch (ch) {
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            _paramBuffer.Add(ch); // update buffer for the long form
                            if (_paramValue > MAX_COLOR) {
                                return; // ignore trailing digits
                            }
                            _paramValue = _paramValue * 10 + (ch - '0');
                            return;
                        case ';':
                            _paramBuffer.Add(ch);
                            // transition to parse long form
                            _state = State.COLOR_SPECIFIER_PARSE_LONG;
                            return;
                    }
                    if (_paramBuffer.Count > 0 && _paramValue >= 0 && _paramValue <= MAX_COLOR) {
                        _color = _paramValue;
                    }
                    _state = State.SIXEL_DATA_WAIT;
                    goto SIXEL_DATA_WAIT;

                case State.COLOR_SPECIFIER_PARSE_LONG:
                    switch (ch) {
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case ';':
                            _paramBuffer.Add(ch);
                            return;
                    }
                    HandleColorSpecifier(new String(_paramBuffer.ToArray()));
                    _state = State.SIXEL_DATA_WAIT;
                    goto SIXEL_DATA_WAIT;
            }
        }

        private void PutSixelData(int pattern, int repeat) {
            do {
                if (_rowBuffer.Width >= SixelConstants.MAX_IMAGE_WIDTH) {
                    return;
                }
                _rowBuffer.Put(pattern, _color, _palette);
            } while (--repeat > 0);
        }

        private void CarriageReturn() {
            if (_image.CurrentSixelDataX > 0) {
                FlushRowBuffer(false);
                _image.CarriageReturn();
            }
            else {
                _rowBuffer.CarriageReturn();
            }
        }

        private void NewLine() {
            FlushRowBuffer(true);  // flush forcibly to expand bitmap
            _image.NextRow(_pixelSize);
            InvalidateIfSlow();
        }

        private void FlushRowBuffer(bool force) {
            if (force || _rowBuffer.Width > 0) {
                _image.Put(_rowBuffer, _pixelSize);
                _rowBuffer.Clear();
            }
        }

        private void HandleRasterAttributes(string paramText) {
            FlushRowBuffer(false); // flush buffer with the current pixel size

            NumericParams para = NumericParamsParser.Parse(paramText);
            if (para.Length >= 4) {
                int pan = para.GetNonZero(0, 1); // pixel aspect ratio numerator
                int pad = para.GetNonZero(1, 1); // pixel aspect ratio denominator
                int h = para.Get(2, 0); // horizontal image size
                int v = para.Get(3, 0); // vertical image size

                if (pan >= 1 && pan <= SixelConstants.MAX_IMAGE_HEIGHT && pad >= 1 && pad <= SixelConstants.MAX_IMAGE_WIDTH) {
                    _pixelSize = new Size(pad, pan);
                }

                if (h >= 1 && h <= SixelConstants.MAX_IMAGE_WIDTH && v >= 1 && v <= SixelConstants.MAX_IMAGE_HEIGHT) {
                    if (_fillBackground) {
                        _image.FillBackground(h, v, _palette[0]);
                    }
                    else {
                        _image.Expand(h, v);
                    }
                    InvalidateIfSlow();
                }
            }
        }

        private void EnsureRegistered() {
            if (!_imageAdded) {
                _manager.Add(_image);
                _imageAdded = true;
            }
        }

        private void InvalidateIfSlow() {
            int tc = Environment.TickCount;
            if (unchecked(tc - _lastUpdateTime) >= 500) {
                EnsureRegistered();
                ForceInvalidate();
                _lastUpdateTime = tc;
            }
        }

        private void ForceInvalidate() {
            Size imageSize = _image.Size;
            _document.InvalidatedRegion.InvalidateImage(_image.LineId, imageSize.Height);
        }

        private void HandleColorSpecifier(string paramText) {
            NumericParams para = NumericParamsParser.Parse(paramText);
            if (para.Length >= 5) {
                int colorNumber = para.Get(0, -1);
                int colorCordinateSystem = para.Get(1, -1);
                int x = para.Get(2, 0);
                int y = para.Get(3, 0);
                int z = para.Get(4, 0);

                if (colorNumber < 0 || colorNumber > MAX_COLOR) {
                    return;
                }

                if (colorCordinateSystem == 1) {
                    if (!HandleColorSpecifierHLS(colorNumber, x, y, z)) {
                        return;
                    }
                }
                else if (colorCordinateSystem == 2) {
                    if (!HandleColorSpecifierRGB(colorNumber, x, y, z)) {
                        return;
                    }
                }
                else {
                    return;
                }

                _color = colorNumber;
            }
        }

        private bool HandleColorSpecifierHLS(int colorNumber, int hue, int lightness, int saturation) {
            if (hue < 0 || hue > 360 || lightness < 0 || lightness > 100 || saturation < 0 || saturation > 100) {
                return false;
            }

            _palette[colorNumber] = HlsSupport.HlsToRgb(hue, lightness, saturation);
            return true;
        }


        private bool HandleColorSpecifierRGB(int colorNumber, int r, int g, int b) {
            if (r < 0 || r > 100 || g < 0 || g > 100 || b < 0 || b > 100) {
                return false;
            }
            _palette.SetColorByLevel(colorNumber, r, g, b);
            return true;
        }

        protected override void Finish() {
            if (_state != State.SIXEL_DATA_WAIT) {
                // terminate current sixel control function
                Input((char)0);
            }
            
            FlushRowBuffer(false);

            if (_image.Size.IsEmpty && !_imageAdded) {
                _image.Dispose();
            }
            else {
                _image.Finish();
                GLineZOrder z = _completed(_image.LineId, _image.ColumnIndex, _image.Size, _image.LastSixelDataRightBottom);
                // set new z-order. the image will be affected by the subsequent updated spans.
                _image.SetZOrder(z);
                EnsureRegistered();
                _manager.ReduceImages(_image);
                ForceInvalidate();
            }
            _rowBuffer.Dispose();
        }

        protected override void Cancel() {
            Finish();
        }
    }

    internal class HlsSupport {

        private const int HUE_MAX = 360;
        private const int LIGHTNESS_MAX = 100;
        private const int SATURATION_MAX = 100;

        public static Color HlsToRgb(int hue, int lightness, int saturation) {
            Debug.Assert(hue >= 0);
            Debug.Assert(hue <= HUE_MAX);
            Debug.Assert(lightness >= 0);
            Debug.Assert(lightness <= LIGHTNESS_MAX);
            Debug.Assert(saturation >= 0);
            Debug.Assert(saturation <= SATURATION_MAX);

            // Hue of DEC HLS is 120 degrees ahead from the standard HLS.
            // Blue   : DEC=0   STD=240
            // Magenta: DEC=60  STD=300
            // Red    : DEC=120 STD=0
            // Yellow : DEC=180 STD=60
            // Green  : DEC=240 STD=120
            // Cyan   : DEC=300 STD=180
            hue = (hue + 240) % 360;

            if (saturation <= 0) {
                int c = 255 * lightness / LIGHTNESS_MAX;
                return Color.FromArgb(c, c, c);
            }

            int v2 = (lightness <= LIGHTNESS_MAX / 2)
                ? lightness * (SATURATION_MAX + saturation)
                : lightness * SATURATION_MAX + saturation * LIGHTNESS_MAX - lightness * saturation;

            int v1 = lightness * SATURATION_MAX * 2 - v2;

            int h3 = hue * 3;

            int r = HueToRgb(v1, v2, h3 + HUE_MAX) * 255 / (HUE_MAX * LIGHTNESS_MAX * SATURATION_MAX);
            int g = HueToRgb(v1, v2, h3) * 255 / (HUE_MAX * LIGHTNESS_MAX * SATURATION_MAX);
            int b = HueToRgb(v1, v2, h3 - HUE_MAX) * 255 / (HUE_MAX * LIGHTNESS_MAX * SATURATION_MAX);
            return Color.FromArgb(r, g, b);
        }

        private static int HueToRgb(int v1, int v2, int h3) {
            if (h3 < 0) {
                h3 += HUE_MAX * 3;
            }

            if (h3 > HUE_MAX * 3) {
                h3 -= HUE_MAX * 3;
            }

            if (2 * h3 < HUE_MAX) {
                return v1 * HUE_MAX + (v2 - v1) * (2 * h3);
            }

            if (2 * h3 < HUE_MAX * 3) {
                return v2 * HUE_MAX;
            }

            if (h3 < HUE_MAX * 2) {
                return v1 * HUE_MAX + (v2 - v1) * (HUE_MAX * 4 - 2 * h3);
            }

            return v1 * HUE_MAX;
        }
    }
}
