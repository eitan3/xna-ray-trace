using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace RayTraceProject.Design
{
    class DesignObject
    {
        private Texture2D icon;

        public Texture2D Icon 
        { 
            get { return this.icon; }
            protected set { this.icon = value; }
        }

        protected DesignObject(Texture2D icon)
        {
        }
    }
}
