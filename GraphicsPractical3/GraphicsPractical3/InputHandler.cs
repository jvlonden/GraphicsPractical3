using System;
using Microsoft.Xna.Framework.Input;
namespace GraphicsPractical3
{
    class InputHandler
    {
        private KeyboardState keyboard;
        private KeyboardState keyboardPrev;

        public InputHandler()
        {
            keyboard = new KeyboardState();
            keyboardPrev = new KeyboardState();
        }

        public bool CheckKey(Keys key, bool hold = true)
        {
            keyboardPrev = keyboard;
            keyboard = Keyboard.GetState();

            if (hold == true && keyboard.IsKeyDown(key))
                    return true;

            else if (!keyboardPrev.IsKeyDown(key) && keyboard.IsKeyDown(key))
                return true;

            return false;
        }        
    }
}
