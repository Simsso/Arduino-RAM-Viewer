using System.Windows;

namespace RAM_Viewer
{
    /// <summary>
    /// Interaktionslogik für HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            TextBox_UserArduinoCode.Text = 
@"int i = 0;

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
";

            TextBox_TransceiverArduinoCode.Text =
@"volatile byte data;
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
";
        }
    }
}
