using System;
using Microsoft.Xna.Framework.Input;
namespace GraphicsPractical3
{
    class InputHandler
    {
        private KeyboardState oldState, newState;

        public InputHandler()
        {
            oldState = Keyboard.GetState();
        }

        public bool CheckKey(Keys key, bool hold = true)
        {
            if (hold)
            { 
                if(newState.IsKeyDown(key))
                    return true;
            }
            else 
            {
                if (newState.IsKeyDown(key))
                    if(oldState.IsKeyUp(key))
                        return true;
            }
            return false;
        }
        public void UpdateStates()
        {
            oldState = newState;
            newState = Keyboard.GetState();
        }
    }
}
