using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using DirectionalLight = MiniEngine.Rendering.Lighting.DirectionalLight;

namespace MiniEngine.Rendering
{
    public sealed class Scene
    {
        private readonly GraphicsDevice Device;
        private Model ship1;

        public Scene(GraphicsDevice device, Camera camera)
        {
            this.Device = device;
            this.Camera = camera;

            this.DirectionalLights = new List<DirectionalLight>
            {
                new DirectionalLight(Vector3.Normalize(Vector3.Forward + Vector3.Down), Color.White * 0.75f),                
            };

        }

        public Camera Camera { get; }

        public List<DirectionalLight> DirectionalLights { get; }

        public void LoadContent(ContentManager content)
        {
            this.ship1 = content.Load<Model>(@"Ship1\Ship1");
        }
        public void Draw()
        {
            using (this.Device.GeometryState())
            {                
                DrawModel(this.ship1, Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateScale(0.5f));
            }
        }

        private void DrawModel(Model model, Matrix world)
        {
            foreach (var mesh in model.Meshes)
            {
                foreach (var effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue(world);
                    effect.Parameters["View"].SetValue(this.Camera.View);
                    effect.Parameters["Projection"].SetValue(this.Camera.Projection);                    
                }

                mesh.Draw();
            }
        }
    }
}
