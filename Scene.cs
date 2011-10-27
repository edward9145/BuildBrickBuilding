//-----------------------------------------------------------------------------
// The scene manager.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace BuildBrickBuilding
{
    class Scene
    {
        Dictionary<Vector3,Block> sceneBlocks;
        Vector3 orginPos;
        Device device;
        Mesh box;

        public Scene(Device dev)
        {
            sceneBlocks = new Dictionary<Vector3, Block>(1);
            orginPos = new Vector3(0, 0, 0);
            device = dev;
            box = Mesh.Box(dev, 1, 1, 1);
        }

        public void addBrick(Brick brick)
        {
            foreach (Block block in brick.getBlocks())
            {
                addBlock(block, brick.getPos() + block.getPos());
            }
        }

        public void addBrick(Brick brick, Vector3 pos)
        {
            foreach (Block block in brick.getBlocks())
            {
                addBlock(block, pos + block.getPos());
            }
        }

        public void addBlock(Block block, Vector3 pos)
        {
            if (sceneBlocks.ContainsKey(pos))
            {
                sceneBlocks[pos] = new Block(block, pos);
            }
            else
            {
                sceneBlocks.Add(pos, new Block(block, pos));
            }
        }

        public void addTmpBricks(Brick brick, Vector3 pos)
        {
            Brick tmpBrick = new Brick(device);
            tmpBrick.setPos(pos);
            this.addBrick(tmpBrick);
        }

        public Block getBlock(Vector3 pos)
        {
            if (sceneBlocks.ContainsKey(pos))
            {
                return sceneBlocks[pos];
            }
            return null;
        }

        public int getBlockNum()
        {
            return sceneBlocks.Count;
        }

        public void removeBlock(Vector3 pos)
        {
            if (sceneBlocks.ContainsKey(pos))
            {
                sceneBlocks.Remove(pos);
            }
//             for (int i = 0; i < sceneBlocks.Count; i++)
//             {
//                 if (((Block)sceneBlocks[i]).getPos() == pos)
//                 {
//                     sceneBlocks.RemoveAt(i);
//                     i--;
//                 }
//             }
        }

        public void draw(Device dev)
        {
            Matrix world = dev.Transform.World;
            dev.Transform.World = Matrix.Translation(orginPos);
            foreach (Block block in sceneBlocks.Values)
            {
                block.draw(dev, box, orginPos);
            }
            dev.Transform.World = world;
        }

        public void reset()
        {
            sceneBlocks.Clear();
        }
    }
}
