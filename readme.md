005 - Values
Keely Honeywell, Alan Chatham
This work has 3 pieces - code for up to 4 Kinect sensors, a website using Raphael.js to show the shapes on an HTML 5 canvas, and Node.js + socket.io code that turns the OSC messages that the kinect code kicks out to socket.io events that the website listens for.
To use:
YouÅfll first need to download and install the Kinect for Windows SDK and its attendant drivers. Once that is set up, you should be able to run the KinectOSC program, either via Visual Studio, or by running the .exe in the debug folder.
Due to security measures in most systems, you'll have to run a local http server to get this to work on one PC. This is easy to do with Python, though. Download python, add it to your system path if it doesn't do so already, then in a command line, navigate to the directory you put the website files in, and type 'python -m SimpleHttpServer' (Python 2.x) or 'python -m http.server' (Python 3.x+).
Next, you'll need to download node.js, then in another command line, get to the directory with 'bridge.js' in it, and run 'node bridge.js'.
Then, open up your browser and go to 'localhost:8000', and things should be running!
