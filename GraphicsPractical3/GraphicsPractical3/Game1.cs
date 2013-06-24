using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace GraphicsPractical3
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        // Often used XNA objects
        private GraphicsDevice device;
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private FrameRateCounter frameRateCounter;
        private InputHandler inputHandler;
        private Effect effect;

        // Game objects and variables
        private Camera camera;

        // Model
        private Model model;
        private Material modelMaterial;

        // Quad
        private VertexPositionNormalTexture[] quadVertices;
        private short[] quadIndices;
        private Matrix quadTransform;

        public Game1()
        {
            this.graphics = new GraphicsDeviceManager(this);
            this.Content.RootDirectory = "Content";
            // Create and add a frame rate counter
            this.frameRateCounter = new FrameRateCounter(this);
            this.Components.Add(this.frameRateCounter);
        }
        
        protected override void Initialize()
        {
            this.device = graphics.GraphicsDevice;
            // Copy over the device's rasterizer state to change the current fillMode
            this.device.RasterizerState = new RasterizerState() { CullMode = CullMode.None };
            // Set up the window
            this.graphics.PreferredBackBufferWidth = 800;
            this.graphics.PreferredBackBufferHeight = 600;
            this.graphics.IsFullScreen = false;
            // Let the renderer draw and update as often as possible
            this.graphics.SynchronizeWithVerticalRetrace = false;
            this.IsFixedTimeStep = false;
            // Flush the changes to the device parameters to the graphics card
            this.graphics.ApplyChanges();
            // Initialize the camera
            this.camera = new Camera(new Vector3(0, 50, 100), new Vector3(0, 0, 0), new Vector3(0, 1, 0));

            this.IsMouseVisible = true;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a SpriteBatch object
            this.spriteBatch = new SpriteBatch(this.device);
            // Load the "Simple" effect
            effect = this.Content.Load<Effect>("Effects/Simple");
            // Load the model and let it use the "Simple" effect
            this.model = this.Content.Load<Model>("Models/femalehead");
            this.model.Meshes[0].MeshParts[0].Effect = effect;
            // Setup the quad
            this.setupQuad();

            //set the color of the object
            this.modelMaterial.DiffuseColor = Color.Red;
            //set the position of the light   
            this.modelMaterial.Light = new Vector3(50, 50, 50);

            //ambient light color
            this.modelMaterial.AmbientColor = Color.Red;
            //ambient light intensity
            this.modelMaterial.AmbientIntensity = 0.2f;

            //specular light settings
            this.modelMaterial.Eye = this.camera.Eye;
            this.modelMaterial.SpecularColor = Color.White;
            this.modelMaterial.SpecularIntensity = 2.0f;
            this.modelMaterial.SpecularPower = 25.0f;

            //apply changes for the model effect
            this.modelMaterial.SetEffectParameters(effect);

            this.inputHandler = new InputHandler();
        }

        private void setupQuad()
        {
            float scale = 50.0f;

            // Normal points up
            Vector3 quadNormal = new Vector3(0, 1, 0);

            this.quadVertices = new VertexPositionNormalTexture[4];
            // Top left
            this.quadVertices[0].Position = new Vector3(-1, 0, -1);
            this.quadVertices[0].Normal = quadNormal;
            this.quadVertices[0].TextureCoordinate = new Vector2(0, 0);
            // Top right
            this.quadVertices[1].Position = new Vector3(1, 0, -1);
            this.quadVertices[1].Normal = quadNormal;
            this.quadVertices[1].TextureCoordinate = new Vector2(1, 0);
            // Bottom left
            this.quadVertices[2].Position = new Vector3(-1, 0, 1);
            this.quadVertices[2].Normal = quadNormal;
            this.quadVertices[2].TextureCoordinate = new Vector2(0, 1);
            // Bottom right
            this.quadVertices[3].Position = new Vector3(1, 0, 1);
            this.quadVertices[3].Normal = quadNormal;
            this.quadVertices[3].TextureCoordinate = new Vector2(1, 1);

            this.quadIndices = new short[] { 0, 1, 2, 1, 2, 3 };
            this.quadTransform = Matrix.CreateScale(scale) * Matrix.CreateTranslation(0.0f, -7.5f, 0.0f);
        }

        protected override void Update(GameTime gameTime)
        {
            float timeStep = (float)gameTime.ElapsedGameTime.TotalSeconds * 60.0f;

            Matrix rotateLeft = Matrix.CreateRotationY(timeStep * 0.02f);
            Matrix rotateRight = Matrix.CreateRotationY(-timeStep * 0.02f);
            Matrix zoomIn = Matrix.CreateTranslation(0.05f * -Vector3.Normalize(timeStep * camera.Eye));
            Matrix zoomOut = Matrix.CreateTranslation(0.05f * Vector3.Normalize(timeStep * camera.Eye));

            if (inputHandler.CheckKey(Keys.Left))
            {
                camera.Eye = Vector3.Transform(camera.Eye, rotateLeft);
            }

            if (inputHandler.CheckKey(Keys.Right))
            {
                camera.Eye = Vector3.Transform(camera.Eye, rotateRight);
            }

            if (inputHandler.CheckKey(Keys.Up) && Vector3.Distance(Vector3.Zero, camera.Eye) > 10)
            {
                camera.Eye = Vector3.Transform(camera.Eye, zoomIn);
            }

            if (inputHandler.CheckKey(Keys.Down) && Vector3.Distance(Vector3.Zero, camera.Eye) < 100)
            {
                camera.Eye = Vector3.Transform(camera.Eye, zoomOut);
            }

            camera.SetEffectParameters(effect);

            // Update the window title
            this.Window.Title = "XNA Renderer | FPS: " + this.frameRateCounter.FrameRate;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clear the screen in a predetermined color and clear the depth buffer
            this.device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.DeepSkyBlue, 1.0f, 0);

            // Get the model's only mesh
            ModelMesh mesh = this.model.Meshes[0];

            // Set the effect parameters
            effect.CurrentTechnique = effect.Techniques["Simple"];

            // Matrices for 3D perspective projection
            this.camera.SetEffectParameters(effect);

            // World transform for the teapot
            Matrix worldTransform = Matrix.CreateScale(0.5f);
            effect.Parameters["World"].SetValue(worldTransform);
            effect.Parameters["WorldNormal"].SetValue(Matrix.Transpose(Matrix.Invert(worldTransform)));

            // Let the shader know there's no texture
            effect.Parameters["HasTexture"].SetValue(false);

            // Draw the model
            mesh.Draw();

            // Pass the texture to the shader and let it know it has to use it
            effect.Parameters["DiffuseTexture"].SetValue(Content.Load<Texture>("Textures/CobblestonesDiffuse"));
            effect.Parameters["HasTexture"].SetValue(true);

            // World transform for the quad
            effect.Parameters["World"].SetValue(quadTransform);
            effect.Parameters["WorldNormal"].SetValue(Matrix.Transpose(Matrix.Invert(quadTransform)));

            // Draw the quad
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    quadVertices,
                    0,
                    quadVertices.Length,
                    quadIndices,
                    0,
                    quadIndices.Length / 3);
            }

            base.Draw(gameTime);
        }
    }
}
