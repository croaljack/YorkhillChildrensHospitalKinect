
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
        public System.Windows.Shapes.Ellipse[] VisibleBubbles = new System.Windows.Shapes.Ellipse[6];

        public int[] bubbleSequence = { 39, 127, 216, 325, 402, 435, 482, 569, 742, 785, 829, 872, 917, 961, 982, 1027, 1050, 1104, 1126, 1127, 1181, 1225, 1268, 1312, 1334, 1378, 1399, 1440, 1481, 1519, 1559, 1592, 1660 ,1694
 };
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

        private int frameCount = 0;

        private int numberOfHitsCount = 0;

        private int colorAt = 0;

        private int circlesUp = 0;

        private int start = 0;

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

            

            top.Visibility = System.Windows.Visibility.Collapsed;
            topLeft.Visibility = System.Windows.Visibility.Collapsed;
            topRight.Visibility = System.Windows.Visibility.Collapsed;
            right.Visibility = System.Windows.Visibility.Collapsed;
            left.Visibility = System.Windows.Visibility.Collapsed;
            bottomRight.Visibility = System.Windows.Visibility.Collapsed;
            bottomLeft.Visibility = System.Windows.Visibility.Collapsed;
            gameOver.Visibility = System.Windows.Visibility.Collapsed;

            PlaySound(player);

        }

        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            frameNumber++;
            /*if (circlesUp > 3)
            {
                gameOver.Visibility = System.Windows.Visibility.Visible;
            }*/

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

                        double rightX = this.coordinateMapper.MapCameraPointToColorSpace(rightHand.Position).X;
                        double rightY = this.coordinateMapper.MapCameraPointToColorSpace(rightHand.Position).Y;
                        double leftX = this.coordinateMapper.MapCameraPointToColorSpace(leftHand.Position).X;
                        double leftY = this.coordinateMapper.MapCameraPointToColorSpace(leftHand.Position).Y;

                        if (start == 0)
                        {
                            //&& rightX >= 1200 && rightX <= 1600 && rightY >= 310 && rightY <= 430)
                            
                                // && leftX >= 1200 && leftX <= 1600 && leftY >= 310 && leftY <= 430
                            start = 1;
                        }

                        if (circlesUp < 6 && start == 1)
                        {
                            CheckBubbleCollision(rightHand, leftHand, rightFoot, leftFoot, head);
                        }

                    }
                }


                // IF POP OF STACK = FRAME NUMBER
                if (frameNumber == bubbleSequence[sequence_counter])
                {
                    CreateBubble();
                    sequence_counter++;
                }

                /* if (frameCount == 20 && start == 1)  TEMPORARILY DISABLED!!
                 {
                     CreateBubble();
                 }*/

                //frameCount++;

            }
        }

        private void CreateBubble()
        {

                Random random = new Random();
                int randomNumber = random.Next(0, 7);
                switch (randomNumber)
                {
                    case 0:
                        if (top.Visibility != System.Windows.Visibility.Visible)
                        {
                            circlesUp++;
                            top.Visibility = System.Windows.Visibility.Visible;
                            VisibleBubbles[circlesUp - 1] = top;
                        }

                        break;
                    case 1:
                        if (topRight.Visibility != System.Windows.Visibility.Visible)
                        {
                            circlesUp++;
                            topRight.Visibility = System.Windows.Visibility.Visible;
                            VisibleBubbles[circlesUp - 1] = topRight;
                    }

                        break;
                    case 2:
                        if (topLeft.Visibility != System.Windows.Visibility.Visible)
                        {
                            circlesUp++;
                            topLeft.Visibility = System.Windows.Visibility.Visible;
                            VisibleBubbles[circlesUp - 1] = topLeft;
                    }

                        break;
                    case 3:
                        if (left.Visibility != System.Windows.Visibility.Visible)
                        {
                            circlesUp++;
                            left.Visibility = System.Windows.Visibility.Visible;
                            VisibleBubbles[circlesUp - 1] = left;
                    }

                        break;
                    case 4:
                        if (right.Visibility != System.Windows.Visibility.Visible)
                        {
                            circlesUp++;
                            right.Visibility = System.Windows.Visibility.Visible;
                            VisibleBubbles[circlesUp -1] = right;
                    }

                        break;
                    case 5:
                        if (bottomRight.Visibility != System.Windows.Visibility.Visible)
                        {
                            circlesUp++;
                            bottomRight.Visibility = System.Windows.Visibility.Visible;
                            VisibleBubbles[circlesUp-1] = bottomRight;
                    }

                        break;
                    case 6:
                        if (bottomLeft.Visibility != System.Windows.Visibility.Visible)
                        {
                            circlesUp++;
                            bottomLeft.Visibility = System.Windows.Visibility.Visible;
                            VisibleBubbles[circlesUp -1] = bottomLeft;
                    }

                        break;
                }
                frameCount = 0;
            }
        

        private void CheckBubbleCollision(Joint rightHand, Joint leftHand, Joint rightFoot, Joint leftFoot, Joint head)
        {
            BubbleShaper(VisibleBubbles);
            // array of Camera space posistions
            positionArray = new CameraSpacePoint[] {rightHand.Position, leftHand.Position, rightFoot.Position, leftFoot.Position, head.Position};
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
                        bubbleHit(top);
                    }
                    if (x >= 1200 && x <= 1600 && y >= 310 && y <= 430 && topRight.Visibility == System.Windows.Visibility.Visible)
                    {
                        topRight.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                        
                    }
                    if (x >= 0 && x <= 750 && y >= 310 && y <= 430 && topLeft.Visibility == System.Windows.Visibility.Visible)
                    {
                        topLeft.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                    }
                    if (x >= 1200 && x <= 1600 && y >= 460 && y <= 735 && right.Visibility == System.Windows.Visibility.Visible)
                    {
                        right.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                    }
                    if (x >= 0 && x <= 750 && y >= 460 && y <= 735 && left.Visibility == System.Windows.Visibility.Visible)
                    {
                        left.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                    }
                    if (x >= 1200 && x <= 1600 && y >= 765 && y <= 1050 && bottomRight.Visibility == System.Windows.Visibility.Visible)
                    {
                        bottomRight.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
                    }
                    if (x >= 0 && x <= 750 && y >= 765 && y <= 1050 && bottomLeft.Visibility == System.Windows.Visibility.Visible)
                    {
                        bottomLeft.Visibility = System.Windows.Visibility.Collapsed;
                        circlesUp--;
                        numberOfHitsCount++;
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

            uint[] colors = new uint[] { 0xE0EEEE, 0xFF3300, 0x47D147, 0x3366FF, 0xFFFF00, 0xFF00FF };
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
                    if (numberOfHitsCount == 10)
                    {
                        colorAt++;
                        numberOfHitsCount = 0;
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
            var uri = new Uri("C:\\Users\\LabLaptop\\Documents\\HackathonProject\\YorkhillChildrensHospitalKinect\\Kinect Project\\audioFiles\\burst a beat theme.mp3");
            player.Open(uri);
            player.Play();
            //testBubble.Play();
        }
        
        private void bubbleHit(System.Windows.Shapes.Ellipse b)
        {
            int i = 0;
            while ( VisibleBubbles[i] != b) {
                i++;
            }
            while (VisibleBubbles[i]!= null)
            {
                VisibleBubbles[i] = VisibleBubbles[i + 1];
                i++;
            }
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
                        b.Opacity = b.Opacity - 0.005;
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
                    //visible[i] = null;
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
    }
}
