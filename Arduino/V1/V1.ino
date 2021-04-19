#include <SoftwareSerial.h>

#define BT1 3
#define BT2 4
#define BT3 5
#define BT4 6
#define BT5 7

#define TL 2

char c = ' ';
bool NL = true, lastisbl = false;
bool b1_pressed = false, b2_pressed = false, b3_pressed = false, b4_pressed = false, b5_pressed = false;


SoftwareSerial hc05(10, 11);

void setup() {
  pinMode(BT1, INPUT);
  pinMode(BT2, INPUT);
  pinMode(BT3, INPUT);
  pinMode(BT4, INPUT);
  pinMode(BT5, INPUT);

  pinMode (TL, OUTPUT);

  Serial.begin(9600);
  hc05.begin(9600);

  Serial.println("Started");
}

bool update_button(int button, bool *button_state) {
  if (digitalRead(button) == HIGH && *button_state == false) {
    *button_state = true;
    return true;
  } else if (digitalRead(button) == LOW && *button_state == true) {
    *button_state = false;
  }
  return false;
}

void loop() {
  if (digitalRead(BT1) == HIGH && b1_pressed == false) {
    b1_pressed = true;
    digitalWrite(TL, HIGH);
  } else if (digitalRead(BT1) == LOW && b1_pressed == true) {
    b1_pressed = false;
    digitalWrite(TL, LOW);
  }

  if (update_button(BT2, &b2_pressed)) {
    hc05.println("ILP+B2");
  }

  if (update_button(BT3, &b3_pressed)) {
    hc05.println("ILP+B3");
  }

  if (update_button(BT4, &b4_pressed)) {
    hc05.println("ILP+B4");
  }

  if (update_button(BT5, &b5_pressed)) {
    hc05.println("ILP+B5");
  }

  if (digitalRead(BT1) == HIGH && b1_pressed == false) {
    digitalWrite(TL, HIGH);
  } else if (digitalRead(BT1) == LOW && b1_pressed == true) {
    digitalWrite(TL, LOW);
  }


  if (hc05.available())
  {
    c = hc05.read();
    Serial.write(c);
    lastisbl = true;
  }

  if (Serial.available())
  {
    c = Serial.read();
    hc05.write(c);

    if (NL) {
      if (lastisbl)
      {
        Serial.print("\n");
      }
      Serial.print("> ");
      NL = false;
    }

    Serial.write(c);
    if (c == 10) {
      NL = true;
    }
    lastisbl = false;
  }
}
