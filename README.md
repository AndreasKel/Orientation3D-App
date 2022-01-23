# Orientation3D
## Overview

This app uses data from an accelerometer and a gyroscope to estimate the current orientation using quaternions.

![App screenshot](https://github.com/Electrobird-Automation/Orientation3D-App/blob/main/Images/Orientation3D-Screenshot.PNG?raw=true)



## Platform

This application was created using Windows Presentation Foundation (WPF) as part of the .Net framework. Therefore, it only runs on Windows.

## Communication

Serial port communication is used to receive data from a device connected with the support of the SerialPort class. The default sample rate that is used for the calculations is 0.015 seconds, as specified on line 27 of MainWindow.xaml.cs.
>The device sending the data must use a string format like the one below:\
"MPUdata"+ " " + gForceX + " " + gForceY + " " + gForceZ + " " + gyroX + " " + gyroY + " " + gyroZ +"\n"

*Where*:
* MPUdata: It's an arbitrary string at the beginning. It can be any other text.
* gForceX: It's the gravitational force equivalent on X-axis, that causes a perception of weight, with a g-force of 1 g equal to the conventional value of gravitational acceleration on Earth, g, of about 9.8 m/s2. 
* gForceY: It's the gravitational force equivalent on Y-axis.
* gForceZ: It's the gravitational force equivalent on Z-axis.
* gyroX: It's the angular velocity on X-axis (**rad/second**).
* gyroY: It's the angular velocity on Y-axis (**rad/second**).
* gyroZ: It's the angular velocity on Z-axis (**rad/second**).

### Example using Arduino
The Orientation3D was tested successfully using an Arduino and an MPU6050. The sample code below is the one used on an Arduino to send the data using Serial Communication.
``` Arduino
void loop()
{ 
  if((elapsedTimeMS=(millis() - startTime)) >= sampleTime)
  {
    startTime = millis();
    recordAccelRegisters();
    recordGyroRegisters();
  
    Serial.println((String) "MPUdata"+ " " + gForceX + " " + gForceY + " " + gForceZ + " " + gyroX + " " + gyroY + " " + gyroZ +"\n");
  }
}
```
## Using Orientation3D

* To launch the app, run the executable file located in Orientation3D > bin > Debug > net5.0-windows > Orientation3D.exe
* Or launch the application using Visual Studio + Start with out Debugging (Ctrl + F5) 
* While the application is running, specify the port name (default: COM3).
* Select the type of filter. (Kalman Filter, Kalman Filter with Bias or Complementary Filter)
* Select a Baud Rate. (It needs to match the baud rate of the device that is transmitting the data)
* Click 'Connect'. The orientation of the airplane will change depending on the data and the quaternions (format: qw, qx, qy, qz) are displayed on the left-hand side of the app
