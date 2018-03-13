﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniEngine.Mathematics;

namespace MiniEngine.Rendering.Lighting
{
    public sealed class Sunlight
    {
        private const int ShadowMapResolution = 1024;

        // Sunlight based on tutorial from
        // http://dev.theomader.com/cascaded-shadow-mapping-1/

        private readonly float[] ViewSpaceSplitDistances = {
            -1,
            -15.0f,
            -50.0f,
            -300.0f
        };

        private readonly BoundingBox SceneBoundingBox;
        private readonly BoundingSphere SceneBoundingSphere;

        public Sunlight(GraphicsDevice device, BoundingBox sceneBoundingBox, BoundingSphere sceneBoundingSphere, Camera camera, Color color)
        {
            this.Camera = camera;
            this.ShadowMap = new RenderTarget2D(device, ShadowMapResolution, ShadowMapResolution, false, SurfaceFormat.Single, DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents);

            this.SceneBoundingBox = sceneBoundingBox;
            this.SceneBoundingSphere = sceneBoundingSphere;

            this.ColorVector = color.ToVector3();

            Move(Vector3.Backward * 10, Vector3.Zero);
        }

        public Vector3 ColorVector { get; set; }

        public Color Color
        {
            get => new Color(this.ColorVector);
            set => this.ColorVector = value.ToVector3();
        }

        public Camera Camera { get; }
        public RenderTarget2D ShadowMap { get; }

        public FrustumSplitProjection[] FrustumSplitProjections { get; private set; }
        
        public Vector3 Position { get; private set; }
        public Vector3 LookAt { get; private set; }
        public Matrix View { get; private set; }
        public Matrix Projection { get; private set; }
        public Matrix Transform { get; private set; }

        public Matrix[] ShadowTransform { get; private set; }
        public Vector4[] ShadowSplitTileBounds { get; private set; }

        public void Move(Vector3 position, Vector3 lookAt)
        {
            this.Position = position;
            this.LookAt = lookAt;

            this.View = Matrix.CreateLookAt(position, lookAt, Vector3.Up);

            var center = Vector3.Transform(this.SceneBoundingSphere.Center, this.View);
            var min = center - new Vector3(this.SceneBoundingSphere.Radius);
            var max = center + new Vector3(this.SceneBoundingSphere.Radius);

            this.Projection = Matrix.CreateOrthographicOffCenter(min.X, max.X, min.Y, max.Y, -max.Z, -min.Z);
            this.Transform = this.View * this.Projection;

            Recompute();
        }

        public void Recompute()
        {
            this.FrustumSplitProjections = Frustum.SplitFrustum(this.View, this.Camera, this.SceneBoundingBox, this.ViewSpaceSplitDistances);
            this.ShadowTransform = new Matrix[4];
            this.ShadowSplitTileBounds = new Vector4[4];
            
            // Note: only works for orthographic projections
            for (var i = 0; i < this.FrustumSplitProjections.Length; i++)
            {
                // compute block index into shadow atlas
                var tileX = i % 2;
                var tileY = i / 2;

                // tile matrix: maps from clip space to shadow atlas block
                var tileMatrix = Matrix.Identity;
                tileMatrix.M11 = 0.25f;
                tileMatrix.M22 = -0.25f;
                tileMatrix.Translation = new Vector3(0.25f + tileX * 0.5f, 0.25f + tileY * 0.5f, 0);                

                // now combine with shadow view and projection
                
                this.ShadowTransform[i] = this.FrustumSplitProjections[i].View * this.FrustumSplitProjections[i].Projection * tileMatrix;


                // [x min, x max, y min, y max]
                var tileBorder = 3.0f / (float)ShadowMapResolution;
                var tileBounds = new Vector4(
                    0.5f * tileX + tileBorder,
                    0.5f * tileX + 0.5f - tileBorder,
                    0.5f * tileY + tileBorder,
                    0.5f * tileY + 0.5f - tileBorder
                );

                this.ShadowSplitTileBounds[i] = tileBounds;
            }
        }
    }
}