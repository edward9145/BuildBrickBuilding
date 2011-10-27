//-----------------------------------------------------------------------------
// File: MainForm.cs
// for Kinect Pioneer.
// Author: Edward.Wu
//-----------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;
using Microsoft.Research.Kinect.Nui;
using Microsoft.Research.Kinect.Audio;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace BuildBrickBuilding
{
	public class MainForm : Form
	{
        //scene
        Scene mainScene;
        Brick holdBrick;
        Brick testBrick;
        Color holdColor;

        //for Kinect
        //Nui nui;
        Runtime kinectRuntime;
        KinectAudioSource kinectSource;
        SpeechRecognitionEngine sre;
        VoiceCmd voiceCmd = new VoiceCmd();

		//for DirectX
        Direct3D.Device device = null; // Our rendering device
        Direct3D.Device brickDevice = null; // Our brick device;
        PresentParameters presentParams = new PresentParameters();
        PresentParameters ppBrickDevice = new PresentParameters();
        VertexBuffer vertexBuffer = null;
        Mesh showPos;
        Mesh showDelPos;
        Material showPosMtrl = new Material();
        Material showDelPosMtrl = new Material();
        Vector3 brickPos = new Vector3(0f, 0f, 0f);
        Vector3 delPos = new Vector3(0f, 0f, 0f);
        Vector3 brickCamPos = new Vector3(0, 3, -5);
        Vector3 camPos = new Vector3(3.0f, 10.0f, -25.0f);
        Vector3 seePos = new Vector3(0, 2, 0);
        Vector3 upPos = new Vector3(0, 1, 0);

        bool pause = false;
        bool isDown = false;
        static int lineBoxNum = 16;
        static int gridRowNum = 20;
        int bufferLen = gridRowNum * 4 + 4 + lineBoxNum * 3 + 6 + 1;
        int mx, my;
        int testID = 0;
        int invalidateControl = 0;
        private const string RecognizerId = "SR_MS_en-US_Kinect_10.0";

        private Panel BuildPanel;
        private Panel ColorPanel;
        private Label BrickLabel;
        private Panel BrickPanel;
        private Label PositionLabel;
        private Label PosXLabel;
        private Label PosYLabel;
        private Label PosZLabel;
        private Label label1;
        private Label label2;
        private Label ColorLabel;

		public MainForm()
		{
			// Set the initial size of our form
            InitializeComponent();
			// And its caption
			this.Text = "Build Brick Building - Kinect";

        #region initial Kinect runtime
            kinectRuntime = new Runtime();
            try
            {
                kinectRuntime.Initialize(RuntimeOptions.UseSkeletalTracking);
            }
            catch (InvalidOperationException)
            {
                //MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                Console.WriteLine("Runtime initialization failed.");
                kinectRuntime = null;
                return;
            }
            kinectRuntime.SkeletonFrameReady += kinectRuntime_SkeletonFrameReady;
            kinectRuntime.SkeletonEngine.TransformSmooth = false;
            //kinectRuntime.SkeletonEngine.TransformSmooth = true;
            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.75f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };
            kinectRuntime.SkeletonEngine.SmoothParameters = parameters;
        #endregion

        #region initial kinect audio
            RecognizerInfo ri = SpeechRecognitionEngine.InstalledRecognizers()
                .Where(r => r.Id == RecognizerId)
                .FirstOrDefault();
            if (ri == null)
                return;
            sre = new SpeechRecognitionEngine(ri.Id);
            // Build a simple grammar of shapes, colors, and some simple program control
            var command = new Choices();
            foreach (string s in voiceCmd.colors.Keys)
            {
                command.Add(s);
            }
            foreach (string s in voiceCmd.addCmd)
            {
                command.Add(s);
            }
            foreach (string s in voiceCmd.removeCmd)
            {
                command.Add(s);
            }
            foreach (string s in voiceCmd.clearCmd)
            {
                command.Add(s);
            }
            foreach (string s in voiceCmd.exitCmd)
            {
                command.Add(s);
            }

            var gb = new GrammarBuilder();
            gb.Append(command);
            // Create the actual Grammar instance, and then load it into the speech recognizer.
            var g = new Grammar(gb);
            sre.LoadGrammar(g);

            sre.SpeechRecognized += sre_SpeechRecognized;
            //sre.SpeechHypothesized += sre_SpeechHypothesized;
            sre.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(sre_SpeechRecognitionRejected);

            var t = new Thread(StartDMO);
            t.Start();

        #endregion

        }

        private void StartDMO()
        {
            kinectSource = new KinectAudioSource();
            kinectSource.SystemMode = SystemMode.OptibeamArrayOnly;
            kinectSource.FeatureMode = true;
            kinectSource.AutomaticGainControl = false;
            kinectSource.MicArrayMode = MicArrayMode.MicArrayAdaptiveBeam;
            var kinectStream = kinectSource.Start();
            sre.SetInputToAudioStream(kinectStream, new SpeechAudioFormatInfo(
                                                  EncodingFormat.Pcm, 16000, 16, 1,
                                                  32000, 2, null));
            sre.RecognizeAsync(RecognizeMode.Multiple);
        }

        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string result = e.Result.Text;
            Console.WriteLine("Speech Recognized: {0}", result);
            if (voiceCmd.exitCmd.Contains(result))
            {
                this.Close();
            }
            else if (voiceCmd.colors.ContainsKey(result))
            {
                holdColor = voiceCmd.colors[result];
                ColorPanel.BackColor = holdColor;
                holdBrick.setColor(holdColor);
                testBrick.setColor(holdColor);
            }
            else if (voiceCmd.addCmd.Contains(result))
            {
                mainScene.addBrick(holdBrick);
                Console.WriteLine((int)(brickPos.X) + "," + (int)(brickPos.Y) + "," + (int)(brickPos.Z));
            }
            else if(voiceCmd.removeCmd.Contains(result))
            {
                mainScene.removeBlock(delPos);
            }
            else if(voiceCmd.clearCmd.Contains(result))
            {
                mainScene.reset();
                brickPos = new Vector3(0, 0, 0);
                delPos = new Vector3(0, 0, 0);
            }
        }
        void sre_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Console.WriteLine("Speech Hypothesized: {0}", e.Result.Text);
        }
        void sre_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Console.WriteLine("Speech Rejected");
        }

        public void kinectRuntime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame skeletonFrame = e.SkeletonFrame;
            foreach (SkeletonData data in skeletonFrame.Skeletons)
            {
                if (data.TrackingState == SkeletonTrackingState.Tracked)
                {
                    Joint wristLeft = data.Joints[JointID.WristLeft];
                    Joint wristRight = data.Joints[JointID.WristRight];
                    Joint elbowLeft = data.Joints[JointID.ElbowLeft];
                    Joint elbowRight = data.Joints[JointID.ElbowRight];
                    
                    Vector3 leftWrist = Vector3.Empty;
                    Vector3 rightWrist = Vector3.Empty;
                    Vector3 leftElbow = Vector3.Empty;
                    Vector3 rightElbow = Vector3.Empty;

                    leftWrist = new Vector3(wristLeft.Position.X, wristLeft.Position.Y, wristLeft.Position.Z);
                    rightWrist = new Vector3(wristRight.Position.X, wristRight.Position.Y, wristRight.Position.Z);
                    leftElbow = Nui.toVector3(elbowLeft);
                    rightElbow = Nui.toVector3(elbowRight);

                    brickPos = new Vector3((float)(int)(leftWrist.X * 20.0f),
                        (float)(int)((leftWrist.Y + 0.6f) * 20.0f),
                        (float)(int)((leftWrist.Z - 1.0f) * -20.0f));

                    delPos = new Vector3((float)(int)(rightWrist.X * 20.0f),
                        (float)(int)((rightWrist.Y + 0.6f) * 20.0f),
                        (float)(int)((rightWrist.Z - 1.0f) * -20.0f));

                    if (Nui.isNear(rightWrist, leftElbow))
                    {
                        if (invalidateControl % 8 == 0)
                        {
                            mainScene.addBrick(holdBrick);
                            Log.show(testBrick.getPos(), "add");
                            //Log.show(Nui.dist2(rightWrist, leftElbow));
                        }
                    }
                    if (Nui.isNear(leftWrist, rightElbow))
                    {
                        if (invalidateControl % 8 == 0)
                        {
                            mainScene.removeBlock(delPos);
                            Log.show(delPos, "del");
                            //Log.show(Nui.dist2(leftWrist, rightElbow));
                        }
                    }

                    if (invalidateControl % 5 == 0)
                    {
                        //update info
                        setPosLabel();
                    }

                    invalidateControl = (invalidateControl > 100) ? 0 : (invalidateControl + 1);

                    break;
                }
            }
        }
        
        public bool InitBrickGraphics()
        {
            try
            {
                ppBrickDevice.Windowed = true; // We don't want to run fullscreen
                ppBrickDevice.SwapEffect = SwapEffect.Discard; // Discard the frames 
                ppBrickDevice.EnableAutoDepthStencil = true; // Turn on a Depth stencil
                ppBrickDevice.AutoDepthStencilFormat = DepthFormat.D16; // And the stencil format
                brickDevice = new Direct3D.Device(0, DeviceType.Hardware,
                    this.BrickPanel, CreateFlags.SoftwareVertexProcessing, ppBrickDevice); //Create a brickDevice
                brickDevice.DeviceReset += new System.EventHandler(this.OnResetDevice);
                this.OnCreateBrickDevice(brickDevice, null);
                this.OnResetBrickDevice(brickDevice, null);
                pause = false;
                return true;
            }
            catch (DirectXException)
            {
                // Catch any errors and return a failure
                return false;
            }
        }
        public void OnCreateBrickDevice(object sender, EventArgs e)
        {
            Direct3D.Device dev = (Direct3D.Device)sender;
            testBrick = new Brick(dev, testID);
            brickCamPos = testBrick.getMaxPos() * 2;
            brickCamPos.X += (brickCamPos.X == 0) ? 4 : 0;
            brickCamPos.Y += (brickCamPos.Y == 0) ? 4 : 0;
            brickCamPos.Z += (brickCamPos.Z == 0) ? 4 : 0;
        }
        public void OnResetBrickDevice(object sender, EventArgs e)
        {
            Direct3D.Device dev = (Direct3D.Device)sender;
            // Turn off culling
            dev.RenderState.CullMode = Cull.None;
            // Turn on the ZBuffer
            dev.RenderState.ZBufferEnable = true;
            dev.RenderState.Lighting = true;    //make sure lighting is enabled
            dev.Lights[0].Type = LightType.Directional;
            dev.Lights[0].Diffuse = System.Drawing.Color.Ivory;
            dev.Lights[0].Direction = new Vector3(9, 4, -3);
            dev.Lights[0].Enabled = true;//turn it on
            dev.Lights[1].Type = LightType.Directional;
            dev.Lights[1].Diffuse = System.Drawing.Color.Ivory;
            dev.Lights[1].Direction = new Vector3(-2, -5, 7);
            dev.Lights[1].Enabled = true;//turn it on
            dev.RenderState.Ambient = System.Drawing.Color.FromArgb(0x505050);
        }
        private void RenderBrick()
        {
            if (pause)
                return;

            brickDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer,
                System.Drawing.Color.SlateGray, 1.0f, 0);
            brickDevice.BeginScene();
            
            brickDevice.Transform.View = Matrix.LookAtLH(
                brickCamPos, new Vector3(0, 0, 0), 
                new Vector3(0, 1, 0));
            brickDevice.Transform.Projection = Matrix.PerspectiveFovLH(
                (float)Math.PI / 4.0f, 1.0f, 1.0f, 100.0f);
            testBrick.drawBrick(brickDevice);
            brickDevice.EndScene();
            brickDevice.Present();
        }


		public bool InitializeGraphics()
		{
			try
			{
				presentParams.Windowed=true; // We don't want to run fullscreen
				presentParams.SwapEffect = SwapEffect.Discard; // Discard the frames 
				presentParams.EnableAutoDepthStencil = true; // Turn on a Depth stencil
				presentParams.AutoDepthStencilFormat = DepthFormat.D16; // And the stencil format
				device = new Direct3D.Device(0, DeviceType.Hardware, 
                    this.BuildPanel, CreateFlags.SoftwareVertexProcessing, presentParams); //Create a device
				device.DeviceReset += new System.EventHandler(this.OnResetDevice);
				this.OnCreateDevice(device, null);
				this.OnResetDevice(device, null);
				pause = false;
				return true;
			}
			catch (DirectXException)
			{
				// Catch any errors and return a failure
				return false;
			}
		}
		public void OnCreateDevice(object sender, EventArgs e)
		{
			Direct3D.Device dev = (Direct3D.Device)sender;
			// Now Create the VB
			vertexBuffer = new VertexBuffer(typeof(CustomVertex.PositionNormalColored), 
                 bufferLen, dev, Usage.WriteOnly, CustomVertex.PositionNormalColored.Format, Pool.Default);
			vertexBuffer.Created += new System.EventHandler(this.OnCreateVertexBuffer);
			this.OnCreateVertexBuffer(vertexBuffer, null);

            showPos = Mesh.Sphere(dev, 0.15f, 10, 10);
            showPosMtrl.Diffuse = Color.YellowGreen;
            showPosMtrl.Ambient = Color.Wheat;
            showDelPos = Mesh.Sphere(dev, 0.15f, 10, 10);
            showDelPosMtrl.Diffuse = Color.Red;
            showDelPosMtrl.Ambient = Color.White;

            mainScene = new Scene(dev);
            holdBrick = new Brick(dev, testID);
            holdColor = holdBrick.getColor(0);
            ColorPanel.BackColor = holdColor;

            //holdBrick.setAphla(true);
		}
		public void OnResetDevice(object sender, EventArgs e)
		{
			Direct3D.Device dev = (Direct3D.Device)sender;
			// Turn off culling
			dev.RenderState.CullMode = Cull.None;
			// Turn on the ZBuffer
			dev.RenderState.ZBufferEnable = true;
			dev.RenderState.Lighting = true;    //make sure lighting is enabled
		}
		public void OnCreateVertexBuffer(object sender, EventArgs e)
		{
			VertexBuffer vb = (VertexBuffer)sender;
			// Create a vertex buffer
			CustomVertex.PositionNormalColored[] verts = 
                (CustomVertex.PositionNormalColored[])vb.Lock(0,0); // Lock the buffer (which will return our structs)
            //6 verts draw 3 coordinates
            verts[bufferLen - 1].Position = new Vector3(0, 0, 0);
            verts[bufferLen - 1].Color = Color.Red.ToArgb();
            verts[bufferLen - 2].Position = new Vector3(10, 0, 0);
            verts[bufferLen - 2].Color = Color.Red.ToArgb();
            verts[bufferLen - 3].Position = new Vector3(0, 0, 0);
            verts[bufferLen - 3].Color = Color.Lime.ToArgb();
            verts[bufferLen - 4].Position = new Vector3(0, 10, 0);
            verts[bufferLen - 4].Color = Color.Lime.ToArgb();
            verts[bufferLen - 5].Position = new Vector3(0, 0, 0);
            verts[bufferLen - 5].Color = Color.Blue.ToArgb();
            verts[bufferLen - 6].Position = new Vector3(0, 0, 10);
            verts[bufferLen - 6].Color = Color.Blue.ToArgb();
            //(RowNum+1)*2 verts draw grid RowNum x RowNum
            int i;
            for (i = 0; i <= gridRowNum; i++)
            {
                bool mid = (i == gridRowNum / 2 || i + 1 == gridRowNum / 2 || i - 1 == gridRowNum / 2);
                verts[i * 2].Position = new Vector3(-i + gridRowNum / 2, 0, -10);
                verts[i * 2].Color = mid ?
                    Color.Black.ToArgb() : Color.Ivory.ToArgb();
                verts[i * 2 + 1].Position = new Vector3(-i + gridRowNum / 2, 0, 10);
                verts[i * 2 + 1].Color = mid ?
                    Color.Black.ToArgb() : Color.Ivory.ToArgb();
                verts[gridRowNum * 2 + 2 + i * 2].Position = new Vector3(-10, 0, -i + gridRowNum / 2);
                verts[gridRowNum * 2 + 2 + i * 2].Color = mid ?
                    Color.Black.ToArgb() : Color.Ivory.ToArgb();
                verts[gridRowNum * 2 + 2 + i * 2 + 1].Position = new Vector3(10, 0, -i + gridRowNum / 2);
                verts[gridRowNum * 2 + 2 + i * 2 + 1].Color = mid ?
                    Color.Black.ToArgb() : Color.Ivory.ToArgb();
            }

            int j = gridRowNum * 4 + 4;
            verts[j + 0].Position = new Vector3(0, 0, 0);
            verts[j + 0].Color = Color.Red.ToArgb();
            verts[j + 1].Position = new Vector3(1, 0, 0);
            verts[j + 1].Color = Color.Red.ToArgb();
            verts[j + 2].Position = new Vector3(1, 1, 0);
            verts[j + 2].Color = Color.Red.ToArgb();
            verts[j + 3].Position = new Vector3(0, 1, 0);
            verts[j + 3].Color = Color.Red.ToArgb();
            verts[j + 4].Position = new Vector3(0, 0, 0);
            verts[j + 4].Color = Color.Red.ToArgb();
            verts[j + 5].Position = new Vector3(0, 0, 1);
            verts[j + 5].Color = Color.Red.ToArgb();
            verts[j + 6].Position = new Vector3(1, 0, 1);
            verts[j + 6].Color = Color.Red.ToArgb();
            verts[j + 7].Position = new Vector3(1, 1, 1);
            verts[j + 7].Color = Color.Red.ToArgb();
            verts[j + 8].Position = new Vector3(0, 1, 1);
            verts[j + 8].Color = Color.Red.ToArgb();
            verts[j + 9].Position = new Vector3(0, 0, 1);
            verts[j + 9].Color = Color.Red.ToArgb();
            verts[j + 10].Position = new Vector3(1, 1, 0);
            verts[j +10].Color = Color.Red.ToArgb();
            verts[j + 11].Position = new Vector3(1, 1, 1);
            verts[j +11].Color = Color.Red.ToArgb();
            verts[j + 12].Position = new Vector3(0, 1, 1);
            verts[j +12].Color = Color.Red.ToArgb();
            verts[j + 13].Position = new Vector3(0, 1, 0);
            verts[j +13].Color = Color.Red.ToArgb();
            verts[j + 14].Position = new Vector3(1, 0, 1);
            verts[j +14].Color = Color.Red.ToArgb();
            verts[j + 15].Position = new Vector3(1, 0, 0);
            verts[j +15].Color = Color.Red.ToArgb();
            j += 16;
            verts[j + 0].Position = new Vector3(0, 0, 0);
            verts[j + 0].Color = Color.OrangeRed.ToArgb();
            verts[j + 1].Position = new Vector3(1, 0, 0);
            verts[j + 1].Color = Color.OrangeRed.ToArgb();
            verts[j + 2].Position = new Vector3(1, 1, 0);
            verts[j + 2].Color = Color.OrangeRed.ToArgb();
            verts[j + 3].Position = new Vector3(0, 1, 0);
            verts[j + 3].Color = Color.OrangeRed.ToArgb();
            verts[j + 4].Position = new Vector3(0, 0, 0);
            verts[j + 4].Color = Color.OrangeRed.ToArgb();
            verts[j + 5].Position = new Vector3(0, 0, 1);
            verts[j + 5].Color = Color.OrangeRed.ToArgb();
            verts[j + 6].Position = new Vector3(1, 0, 1);
            verts[j + 6].Color = Color.OrangeRed.ToArgb();
            verts[j + 7].Position = new Vector3(1, 1, 1);
            verts[j + 7].Color = Color.OrangeRed.ToArgb();
            verts[j + 8].Position = new Vector3(0, 1, 1);
            verts[j + 8].Color = Color.OrangeRed.ToArgb();
            verts[j + 9].Position = new Vector3(0, 0, 1);
            verts[j + 9].Color = Color.OrangeRed.ToArgb();
            verts[j + 10].Position = new Vector3(0, 0, 0);
            verts[j + 10].Color = Color.OrangeRed.ToArgb();
            verts[j + 11].Position = new Vector3(0, 1, 0);
            verts[j + 11].Color = Color.OrangeRed.ToArgb();
            verts[j + 12].Position = new Vector3(0, 1, 1);
            verts[j + 12].Color = Color.OrangeRed.ToArgb();
            verts[j + 13].Position = new Vector3(1, 1, 1);
            verts[j + 13].Color = Color.OrangeRed.ToArgb();
            verts[j + 14].Position = new Vector3(1, 1, 0);
            verts[j + 14].Color = Color.OrangeRed.ToArgb();
            verts[j + 15].Position = new Vector3(1, 0, 0);
            verts[j + 15].Color = Color.OrangeRed.ToArgb();
            verts[j + 16].Position = new Vector3(1, 0, 1);
            verts[j + 16].Color = Color.OrangeRed.ToArgb();
            // Unlock (and copy) the data
            vb.Unlock();
		}
		private void SetupMatrices()
		{
            //world = device.Transform.World;
			// Set up our view matrix.
			device.Transform.View = Matrix.LookAtLH( 
                camPos, seePos, upPos);
            //view = device.Transform.View;
			// Set up a perspective transform.
			device.Transform.Projection = Matrix.PerspectiveFovLH( 
                (float)Math.PI / 4.0f, 1.25f, 1.0f, 100.0f );
            //projection = device.Transform.Projection;
		}
		private void SetupLights()
		{	
			//Set up a directional light
			device.Lights[0].Type = LightType.Directional;
            device.Lights[0].Diffuse = System.Drawing.Color.Ivory;
            device.Lights[0].Direction = new Vector3(9,4,-3);
			device.Lights[0].Enabled = true;//turn it on
            device.Lights[1].Type = LightType.Directional;
            device.Lights[1].Diffuse = System.Drawing.Color.Ivory;
            device.Lights[1].Direction = new Vector3(-2, -5, 7);
            device.Lights[1].Enabled = true;//turn it on
			//Finally, turn on some ambient light.
			//Ambient light is light that scatters and lights all objects evenly
			device.RenderState.Ambient = System.Drawing.Color.FromArgb(0x505050);
		}

		private void Render()
		{
			if (pause)
				return;

			//Clear the buffer
			device.Clear(ClearFlags.Target | ClearFlags.ZBuffer,
                System.Drawing.Color.LightSteelBlue, 1.0f, 0);
			//Begin the scene
			device.BeginScene();
			// Setup the lights and materials
			SetupLights();
			// Setup the world, view, and projection matrices
			SetupMatrices();

            //draw things
            device.RenderState.Lighting = true;

            device.Transform.World = Matrix.Identity;
            mainScene.draw(device);

            holdBrick.setPos(brickPos);
            holdBrick.draw(device, new Vector3(0,0,0));

            device.Material = showPosMtrl;
            device.Transform.World = Matrix.Translation(brickPos);
            showPos.DrawSubset(0);

            device.Material = showDelPosMtrl;
            device.Transform.World = Matrix.Translation(delPos + new Vector3(1, 1, 1));
            showDelPos.DrawSubset(0);

            //draw lines
            device.RenderState.Lighting = false;

            device.Transform.World = Matrix.Identity;
            device.SetStreamSource(0, vertexBuffer, 0);
            device.VertexFormat = CustomVertex.PositionNormalColored.Format;
            device.DrawPrimitives(PrimitiveType.LineList, 0, gridRowNum*2 + 2);
            device.DrawPrimitives(PrimitiveType.LineList, bufferLen - 6, 3);
            device.Transform.World = Matrix.Translation(delPos);
            device.DrawPrimitives(PrimitiveType.LineStrip, gridRowNum * 4 + 4, 15);
            foreach (Block block in holdBrick.getBlocks())
            {
                device.Transform.World = Matrix.Translation(brickPos+block.getPos());
                device.DrawPrimitives(PrimitiveType.LineStrip, gridRowNum * 4 + 4 + 16, 16);
            }
            
			//End the scene
			device.EndScene();
			// Update the screen
			device.Present();
		}

		protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
		{
            if (pause) return;
			this.Render(); // Render on painting
            this.RenderBrick();
		}
		protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
		{
			if ((int)(byte)e.KeyChar == (int)System.Windows.Forms.Keys.Escape)
				this.Close(); // Esc was pressed
		}
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (kinectRuntime != null)
            {
                kinectRuntime.Uninitialize();
                Console.WriteLine("Kinect runtime has uninitialized.");
            }
            if (sre != null)
            {
                sre.RecognizeAsyncCancel();
                sre.RecognizeAsyncStop();
                kinectSource.Dispose();
                Console.WriteLine("Kinect Source has Disposed.");
            }
            base.OnFormClosed(e);
        }
		protected override void OnResize(System.EventArgs e)
		{
            pause = ((this.WindowState == FormWindowState.Minimized) || !this.Visible);
            Console.WriteLine("pause " + pause);
		}
        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool hasSetColor = false;
            bool hasChangedBrick = false;
            switch (e.KeyCode)
            {
                case Keys.D1:
                    hasChangedBrick = true;
                    testID = 1;
                    break;
                case Keys.D2:
                    hasChangedBrick = true;
                    testID = 2;
                    break;
                case Keys.D0:
                    hasChangedBrick = true;
                    testID = 0;
                    break;
                case Keys.W:
                    brickPos.Z += 1;
                    break;
                case Keys.A:
                    brickPos.X -= 1;
                    break;
                case Keys.S:
                    brickPos.Z -= 1;
                    break;
                case Keys.D:
                    brickPos.X += 1;
                    break;
                case Keys.Q:
                    brickPos.Y += 1;
                    break;
                case Keys.E:
                    brickPos.Y -= 1;
                    break;
                case Keys.C:
                    holdBrick.rotateY(90);
                    Log.show(holdBrick);
                    break;
                case Keys.Z:
                    holdBrick.rotateZ(90);
                    break;
                case Keys.X:
                    holdBrick.rotateX(90);                    
                    break;
                case Keys.L:
                    Log.show(mainScene.getBlock(delPos));
                    break;
                case Keys.I:
                    Log.show(mainScene.getBlockNum());
                    break;
                case Keys.NumPad8:
                    delPos.Z += 1;
                    break;
                case Keys.NumPad4:
                    delPos.X -= 1;
                    break;
                case Keys.NumPad2:
                    delPos.Z -= 1;
                    break;
                case Keys.NumPad6:
                    delPos.X += 1;
                    break;
                case Keys.NumPad9:
                    delPos.Y += 1;
                    break;
                case Keys.NumPad3:
                    delPos.Y -= 1;
                    break;
                case Keys.NumPad5:
                    mainScene.removeBlock(delPos);
                    break;
                case Keys.NumPad0:
                    mainScene.removeBlock(delPos);
                    break;
                case Keys.Space:
                    mainScene.addBrick(holdBrick);
                    Console.WriteLine((int)(brickPos.X) + "," + (int)(brickPos.Y) + "," + (int)(brickPos.Z));
                    break;
                case Keys.R:
                    mainScene.reset();
                    break;
                case Keys.Insert:
                    holdColor = Color.FromArgb(
                        holdColor.R < 255 - 5 ? (holdColor.R + 5) : 255, 
                        holdColor.G, 
                        holdColor.B);
                    hasSetColor = true;
                    break;
                case Keys.Delete:
                    holdColor = Color.FromArgb(
                        holdColor.R > 5 ? (holdColor.R - 5) : 0,
                        holdColor.G,
                        holdColor.B);
                    hasSetColor = true;
                    break;
                case Keys.Home:
                    holdColor = Color.FromArgb(
                        holdColor.R,
                        holdColor.G < 255 - 5 ? (holdColor.G + 5) : 255,
                        holdColor.B);
                    hasSetColor = true;
                    break;
                case Keys.End:
                    holdColor = Color.FromArgb(
                        holdColor.R,
                        holdColor.G > 5 ? (holdColor.G - 5) : 0,
                        holdColor.B);
                    hasSetColor = true;
                    break;
                case Keys.PageUp:
                    holdColor = Color.FromArgb(
                        holdColor.R,
                        holdColor.G,
                        holdColor.B < 255 - 5 ? (holdColor.B + 5) : 255);
                    hasSetColor = true;
                    break;
                case Keys.PageDown:
                    holdColor = Color.FromArgb(
                         holdColor.R,
                         holdColor.G,
                         holdColor.B > 5 ? (holdColor.B - 5) : 0);
                    hasSetColor = true;
                    break;
                default:
                    Log.show(brickPos);
                    break;
            }
            if (hasSetColor){
                ColorPanel.BackColor = holdColor;
                holdBrick.setColor(holdColor);
                testBrick.setColor(holdColor);
            }
            if (hasChangedBrick)
            {
                holdBrick = new Brick(device, testID);
                testBrick = new Brick(brickDevice, testID);
            }
            setPosLabel();
            this.Render();
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            camPos.Z = camPos.Z + e.Delta / 80;
            seePos.Z = seePos.Z + e.Delta / 150;                
            this.Render();
        }

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main() 
		{

            using (MainForm frm = new MainForm())
            {
                if (!frm.InitBrickGraphics())
                {
                    MessageBox.Show("Could not initialize Direct3D.  This tutorial will exit.");
                    return;
                }
                if (!frm.InitializeGraphics()) // Initialize Direct3D
                {
                    MessageBox.Show("Could not initialize Direct3D.  This tutorial will exit.");
                    return;
                }

                frm.Show();

                // While the form is still valid, render and process messages
                while(frm.Created)
                {
                    frm.Render();
                    frm.RenderBrick();
                    Application.DoEvents();
                }
            }
		}

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.BuildPanel = new System.Windows.Forms.Panel();
            this.ColorPanel = new System.Windows.Forms.Panel();
            this.ColorLabel = new System.Windows.Forms.Label();
            this.BrickLabel = new System.Windows.Forms.Label();
            this.BrickPanel = new System.Windows.Forms.Panel();
            this.PositionLabel = new System.Windows.Forms.Label();
            this.PosXLabel = new System.Windows.Forms.Label();
            this.PosYLabel = new System.Windows.Forms.Label();
            this.PosZLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // BuildPanel
            // 
            this.BuildPanel.Location = new System.Drawing.Point(12, 12);
            this.BuildPanel.Name = "BuildPanel";
            this.BuildPanel.Size = new System.Drawing.Size(800, 600);
            this.BuildPanel.TabIndex = 0;
            this.BuildPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BuildPanel_MouseDown);
            this.BuildPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.BuildPanel_MouseMove);
            this.BuildPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BuildPanel_MouseUp);
            // 
            // ColorPanel
            // 
            this.ColorPanel.Location = new System.Drawing.Point(72, 638);
            this.ColorPanel.Name = "ColorPanel";
            this.ColorPanel.Size = new System.Drawing.Size(90, 35);
            this.ColorPanel.TabIndex = 1;
            // 
            // ColorLabel
            // 
            this.ColorLabel.AutoSize = true;
            this.ColorLabel.Font = new System.Drawing.Font("Ravie", 9.267326F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ColorLabel.Location = new System.Drawing.Point(14, 654);
            this.ColorLabel.Name = "ColorLabel";
            this.ColorLabel.Size = new System.Drawing.Size(52, 19);
            this.ColorLabel.TabIndex = 2;
            this.ColorLabel.Text = "Color";
            // 
            // BrickLabel
            // 
            this.BrickLabel.AutoSize = true;
            this.BrickLabel.Font = new System.Drawing.Font("Ravie", 9.267326F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BrickLabel.Location = new System.Drawing.Point(638, 654);
            this.BrickLabel.Name = "BrickLabel";
            this.BrickLabel.Size = new System.Drawing.Size(56, 19);
            this.BrickLabel.TabIndex = 4;
            this.BrickLabel.Text = "Brick";
            // 
            // BrickPanel
            // 
            this.BrickPanel.Location = new System.Drawing.Point(700, 575);
            this.BrickPanel.Name = "BrickPanel";
            this.BrickPanel.Size = new System.Drawing.Size(100, 100);
            this.BrickPanel.TabIndex = 3;
            // 
            // PositionLabel
            // 
            this.PositionLabel.AutoSize = true;
            this.PositionLabel.Font = new System.Drawing.Font("Ravie", 9.267326F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PositionLabel.Location = new System.Drawing.Point(388, 654);
            this.PositionLabel.Name = "PositionLabel";
            this.PositionLabel.Size = new System.Drawing.Size(80, 19);
            this.PositionLabel.TabIndex = 6;
            this.PositionLabel.Text = "Position";
            // 
            // PosXLabel
            // 
            this.PosXLabel.AutoSize = true;
            this.PosXLabel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.PosXLabel.Font = new System.Drawing.Font("Ravie", 9.267326F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PosXLabel.Location = new System.Drawing.Point(474, 654);
            this.PosXLabel.Name = "PosXLabel";
            this.PosXLabel.Size = new System.Drawing.Size(21, 19);
            this.PosXLabel.TabIndex = 7;
            this.PosXLabel.Text = "X";
            // 
            // PosYLabel
            // 
            this.PosYLabel.AutoSize = true;
            this.PosYLabel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.PosYLabel.Font = new System.Drawing.Font("Ravie", 9.267326F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PosYLabel.Location = new System.Drawing.Point(521, 654);
            this.PosYLabel.Name = "PosYLabel";
            this.PosYLabel.Size = new System.Drawing.Size(21, 19);
            this.PosYLabel.TabIndex = 8;
            this.PosYLabel.Text = "Y";
            // 
            // PosZLabel
            // 
            this.PosZLabel.AutoSize = true;
            this.PosZLabel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.PosZLabel.Font = new System.Drawing.Font("Ravie", 9.267326F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PosZLabel.Location = new System.Drawing.Point(568, 654);
            this.PosZLabel.Name = "PosZLabel";
            this.PosZLabel.Size = new System.Drawing.Size(21, 19);
            this.PosZLabel.TabIndex = 9;
            this.PosZLabel.Text = "Z";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Ravie", 9.267326F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(501, 654);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(14, 19);
            this.label1.TabIndex = 10;
            this.label1.Text = ",";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Ravie", 9.267326F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(548, 654);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(14, 19);
            this.label2.TabIndex = 11;
            this.label2.Text = ",";
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(834, 684);
            this.ControlBox = false;
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.PosZLabel);
            this.Controls.Add(this.PosYLabel);
            this.Controls.Add(this.PosXLabel);
            this.Controls.Add(this.PositionLabel);
            this.Controls.Add(this.BrickPanel);
            this.Controls.Add(this.BrickLabel);
            this.Controls.Add(this.ColorLabel);
            this.Controls.Add(this.ColorPanel);
            this.Controls.Add(this.BuildPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void BuildPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (!isDown)
            {
                mx = e.X;
                my = e.Y;
                isDown = true;
            }
        }

        private void BuildPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDown = false;
        }

        private void BuildPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDown)
            {
                int dx, dy;
                dx = e.X - mx;
                dy = e.Y - my;
                mx = e.X;
                my = e.Y;
                camPos.X -= dx / 5;
                camPos.Y += dy / 5;
                //Console.WriteLine(dx + "," + dy);
            }
        }

        public void setPosLabel()
        {
            PosXLabel.Text = brickPos.X.ToString();
            PosYLabel.Text = brickPos.Y.ToString();
            PosZLabel.Text = brickPos.Z.ToString();
        }
	}
}
