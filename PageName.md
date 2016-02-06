# Introduction #

SensorMonHTTP is a Windows program (reliant on .NET 4.0 Framework being installed) to monitor various hardware sensor parameters on the host machine. The values can be accessed via a web browser (either on the machine itself, or any device in the local network). The program needs GPU-Z running in the background.

# Details #

SensorMonHTTP doesn't access any of the sensors in the machine directly. Instead, it relies on GPU-Z's shared memory as well as the data management interface provided by OpenHardwareMonitor. While GPU-Z needs to be running for the shared memory to be active, the OpenHardwareMonitor's sensor readouts don't need the executable to be running in the background.

# Usage #

SensorMonHTTP is a CLI program. It takes one parameter, --port (or -p). If the parameter is not provided, a default port of 55555 is assumed. The program first checks if GPU-Z is running in the background before proceeding with the starting of the web server on the requisite port.

Accessing http://<IP of machine running SensorMonHTTP>:Port gives a nicely formatted JSON string with the various sensor names, values and units.

# Thanks #

SensorMonHTTP is built upon the code base provided by JohnnyUT in the GpuzDemo project hosted here: https://github.com/JohnnyUT/GpuzShMem/tree/master/GpuzDemo ; In addition, it also uses code from this StackOverflow thread: http://stackoverflow.com/questions/11784801/get-hardware-temp-from-openhardwaremonitor for accessing information through OpenHardwareMonitorLib.dll