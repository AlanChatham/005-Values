using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Kinect;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectOSC
{
    /// <summary>
    /// VisualKinectUnits hold a LocatedSensor kinect sensor class,
    ///  as well as an optional color bitmap and image to draw skeletons on
    /// </summary>
    class VisualKinectUnit
    {
        public LocatedSensor locatedSensor { get; set; }
        private System.Windows.Controls.Image skeletonDrawingImage;
        private System.Windows.Controls.Image colorImage;
        private WriteableBitmap colorBitmap;
        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;
        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        public VisualKinectUnit() {
        }

        #region drawingSettings
        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        #endregion

        /// <summary>
        /// Constructor for VisualKinectUnit
        /// </summary>
        /// <param name="sensor">LocatedSensor class kinect sensor</param>
        /// <param name="skeletonDrawingImage">Image that we'll draw the skeleton on</param>
        /// <param name="colorImage">Image we'll use to push the color camera video to</param>
        public VisualKinectUnit(LocatedSensor locatedSensor, System.Windows.Controls.Image skeletonDrawingImage = null, System.Windows.Controls.Image colorImage = null) {
            // Get in some parameters
            this.locatedSensor = locatedSensor;
            this.skeletonDrawingImage = skeletonDrawingImage;
            this.colorImage = colorImage;

            // Set up the basics for drawing a skeleton
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();
            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);
            // Turn on the skeleton stream to receive skeleton frames
            locatedSensor.sensor.SkeletonStream.Enable();
            // Turn on the color stream to receive color frames
            locatedSensor.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            // This is the bitmap we'll display on-screen
            colorBitmap = (new WriteableBitmap(locatedSensor.sensor.ColorStream.FrameWidth,
                                               locatedSensor.sensor.ColorStream.FrameHeight,
                                               96.0, 96.0, PixelFormats.Bgr32, null));

            // Add an event handler to be called whenever there is new color frame data
            if (colorImage != null) {
                locatedSensor.sensor.ColorFrameReady += this.refreshColorImage;
            }
            // Add an event handler to be called whenever there is new color frame data
            if (skeletonDrawingImage != null) {
                locatedSensor.sensor.SkeletonFrameReady += this.refreshSkeletonDrawing;
                this.skeletonDrawingImage.Source = imageSource;
            }
        }

        public void Stop() {
            if (this.locatedSensor != null) {
                locatedSensor.sensor.Stop();
            }
        }

        /// <summary>
        /// Draw the Color Frame to screen
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        /// //Refactor this by having you pass in a bitmap to draw to
        private void refreshColorImage(object sender, ColorImageFrameReadyEventArgs e) {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
                if (colorFrame != null) {
                    // Copy the pixel data from the image to a temporary array
                    byte[] colorPixels = new byte[colorFrame.PixelDataLength];
                    colorFrame.CopyPixelDataTo(colorPixels);

                    // Write the pixel data into our bitmap
                    Int32Rect sourceRect = new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight);
                    this.colorBitmap.WritePixels(sourceRect, colorPixels, this.colorBitmap.PixelWidth * sizeof(int), 0);

                    this.colorImage.Source = colorBitmap;
                }
            }
        }

        private void refreshSkeletonDrawing(object sender, SkeletonFrameReadyEventArgs e) {
            using (DrawingContext dc = this.drawingGroup.Open()) {
                bool noTrackedSkeletons = true;
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, this.skeletonDrawingImage.Width, this.skeletonDrawingImage.Height));
                if (this.locatedSensor.relativeSkeletons.Count > 0) {
                    foreach (Skeleton skel in this.locatedSensor.relativeSkeletons) {
                    //foreach (Skeleton skel in this.locatedSensor.globalSkeletons) {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked) {
                            noTrackedSkeletons = false;
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly) {
                            dc.DrawEllipse(
                           this.centerPointBrush,
                           null,
                           this.SkeletonPointToScreen(skel.Position),
                           BodyCenterThickness,
                           BodyCenterThickness);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.NotTracked) {
                        }
                    }
                    if (noTrackedSkeletons) {
                    }
                }
                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.skeletonDrawingImage.Width, this.skeletonDrawingImage.Height));
            }
        }
        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext) {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.skeletonDrawingImage.Height - ClipBoundsThickness, this.skeletonDrawingImage.Width, ClipBoundsThickness));
            }
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.skeletonDrawingImage.Width, ClipBoundsThickness));
            }
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.skeletonDrawingImage.Height));
            }
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right)) {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.skeletonDrawingImage.Width - ClipBoundsThickness, 0, ClipBoundsThickness, this.skeletonDrawingImage.Height));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext) {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            // Render Joints
            foreach (Joint joint in skeleton.Joints) {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked) {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred) {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null) {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint) {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.locatedSensor.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(
                                                                             skelpoint,
                                                                             DepthImageFormat.Resolution640x480Fps30);
            // Now adjust that point by the actual size of our drawing image
            double imageWidthRatio = this.skeletonDrawingImage.Width / 640;
            double imageHeightRatio = this.skeletonDrawingImage.Height / 480;
            return new Point(depthPoint.X * imageWidthRatio, depthPoint.Y * imageHeightRatio);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1) {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked) {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred) {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked) {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }
    }
}
