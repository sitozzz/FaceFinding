
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Gms.Vision;
using Android.Util;
using Android.Graphics;

namespace LiveCam.Droid
{
    public class GraphicOverlay : View
    {
        private Object mLock = new Object();
        private int mPreviewWidth;
        private float mWidthScaleFactor = 1.0f;
        private int mPreviewHeight;
        private float mHeightScaleFactor = 1.0f;
        private CameraFacing mFacing = CameraFacing.Front;
        private HashSet<Graphic> mGraphics = new HashSet<Graphic>();

        public int PreviewWidth { get => mPreviewWidth; set => mPreviewWidth = value; }
        public float WidthScaleFactor { get => mWidthScaleFactor; set => mWidthScaleFactor = value; }
        public int PreviewHeight { get => mPreviewHeight; set => mPreviewHeight = value; }
        public float HeightScaleFactor { get => mHeightScaleFactor; set => mHeightScaleFactor = value; }
        public CameraFacing CameraFacing { get => mFacing; set => mFacing = value; }

        public GraphicOverlay(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            
        }

        public void Clear()
        {
            lock(mLock) {
                mGraphics.Clear();
            }
            PostInvalidate();
        }

        /// <summary>
        /// Adds a graphic to the overlay.
        /// </summary>
        /// <param name="graphic"></param>
        public void Add(Graphic graphic)
        {
            
            lock(mLock) {
                mGraphics.Add(graphic);
            }
            PostInvalidate();
        }

        /// <summary>
        /// Removes a graphic from the overlay.
        /// </summary>
        /// <param name="graphic"></param>
        public void Remove(Graphic graphic)
        {
            lock(mLock) {
                mGraphics.Remove(graphic);
            }
            PostInvalidate();
        }
       
        /// <summary>
        ///  Sets the camera attributes for size and facing direction, which informs how to transform image coordinates later.
        /// </summary>
        /// <param name="previewWidth"></param>
        /// <param name="previewHeight"></param>
        /// <param name="facing"></param>
        public void SetCameraInfo(int previewWidth, int previewHeight, CameraFacing facing)
        {
            lock(mLock) {
                PreviewWidth = previewWidth;
                PreviewHeight = previewHeight;
                CameraFacing = facing;
            }
            PostInvalidate();
        }

        public override void Draw(Canvas canvas)
        {
            base.Draw(canvas);
            Paint paint = new Paint();
            paint.Color = Color.White;
            //Фон
            canvas.DrawColor(Color.Black);
            //Линия центра canvas'a
            canvas.DrawLine(canvas.Width / 2, 0, canvas.Width/2, canvas.Height, paint);
            lock(mLock) {
                if ((PreviewWidth != 0) && (PreviewHeight != 0))
                {
                    WidthScaleFactor = (float)canvas.Width / (float)PreviewWidth;
                    HeightScaleFactor = (float)canvas.Height / (float)PreviewHeight;
                }

                foreach (Graphic graphic in mGraphics)
                {
                    graphic.Draw(canvas);
                }
            }
        }
    }

    /**
     * Base class for a custom graphics object to be rendered within the graphic overlay.  Subclass
     * this and implement the {@link Graphic#draw(Canvas)} method to define the
     * graphics element.  Add instances to the overlay using {@link GraphicOverlay#add(Graphic)}.
     */
    public abstract class Graphic
    {
        private GraphicOverlay mOverlay;

        public Graphic(GraphicOverlay overlay)
        {
            mOverlay = overlay;
        }

        /**
         * Draw the graphic on the supplied canvas.  Drawing should use the following methods to
         * convert to view coordinates for the graphics that are drawn:
         * <ol>
         * <li>{@link Graphic#scaleX(float)} and {@link Graphic#scaleY(float)} adjust the size of
         * the supplied value from the preview scale to the view scale.</li>
         * <li>{@link Graphic#translateX(float)} and {@link Graphic#translateY(float)} adjust the
         * coordinate from the preview's coordinate system to the view coordinate system.</li>
         * </ol>
         *
         * @param canvas drawing canvas
         */
        public abstract void Draw(Canvas canvas);

        /**
         * Adjusts a horizontal value of the supplied value from the preview scale to the view
         * scale.
         */
        public float ScaleX(float horizontal)
        {
            return horizontal * mOverlay.WidthScaleFactor;
        }

        /**
         * Adjusts a vertical value of the supplied value from the preview scale to the view scale.
         */
        public float ScaleY(float vertical)
        {
            return vertical * mOverlay.HeightScaleFactor;
        }

        /**
         * Adjusts the x coordinate from the preview's coordinate system to the view coordinate
         * system.
         */
        public float TranslateX(float x)
        {
            if (mOverlay.CameraFacing == CameraFacing.Front)
            {
                return mOverlay.Width - ScaleX(x);
            }
            else
            {
                return ScaleX(x);
            }
        }

        /**
         * Adjusts the y coordinate from the preview's coordinate system to the view coordinate
         * system.
         */
        public float TranslateY(float y)
        {
            return ScaleY(y);
        }

        public void PostInvalidate()
        {
            mOverlay.PostInvalidate();
        }
    }
}