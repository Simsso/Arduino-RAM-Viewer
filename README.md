# RAM-Viewer
The Arduino RAM-Viewer is a tool that allows you to view the content of your Arduino’s RAM at specific times. This may be helpful in many situations, especially to do deep debugging. The whole system you need to make use of my tool consists of a Windows PC and two Arduinos.

One Arduino runs your program. It’s the Arduino from which you want to view the RAM content, I will name it the “User Arduino”. The other Arduino works as a transceiver between your PC and the User Arduino. This one is called the “Transceiver Arduino”. The advantage of this configuration is that the User Arduino will only be sparsely occupied with code that is not part of its original task. 

Almost all RAM-viewer depending tasks are outsourced to the transceiver; most important here is the communication with the PC. For the User Arduino, the only additional task left is to serially transmit the individual bytes. It was an important specification to keep the additional code on the User Arduino as short as possible.

Video: https://www.youtube.com/watch?v=oL_8Slv-a80

## Wiring
| User Arduino | Transceiver Ardunio | PC | Description |
| --- | --- | --- | --- |
|GND|GND||Ground|
|2 (can be changed)|8 (fixed)||Data|
|3 (can be changed)|2 (fixed)||Clock|
||USB|USB|Serial connection|

## Code
### User Arduino
Call the function `saveRAM()` every time you want to see a new memory stamp on your PC Software.
```Arduino
int i = 0;
void setup() {
 pinMode(2, OUTPUT); // data pin
 pinMode(3, OUTPUT); // clock pin
}
void loop() {
 i = 100;
 saveRAM();
 delay(1000);
 i = 200;
 saveRAM();
 delay(10000);
}
void saveRAM() {
 for (int i = 256; i < 2304; i++) {
   delayMicroseconds(40);
   shiftOut(2, 3, MSBFIRST, * ((byte *)i));
 }
}
```

### Transceiver Arduino
```Arduino
volatile byte data;
volatile byte bitsRemaining = 7;
void setup() {
  pinMode(2, INPUT); // clock pin
  pinMode(8, INPUT); // data pin
  
  attachInterrupt(0, bitAvailable, RISING);
  Serial.begin(57600);
  
  cli(); // disable interrupts
  
  // reset timer 1
  TCCR1A = 0; // set TCCR1A register to 0
  TCCR1B = 0; // set TCCR1B register to 0
  TCNT1  = 0; // reset counter value
  
  OCR1A = 61; // compare match register
  // set prescaler to 256
  TCCR1B |= (1 << CS12);   
  
  TCCR1B |= (1 << WGM12); // turn on CTC mode
  TIMSK1 |= (1 << OCIE1A); // enable timer compare interrupt
  
  sei(); // allow interrupts
}
void loop() {
  if (bitsRemaining >= 8) {
    Serial.write(data);
    bitsRemaining = 7;
  }
}
void bitAvailable() {
  TCNT1 = 0; // reset counter value
  data = (data << 1) | (PINB & 1);
  bitsRemaining--;
}
ISR(TIMER1_COMPA_vect) { // timer 1 
  // no data incoming in the last 1 millisecond
  bitsRemaining = 7;
}
```
