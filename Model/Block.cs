//-----------------------------------------------------------------------------
// block is the basic box;
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;

namespace BuildBrickBuilding
{
    public class Block
    {
        Vector3 position { get; set; }
        Color color;
        Material mtrl;
        bool isAlpha;

        public Block()
        {
            position = new Vector3(0, 0, 0);
            color = Color.Orange;
            mtrl = new Material();
            mtrl.Diffuse = color;
            mtrl.Ambient = Color.White;
            isAlpha = false;
        }

        public Block(Vector3 pos, Color col)
        {
            position = pos;
            color = col;
            mtrl = new Material();
            mtrl.Diffuse = color;
            mtrl.Ambient = Color.White;
            isAlpha = false;
        }

        public Block(Block block)
        {
            this.position = block.position;
            this.color = block.color;
            this.mtrl = new Material(); ;
            this.mtrl.Diffuse = this.color;
            this.mtrl.Ambient = Color.White;
            isAlpha = false;
        }

        public Block(Block block, Vector3 pos)
        {
            this.position = pos;
            this.color = block.color;
            this.mtrl = new Material(); ;
            this.mtrl.Diffuse = this.color;
            this.mtrl.Ambient = Color.White;
            isAlpha = false;
        }

        public void setPos(Vector3 pos)
        {
            position = new Vector3(pos.X, pos.Y, pos.Z);
        }

        public void setColor(Color col)
        {
            color = col;
            mtrl.Diffuse = color;
        }

        public void setAlpha(bool b)
        {
            isAlpha = b;
        }

        public Vector3 getPos()
        {
            return position;
        }

        public Color getColor()
        {
            return color;
        }

        public void draw(Device dev, Mesh box, Vector3 origin)
        {
            dev.Transform.World = Matrix.Translation(
                origin + position + new Vector3(0.5f, 0.5f, 0.5f));
            if (isAlpha)
            {
                //dev.RenderState.DiffuseMaterialSource = ColorSource.Material;
                dev.RenderState.AlphaBlendEnable = true;
                dev.RenderState.SourceBlend = Blend.SourceColor;
                dev.RenderState.DestinationBlend = Blend.InvSourceAlpha;
            }
            else
            {
                dev.RenderState.AlphaBlendEnable = false;
            }
            dev.Material = this.mtrl;
            box.DrawSubset(0);
        }

        public void drawBlock(Device dev, Mesh box, float angle)
        {
            if (isAlpha)
            {
                //dev.RenderState.DiffuseMaterialSource = ColorSource.Material;
                dev.RenderState.AlphaBlendEnable = true;
                dev.RenderState.SourceBlend = Blend.SourceColor;
                dev.RenderState.DestinationBlend = Blend.InvSourceAlpha;
            }
            else
            {
                dev.RenderState.AlphaBlendEnable = false;
            }
            
            dev.Transform.World = 
                Matrix.Translation(position) * Matrix.RotationY(angle);
            
            dev.Material = this.mtrl;
            box.DrawSubset(0);
        }
    }
}
