﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MiniEngine.Controllers;
using MiniEngine.Input;
using MiniEngine.Rendering;
using MiniEngine.Rendering.Lighting;
using MiniEngine.Scenes;
using MiniEngine.Utilities;

namespace MiniEngine
{    
    public class GameLoop : Game
    {
        private readonly GraphicsDeviceManager Graphics;
        private readonly KeyboardInput KeyboardInput;
        private readonly MouseInput MouseInput;

        private bool detailView = true;
        private int viewIndex = 0;
        private int viewOptions = 4;

        private Camera camera;
        private SpriteBatch spriteBatch;
        private IScene[] scenes;
        private int currentSceneIndex = 0;
        private CameraController cameraController;
        private RenderSystem renderSystem;
        

        public GameLoop()
        {
            this.Graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1080,
                PreferredBackBufferHeight = 768,
                SynchronizeWithVerticalRetrace = false,
                GraphicsProfile = GraphicsProfile.HiDef
            };            

            this.KeyboardInput = new KeyboardInput();
            this.MouseInput = new MouseInput();

            this.Content.RootDirectory = "Content";
            this.IsMouseVisible = true;
        }
        
        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            this.spriteBatch = new SpriteBatch(this.GraphicsDevice);
            
            this.camera = new Camera(this.GraphicsDevice.Viewport);
            this.cameraController = new CameraController(this.KeyboardInput, this.MouseInput, camera);

            this.scenes = new IScene[]
            {                
                new SponzaScene(this.GraphicsDevice, this.camera),
                new ZimaScene(this.GraphicsDevice)
            };

            foreach (var scene in this.scenes)
            {
                scene.LoadContent(this.Content, this.GraphicsDevice);
            }
            
            this.renderSystem = new RenderSystem(this.GraphicsDevice, this.Content, this.scenes[0]);
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            this.KeyboardInput.Update();
            this.MouseInput.Update();

            if (this.KeyboardInput.Click(Keys.OemTilde))
            {
                this.detailView = !this.detailView;
            }

            if (this.KeyboardInput.Click(Keys.Tab))
            {
                this.currentSceneIndex = (this.currentSceneIndex + 1) % this.scenes.Length;
                this.renderSystem.Scene = this.scenes[this.currentSceneIndex];
            }

            if (this.KeyboardInput.Click(Keys.F))
            {
                this.IsFixedTimeStep = !this.IsFixedTimeStep;
            }

            if (this.KeyboardInput.Click(Keys.OemPlus))
            {
                this.viewIndex = (this.viewIndex + 1) % this.viewOptions;
            }
            else if (this.KeyboardInput.Click(Keys.OemMinus))
            {
                this.viewIndex = (this.viewIndex + this.viewOptions - 1) % this.viewOptions;
            }

            if (this.KeyboardInput.Click(Keys.LeftControl))
            {
                this.renderSystem.EnableFXAA = !this.renderSystem.EnableFXAA;                
            }

            // HACK: dropping some lights
            var selectedScene = this.scenes[this.currentSceneIndex];
            if (this.KeyboardInput.Click(Keys.Q))
            {
                var color = ColorUtilities.PickRandomColor();
                var light = new PointLight(this.camera.Position, color, 10, 1.0f);
                selectedScene.PointLights.Add(light);
            }

            if (this.KeyboardInput.Click(Keys.LeftAlt))
            {
                var light = new ShadowCastingLight(this.GraphicsDevice, this.camera.Position, this.camera.LookAt, Color.White);
                selectedScene.ShadowCastingLights.Add(light);
            }

            if (this.KeyboardInput.Click(Keys.H))
            {
                selectedScene.Sunlights.ForEach(x => x.Move(this.camera.Position, this.camera.LookAt));
            }

            this.cameraController.Update(gameTime.ElapsedGameTime);

            this.renderSystem.Scene.Update(gameTime.ElapsedGameTime);

            base.Update(gameTime);
        }        

        protected override void Draw(GameTime gameTime)
        {
            this.GraphicsDevice.Clear(Color.CornflowerBlue);

            this.renderSystem.Render(this.camera);
            this.Window.Title = $"{gameTime.ElapsedGameTime.TotalMilliseconds:F2}ms, {(1.0f / gameTime.ElapsedGameTime.TotalSeconds):F2} fps, Fixed Time Step: {this.IsFixedTimeStep} (press 'F' so switch), Camera Position {this.camera.Position}";

            this.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise);

            var gBuffer = this.renderSystem.GetIntermediateRenderTargets();
            this.viewOptions = gBuffer.Length;

            if (this.detailView)
            {
                var step = this.GraphicsDevice.Viewport.Width / gBuffer.Length;
                for (var i = 0; i < gBuffer.Length; i++)
                {
                    var target = gBuffer[i];
                    this.spriteBatch.Draw(
                        target,
                        new Vector2(step * i, 0),
                        null,
                        Color.White,
                        0.0f,
                        Vector2.Zero,
                        1.0f / gBuffer.Length,
                        SpriteEffects.None,
                        0);
                }
            }
            else
            {
                this.spriteBatch.Draw(
                    gBuffer[this.viewIndex],
                    new Vector2(0, 0),
                    null,
                    Color.White,
                    0.0f,
                    Vector2.Zero,
                    1.0f,
                    SpriteEffects.None,
                    0);
            }

            this.spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
