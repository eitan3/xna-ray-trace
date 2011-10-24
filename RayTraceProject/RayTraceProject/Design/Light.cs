using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RayTraceProject.Design
{
    class SpotLightObject : DesignObject
    {
        public SpotLightObject(ContentManager content) : base(content.Load<Texture2D>("Designtime\\lightbulb"))
        {

        }
    }
}
