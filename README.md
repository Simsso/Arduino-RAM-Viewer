# RAM-Viewer
The Arduino RAM-Viewer is a tool that allows you to view the content of your Arduino’s RAM at specific times. This may be helpful in many situations, especially to do deep debugging. The whole system you need to make use of my tool consists of a Windows PC and two Arduinos.

One Arduino runs your program. It’s the Arduino from which you want to view the RAM content, I will name it the “User Arduino”. The other Arduino works as a transceiver between your PC and the User Arduino. This one is called the “Transceiver Arduino”. The advantage of this configuration is that the User Arduino will only be sparsely occupied with code that is not part of its original task. 

Almost all RAM-viewer depending tasks are outsourced to the transceiver; most important here is the communication with the PC. For the User Arduino, the only additional task left is to serially transmit the individual bytes. It was an important specification to keep the additional code on the User Arduino as short as possible.

Video: https://www.youtube.com/watch?v=oL_8Slv-a80
