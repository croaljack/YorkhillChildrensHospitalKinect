﻿
namespace Microsoft.Samples.Kinect.BodyIndexBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Text;
    using System.Collections.Generic;

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // body reader and list of bodies
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;
        private int bodycount;

        private int frameNumber = 0;

        private CoordinateMapper coordinateMapper = null;

        private CameraSpacePoint[] positionArray = null;

        //global soundplayer constructor
        MediaPlayer player = new MediaPlayer();
        public System.Windows.Shapes.Ellipse[] VisibleBubbles = new System.Windows.Shapes.Ellipse[7];

        public int[] bubbleSequence = { 39, 127, 216, 325, 402, 435, 482, 569, 742, 785, 829, 872, 917, 961, 982, 1027, 1050, 1104, 1126, 1127, 1181, 1225, 1268, 1312, 1334, 1378, 1399, 1440, 1481, 1519, 1559, 1592, 1660 ,1694, 1895, 1955, 2016, 2075, 2104, 2135, 2192, 2249, 2263, 2269, 2276, 2290, 2296, 2303, 2331, 2358, 2385, 2399, 2412, 2440, 2474, 2488, 2494, 2508, 2515, 2521, 2542, 2549, 2562, 2569, 2576, 2617, 2646, 2682, 2713, 2740, 2753, 2759, 2766, 2794, 2813, 2821, 2834, 2862, 2876, 2910, 2930, 2944, 2958, 2985, 2999, 3005, 3012, 3283, 3534, 5163, 5174, 5179, 5184, 5195, 5200, 5205, 5216, 5221, 5226, 5237, 5242, 5247, 5263, 5268 };
        private int sequence_counter = 0;

        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private const int BytesPerPixel = 4;

        /// <summary>
        /// Collection of colors to be used to display the BodyIndexFrame data.
        /// </summary>
        private static readonly uint[] BodyColor =
        {
            0x000000,
            0x000000,
            0x000000,
            0x000000,
            0x000000,
            0x000000,
        };

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for body index frames
        /// </summary>
        private BodyIndexFrameReader bodyIndexFrameReader = null;

        /// <summary>
        /// Description of the data contained in the body index frame
        /// </summary>
        private FrameDescription bodyIndexFrameDescription = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap bodyIndexBitmap = null;


        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private uint[] bodyIndexPixels = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        private int numberOfHitsCount = 0;

        private int colorAt = 0;

        private int colorCount = 0;

        private int circlesUp = 0;

        private int start = 1;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            //PlaySound(player);

            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            coordinateMapper = kinectSensor.CoordinateMapper;
            // get body count
            this.bodycount = this.kinectSensor.BodyFrameSource.BodyCount;

            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

            // set length of body array
            this.bodies = new Body[this.bodycount];

            // open the reader for the depth frames
            this.bodyIndexFrameReader = this.kinectSensor.BodyIndexFrameSource.OpenReader();

            // wire handler for frame arrival
            this.bodyIndexFrameReader.FrameArrived += this.Reader_FrameArrived;

            // play music
            PlaySound(player);

            this.bodyIndexFrameDescription = this.kinectSensor.BodyIndexFrameSource.FrameDescription;

            // allocate space to put the pixels being converted
            this.bodyIndexPixels = new uint[this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height];

            // create the bitmap to display
            this.bodyIndexBitmap = new WriteableBitmap(this.bodyIndexFrameDescription.Width, this.bodyIndexFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            restart();

            PlaySound(player);

        }

        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            frameNumber++;
            // end game at somepoint
            if (frameNumber > 2000)
            {
                gameOver.Visibility = System.Windows.Visibility.Visible;
                Score.Visibility = System.Windows.Visibility.Hidden;
                finalScore.Visibility = System.Windows.Visibility.Visible;
                finalScore.Content = " Your Score is:  " + numberOfHitsCount;
                start = -1;
            }

            if (frameNumber == 2300)
            {
                restart();
            }

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {

                if (bodyFrame != null)
                {
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    for (int i = 0; i < this.bodycount; i++)
                    {
                        Joint rightHand = bodies[i].Joints[JointType.HandRight];
                        Joint leftHand = bodies[i].Joints[JointType.HandLeft];
                        Joint rightFoot = bodies[i].Joints[JointType.FootRight];
                        Joint leftFoot = bodies[i].Joints[JointType.FootLeft];
                        Joint head = bodies[i].Joints[JointType.Head];
                        Joint bum = bodies[i].Joints[JointType.SpineBase];

                        if (circlesUp < 6 && start == 1)
                        {
                            CheckBubbleCollision(rightHand, leftHand, rightFoot, leftFoot, head, bum);
                        }
                    }
                }


                //if (frameNumber == bubbleSequence[sequence_counter])
                if (frameNumber % 50 == 0)
                {
                    CreateBubble();
                }
            }
        }

        private void restart()
        {
            player.Stop();
            player.Play();

            start = 1;
            frameNumber = 0;
            numberOfHitsCount = 0;
            VisibleBubbles = new System.Windows.Shapes.Ellipse[7];
            circlesUp = 0;
            colorCount = 0;

            Score.Visibility = System.Windows.Visibility.Visible;
            finalScore.Visibility = System.Windows.Visibility.Collapsed;
            top.Visibility = System.Windows.Visibility.Collapsed;
            topLeft.Visibility = System.Windows.Visibility.Collapsed;
            topRight.Visibility = System.Windows.Visibility.Collapsed;
            right.Visibility = System.Windows.Visibility.Collapsed;
            left.Visibility = System.Windows.Visibility.Collapsed;
            bottomRight.Visibility = System.Windows.Visibility.Collapsed;
            bottomLeft.Visibility = System.Windows.Visibility.Collapsed;
            gameOver.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void CreateBubble()
        {
            Random random = new Random();
            int randomNumber = random.Next(0, 7);
            //randomNumber = randomNumberChooser();
                switch (randomNumber)
                {
                    case 0:
                        if (top.Visibility != System.Windows.Visibility.Visible)
                        {
                            top.Visibility = System.Windows.Visibility.Visible;
                            try {
                                VisibleBubbles[circlesUp] = top;
                                circlesUp++;
                            }
                            catch { IndexOutOfRangeException e0; }
                        }

                        break;
                    case 1:
                        if (topRight.Visibility != System.Windows.Visibility.Visible)
                        {
                            topRight.Visibility = System.Windows.Visibility.Visible;
                            try {
                                VisibleBubbles[circlesUp] = topRight;
                                circlesUp++;
                            }
                            catch { IndexOutOfRangeException e1; }
                        }

                        break;
                    case 2:
                        if (topLeft.Visibility != System.Windows.Visibility.Visible)
                        {
                            topLeft.Visibility = System.Windows.Visibility.Visible;
                            try {
                                VisibleBubbles[circlesUp] = topLeft;
                                circlesUp++;
                            }
                            catch { IndexOutOfRangeException e2; }
                        }
                        break;
                    case 3:
                        if (left.Visibility != System.Windows.Visibility.Visible)
                        {
                            left.Visibility = System.Windows.Visibility.Visible;
                            try {
                                VisibleBubbles[circlesUp] = left;
                                circlesUp++;
                            }
                            catch { IndexOutOfRangeException e3; }
                        }
                        break;
                    case 4:
                        if (right.Visibility != System.Windows.Visibility.Visible)
                        {
                            right.Visibility = System.Windows.Visibility.Visible;
                            try {
                                VisibleBubbles[circlesUp] = right;
                                circlesUp++;
                            }
                            catch { IndexOutOfRangeException e4;}
                        }
                        break;
                    case 5:
                        if (bottomRight.Visibility != System.Windows.Visibility.Visible)
                        {
                            bottomRight.Visibility = System.Windows.Visibility.Visible;
                            try
                            {
                                VisibleBubbles[circlesUp] = bottomRight;
                                circlesUp++;
                            }
                            catch { IndexOutOfRangeException e5; }
                        }

                        break;
                    case 6:
                        if (bottomLeft.Visibility != System.Windows.Visibility.Visible)
                        {
                            bottomLeft.Visibility = System.Windows.Visibility.Visible;
                        try {
                            VisibleBubbles[circlesUp] = bottomLeft;
                            circlesUp++;
                        }
                        catch { IndexOutOfRangeException e6; }
                    }
                        break;
                }
            }
        

        private void CheckBubbleCollision(Joint rightHand, Joint leftHand, Joint rightFoot, Joint leftFoot, Joint head, Joint bum)
        {
            BubbleShaper(VisibleBubbles);
            // array of Camera space posistions
            positionArray = new CameraSpacePoint[] {rightHand.Position, leftHand.Position, rightFoot.Position, leftFoot.Position, head.Position, bum.Position};
            //, rightFoot.Position, leftFoot.Position, head.Position
                foreach (CameraSpacePoint j in positionArray)
                {
                    double x = this.coordinateMapper.MapCameraPointToColorSpace(j).X;
                    double y = this.coordinateMapper.MapCameraPointToColorSpace(j).Y;

                    if (x >= 940 && x <= 1045 && y >= 0 && y <= 310 && top.Visibility == System.Windows.Visibility.Visible)
                    {
                        top.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                        colorCount++;
                        bubbleHit(top);
                    }
                    if (x >= 1200 && x <= 1600 && y >= 310 && y <= 430 && topRight.Visibility == System.Windows.Visibility.Visible)
                    {
                        topRight.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                        colorCount++;
                        bubbleHit(topRight);
                }
                    if (x >= 0 && x <= 750 && y >= 310 && y <= 430 && topLeft.Visibility == System.Windows.Visibility.Visible)
                    {
                        topLeft.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                        colorCount++;
                        bubbleHit(topLeft);
                }
                    if (x >= 1200 && x <= 1600 && y >= 460 && y <= 735 && right.Visibility == System.Windows.Visibility.Visible)
                    {
                        right.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                        colorCount++;
                        bubbleHit(right);
                }
                    if (x >= 0 && x <= 750 && y >= 460 && y <= 735 && left.Visibility == System.Windows.Visibility.Visible)
                    {
                        left.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                        colorCount++;
                        bubbleHit(left);
                }
                    if (x >= 1200 && x <= 1600 && y >= 765 && y <= 1050 && bottomRight.Visibility == System.Windows.Visibility.Visible)
                    {
                        bottomRight.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                        colorCount++;
                        bubbleHit(bottomRight);
                }
                    if (x >= 0 && x <= 750 && y >= 765 && y <= 1050 && bottomLeft.Visibility == System.Windows.Visibility.Visible)
                    {
                        bottomLeft.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                        colorCount++;
                        bubbleHit(bottomLeft);
                }
                Score.Content = numberOfHitsCount.ToString();
            }
            

        }


        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.bodyIndexBitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }


        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.bodyIndexFrameReader != null)
            {
                // remove the event handler
                this.bodyIndexFrameReader.FrameArrived -= this.Reader_FrameArrived;

                // BodyIndexFrameReder is IDisposable
                this.bodyIndexFrameReader.Dispose();
                this.bodyIndexFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyIndexFrameArrivedEventArgs e)
        {
            bool bodyIndexFrameProcessed = false;

            using (BodyIndexFrame bodyIndexFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyIndexFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer bodyIndexBuffer = bodyIndexFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.bodyIndexFrameDescription.Width * this.bodyIndexFrameDescription.Height) == bodyIndexBuffer.Size) &&
                            (this.bodyIndexFrameDescription.Width == this.bodyIndexBitmap.PixelWidth) && (this.bodyIndexFrameDescription.Height == this.bodyIndexBitmap.PixelHeight))
                        {
                            this.ProcessBodyIndexFrameData(bodyIndexBuffer.UnderlyingBuffer, bodyIndexBuffer.Size);
                            bodyIndexFrameProcessed = true;
                        }
                    }
                }
            }

            if (bodyIndexFrameProcessed)
            {
                this.RenderBodyIndexPixels();
            }
            
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the BodyIndexFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the bodyIndexFrameData pointer.
        /// </summary>
        /// <param name="bodyIndexFrameData">Pointer to the BodyIndexFrame image data</param>
        /// <param name="bodyIndexFrameDataSize">Size of the BodyIndexFrame image data</param>
        private unsafe void ProcessBodyIndexFrameData(IntPtr bodyIndexFrameData, uint bodyIndexFrameDataSize)
        {

            uint[] colors = new uint[] { 0xFF3300, 0x47D147, 0xE0E00E, 0x3366FF, 0xFFFF00, 0xFF00FF };
            int numberOfColors = 6;

            byte* frameData = (byte*)bodyIndexFrameData;

            // convert body index to a visual representation
            for (int i = 0; i < (int)bodyIndexFrameDataSize; ++i)
            {
                // the BodyColor array has been sized to match
                // BodyFrameSource.BodyCount
                if (frameData[i] < BodyColor.Length)
                {
                    // this pixel is part of a player,
                    // display the appropriate color
                    this.bodyIndexPixels[i] = BodyColor[frameData[i]];
                }
                else
                {
                    // this pixel is not part of a player
                    if (colorCount == 10)
                    {
                        colorAt++;
                        colorCount = 0;
                    }
                    if (colorAt >= numberOfColors)
                    {
                        colorAt = 0;
                    }
                
                    this.bodyIndexPixels[i] = colors[colorAt];
                }
            }
        }

        private void PlaySound(MediaPlayer player)
        {
            var paths = new Uri("file://" + Directory.GetCurrentDirectory() + "/Audio/babt.mp3", UriKind.Absolute);

            player.Open(paths);
            player.Play();
            
        }
        
        private void bubbleHit(System.Windows.Shapes.Ellipse b)
        {
            int i = 0;
            try {
                while (VisibleBubbles[i] != b) {
                    i++;
                }
                while (VisibleBubbles[i] != null)
                {
                    VisibleBubbles[i] = VisibleBubbles[i + 1];
                    i++;
                }
            }
            catch { IndexOutOfRangeException e; }
            b.Height = 2;
            b.Width = 2;
            b.Opacity = 1.0;
        }

        private void BubbleShaper(System.Windows.Shapes.Ellipse[] visible)
        {
            Boolean reshuffle = false;
            if (visible[0] != null)
            {
                for (int i = 0;  i < circlesUp; i ++)
                {
                    System.Windows.Shapes.Ellipse b = visible[i];
                    if (b.Height < 55)
                    {
                        b.Height = b.Height + 1;
                        b.Width = b.Width + 1;
                    }
                   else if (b.Opacity > 0)
                    {
                        b.Opacity = b.Opacity - 0.002;
                    }
                    else 
                    {
                        b.Visibility = System.Windows.Visibility.Collapsed;
                        b.Height = 2;
                        b.Width = 2;
                        b.Opacity = 1.0;
                        reshuffle = true;
                        circlesUp--;
                    }
                }
                if (reshuffle == true)
                {
                    int i = 0;
                    while (visible[i] != null && i!=7)
                    {
                        
                        visible[i] = visible[i + 1];
                        i++;
                    }
                }
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderBodyIndexPixels()
        {
            this.bodyIndexBitmap.WritePixels(
                new Int32Rect(0, 0, this.bodyIndexBitmap.PixelWidth, this.bodyIndexBitmap.PixelHeight),
                this.bodyIndexPixels,
                this.bodyIndexBitmap.PixelWidth * (int)BytesPerPixel,
                0);
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
    }
}
