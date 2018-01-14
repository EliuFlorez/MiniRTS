using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniEngine.Rendering.Lighting;
using MiniEngine.Rendering.Primitives;

namespace MiniEngine.Rendering
{
    public sealed class RenderSystem
    {
        private readonly GraphicsDevice Device;
        private readonly RenderTarget2D ColorTarget;
        private readonly RenderTarget2D NormalTarget;
        private readonly RenderTarget2D DepthTarget;


        public RenderSystem(GraphicsDevice device, Scene scene)
        {
            this.Device = device;

            this.Scene = scene;

            var width = device.PresentationParameters.BackBufferWidth;
            var height = device.PresentationParameters.BackBufferHeight;

            this.ColorTarget  = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24, 16, RenderTargetUsage.DiscardContents);
            this.NormalTarget = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 16, RenderTargetUsage.DiscardContents);
            this.DepthTarget  = new RenderTarget2D(device, width, height, false, SurfaceFormat.Single, DepthFormat.None, 16, RenderTargetUsage.DiscardContents);
        }       

        public Scene Scene { get; set; }        

        public RenderTarget2D[] GetIntermediateRenderTargets() => new[]
        {
            this.ColorTarget,          
        };

        public void Render()
        {
            this.Device.SetRenderTargets(this.ColorTarget, this.NormalTarget, this.DepthTarget);
            //this.Device.SetRenderTarget(this.ColorTarget);            

            // Draw scene
            this.Scene.Draw();

            // Resolve the G-Buffer
            this.Device.SetRenderTargets(null);
        }      
    }
}
