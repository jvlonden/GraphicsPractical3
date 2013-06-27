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
        private Random random;

        // Effects
        private Effect effect;
        private Effect postProcess;
        private int numberOfTechniques;
        private int currentTechniqueNumber;
        private string currentTechnique;

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

        //postprocess
        private RenderTarget2D target;
        private bool grayScale;
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
            // RNG
            this.random = new Random();

            this.IsMouseVisible = true;
            //initialize rendertarget
            target = new RenderTarget2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight, false, GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);

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
            //load postprocessing effect
            postProcess = this.Content.Load<Effect>("Effects/PostProcessing");
            currentTechniqueNumber = 0;
            numberOfTechniques = 3;

            // Load the model and let it use the "Simple" effect
            this.model = this.Content.Load<Model>("Models/femalehead");
            this.model.Meshes[0].MeshParts[0].Effect = effect;

            // Setup the quad
            this.setupQuad();

            //set the color of the object
            this.modelMaterial.DiffuseColor = Color.GreenYellow;
            //set the position of the light   
            this.modelMaterial.Light = new Vector3(50, 50, 50);

            //Multiple Lights creation 
            CreateMultipleSpots();

            //ambient light color
            this.modelMaterial.AmbientColor = Color.Black;
            //ambient light intensity
            this.modelMaterial.AmbientIntensity = 0.2f;

            //specular light settings
            this.modelMaterial.SpecularColor = Color.White;
            this.modelMaterial.SpecularIntensity = 1.5f;
            this.modelMaterial.Roughness = 2.0f;
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
            this.quadTransform = Matrix.CreateScale(scale) * Matrix.CreateTranslation(0.0f, -9.0f, 0.0f);
        }

        private void DrawSceneToTexture(RenderTarget2D renderTarget)
        {
            // Set the render target
            GraphicsDevice.SetRenderTarget(renderTarget);

            GraphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };

            // Draw the scene
            DrawScene();

            // Drop the render target
            GraphicsDevice.SetRenderTarget(null);
        }
        //----------------------------------------------------------------------------
        // Name: MultipleLightCreation()
        // Desc: Create multiple lights 
        //----------------------------------------------------------------------------
        private void CreateMultipleSpots()
        {
            Vector3 pos, dir, foc;
            Vector4 col;

            modelMaterial.SpotPos = new Vector3[10];
            modelMaterial.SpotDir = new Vector3[10];
            modelMaterial.SpotCol = new Vector4[10];

            modelMaterial.SpotPos[0] = new Vector3(60, 60, 90);
            modelMaterial.SpotDir[0] = Vector3.Normalize(new Vector3(-60, -60, -90));
            modelMaterial.SpotCol[0] = Color.Red.ToVector4();

            for (int i = 1; i < 10; i++)
            { 
                pos = new Vector3(random.Next(-90, 90),
                                  60,
                                  random.Next(-90, 90));

                foc = new Vector3(random.Next(-30, 30),
                                  0,
                                  random.Next(-30, 30));

                dir = Vector3.Normalize(foc - pos);

                switch (i)
                { 
                    case 1:
                        col = Color.Blue.ToVector4();
                        break;
                    case 2:
                        col = Color.Green.ToVector4();
                        break;
                    case 3:
                        col = Color.Yellow.ToVector4();
                        break;
                    case 4:
                        col = Color.Orange.ToVector4();
                        break;
                    case 5:
                        col = Color.Purple.ToVector4();
                        break;
                    case 6:
                        col = Color.Pink.ToVector4();
                        break;
                    case 7:
                        col = Color.White.ToVector4();
                        break;
                    case 8:
                        col = Color.Cyan.ToVector4();
                        break;
                    case 9:
                        col = Color.Magenta.ToVector4();
                        break;
                    default:
                        col = Color.Black.ToVector4();
                        break;
                }

                modelMaterial.SpotPos[i] = pos;
                modelMaterial.SpotDir[i] = dir;
                modelMaterial.SpotCol[i] = col;
            }

        }

        //----------------------------------------------------------------------------
        // Name: HandleInput()
        // Desc: Handles the Input
        //----------------------------------------------------------------------------
        private void HandleInput(float timeStep)
        {
            inputHandler.UpdateStates();
            CameraControls(timeStep);
            TechniqueCycle();
            if (inputHandler.CheckKey(Keys.Enter, false))
            {
                CreateMultipleSpots();
                modelMaterial.SetEffectParameters(effect);
            }
            //grayscale toggle
            if(inputHandler.CheckKey(Keys.G,false))
            {
                if (grayScale)
                    grayScale = false;
                else
                    grayScale = true;
            }
        }
        private void CameraControls(float timeStep)
        {
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
        }
        private void TechniqueCycle()
        {
            if (inputHandler.CheckKey(Keys.Space, false))
                currentTechniqueNumber++;
            if (currentTechniqueNumber >= numberOfTechniques)
                currentTechniqueNumber = 0;

            switch (currentTechniqueNumber)
            {
                case 0:
                    effect.CurrentTechnique = effect.Techniques["Simple"];
                    currentTechnique = "Cook-Torrance";
                    break;
                case 1:
                    effect.CurrentTechnique = effect.Techniques["Spotlight"];
                    currentTechnique = "Spotlight";
                    break;
                case 2:
                    effect.CurrentTechnique = effect.Techniques["MultipleLightsSources"];
                    currentTechnique = "Multiple Spotlights";
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
            spriteBatch.DrawString(spriteFont, "Drawn:  " + cullCount[0] + "\r\n" +
                                               "Culled: " + cullCount[1], 
                                               new Vector2(20.0f, 40.0f), Color.Red);

            spriteBatch.DrawString(spriteFont, "Current technique: " + currentTechnique, new Vector2(20.0f, 20.0f), Color.Red);
            spriteBatch.DrawString(spriteFont, "CONTROLS" + "\r\n" + "\r\n" +
                                               "Enter:               New Random Spotlights"  + "\r\n" +
                                               "Spacebar:            Next Technique"         + "\r\n" +
                                               "Up and Down:         Zoom"                   + "\r\n" +
                                               "Left and Right:      Rotate"                 + "\r\n" +
                                               "G:                   apply grayscale effect",
                                               new Vector2(485, 20), Color.Red);
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

        private void DrawScene()
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

        }

        protected override void Draw(GameTime gameTime)
        {
            DrawSceneToTexture(target);

            if (grayScale)
            {
                postProcess.CurrentTechnique = postProcess.Techniques["Grayscale"];
            }
            else
            {
                postProcess.CurrentTechnique = postProcess.Techniques["normal"];
            }


            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.Default,
                RasterizerState.CullNone, postProcess);

            spriteBatch.Draw(target, new Rectangle(0, 0, 800, 480), Color.White);

            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
