﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniEngine.Rendering.Lighting;
using MiniEngine.Rendering.Primitives;
using MiniEngine.Scenes;

namespace MiniEngine.Rendering
{
    public sealed class RenderSystem
    {
        private readonly GraphicsDevice Device;
        private readonly Effect ClearEffect;                
        private readonly Effect CombineEffect;
        private readonly Effect PostProcessEffect;
        private readonly Quad Quad;
        private readonly RenderTarget2D ColorTarget;
        private readonly RenderTarget2D NormalTarget;
        private readonly RenderTarget2D DepthTarget;
        private readonly RenderTarget2D LightTarget;
        private readonly RenderTarget2D CombineTarget;        

        private readonly DirectionalLightSystem DirectionalLightSystem;
        private readonly PointLightSystem PointLightSystem;
        private readonly ShadowCastingLightSystem ShadowCastingLightSystem;
        private readonly ShadowSystem ShadowSystem;

        public RenderSystem(GraphicsDevice device, Effect clearEffect, Effect directionalLightEffect, Effect pointLightEffect, Effect shadowMapEffect, Effect shadowCastingLightEffect,
            Model sphere, Effect combineEffect, Effect postProcessEffect, IScene scene)
        {
            this.Device = device;
            this.ClearEffect = clearEffect;            
            this.CombineEffect = combineEffect;
            this.PostProcessEffect = postProcessEffect;

            this.Scene = scene;

            this.Quad = new Quad();

            var width = device.PresentationParameters.BackBufferWidth;
            var height = device.PresentationParameters.BackBufferHeight;

            // Do not enable AA as we use FXAA during post processing
            const int aaSamples = 0;

            this.ColorTarget = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24, aaSamples, RenderTargetUsage.DiscardContents);
            this.NormalTarget = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, aaSamples, RenderTargetUsage.DiscardContents);
            this.DepthTarget  = new RenderTarget2D(device, width, height, false, SurfaceFormat.Single, DepthFormat.None, aaSamples, RenderTargetUsage.DiscardContents);
            this.LightTarget  = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, aaSamples, RenderTargetUsage.DiscardContents);
            this.CombineTarget = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, aaSamples, RenderTargetUsage.DiscardContents);            

            this.DirectionalLightSystem = new DirectionalLightSystem(device, directionalLightEffect);
            this.PointLightSystem = new PointLightSystem(device, pointLightEffect, sphere);
            this.ShadowCastingLightSystem = new ShadowCastingLightSystem(device, shadowMapEffect, shadowCastingLightEffect);
            this.ShadowSystem = new ShadowSystem(device, shadowMapEffect, null);

            this.ShadowingLight = new ShadowingLight(device, Vector3.Down);
        }

        public bool EnableFXAA { get; set; } = true;

        public IScene Scene { get; set; }        

        public RenderTarget2D[] GetIntermediateRenderTargets() => new[]
        {
            this.ShadowingLight.ShadowMaps[0],
            this.ShadowingLight.ShadowMaps[1],
            this.ShadowingLight.ShadowMaps[2],
            this.ShadowingLight.ShadowMaps[3],
            this.ColorTarget,
            this.NormalTarget,
            this.DepthTarget,
            //this.LightTarget,
            //this.CombineTarget            
        };

        public void Render(Camera camera)
        {
            RenderGBuffer(camera);

            RenderLights(camera);            

            Combine();

            PostProcess();
        }   

        private void PostProcess()
        {            
            using (this.Device.PostProcessState())
            {
                // Post process the image
                foreach (var pass in this.PostProcessEffect.Techniques[0].Passes)
                {
                    this.PostProcessEffect.Parameters["ScaleX"].SetValue(1.0f / this.CombineTarget.Width);
                    this.PostProcessEffect.Parameters["ScaleY"].SetValue(1.0f / this.CombineTarget.Height);
                    this.PostProcessEffect.Parameters["ColorMap"].SetValue(this.CombineTarget);
                    this.PostProcessEffect.Parameters["NormalMap"].SetValue(this.NormalTarget);                                        
                    this.PostProcessEffect.Parameters["Strength"].SetValue(this.EnableFXAA? 2.0f : 0.0f);                    
                    pass.Apply();

                    this.Quad.Render(this.Device);
                }
            }            
        }        

        private void Combine()
        {
            this.Device.SetRenderTarget(this.CombineTarget);

            using (this.Device.PostProcessState())
            {
                // Combine everything
                foreach (var pass in this.CombineEffect.Techniques[0].Passes)
                {
                    this.CombineEffect.Parameters["ColorMap"].SetValue(this.ColorTarget);
                    this.CombineEffect.Parameters["LightMap"].SetValue(this.LightTarget);                    

                    pass.Apply();
                    this.Quad.Render(this.Device);
                }
            }

            this.Device.SetRenderTarget(null);
        }

        public ShadowingLight ShadowingLight { get; }

        private void RenderLights(Camera camera)
        {
            this.ShadowSystem.RenderShadowMaps(camera, this.ShadowingLight, this.Scene);

            //this.ShadowCastingLightSystem.RenderShadowMaps(this.Scene.ShadowCastingLights, this.Scene);

            this.Device.SetRenderTarget(this.LightTarget);

            this.Device.Clear(new Color(this.Scene.AmbientLight.R, this.Scene.AmbientLight.G, this.Scene.AmbientLight.B, (byte)0));

            this.PointLightSystem.Render(this.Scene.PointLights, camera, this.ColorTarget, this.NormalTarget, this.DepthTarget);
            this.DirectionalLightSystem.Render(this.Scene.DirectionalLights, camera, this.ColorTarget, this.NormalTarget, this.DepthTarget);
            //this.ShadowCastingLightSystem.RenderLights(this.Scene.ShadowCastingLights, camera, this.ColorTarget, this.NormalTarget, this.DepthTarget);

            this.Device.SetRenderTarget(null);
        }

        private void RenderGBuffer(IViewPoint viewPoint)
        {
            this.Device.SetRenderTargets(this.ColorTarget, this.NormalTarget, this.DepthTarget);

            using (this.Device.PostProcessState())
            {

                foreach (var pass in this.ClearEffect.Techniques[0].Passes)
                {
                    pass.Apply();
                    this.Quad.Render(this.Device);
                }
            }

            using (this.Device.GeometryState())
            {
                this.Scene.Draw(viewPoint);
            }

            this.Device.SetRenderTargets(null);
        }
    }
}
