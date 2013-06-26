using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GraphicsPractical3
{
    public struct Material
    {
        // Color of the ambient light
        public Color AmbientColor;
        // Intensity of the ambient light
        public float AmbientIntensity;
        // The color of the surface (can be ignored if texture is used, or not if you want to blend)
        public Color DiffuseColor;
        // The texture of the surface
        public Texture DiffuseTexture;
        // Color of the specular highlight (mostly equal to the color of the light source)
        public Color SpecularColor;
        // The intensity factor of the specular highlight, controls it's size
        public float SpecularIntensity;
        // Position of the light
        public Vector3 Light;
        //Position of the Spotlight
        public Vector3[] SpotPos;
        public Vector3[] SpotDir;
        public Vector4[] SpotCol;

        // Roughness of the object
        public float Roughness;
        // Reflection Coefficient for Cook-Torrance
        public float ReflectionCoefficient;

        // Using this function requires all these elements to be present as top-level variables in the shader code. Comment out the ones that you don't use
        public void SetEffectParameters(Effect effect)
        {
            effect.Parameters["AmbientColor"].SetValue(this.AmbientColor.ToVector4());
            effect.Parameters["AmbientIntensity"].SetValue(this.AmbientIntensity);
            effect.Parameters["DiffuseColor"].SetValue(this.DiffuseColor.ToVector4());
            effect.Parameters["DiffuseTexture"].SetValue(this.DiffuseTexture);
            effect.Parameters["SpecularColor"].SetValue(this.SpecularColor.ToVector4());
            effect.Parameters["SpecularIntensity"].SetValue(this.SpecularIntensity);
            effect.Parameters["HasTexture"].SetValue(this.DiffuseTexture != null);
            effect.Parameters["Light"].SetValue(this.Light);
            effect.Parameters["SpotPos"].SetValue(this.SpotPos);
            effect.Parameters["SpotDir"].SetValue(this.SpotDir);
            effect.Parameters["SpotCol"].SetValue(this.SpotCol);
            effect.Parameters["Roughness"].SetValue(this.Roughness);
            effect.Parameters["ReflectionCoefficient"].SetValue(this.ReflectionCoefficient);
        }
    }
}