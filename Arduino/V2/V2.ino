#include <SoftwareSerial.h>
#include <EEPROM.h>

#define BT1 3
#define BT2 4
#define BT3 5
#define BT4 6
#define BT5 7

#define TL 2

#define joyX A0
#define joyY A1

#define LASER_MODE_ADDRESS 0
#define DEADZONE_ADDRESS 1

bool is_connected;
bool laser_state = false;
bool b1_pressed = false, b2_pressed = false, b3_pressed = false, b4_pressed = false, b5_pressed = false;
String command;
int laser_mode;
int xValue, yValue, deadzone, joy_tick_counter = 0;

typedef enum
{
  B_UNCHANGED = -1,
  B_RELEASED,
  B_PRESSED
} STATES;

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
  Serial.setTimeout(50);

  laser_mode = (int)EEPROM.read(LASER_MODE_ADDRESS);
  deadzone = (int)EEPROM.read(DEADZONE_ADDRESS);

  Serial.println("Started");
  Serial.print("Laser mode : ");
  Serial.println(laser_mode);

  Serial.print("Deadzone : ");
  Serial.println(deadzone);
}

STATES update_button(int button, bool *button_state) {
  if (digitalRead(button) == HIGH && *button_state == false) {
    *button_state = true;
    return B_PRESSED;
  } else if (digitalRead(button) == LOW && *button_state == true) {
    *button_state = false;
    return B_RELEASED;
  }
  return B_UNCHANGED;
}

void update_laser_mode(int mode) {
  if (mode != 0 && mode != 1 && mode != 2) {
    mode = 1;
  }

  EEPROM.update(LASER_MODE_ADDRESS, mode);
  laser_mode = mode;
  hc05.print("OK+LASERMODE=");
  hc05.println(mode);
}

void update_deadzone(int value) {
  value = constrain(value, 0, 100);
  EEPROM.update(DEADZONE_ADDRESS, value);
  deadzone = value;
  hc05.print("OK+DEADZONE=");
  hc05.println(deadzone);
}

int joyRawToPhys(int raw) {
  return map(raw, 0, 1023, -100, 100);
}

int getJoyValue(int joy) {
  int value = joyRawToPhys(analogRead(joy));
  if (abs(value) < deadzone) {
    value = 0;
  }
  return value;
}

void loop() {
  switch (update_button(BT1, &b1_pressed)) {
    case B_PRESSED:
      if (laser_mode == 0 && is_connected) {
        hc05.println("ILP+B1=0");
      } else if (laser_mode == 2) {
        digitalWrite(TL, laser_state ? LOW : HIGH);
        laser_state = !laser_state;
      } else { // laser_mode == 1 or bluetooth not connected
        digitalWrite(TL, HIGH);
        laser_state = true;
      }
      break;
    case B_RELEASED:
      if (laser_mode == 0 && is_connected) {
        hc05.println("ILP+B1=1");
      } else if (laser_state && laser_mode != 2 && digitalRead(BT1) == LOW) { // laser_mode == 1 or bluetooth not connected
        digitalWrite(TL, LOW);
        laser_state = false;
      }
      break;
  }

  switch (update_button(BT2, &b2_pressed)) {
    case B_PRESSED:
      hc05.println("ILP+B2=0");
      break;
    case B_RELEASED:
      hc05.println("ILP+B2=1");
      break;
  }

  switch (update_button(BT3, &b3_pressed)) {
    case B_PRESSED:
      hc05.println("ILP+B3=0");
      break;
    case B_RELEASED:
      hc05.println("ILP+B3=1");
      break;
  }

  switch (update_button(BT4, &b4_pressed)) {
    case B_PRESSED:
      hc05.println("ILP+B4=0");
      break;
    case B_RELEASED:
      hc05.println("ILP+B4=1");
      break;
  }

  switch (update_button(BT5, &b5_pressed)) {
    case B_PRESSED:
      hc05.println("ILP+B5=0");
      break;
    case B_RELEASED:
      hc05.println("ILP+B5=1");
      break;
  }


  if (hc05.available())
  {
    if (!is_connected) {
      is_connected = true;
    }
    command = hc05.readString();
    Serial.println(command);
    if (command.equals("ILP")) {
      hc05.println("OK"); // Handshake
    } else if (command.equals("ILP+LASERMODE?")) {
      hc05.print("OK+LASERMODE=");
      hc05.println(laser_mode);
    } else if (command.startsWith("ILP+LASERMODE=")) {
      update_laser_mode(command.substring(command.indexOf("=") + 1).toInt());
    } else if (command.equals("ILP+LASER?")) {
      hc05.print("OK+LASER=");
      hc05.println(laser_state);
    } else if (command.equals("ILP+LASER")) {
      digitalWrite(TL, laser_state ? LOW : HIGH);
      laser_state = !laser_state;
      hc05.print("OK+LASER=");
      hc05.println(laser_state);
    } else if (command.equals("ILP+DEADZONE?")) {
      hc05.print("OK+DEADZONE=");
      hc05.println(deadzone);
    } else if (command.startsWith("ILP+DEADZONE=")) {
      update_deadzone(command.substring(command.indexOf("=") + 1).toInt());
    }
  }

  if (is_connected && joy_tick_counter > 100) {
    hc05.print("ILP+JOYSTICK=");
    hc05.print(getJoyValue(joyX));
    hc05.print(",");
    hc05.println(getJoyValue(joyY));
    joy_tick_counter = 0;
  }
  joy_tick_counter++;

  delay(10);
}
