using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MiniEngine.Rendering.Lighting
{
    public sealed class ShadowingLight
    {
        public const int ShadowMapSize = 1024;
        public const int NumCascades = 4;

        public ShadowingLight(GraphicsDevice device, Vector3 direction)
        {
            this.ShadowMaps = new RenderTarget2D[NumCascades];
            for (var i = 0; i < NumCascades; i++)
            {
                this.ShadowMaps[i] = new RenderTarget2D(device, ShadowMapSize, ShadowMapSize, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents);
            }

            this.Direction = direction;
        }

        public RenderTarget2D[] ShadowMaps { get; }
        public Vector3 Direction { get; set; }
    }
}
