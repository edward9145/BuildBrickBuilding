//-----------------------------------------------------------------------------
// Brick is composed by several blocks. 
// Brick is the basic model.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Drawing;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;

namespace BuildBrickBuilding
{
    public class Brick
    {
        Device device;
        List<Block> brickBlocks;
        Vector3 position;
        Mesh box;
        static int[] COS = { 1, 0, -1, 0 };
        static int[] SIN = { 0, 1, 0, -1 };

        public Brick(Device dev, int testID)
        {
            brickBlocks = new List<Block>(1);
            position = new Vector3(0, 0, 0);
            testBrick(testID);
            box = Mesh.Box(dev, 1, 1, 1);
            device = dev;
        }

        public Brick(Device dev)
        {
            brickBlocks = new List<Block>(1);
            brickBlocks.Add(new Block());
            position = new Vector3(0, 0, 0);
            box = Mesh.Box(dev, 1, 1, 1);
            device = dev;
        }

        public Brick(Brick brick)
        {
            brickBlocks = new List<Block>(brick.brickBlocks.Count);
            foreach (Block block in brick.brickBlocks)
            {
                brickBlocks.Add(new Block(block));
            }
            this.position = new Vector3(brick.position.X, brick.position.Y, position.Z);
            box = Mesh.Box(brick.device, 1, 1, 1);
        }

        public Brick(Device dev, List<Block> list)
        {
            brickBlocks = new List<Block>(list.Count);
            foreach (Block block in list)
            {
                brickBlocks.Add(new Block(block));
            }
            position = new Vector3(0, 0, 0);
            box = Mesh.Box(dev, 1, 1, 1);
        }

        public void setAphla(bool b)
        {
            foreach (Block block in brickBlocks)
            {
                Color color = block.getColor();
                Color alpha = Color.FromArgb(127, 
                    color.R > 20 ? (color.R - 20) : color.R,
                    color.G > 20 ? (color.G - 20) : color.G,
                    color.B > 20 ? (color.B - 20) : color.B);
                block.setColor(alpha);
                block.setAlpha(b);
            }
        }

        public void testBrick(int id)
        {
            Vector3 blockPos;
            switch(id){
                case 1:
                    blockPos = new Vector3(0, 0, 1);
                    brickBlocks.Add(new Block(blockPos, Color.Blue));
                    blockPos = new Vector3(1, 0, 0);
                    brickBlocks.Add(new Block(blockPos, Color.Red));
                    blockPos = new Vector3(0, 1, 0);
                    brickBlocks.Add(new Block(blockPos, Color.Lime));
                    break;
                case 2:
                    for (int i = 0; i < 3; i++)
                    {
                        blockPos = new Vector3(i, 0, 0);
                        brickBlocks.Add(new Block(blockPos,Color.PeachPuff));
                    }
                    for (int i = 1; i < 3; i++)
                    {
                        blockPos = new Vector3(0, i, 0);
                        brickBlocks.Add(new Block(blockPos, Color.PowderBlue));
                    }
                    break;
                default:
                    brickBlocks.Add(new Block());
                break;
            }
        }

        public void setColor(Color color)
        {
            foreach (Block block in brickBlocks)
            {
                block.setColor(color);
            }
        }

        public Color getColor(int n)
        {
            if (n >= brickBlocks.Count || n < 0) return Color.White;
            return ((Block)brickBlocks[n]).getColor();
        }

        public void setPos(Vector3 pos)
        {
            position = new Vector3(pos.X, pos.Y, pos.Z);
        }

        public void setBlock(List<Block> list)
        {
            brickBlocks = list;
        }

        public void setBrick(Block block, int n)
        {
            if (n <= 0 || n >= brickBlocks.Count) return;
            brickBlocks[n] = block;
        }

        public Vector3 getPos()
        {
            return position;
        }

        public List<Block> getBlocks()
        {
            return brickBlocks;
        }

        public Block getBlock(int n)
        {
            if (n <= 0 || n >= brickBlocks.Count) return null;
            return (Block)brickBlocks[n];
        }

        public Vector3 getMaxPos()
        {
            Vector3 max = new Vector3();
            foreach (Block block in brickBlocks)
            {
                if (block.getPos().LengthSq() > max.LengthSq())
                {
                    max = block.getPos();
                }
            }
            return max;
        }

        public void rotateX(int pitch)
        {
            pitch = pitch / 90;
            pitch = pitch % 4;
            Vector3 offset = new Vector3(0, 0.5f, 0.5f);
            Vector3 rotate = new Vector3();
            float z, y;
            int cos = COS[pitch];
            int sin = SIN[pitch];
            foreach (Block block in brickBlocks)
            {
                rotate = block.getPos() + offset;
                z = rotate.Z * cos - rotate.Y * sin;
                y = rotate.Z * sin + rotate.Y * cos;
                rotate.Z = (int)(z - offset.Z);
                rotate.Y = (int)(y - offset.Y);
                block.setPos(rotate);
            }
        }
        public void rotateY(int yaw)
        {
            yaw = yaw / 90;
            yaw = yaw % 4;
            Vector3 offset = new Vector3(0.5f, 0, 0.5f);
            Vector3 rotate = new Vector3();
            float x, z;
            int cos = COS[yaw];
            int sin = SIN[yaw];
            foreach (Block block in brickBlocks)
            {
                rotate = block.getPos() + offset;
                x = rotate.X * cos - rotate.Z * sin;
                z = rotate.X * sin + rotate.Z * cos;
                rotate.X = (int)(x - offset.X);
                rotate.Z = (int)(z - offset.Z);
                block.setPos(rotate);
            }
        }
        public void rotateZ(int roll)
        {
            roll = roll / 90;
            roll = roll % 4;
            Vector3 offset = new Vector3(0.5f, 0.5f, 0);
            Vector3 rotate = new Vector3();
            float x, y;
            int cos = COS[roll];
            int sin = SIN[roll];
            foreach (Block block in brickBlocks)
            {
                rotate = block.getPos() + offset;
                x = rotate.X * cos - rotate.Y * sin;
                y = rotate.X * sin + rotate.Y * cos;
                rotate.X = (int)(x - offset.X);
                rotate.Y = (int)(y - offset.Y);
                block.setPos(rotate);
            }
        }

        public void draw(Device dev, Vector3 origin)
        {
            dev.Transform.World = Matrix.Translation(
                origin + position);
            foreach (Block block in brickBlocks)
            {
                block.draw(dev, box, position);
            }
        }

        public void drawBrick(Device dev)
        {
            int iTime = Environment.TickCount % 5000;
            float fAngle = iTime * (2.0f * (float)Math.PI) / 5000.0f;

            foreach (Block block in brickBlocks)
            {
                block.drawBlock(dev, box, fAngle);
            }
        }
    }
}
