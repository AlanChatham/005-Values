
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace KinectOSC
{
    using Ventuz.OSC;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using MathNet.Numerics.LinearAlgebra.Double;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Global Settings

       // float[] kinectXPositions = { 7.88f, 5.6f, 3.28f, 1.0f }; // X is positive left, if looking at the screen
        float[] kinectXPositions = { 6.7f, 4.8f, 2.9f, 1.0f }; // X is positive left, if looking at the screen
        //float[] kinectXPositions = { 7.88f, 5.6f, 0.0f, 1.0f }; // X is positive left, if looking at the screen
        float[] kinectYPositions = { 0, 0, 0, 0 }; // Y is positive up
        float[] kinectZPositions = { 0, 0, 0, 0 }; // Z is positive towards the screen, so offsets will usually be positive
        float[] kinectAngles = { 00, 0, 0, 0 };

       
        /// <summary>
        /// Store the OSC On checkbox value for quick access
        /// </summary>
        private bool oscOn = true;

        /// <summary>
        /// Store the Show OSC Data checkbox value for quick access
        /// </summary>
        private bool showOscData = false;

        /// <summary>
        /// If this is true the skeleton will be drawn on screen
        /// </summary>
        private bool showSkeleton = false;

        /// <summary>
        /// Flag to choose to send data specifically in a format that Animata will appreciate
        /// </summary>
        private bool sendAnimataData = true;

        /// <summary>
        /// If this is true then each variable of each of the joints will be sent separately (each osc element will have one float)
        /// </summary>
        private string oscAddress = "";

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private List<VisualKinectUnit> visualKinectUnitList;

        private List<System.Windows.Controls.Image> skeletonImageList;
        private List<System.Windows.Controls.Image> colorImageList;

        // OSC 
        private static UdpWriter oscWriter;
        private static string[] oscArgs = new string[2];

        private static UdpWriter deltaToscWriter;

        #endregion

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e) {
            // Setup osc sender
            oscArgs[0] = "127.0.0.1";
            oscArgs[1] = OscPort.Text;
            oscWriter = new UdpWriter(oscArgs[0], Convert.ToInt32(oscArgs[1]));

            deltaToscWriter = new UdpWriter(oscArgs[0], 7114);
            // Initialize Data viewer
            oscViewer.Text = "\nData will be shown here\nwhen there is a skeleton\nbeing tracked.";  

            // Set up our lists
            visualKinectUnitList = new List<VisualKinectUnit>();

            skeletonImageList = new List<System.Windows.Controls.Image>();
            skeletonImageList.Add(Image0);
            skeletonImageList.Add(Image1);
            skeletonImageList.Add(Image2);
            skeletonImageList.Add(Image3);

            colorImageList = new List<System.Windows.Controls.Image>();
            colorImageList.Add(ColorImage0);
            colorImageList.Add(ColorImage1);
            colorImageList.Add(ColorImage2);
            colorImageList.Add(ColorImage3);

            masterSkeletonList = new List<Skeleton>();
            leadSkeletonIDs = new List<int>();
            prunedSkeletonList = new List<Skeleton>();

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit
            int numberOfKinects = 0;
            foreach (var potentialSensor in KinectSensor.KinectSensors) {
                if (potentialSensor.Status == KinectStatus.Connected) {
                    // Start the sensor!
                    try {
                        potentialSensor.Start();
                        // Good to go, so count this one as connected!
                        // So let's set up some environment for this...
                       
                        LocatedSensor sensor = new LocatedSensor(potentialSensor, kinectXPositions[numberOfKinects],
                                                                                  kinectYPositions[numberOfKinects],
                                                                                  kinectZPositions[numberOfKinects],
                                                                                  kinectAngles[numberOfKinects]);
                        if ((numberOfKinects < colorImageList.Count) && (numberOfKinects < skeletonImageList.Count)) {
                            System.Windows.Controls.Image colorImage = colorImageList[numberOfKinects];
                            System.Windows.Controls.Image skeletonImage = skeletonImageList[numberOfKinects];
                            VisualKinectUnit newSensor = new VisualKinectUnit(sensor, skeletonImage, colorImage);
                            // Add a callback to our updateSkeletons function, so every frameReady event,
                            //  we update our global list of skeletons
                            newSensor.locatedSensor.sensor.SkeletonFrameReady += updateSkeletons;

                            //newSensor.locatedSensor.sensor.SkeletonFrameReady += sendOSCHeadOnly;
                            newSensor.locatedSensor.sensor.SkeletonFrameReady += sendOSCHands;
                            //newSensor.locatedSensor.sensor.SkeletonFrameReady += sendOSCForearms;
                            //newSensor.locatedSensor.sensor.SkeletonFrameReady += sendOSCAsAnimataData;

                            visualKinectUnitList.Add(newSensor);
                        }
                        else {
                            visualKinectUnitList.Add(new VisualKinectUnit(sensor));
                        }
                        numberOfKinects++;
                        Console.WriteLine("Number of Kinects : " + numberOfKinects);
                    }
                    catch (IOException) {
                        Console.WriteLine("Couldn't start one of the Kinect sensors...");
                    }
                }
            }
        }

     
        /// <summary>
        /// This list holds all the skeletons seen by all sensors,
        ///  with position data in the global coordinate frame
        /// We trust that there won't be a conflict between random
        ///  IDs assigned by kinect sensors
        /// </summary>
        private List<Skeleton> masterSkeletonList;
        /// <summary>
        /// Skeletons within this radius of each other will be assumed
        ///  to be the same skeleton
        /// </summary>
        private float sameSkeletonRadius = 1.0f;

        List<Skeleton> prunedSkeletonList;

        private List<int> leadSkeletonIDs;

        private void updateSkeletons(object sender, SkeletonFrameReadyEventArgs e) {
            masterSkeletonList = new List<Skeleton>();
            List<int> currentSkeletonIDs = new List<int>();
            // From each of our kinect sensors...
            foreach (VisualKinectUnit kinect in this.visualKinectUnitList){
                // Read all our skeleton data
                foreach (Skeleton skel in kinect.locatedSensor.globalSkeletons){
                    // And if the skeleton is being tracked...
                    if (skel.TrackingState == SkeletonTrackingState.Tracked) {
                        currentSkeletonIDs.Add(skel.TrackingId);
                        bool isInMasterList = false;
                        // if it's in our master list already, 
                        for( int i = 0; i < masterSkeletonList.Count; i++) {
                            // update the skeleton to the fresh data
                            if (skel.TrackingId == masterSkeletonList[i].TrackingId) {
                                masterSkeletonList[i] = skel;
                                isInMasterList = true;
                                break;
                            }
                        }
                        if (!isInMasterList) {
                            masterSkeletonList.Add(skel);
                        }
                    }
                }
            }
//            Console.WriteLine("Size of master list: " + masterSkeletonList.Count);
            // Now, make sure we remove extra IDs of skeletons that aren't in our view anymore
            for (int i = leadSkeletonIDs.Count - 1; i >= 0; i--) {
                if (currentSkeletonIDs.Find(item => item == leadSkeletonIDs[i]) == 0) {
                    leadSkeletonIDs.RemoveAt(i);
                }
            }

            // Now let's pick a skeleton to persistently follow if we're not following one
            if (leadSkeletonIDs.Count == 0 && currentSkeletonIDs.Count > 0) {
                leadSkeletonIDs.Add(currentSkeletonIDs[0]);
            }

            // And let's find duplicate skeletons that are our lead skeletons
            if (leadSkeletonIDs.Count > 0) {
                Skeleton trackedSkeleton = new Skeleton();
                // Find our tracked skeleton
                foreach (Skeleton skel in masterSkeletonList) {
                    if ((skel.TrackingState == SkeletonTrackingState.Tracked ) && (currentSkeletonIDs.Find(id => id == skel.TrackingId) != 0) ){
                        trackedSkeleton = skel;
                        break;
                    }
                }
                // Iterate through it agian, since we might have missed it the first time
                foreach (Skeleton skel in masterSkeletonList) {
                    if ((skel.TrackingState == SkeletonTrackingState.Tracked) && 
                        skel.Position.X < trackedSkeleton.Position.X + sameSkeletonRadius &&
                        skel.Position.X > trackedSkeleton.Position.X - sameSkeletonRadius &&
                        skel.Position.Z < trackedSkeleton.Position.Z + sameSkeletonRadius &&
                        skel.Position.Z > trackedSkeleton.Position.Z - sameSkeletonRadius &&
                        skel != trackedSkeleton)
                    {
                        if (leadSkeletonIDs.Find(item => item == skel.TrackingId) == 0)
                            leadSkeletonIDs.Add(skel.TrackingId);
                    }
                }
            }

            // Now let's prune our skeleton list to remove the duplicates
            prunedSkeletonList = new List<Skeleton>();

            foreach (Skeleton skel in masterSkeletonList) {
                if (skel.TrackingState != SkeletonTrackingState.NotTracked) {
                    Boolean isUnique = true;
                    for (int i = 0; i < prunedSkeletonList.Count; i++) {
                        if (isTheSameSkeleton(skel, prunedSkeletonList[i])) {
                            isUnique = false;
                        }
                    }
                    if (isUnique) {
                        prunedSkeletonList.Add(skel);
                    }
                }
            }
        }

        Boolean isTheSameSkeleton(Skeleton a, Skeleton b) {
            if ((a.TrackingState == SkeletonTrackingState.Tracked) &&
                        a.Position.X < b.Position.X + sameSkeletonRadius &&
                        a.Position.X > b.Position.X - sameSkeletonRadius &&
                        a.Position.Z < b.Position.Z + sameSkeletonRadius &&
                        a.Position.Z > b.Position.Z - sameSkeletonRadius) {
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (VisualKinectUnit unit in visualKinectUnitList){
                unit.Stop();
            }
        }

        private int sendOSC(int counter, Skeleton skel) {
            var elements = new List<OscElement>();
            var oscText = "";
            if (sendAnimataData){
                //oscText = sendOSCHeadOnly(counter, skel, oscText);
            }
            
            if (showOscData) {
                oscViewer.Text = oscText;
            }
            counter++;
            return counter;
        }

        

        // This function will send out an entire skeleton data
        private void sendOSCAsAnimataData(object sender, SkeletonFrameReadyEventArgs e) {
            if (prunedSkeletonList.Count == 0)
                return;
            int i = 0;
            foreach (Skeleton skel in prunedSkeletonList) {

              //  sendOneOSCHand(skel.Joints[JointType.HandLeft],i);
              //  i++;
                sendOneOSCAnimataSkeleton(skel, i);
                i++;
            }
        }

        // Measurements for one screen
        static int SCREEN_WIDTH_PX = 640;
        static float SCREEN_WIDTH_METERS = 1.89f;
        static int SCREEN_HEIGHT_PX = 720;
        static float SCREEN_HEIGHT_METERS = 1.52f;

        static float SCREEN_WIDTH_PX_M_RATIO = SCREEN_WIDTH_PX / SCREEN_WIDTH_METERS;
        static float SCREEN_HEIGHT_PX_M_RATIO = SCREEN_HEIGHT_PX / SCREEN_HEIGHT_METERS;

        // Send out one skeleton data via OSC in Animata-friendly format
        private void sendOneOSCAnimataSkeleton(Skeleton skel, int counter){
            double playerHeight = skeletonHeight(skel);
            // joints bundled individually as 2 floats (x, y)
            string oscText = "";
            foreach (Joint joint in skel.Joints) {
                string jointName = "s" + counter + joint.JointType.ToString();
                var jointElement = new List<OscElement>();

                // Joint positions are returned in meters, so we'll assume a 2 meter tall person
                // and scale that range to pixels for the animation
                Point origin = new Point(-1, -1); // Offset in meters to map our origin to the characters
                double playerHeightRatio = 2 / (playerHeight + .2);
                // Translate Kinect joint positions to pixel coordinates
                float xScale = sanitizeTextToFloat(XScaleTextBox.Text);
                float xOffset = sanitizeTextToFloat(XOffsetTextBox.Text);
                float yScale = sanitizeTextToFloat(YScaleTextBox.Text);
                float yOffset = sanitizeTextToFloat(YOffsetTextBox.Text);
                

                float jointX = joint.Position.X * SCREEN_WIDTH_PX_M_RATIO * xScale + xOffset;
                float jointY = -1 * joint.Position.Y * SCREEN_HEIGHT_PX_M_RATIO * yScale + yOffset;

                jointX = (float)Math.Round(jointX, 4);
                jointY = (float)Math.Round(jointY, 4);
                jointElement.Add(new OscElement(
                                        "/joint", jointName,
                                        (float)Math.Round(jointX, 4), (float)Math.Round(jointY, 4)));
                oscWriter.Send(new OscBundle(DateTime.Now, jointElement.ToArray()));

                if (showOscData) {
                    oscText += jointName + " " + jointX + " " + jointY + "\n"; //GenerateOscDataDump(counter, jointName, joint.Position
                }
            }
            if (showOscData){
                oscViewer.Text =  oscText;
            }
            return;
        }

        private float sanitizeTextToFloat(string input) {
            float output = 1.0f;
            if (!float.TryParse(input, out output)){
                    output  = 1.0f;
                }
            return output;
        }

        private void sendOSCHeadOnly(object sender, SkeletonFrameReadyEventArgs e)
        {
            // If there isn't a skeleton we want to send, we don't send anything
            if (leadSkeletonIDs.Count == 0)
                return;
            Skeleton headTrackedSkeleton = new Skeleton();
            headTrackedSkeleton = null;
            // Get the skeleton we want to send
            foreach (Skeleton s in masterSkeletonList) {
                if (s.TrackingId == leadSkeletonIDs[0]) {
                    headTrackedSkeleton = s;
                }
            }
            // Just in case, if we didn't find a skeleton, get out of here
            if (headTrackedSkeleton == null) {
                Console.WriteLine("Had skeletons in our leadSkeletonIDs list, but couldn't find the appropriate skeleton... How odd.");
                return;
            }
            double playerHeight = skeletonHeight(headTrackedSkeleton);
            // joints bundled individually as 2 floats (x, y)
            Joint headJoint = headTrackedSkeleton.Joints[JointType.Head];

           var jointElement = new List<OscElement>();
                
            // Joint positions are returned in meters, so we'll assume a 2 meter tall person
            // and scale that range to pixels for the animation

           var jointX = headJoint.Position.X;
           var jointY = headJoint.Position.Y;
           var jointZ = headJoint.Position.Z;
            jointElement.Add(new OscElement(
                                    "/head",
                                    (float)Math.Round(jointX, 4), (float)Math.Round(jointY, 4),
                                    (float)Math.Round(jointZ, 4)));
            oscWriter.Send(new OscBundle(DateTime.Now, jointElement.ToArray()));
            deltaToscWriter.Send(new OscBundle(DateTime.Now, jointElement.ToArray()));
                
            if (showOscData)
            {
                string oscText = "\n\n/head " +
                           (float)Math.Round(jointX, 2) + " " +
                           (float)Math.Round(jointY, 2) + " " +
                                    (float)Math.Round(jointZ, 2);
                oscViewer.Text =  oscText;
            }

            // If there isn't a skeleton we want to send, we don't send anything
            if (prunedSkeletonList.Count == 0)
                return;
            int i = 0;
            foreach (Skeleton skel in prunedSkeletonList) {
                if (skel.TrackingId != headTrackedSkeleton.TrackingId) {
                    sendOneOSCHand(skel.Joints[JointType.HandLeft], i);
                    i++;
                    sendOneOSCHand(skel.Joints[JointType.HandRight], i);
                    i++;
                }
            }
        }

        private void sendOSCHands(object sender, SkeletonFrameReadyEventArgs e) {
            if (oscOn) {
                // If there isn't a skeleton we want to send, we don't send anything
                if (prunedSkeletonList.Count == 0)
                    return;
                int i = 0;
                foreach (Skeleton skel in prunedSkeletonList) {

                    sendOneOSCHand(skel.Joints[JointType.HandLeft], i);
                    i++;
                    sendOneOSCHand(skel.Joints[JointType.HandRight], i);
                    i++;
                }
            }
        }

        private void sendOneOSCHand(Joint joint, int i) {
            var jointElement = new List<OscElement>();
            var jointX = joint.Position.X;
            var jointY = joint.Position.Y;
            var jointZ = joint.Position.Z;
            jointElement.Add(new OscElement(
                                    "/hand",
                                    (float)Math.Round(jointX, 4), (float)Math.Round(jointY, 4),
                                    (float)Math.Round(jointZ, 4), i));
            oscWriter.Send(new OscBundle(DateTime.Now, jointElement.ToArray()));

            if (showOscData) {
                string oscText = "\n\n/hand " +
                            (float)Math.Round(jointX, 2) + " " +
                            (float)Math.Round(jointY, 2) + " " +
                                    (float)Math.Round(jointZ, 2);
                oscViewer.Text = oscText;
            }
        }

        private void sendOSCForearms(object sender, SkeletonFrameReadyEventArgs e) {
            // If there isn't a skeleton we want to send, we don't send anything
            if (prunedSkeletonList.Count == 0)
                return;
            int i = 0;
            foreach (Skeleton skel in prunedSkeletonList) {

                sendOneOSCForearm(skel.Joints[JointType.HandLeft], skel.Joints[JointType.ElbowLeft],i);
                i++;
                sendOneOSCForearm(skel.Joints[JointType.HandRight], skel.Joints[JointType.ElbowRight], i);
                i++;
            }
        }

        private void sendOneOSCForearm(Joint startJoint, Joint endJoint, int i) {
            var jointElement = new List<OscElement>();
           jointElement.Add(new OscElement(
                                    "/forearm", 
                                    i,
                                    (float)Math.Round(startJoint.Position.X, 4),
                                    (float)Math.Round(startJoint.Position.Y, 4),
                                    (float)Math.Round(startJoint.Position.Z, 4), 
                                    (float)Math.Round(endJoint.Position.X, 4),
                                    (float)Math.Round(endJoint.Position.Y, 4),
                                    (float)Math.Round(endJoint.Position.Z, 4)
                                    ));
            deltaToscWriter.Send(new OscBundle(DateTime.Now, jointElement.ToArray()));

            if (showOscData) {
                string oscText = "\n\n/hand " + i + " " +
                                    (float)Math.Round(startJoint.Position.X, 2) + " " +
                                    (float)Math.Round(startJoint.Position.Y, 2) + " " +
                                    (float)Math.Round(startJoint.Position.Z, 2) + "\n" +
                                    (float)Math.Round(endJoint.Position.X, 2) + " " +
                                    (float)Math.Round(endJoint.Position.Y, 2) + " " +
                                    (float)Math.Round(endJoint.Position.Z, 2);
                oscViewer.Text = oscText;
            }
        }




        private String GenerateOscDataDump(int counter, string jointName, SkeletonPoint jointPoint)
        {
            var dataDump = "";
            if (oscAddress != "")
            {
                dataDump += oscAddress + (counter - 3) + "/" + Math.Round(jointPoint.X, 4) + "\n";
                dataDump += oscAddress + (counter - 2) + "/" + Math.Round(jointPoint.Y, 4) + "\n";
                dataDump += oscAddress + (counter - 1) + "/" + Math.Round(jointPoint.Z, 4) + "\n";
            }
            else
            {
                dataDump += "/skeleton" + counter + "/" + jointName + "/x" + Math.Round(jointPoint.X, 4) + "\n";
                dataDump += "/skeleton" + counter + "/" + jointName + "/y" + Math.Round(jointPoint.Y, 4) + "\n";
                dataDump += "/skeleton" + counter + "/" + jointName + "/z" + Math.Round(jointPoint.Z, 4) + "\n";
            }
            return dataDump;
        }

        private String GenerateOscDataDump(int counter, string jointName, SkeletonPoint jointPoint, Point jointPosition)
        {
            var dataDump = "";
            if (oscAddress != "")
            {
                dataDump += oscAddress + (counter - 3) + "/" + Math.Round(jointPosition.X, 3) + "\n";
                dataDump += oscAddress + (counter - 2) + "/" + Math.Round(jointPosition.Y, 3) + "\n";
                dataDump += oscAddress + (counter - 1) + "/" + Math.Round(jointPoint.Z, 3) + "\n";
            }
            else
            {
                dataDump += "/skeleton" + counter + "/" + jointName + "/x" + Math.Round(jointPosition.X, 3) + "\n";
                dataDump += "/skeleton" + counter + "/" + jointName + "/y" + Math.Round(jointPosition.Y, 3) + "\n";
                dataDump += "/skeleton" + counter + "/" + jointName + "/z" + Math.Round(jointPoint.Z, 3) + "\n";
            }
            return dataDump;
        }


        public static long GetTimestamp()
        {
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000;
            return ticks;
        }

        private void CheckBoxOscOnChanged(object sender, RoutedEventArgs e)
        {
            oscOn = this.checkBoxOscOn.IsChecked.GetValueOrDefault();
        }
        private void CheckBoxShowOscDataChanged(object sender, RoutedEventArgs e)
        {
            showOscData = this.checkBoxShowOscData.IsChecked.GetValueOrDefault();
            if (showOscData)
            {
                oscViewer.Visibility = Visibility.Visible;
                CloseOscViewer.Visibility = Visibility.Visible;
            }
            else
            {
                oscViewer.Visibility = Visibility.Collapsed;
                CloseOscViewer.Visibility = Visibility.Collapsed;
            }
        }

        private void ChangePortClicked(object sender, RoutedEventArgs e)
        {
            oscArgs[1] = OscPort.Text;
            oscWriter = new UdpWriter(oscArgs[0], Convert.ToInt32(oscArgs[1]));
        }

        private void CloseOscViewerClicked(object sender, RoutedEventArgs e)
        {
            oscViewer.Visibility = Visibility.Collapsed;
            CloseOscViewer.Visibility = Visibility.Collapsed;
            checkBoxShowOscData.IsChecked = false;
        }

        private void ChangeAddressClicked(object sender, RoutedEventArgs e)
        {
            UpdateOscAddress();
        }

        private void OscAddressKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UpdateOscAddress();
            }
        }

        private void UpdateOscAddress()
        {
            oscAddress = OscAddress.Text;
            if (oscAddress.Substring(0, 1) != "/")
            {
                oscAddress = "/" + oscAddress;
                OscAddress.Text = oscAddress;
                ChangeAddress.Focus();
            }
        }

        private void OscPortKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                oscArgs[1] = OscPort.Text;
                oscWriter = new UdpWriter(oscArgs[0], Convert.ToInt32(oscArgs[1]));
            }
        }

        private void CheckBoxSendAnimataData(object sender, RoutedEventArgs e)
        {
            sendAnimataData = this.checkBoxSendAnimataData.IsChecked.GetValueOrDefault();
        }



        private double Length(Joint p1, Joint p2)
        {
            return Math.Sqrt(
                Math.Pow(p1.Position.X - p2.Position.X, 2) +
                Math.Pow(p1.Position.Y - p2.Position.Y, 2) +
                Math.Pow(p1.Position.Z - p2.Position.Z, 2));
        }

        private double Length(params Joint[] joints)
        {
            double length = 0;
            for (int index = 0; index < joints.Length - 1; index++) {
                length += Length(joints[index], joints[index + 1]);
            }
            return length;
        }

        private int NumberOfTrackedJoints(params Joint[] joints)
        {
            int trackedJoints = 0;
            foreach (var joint in joints)
            {
                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    trackedJoints++;
                }
            }
            return trackedJoints;
        }

        private double skeletonHeight(Skeleton skeleton)
        {
            const double HEAD_DIVERGENCE = 0.1;

            var head = skeleton.Joints[JointType.Head];
            var neck = skeleton.Joints[JointType.ShoulderCenter];
            var spine = skeleton.Joints[JointType.Spine];
            var waist = skeleton.Joints[JointType.HipCenter];
            var hipLeft = skeleton.Joints[JointType.HipLeft];
            var hipRight = skeleton.Joints[JointType.HipRight];
            var kneeLeft = skeleton.Joints[JointType.KneeLeft];
            var kneeRight = skeleton.Joints[JointType.KneeRight];
            var ankleLeft = skeleton.Joints[JointType.AnkleLeft];
            var ankleRight = skeleton.Joints[JointType.AnkleRight];
            var footLeft = skeleton.Joints[JointType.FootLeft];
            var footRight = skeleton.Joints[JointType.FootRight];

            // Find which leg is tracked more accurately.
            int legLeftTrackedJoints = NumberOfTrackedJoints(hipLeft, kneeLeft, ankleLeft, footLeft);
            int legRightTrackedJoints = NumberOfTrackedJoints(hipRight, kneeRight, ankleRight, footRight);
            double legLength = legLeftTrackedJoints > legRightTrackedJoints ? Length(hipLeft, kneeLeft, ankleLeft, footLeft) : Length(hipRight, kneeRight, ankleRight, footRight);

            return Length(head, neck, spine, waist) + legLength + HEAD_DIVERGENCE;
        }
    }
}