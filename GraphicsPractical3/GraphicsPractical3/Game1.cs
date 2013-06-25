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
        private SpriteFont spriteFont;

        // Effects
        private Effect effect;
        private int numberOfTechniques;
        private int currentTechniqueNumber;

        // Game objects and variables
        private Camera camera;
        private InputHandler inputHandler;

        // Model
        private Model model;
        private Material modelMaterial;

        // Quad
        private VertexPositionNormalTexture[] quadVertices;
        private short[] quadIndices;
        private Matrix quadTransform;

        // Multiple Light Sources
        private int NumberOfLights;
        private Vector3[] MLS;
        private Vector3[] MLSDiffuseColors;

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

            // Create a SpriteFor object
            this.spriteFont = this.Content.Load<SpriteFont>("Fonts/Font");

            // Load the "Simple" effect
            effect = this.Content.Load<Effect>("Effects/Simple");
            currentTechniqueNumber = 1;
            numberOfTechniques = 4;

            // Load the model and let it use the "Simple" effect
            this.model = this.Content.Load<Model>("Models/femalehead");
            this.model.Meshes[0].MeshParts[0].Effect = effect;

            // Setup the quad
            this.setupQuad();

            //set the color of the object
            this.modelMaterial.DiffuseColor = Color.Gold;
            //set the position of the light   
            this.modelMaterial.Light = new Vector3(50, 50, 50);
            //set the spotlights parameters
            this.modelMaterial.SpotlightPos = new Vector3(20,20,30);
            this.modelMaterial.spotColor = Color.Red;
            this.modelMaterial.spotDirection = new Vector3(-0.5f, -1, -0.5f);

            //Multiple Lights creation 
            NumberOfLights = 10;
            MLS = new Vector3[NumberOfLights];
            MLSDiffuseColors = new Vector3[NumberOfLights];
            MultipleLightCreation();


            //ambient light color
            this.modelMaterial.AmbientColor = Color.Gold;
            //ambient light intensity
            this.modelMaterial.AmbientIntensity = 0.2f;

            //specular light settings
            this.modelMaterial.SpecularColor = Color.White;
            this.modelMaterial.SpecularIntensity = 2.0f;
            this.modelMaterial.Roughness = 05f;
            this.modelMaterial.ReflectionCoefficient = 1.42f;

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

        //----------------------------------------------------------------------------
        // Name: MultipleLightCreation()
        // Desc: Create multiple lights 
        //----------------------------------------------------------------------------
        private void MultipleLightCreation()
        {            
            for (int i = 0; i < NumberOfLights; i++)
            {

                MLSDiffuseColors[i] = new Vector3(0, 10, 0);
            }
            MLS[1] = new Vector3(10, 70, 10);
            MLS[2] = new Vector3(-10, 70, 10);
            MLS[3] = new Vector3(10, 70, -10);
            MLS[4] = new Vector3(-10, 70, -10);

            this.modelMaterial.MLS = MLS;
            this.modelMaterial.MLSDiffuseColors = MLSDiffuseColors;
        }

        //----------------------------------------------------------------------------
        // Name: HandleInput()
        // Desc: Handles the Input
        //----------------------------------------------------------------------------
        private void HandleInput(float timeStep)
        {
            inputHandler.UpdateStates();
            // Camera Movement
            float rotationScale = 0.02f;
            float zoomScale = 0.05f;

            Matrix rotateLeft = Matrix.CreateRotationY(timeStep * rotationScale);
            Matrix rotateRight = Matrix.CreateRotationY(-timeStep * rotationScale);
            Matrix zoomIn = Matrix.CreateTranslation(zoomScale * -Vector3.Normalize(timeStep * camera.Eye));
            Matrix zoomOut = Matrix.CreateTranslation(zoomScale * Vector3.Normalize(timeStep * camera.Eye));

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


            // Technique Cycle
            if (inputHandler.CheckKey(Keys.Space, false))
                currentTechniqueNumber++;
            if (currentTechniqueNumber >= numberOfTechniques)
                currentTechniqueNumber = 0;

            switch (currentTechniqueNumber)
            { 
                case 0:
                    effect.CurrentTechnique = effect.Techniques["Simple"];
                    break;
                case 1:
                    effect.CurrentTechnique = effect.Techniques["Spotlight"];
                    break;
                case 2:
                    effect.CurrentTechnique = effect.Techniques["MultipleLightsSources"];
                    break;
                case 3:
                    effect.CurrentTechnique = effect.Techniques["GrayScale"];
                    break;
            }
        }

        //----------------------------------------------------------------------------
        // Name: InFustrum()
        // Desc: Checks wether an object is inside the fustrum
        //----------------------------------------------------------------------------
        private bool InFustrum(ModelMesh mesh, Matrix transform)
        {
            // check for intersection between the fustrum and the transformed modelmesh
            if (camera.Fustrum.Intersects(mesh.BoundingSphere.Transform(transform)))
                return true;
            return false;
        }

        private void DrawText(int[] cullCount)
        {
            // Write some text on the screen
            spriteBatch.Begin();
            spriteBatch.DrawString(spriteFont, "Drawn: " + cullCount[0], new Vector2(20.0f, 20.0f), Color.Red);
            spriteBatch.DrawString(spriteFont, "Culled: " + cullCount[1], new Vector2(20.0f, 40.0f), Color.Red);
            spriteBatch.End();

            // Reset everything that SpriteBatch fucks up
            this.device.BlendState = BlendState.Opaque;
            this.device.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = new RasterizerState() { CullMode = CullMode.None };
        }

        //----------------------------------------------------------------------------
        // Name: DrawModel(
        // Desc: Draws the Model
        //----------------------------------------------------------------------------
        private int[] DrawModels(ModelMesh[] meshes, Matrix[] worldTransforms)
        {
            int drawn = meshes.Length;
            int culled = 0;
            for (int i = 0; i < meshes.Length; i++)
            {
                // Don't draw it if it isn't in the FoV
                if (InFustrum(meshes[i], worldTransforms[i]))
                {
                    // Matrices for 3D perspective projection
                    this.camera.SetEffectParameters(effect);

                    // World transform for the teapot
                    effect.Parameters["World"].SetValue(worldTransforms[i]);
                    effect.Parameters["WorldNormal"].SetValue(Matrix.Transpose(Matrix.Invert(worldTransforms[i])));

                    // Let the shader know there's no texture
                    effect.Parameters["HasTexture"].SetValue(false);

                    // Draw the model
                    meshes[i].Draw();
                }
                else
                {
                    drawn--;
                    culled++;
                }
            }
            return new int[2] { drawn, culled };
        }

        //----------------------------------------------------------------------------
        // Name: DrawTexturedQuad()
        // Desc: Draws the Textured Quad
        //----------------------------------------------------------------------------
        private void DrawTexturedQuad()
        {
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
        }

        protected override void Update(GameTime gameTime)
        {
            float timeStep = (float)gameTime.ElapsedGameTime.TotalSeconds * 60.0f;

            HandleInput(timeStep);
            
            // Update the window title
            this.Window.Title = "XNA Renderer | FPS: " + this.frameRateCounter.FrameRate;

            base.Update(gameTime);

        }        

        protected override void Draw(GameTime gameTime)
        {    

            // Clear the screen in a predetermined color and clear the depth buffer
            this.device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

            ModelMesh[] meshes = new ModelMesh[3] { this.model.Meshes[0],
                                                    this.model.Meshes[0],
                                                    this.model.Meshes[0] };

            Matrix[] worldTransforms = new Matrix[3] { Matrix.CreateScale(0.5f),
                                                       Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(20.0f, 0.0f, -20.0f), 
                                                       Matrix.CreateScale(0.5f) * Matrix.CreateTranslation(-20.0f, 0.0f, -20.0f) };
            
            // Draw the models
            int[] cullCount = DrawModels(meshes, worldTransforms);
            // Draw the text
            DrawText(cullCount);
            // Draw the Textured Quad
            DrawTexturedQuad();            

            base.Draw(gameTime);
        }
    }
}
