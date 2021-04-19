// http://www.martyncurrey.com/hc-05-with-firmware-2-0-20100601/
// Press button on command
// AT+NAME?
// AT+NAME=XXXX


#include <SoftwareSerial.h>

char c = ' ';
boolean NL = true;

SoftwareSerial hc05(10, 11);

void setup() {
  Serial.begin(9600);
  Serial.println("ENTER AT Commands:");
  hc05.begin(38400);
}

void loop() {
  // Read from the Bluetooth module and send to the Arduino Serial Monitor
  if (hc05.available())
  {
    c = hc05.read();
    Serial.write(c);
  }


  // Read from the Serial Monitor and send to the Bluetooth module
  if (Serial.available())
  {
    c = Serial.read();
    hc05.write(c);

    // Echo the user input to the main window. The ">" character indicates the user entered text.
    if (NL) {
      Serial.print("> ");
      NL = false;
    }
    Serial.write(c);
    if (c == 10) {
      NL = true;
    }
  }
}
