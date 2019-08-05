﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace Microsoft.Azure.Kinect.Sensor
{
    /// <summary>
    /// Class that allows the calibrated transformation of Azure Kinect Images.
    /// </summary>
    public class Transformation : IDisposable
    {
        private readonly NativeMethods.k4a_transformation_t handle;
        private readonly Calibration calibration;
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Initializes a new instance of the <see cref="Transformation"/> class.
        /// </summary>
        /// <param name="calibration">Calibration to use for transformation operations.</param>
        public Transformation(Calibration calibration)
        {
            this.calibration = calibration;
            this.handle = NativeMethods.k4a_transformation_create(ref calibration);
            if (this.handle == null)
            {
                throw new AzureKinectException("Failed to create transformation object");
            }
        }

        /// <summary>
        /// Transforms an Image from the depth camera perspective to the color camera perspective.
        /// </summary>
        /// <param name="capture">Capture with the depth image.</param>
        /// <returns>A depth image transformed in to the color camera perspective.</returns>
        public Image DepthImageToColorCamera(Capture capture)
        {
            if (capture == null)
            {
                throw new ArgumentNullException(nameof(capture));
            }

            return this.DepthImageToColorCamera(capture.Depth);
        }

        /// <summary>
        /// Transforms an Image from the depth camera perspective to the color camera perspective.
        /// </summary>
        /// <param name="depth">The depth image to transform.</param>
        /// <returns>A depth image transformed in to the color camera perspective.</returns>
        public Image DepthImageToColorCamera(Image depth)
        {
            if (depth == null)
            {
                throw new ArgumentNullException(nameof(depth));
            }

            Image image = new Image(
                ImageFormat.Depth16,
                this.calibration.color_camera_calibration.resolution_width,
                this.calibration.color_camera_calibration.resolution_height)
            {
                Timestamp = depth.Timestamp,
            };

            this.DepthImageToColorCamera(depth, image);

            return image;
        }

        /// <summary>
        /// Transforms an Image from the depth camera perspective to the color camera perspective.
        /// </summary>
        /// <param name="capture">Capture with the depth image.</param>
        /// <param name="transformed">An Image to hold the output.</param>
        /// <remarks>
        /// The <paramref name="transformed"/> Image must be of the resolution of the color camera, and
        /// of the pixel format of the depth image.
        /// </remarks>
        public void DepthImageToColorCamera(Capture capture, Image transformed)
        {
            if (capture == null)
            {
                throw new ArgumentNullException(nameof(capture));
            }

            if (transformed == null)
            {
                throw new ArgumentNullException(nameof(transformed));
            }

            this.DepthImageToColorCamera(capture.Depth, transformed);
        }

        /// <summary>
        /// Transforms an Image from the depth camera perspective to the color camera perspective.
        /// </summary>
        /// <param name="depth">Depth image to transform.</param>
        /// <param name="transformed">An Image to hold the output.</param>
        /// <remarks>
        /// The <paramref name="transformed"/> Image must be of the resolution of the color camera, and
        /// of the pixel format of the depth image.
        /// </remarks>
        public void DepthImageToColorCamera(Image depth, Image transformed)
        {
            if (depth == null)
            {
                throw new ArgumentNullException(nameof(depth));
            }

            if (transformed == null)
            {
                throw new ArgumentNullException(nameof(transformed));
            }

            lock (this)
            {
                if (this.disposedValue)
                {
                    throw new ObjectDisposedException(nameof(Transformation));
                }

                // Create a new reference to the Image objects so that they cannot be disposed while
                // we are performing the transformation
                using (Image depthReference = depth.Reference())
                using (Image transformedReference = transformed.Reference())
                {
                    // Ensure changes made to the managed memory are visible to the native layer
                    depthReference.FlushMemory();

                    AzureKinectException.ThrowIfNotSuccess(NativeMethods.k4a_transformation_depth_image_to_color_camera(
                        this.handle,
                        depthReference.DangerousGetHandle(),
                        transformedReference.DangerousGetHandle()));

                    // Copy the native memory back to managed memory if required
                    transformedReference.InvalidateMemory();
                }
            }
        }

        /// <summary>
        /// Transforms an Image from the color camera perspective to the depth camera perspective.
        /// </summary>
        /// <param name="capture">Capture containing depth and color images.</param>
        /// <returns>A color image in the perspective of the depth camera.</returns>
        public Image ColorImageToDepthCamera(Capture capture)
        {
            if (capture == null)
            {
                throw new ArgumentNullException(nameof(capture));
            }

            return this.ColorImageToDepthCamera(capture.Depth, capture.Color);
        }

        /// <summary>
        /// Transforms an Image from the color camera perspective to the depth camera perspective.
        /// </summary>
        /// <param name="depth">Depth map of the space the color image is being transformed in to.</param>
        /// <param name="color">Color image to transform in to the depth space.</param>
        /// <returns>A color image in the perspective of the depth camera.</returns>
        public Image ColorImageToDepthCamera(Image depth, Image color)
        {
            if (depth == null)
            {
                throw new ArgumentNullException(nameof(depth));
            }

            if (color == null)
            {
                throw new ArgumentNullException(nameof(color));
            }

            Image transformed = new Image(
                ImageFormat.ColorBGRA32,
                this.calibration.depth_camera_calibration.resolution_width,
                this.calibration.depth_camera_calibration.resolution_height)
            {
                Exposure = color.Exposure,
                ISOSpeed = color.ISOSpeed,
                Timestamp = color.Timestamp,
                WhiteBalance = color.WhiteBalance,
            };

            this.ColorImageToDepthCamera(depth, color, transformed);

            return transformed;
        }

        /// <summary>
        /// Transforms an Image from the color camera perspective to the depth camera perspective.
        /// </summary>
        /// <param name="capture">Capture containing depth and color images.</param>
        /// <param name="transformed">An Image to hold the output.</param>
        /// <remarks>
        /// The <paramref name="transformed"/> Image must be of the resolution of the depth camera, and
        /// of the pixel format of the color image.
        /// </remarks>
        public void ColorImageToDepthCamera(Capture capture, Image transformed)
        {
            if (capture == null)
            {
                throw new ArgumentNullException(nameof(capture));
            }

            if (transformed == null)
            {
                throw new ArgumentNullException(nameof(transformed));
            }

            this.ColorImageToDepthCamera(capture.Depth, capture.Color, transformed);
        }

        /// <summary>
        /// Transforms an Image from the color camera perspective to the depth camera perspective.
        /// </summary>
        /// <param name="depth">Depth map of the space the color image is being transformed in to.</param>
        /// <param name="color">Color image to transform in to the depth space.</param>
        /// <param name="transformed">An Image to hold the output.</param>
        /// <remarks>
        /// The <paramref name="transformed"/> Image must be of the resolution of the depth camera, and
        /// of the pixel format of the color image.
        /// </remarks>
        public void ColorImageToDepthCamera(Image depth, Image color, Image transformed)
        {
            if (depth == null)
            {
                throw new ArgumentNullException(nameof(depth));
            }

            if (color == null)
            {
                throw new ArgumentNullException(nameof(color));
            }

            if (transformed == null)
            {
                throw new ArgumentNullException(nameof(transformed));
            }

            lock (this)
            {
                if (this.disposedValue)
                {
                    throw new ObjectDisposedException(nameof(Transformation));
                }

                // Create a new reference to the Image objects so that they cannot be disposed while
                // we are performing the transformation
                using (Image depthReference = depth.Reference())
                using (Image colorReference = color.Reference())
                using (Image transformedReference = transformed.Reference())
                {
                    // Ensure changes made to the managed memory are visible to the native layer
                    depthReference.FlushMemory();
                    colorReference.FlushMemory();

                    AzureKinectException.ThrowIfNotSuccess(NativeMethods.k4a_transformation_color_image_to_depth_camera(
                        this.handle,
                        depthReference.DangerousGetHandle(),
                        colorReference.DangerousGetHandle(),
                        transformedReference.DangerousGetHandle()));

                    // Copy the native memory back to managed memory if required
                    transformedReference.InvalidateMemory();
                }
            }
        }

        /// <summary>
        /// Creates a point cloud from a depth image.
        /// </summary>
        /// <param name="depth">The depth map to generate the point cloud from.</param>
        /// <param name="camera">The perspective the depth map is from.</param>
        /// <remarks>
        /// If the depth map is from the original depth perspecive, <paramref name="camera"/> should be Depth. If it has
        /// been transformed to the color camera perspective, <paramref name="camera"/> should be Color.
        ///
        /// The returned image will be of format Custom. Each pixel will be an XYZ set of 16 bit values,
        /// therefore its stride must is 2(bytes) * 3(x,y,z) * width of the <paramref name="depth"/> image in pixels.
        /// </remarks>
        /// <returns>A point cloud image.</returns>
        public Image DepthImageToPointCloud(Image depth, CalibrationDeviceType camera = CalibrationDeviceType.Depth)
        {
            if (depth == null)
            {
                throw new ArgumentNullException(nameof(depth));
            }

            Image pointCloud = new Image(
                ImageFormat.Custom,
                depth.WidthPixels,
                depth.HeightPixels,
                sizeof(short) * 3 * depth.WidthPixels);

            this.DepthImageToPointCloud(depth, pointCloud, camera);

            return pointCloud;
        }

        /// <summary>
        /// Creates a point cloud from a depth image.
        /// </summary>
        /// <param name="depth">The depth map to generate the point cloud from.</param>
        /// <param name="pointCloud">The image to store the output point cloud.</param>
        /// <param name="camera">The perspective the depth map is from.</param>
        /// <remarks>
        /// If the depth map is from the original depth perspecive, <paramref name="camera"/> should be Depth. If it has
        /// been transformed to the color camera perspective, <paramref name="camera"/> should be Color.
        ///
        /// The <paramref name="pointCloud"/> image must be of format Custom. Each pixel will be an XYZ set of 16 bit values,
        /// therefore its stride must be 2(bytes) * 3(x,y,z) * width of the <paramref name="depth"/> image in pixels.
        /// </remarks>
        public void DepthImageToPointCloud(Image depth, Image pointCloud, CalibrationDeviceType camera = CalibrationDeviceType.Depth)
        {
            if (depth == null)
            {
                throw new ArgumentNullException(nameof(depth));
            }

            if (pointCloud == null)
            {
                throw new ArgumentNullException(nameof(pointCloud));
            }

            lock (this)
            {
                if (this.disposedValue)
                {
                    throw new ObjectDisposedException(nameof(Transformation));
                }

                // Create a new reference to the Image objects so that they cannot be disposed while
                // we are performing the transformation
                using (Image depthReference = depth.Reference())
                using (Image pointCloudReference = pointCloud.Reference())
                {
                    // Ensure changes made to the managed memory are visible to the native layer
                    depthReference.FlushMemory();

                    AzureKinectException.ThrowIfNotSuccess(NativeMethods.k4a_transformation_depth_image_to_point_cloud(
                        this.handle,
                        depthReference.DangerousGetHandle(),
                        camera,
                        pointCloudReference.DangerousGetHandle()));

                    // Copy the native memory back to managed memory if required
                    pointCloudReference.InvalidateMemory();
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);

            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implements the dispose operation.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.handle.Close();
                }

                this.disposedValue = true;
            }
        }
    }
}