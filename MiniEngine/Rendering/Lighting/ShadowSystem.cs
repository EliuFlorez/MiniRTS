using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniEngine.Rendering.Primitives;
using MiniEngine.Scenes;

namespace MiniEngine.Rendering.Lighting
{
    public sealed class ShadowSystem
    {
        private readonly GraphicsDevice Device;
        private readonly Effect ShadowMapEffect;
        private readonly Effect LightEffect;
        private readonly Quad Quad;

        public ShadowSystem(GraphicsDevice device, Effect shadowMapEffect, Effect lightEffect)
        {
            this.LightEffect = lightEffect;
            this.ShadowMapEffect = shadowMapEffect;
            this.Device = device;

            this.Quad = new Quad();
        }

        public void RenderShadowMaps(Camera camera, ShadowingLight light, IScene geometry)
        {
            const int numCascades = ShadowingLight.NumCascades;
            const int shadowMapSize = ShadowingLight.ShadowMapSize;

            var bounds = geometry.ComputeBoundingBox();

            var MinDistance = camera.NearPlane;
            var MaxDistance = Vector3.Distance(bounds.Min, bounds.Max);
            
            // Manual cascade splits
            var cascadeSplits = new float[numCascades];
            //var lambda = 1.0f;
            //var nearClip = MinDistance;
            //var farClip = MaxDistance;
            //var clipRange = farClip - nearClip;

            //var minZ = nearClip + MinDistance * clipRange;
            //var maxZ = nearClip + MaxDistance * clipRange;

            //var range = maxZ - minZ;
            //var ratio = maxZ / minZ;

            //for (var i = 0; i < numCascades; i++)
            //{
            //    var p = (i + 1) / (float) numCascades;
            //    var log = minZ * (float) Math.Pow(ratio, p);
            //    var uniform = minZ + range * p;
            //    var d = lambda * (log - uniform) + uniform;
            //    cascadeSplits[i] = (d - nearClip) / clipRange;
            //}

            cascadeSplits[0] = 5;
            cascadeSplits[1] = 15;
            cascadeSplits[2] = 50;
            cascadeSplits[3] = 250;

            
            // TODO: where
            //var globalShadowMatrix = MakeGlobalShadowMatrix(Vector3.Normalize(light.Direction), camera);
            //this.ShadowMapEffect.Parameters["ShadowMatrix"].SetValue(globalShadowMatrix);


            var originalViewport = this.Device.Viewport;
            this.Device.Viewport = new Viewport(0, 0, shadowMapSize, shadowMapSize);

            using (this.Device.GeometryState())
            {

                // Render the mesh for each cascade
                for (var cascadeIndex = 0; cascadeIndex < numCascades; cascadeIndex++)
                {
                    var shadowMap = light.ShadowMaps[cascadeIndex];
                    this.Device.SetRenderTarget(shadowMap);
                    this.Device.Clear(Color.Black);

                    // Get the 8 points of the view frustum in world space
                    var frustumCornersWS = new[]
                    {
                        new Vector3(-1.0f, 1.0f, 0.0f),
                        new Vector3(1.0f, 1.0f, 0.0f),
                        new Vector3(1.0f, -1.0f, 0.0f),
                        new Vector3(-1.0f, -1.0f, 0.0f),
                        new Vector3(-1.0f, 1.0f, 1.0f),
                        new Vector3(1.0f, 1.0f, 1.0f),
                        new Vector3(1.0f, -1.0f, 1.0f),
                        new Vector3(-1.0f, -1.0f, 1.0f),
                    };

                    var prevSplitDist = cascadeIndex == 0 ? MinDistance : cascadeSplits[cascadeIndex - 1];
                    var splitDist = cascadeSplits[cascadeIndex];

                    var invViewProj = camera.InverseViewProjection;

                    for (var i = 0; i < frustumCornersWS.Length; i++)
                    {
                        frustumCornersWS[i] = Vector3.Transform(frustumCornersWS[i], invViewProj);
                    }

                    // Get the corners of the current cascade slice of the view frustum
                    for (var i = 0; i < 4; i++)
                    {
                        var cornerRay = frustumCornersWS[i + 4] - frustumCornersWS[i];
                        var nearCornerRay = cornerRay * prevSplitDist;
                        var farCornerRay = cornerRay * splitDist;
                        frustumCornersWS[i + 4] = frustumCornersWS[i] + farCornerRay;
                        frustumCornersWS[i] = frustumCornersWS[i] + nearCornerRay;
                    }

                    // Calculate the centroid of the view frustum slice
                    var frustumCenter = Vector3.Zero;
                    for (var i = 0; i < 8; i++)
                    {
                        frustumCenter = frustumCenter + frustumCornersWS[i];
                    }

                    frustumCenter *= 1.0f / 8.0f;

                    // Pick the up vector to use for the light camera

                    // This needs to be constant for it to be stable
                    var upDir = Vector3.Up;

                    // Calculate the radius of a bounding sphere surrounding the frustum corners
                    var sphereRadius = 0.0f;
                    for (var i = 0; i < 8; i++)
                    {
                        var dist = Vector3.Distance(frustumCornersWS[i], frustumCenter);
                        sphereRadius = Math.Max(sphereRadius, dist);
                    }

                    sphereRadius = (float) Math.Ceiling(sphereRadius * 16.0f) / 16.0f;

                    var maxExtents = new Vector3(sphereRadius, sphereRadius, sphereRadius);
                    var minExtents = -maxExtents;

                    var cascadeExtents = maxExtents - minExtents;

                    // Get position of the shadow camera
                    var shadowCameraPos = frustumCenter + light.Direction * -minExtents.Z;

                    // Come up with a new orthographic camera for the shadow caster
                    var shadowCamera = new OrthographicCamera(
                        minExtents.X,
                        minExtents.Y,
                        maxExtents.X,
                        maxExtents.Y,
                        0.0f,
                        cascadeExtents.Z);
                    shadowCamera.SetLookAt(shadowCameraPos, frustumCenter, upDir);

                    // Create the rounding matrix, by projecting the world-space origin and determining
                    // the fractional offset in texel space
                    var shadowMatrix = shadowCamera.ViewProjection;
                    var shadowOrigin = new Vector4(0, 0, 0, 1.0f);
                    shadowOrigin = Vector4.Transform(shadowOrigin, shadowMatrix);
                    shadowOrigin = shadowOrigin * (shadowMapSize / 2.0f);

                    var roundedOrigin = new Vector4(
                        (float) Math.Round(shadowOrigin.X),
                        (float) Math.Round(shadowOrigin.Y),
                        (float) Math.Round(shadowOrigin.Z),
                        (float) Math.Round(shadowOrigin.W));

                    var roundOffset = roundedOrigin - shadowOrigin;
                    roundOffset = roundOffset * (2.0f / shadowMapSize);
                    roundOffset.Z = 0.0f;
                    roundOffset.W = 0.0f;

                    var shadowProj = shadowCamera.Projection;
                    shadowProj.Translation = shadowProj.Translation
                                             + new Vector3(roundOffset.X, roundOffset.Y, roundOffset.Z);
                    shadowCamera.SetProjection(shadowProj);


                    geometry.Draw(this.ShadowMapEffect, shadowCamera);                    
                    this.Device.SetRenderTarget(null);
                }
            }

            this.Device.Viewport = originalViewport;
        }

        // Makes the "global" shadow matrix used as the reference point for the cascades
        private static Matrix MakeGlobalShadowMatrix(Vector3 lightDirection, Camera camera)
        {
            // Get the 8 points of the view frustum in world space
            var frustumCorners = new []
            {
                new Vector3(-1.0f, 1.0f, 0.0f),
                new Vector3(1.0f, 1.0f, 0.0f),
                new Vector3(1.0f, -1.0f, 0.0f),
                new Vector3(-1.0f, -1.0f, 0.0f),
                new Vector3(-1.0f, 1.0f, 1.0f),
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(1.0f, -1.0f, 1.0f),
                new Vector3(-1.0f, -1.0f, 1.0f),
            };

            var frustumCenter = Vector3.Zero;
            for (var i = 0; i < 8; i++)
            {
                frustumCorners[i] = Vector3.Transform(frustumCorners[i], camera.InverseViewProjection);
                frustumCenter += frustumCorners[i];
            }

            frustumCenter /= 8;

            // The up direction needs to be constant to be stable
            var upDir = Vector3.Up;

            var lightCameraPos = frustumCenter;
            var lookAt = frustumCenter - lightDirection;
            var lightView = Matrix.CreateLookAt(lightCameraPos, lookAt, upDir);

            var shadowCameraPosition = frustumCenter + lightDirection * 0.5f;

            // Create an orthographic camera for the shadow caster
            var minX = -0.5f;
            var minY = -0.5f;
            var maxX = 0.5f;
            var maxY = 0.5f;
            var nearClip = 0.0f;
            var farClip = 1.0f;

            var orthographicCamera = new OrthographicCamera(minX, minY, maxX, maxY, nearClip, farClip);
            orthographicCamera.SetLookAt(shadowCameraPosition, frustumCenter, upDir);
            
            var texScaleBias = Matrix.CreateScale(0.5f, -0.5f, 1.0f);           
            texScaleBias.Translation = new Vector3(0.5f, 0.5f, 0.0f);
            return orthographicCamera.ViewProjection * texScaleBias;
        }

        private class OrthographicCamera : IViewPoint
        {           
            public OrthographicCamera(float minX, float minY, float maxX, float maxY, float nearClip, float farClip)
            {
                this.MinX = minX;
                this.MinY = minY;
                this.MaxX = maxX;
                this.MaxY = maxY;
                this.NearClip = nearClip;
                this.FarClip = farClip;

                CreateProjection();
            }

            private void CreateProjection()
            {                
                this.Projection = Matrix.CreateOrthographicOffCenter(
                    this.MinX,
                    this.MaxX,
                    this.MinY,
                    this.MaxY,
                    this.NearClip,
                    this.FarClip);

                this.ViewProjection = this.View * this.Projection;
            }

            public void SetLookAt(Vector3 position, Vector3 lookAt, Vector3 up)
            {
                this.View = Matrix.CreateLookAt(position, lookAt, up);
                CreateProjection();
            }

            public float MinX { get; }
            public float MinY { get; }
            public float MaxX { get; }
            public float MaxY { get; }
            public float NearClip { get; }
            public float FarClip { get; }

            public Matrix Projection { get; private set; }
            public Matrix View { get; private set; }
            public Matrix ViewProjection { get; private set; }

            public void SetProjection(Matrix projection)
            {
                this.Projection = projection;
                this.ViewProjection = this.View * this.Projection;
            }
        }
    }    
}
