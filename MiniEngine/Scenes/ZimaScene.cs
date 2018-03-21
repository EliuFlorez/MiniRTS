using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MiniEngine.Rendering;
using MiniEngine.Rendering.Lighting;
using MiniEngine.Units;
using MiniEngine.Utilities;
using DirectionalLight = MiniEngine.Rendering.Lighting.DirectionalLight;

namespace MiniEngine.Scenes
{
    public sealed class ZimaScene : AScene
    {
        private readonly Color[] ColorWheel = {
            new Color(255, 0, 0),
            new Color(255, 128, 0),
            new Color(255, 255, 0),
            new Color(128, 255, 0),
            new Color(0, 255, 0),
            new Color(0, 255, 128),
            new Color(0, 255, 255),
            new Color(0, 128, 255),
            new Color(0, 0, 255)
        };

        private readonly Matrix Ship1World = Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateScale(0.5f);

        private readonly Matrix LizardWorld = Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateScale(0.05f)
                                              * Matrix.CreateTranslation(Vector3.Left * 50);

        private readonly Matrix Ship2World = Matrix.CreateRotationX(-MathHelper.PiOver2)
                                             * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateScale(0.5f)
                                             * Matrix.CreateTranslation(Vector3.Right * 50);

        private Model lizard;
        private Model ship1;
        private Model ship2;

        private Seconds totalElapsed;

        public ZimaScene(GraphicsDevice device)
            : base(device)
        {

            this.DirectionalLights.Add(new DirectionalLight(Vector3.Normalize(Vector3.Forward + Vector3.Down), Color.White * 0.75f));
            this.DirectionalLights.Add(new DirectionalLight(Vector3.Normalize(Vector3.Forward + Vector3.Up + Vector3.Left), Color.BlueViolet * 0.25f));
            
            foreach (var color in this.ColorWheel)
            {
                this.PointLights.Add(new PointLight(Vector3.Zero, color, 120, 1.0f));
            }

            this.totalElapsed = 0;            
        }

        public override void LoadContent(ContentManager content)
        {
            this.lizard = content.Load<Model>(@"Lizard\Lizard");
            this.ship1 = content.Load<Model>(@"Ship1\Ship1");
            this.ship2 = content.Load<Model>(@"Ship2\Ship2");
        }

        public override void Update(Seconds elapsed)
        {
            //var step = MathHelper.TwoPi / this.PointLights.Count;
            //for (var i = 0; i < this.PointLights.Count; i++)
            //{
            //    var x = Math.Sin(step * i + this.totalElapsed.Value) * 100;
            //    var y = Math.Cos(step * i + this.totalElapsed.Value) * 100;

            //    this.PointLights[i].Position = new Vector3((float)x, (float)y, 0);
            //}

            //this.totalElapsed += elapsed;
        }

        public override BoundingBox ComputeBoundingBox()
        {
            var a = this.ship1.ComputeBoundingBox(this.Ship1World);
            var b = this.ship2.ComputeBoundingBox(this.Ship2World);
            var c = this.lizard.ComputeBoundingBox(this.LizardWorld);


            return BoundingBox.CreateMerged(BoundingBox.CreateMerged(a, b), c);
        }

        public override BoundingSphere ComputeBoundingSphere()
        {
            var a = this.ship1.ComputeBoundingSphere(this.Ship1World);
            var b = this.ship2.ComputeBoundingSphere(this.Ship2World);
            var c = this.lizard.ComputeBoundingSphere(this.LizardWorld);


            return BoundingSphere.CreateMerged(BoundingSphere.CreateMerged(a, b), c);
        }

        public override void Draw(IViewPoint viewPoint)
        {
            DrawModel(this.ship1, this.Ship1World, viewPoint);
            DrawModel(this.lizard, this.LizardWorld, viewPoint);
            DrawModel(this.ship2, this.Ship2World, viewPoint);
        }

        public override void Draw(Effect effectOverride, IViewPoint viewPoint)
        {
            DrawModel(effectOverride, this.ship1, this.Ship1World, viewPoint);
            DrawModel(effectOverride, this.lizard, this.LizardWorld, viewPoint);
            DrawModel(effectOverride, this.ship2, this.Ship2World, viewPoint);
        }
    }
}
