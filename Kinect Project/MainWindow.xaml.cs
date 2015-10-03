
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

        private CoordinateMapper coordinateMapper = null;

        private CameraSpacePoint[] positionArray = null;

        //global soundplayer constructor
        MediaPlayer player = new MediaPlayer();

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

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {

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

            PlaySound(player);


        }

        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e) {


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

                        CheckBubbleCollision(rightHand, leftHand, rightFoot, leftFoot, head);
                    }
                }
            }

            if (frameCount%15 == 0)
            {
                Random random = new Random();
                int randomNumber = random.Next(0, 7);
                switch (randomNumber)
                {
                    case 0:
                        top.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 1:
                        topRight.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 2:
                        topLeft.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 3:
                        left.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 4:
                        right.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 5:
                        bottomRight.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 6:
                        bottomLeft.Visibility = System.Windows.Visibility.Visible;
                        break;
                }
                frameCount = 0;
            }
            frameCount++;


            }

        private void CheckBubbleCollision(Joint rightHand, Joint leftHand, Joint rightFoot, Joint leftFoot, Joint head)
        {

            // array of Camera space posistions
            positionArray = new CameraSpacePoint[] {rightHand.Position, leftHand.Position, rightFoot.Position, leftFoot.Position, head.Position};
            //, rightFoot.Position, leftFoot.Position, head.Position

            foreach (CameraSpacePoint j in positionArray)
            {
                double x = this.coordinateMapper.MapCameraPointToColorSpace(j).X;
                double y = this.coordinateMapper.MapCameraPointToColorSpace(j).Y;
                
                if (x >= 940 && x <= 1045 && y >= 0 && y <= 310)
                {
                    top.Visibility = System.Windows.Visibility.Collapsed;
                }
                if (x >= 1200 && x <= 1600 && y >= 310 && y <= 430)
                {
                    topRight.Visibility = System.Windows.Visibility.Collapsed;
                }
                if (x >= 0 && x <= 750 && y >= 310 && y <= 430)
                {
                    topLeft.Visibility = System.Windows.Visibility.Collapsed;
                }
                if (x >= 1200 && x <= 1600 && y >= 460 && y <= 735)
                {
                    right.Visibility = System.Windows.Visibility.Collapsed;
                }
                if (x >= 0 && x <= 750 && y >= 460 && y <= 735)
                {
                    left.Visibility = System.Windows.Visibility.Collapsed;
                }
                if (x >= 1200 && x <= 1600 && y >= 765 && y <= 1050)
                {
                    bottomRight.Visibility = System.Windows.Visibility.Collapsed;
                }
                if (x >= 0 && x <= 750 && y >= 765 && y <= 1050)
                {
                    bottomLeft.Visibility = System.Windows.Visibility.Collapsed;
                }
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
                    this.bodyIndexPixels[i] = 0xE0EEEE;
                }
            }
        }

        private void PlaySound(MediaPlayer player)
        {
            var uri = new Uri("C:\\Users\\LabLaptop\\Documents\\HackathonProject\\YorkhillChildrensHospitalKinect\\Audio files\\burst a beat theme.mp3");
            player.Open(uri);
            player.Play();
            testBubble.Play();
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
