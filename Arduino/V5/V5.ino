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
#define LASER_BUTTON_ADDRESS 2

bool is_connected;
bool laser_state = false;
bool b_pressed[5] = {false, false, false, false, false};
String command;
int laser_mode;
int laser_button;
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
  pinMode(BT5, INPUT_PULLUP);

  pinMode (TL, OUTPUT);

  Serial.begin(9600);
  hc05.begin(9600);

  laser_mode = (int)EEPROM.read(LASER_MODE_ADDRESS);
  deadzone = (int)EEPROM.read(DEADZONE_ADDRESS);
  laser_button = (int)EEPROM.read(LASER_BUTTON_ADDRESS);
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

void update_laser_mode(int mode, Stream *stream) {
  if (mode != 0 && mode != 1 && mode != 2) {
    mode = 1;
  }

  EEPROM.update(LASER_MODE_ADDRESS, mode);
  laser_mode = mode;
  stream->print("OK+LASERMODE=");
  stream->println(mode);
}

void update_laser_button(int button, Stream *stream) {
  if (!(button >= 0 && button <= 5)) {
    button = 0;
  }

  EEPROM.update(LASER_BUTTON_ADDRESS, button);
  laser_button = button;
  stream->print("OK+LASERBUTTON=");
  stream->println(button);
}

void update_deadzone(int value, Stream *stream) {
  value = constrain(value, 0, 100);
  EEPROM.update(DEADZONE_ADDRESS, value);
  deadzone = value;
  stream->print("OK+DEADZONE=");
  stream->println(deadzone);
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

void handle_commands(Stream *stream) {
  if (!is_connected) {
    is_connected = true;
  }
  command = stream->readString();
  if (command.equals("ILP")) {
    stream->println("OK"); // Handshake
  } else if (command.equals("ILP+LASERMODE?")) {
    stream->print("OK+LASERMODE=");
    stream->println(laser_mode);
  } else if (command.startsWith("ILP+LASERMODE=")) {
    update_laser_mode(command.substring(command.indexOf("=") + 1).toInt(), stream);
  } else if (command.equals("ILP+LASERBUTTON?")) {
    stream->print("OK+LASERBUTTON=");
    stream->println(laser_button);
  } else if (command.startsWith("ILP+LASERBUTTON=")) {
    update_laser_button(command.substring(command.indexOf("=") + 1).toInt(), stream);
  } else if (command.equals("ILP+LASER?")) {
    stream->print("OK+LASER=");
    stream->println(laser_state);
  } else if (command.equals("ILP+LASER")) {
    digitalWrite(TL, laser_state ? LOW : HIGH);
    laser_state = !laser_state;
    stream->print("OK+LASER=");
    stream->println(laser_state);
  } else if (command.equals("ILP+DEADZONE?")) {
    stream->print("OK+DEADZONE=");
    stream->println(deadzone);
  } else if (command.startsWith("ILP+DEADZONE=")) {
    update_deadzone(command.substring(command.indexOf("=") + 1).toInt(), stream);
  } else if (command.startsWith("ILP+UPDATELASER=")) {
    update_laser_mode(command.substring(command.indexOf("=") + 1).toInt(), stream);
    update_laser_button(command.substring(command.indexOf(",") + 1).toInt(), stream);
  }
}

void loop() {
  for(int i = 0; i < 5; ++i) {
    switch (update_button(BT1 + i, &b_pressed[i])) {
      case B_PRESSED:
         if ((i == 0 && !is_connected) || (laser_button == i + 1 && laser_mode == 1)){ // If not connected use mode 1 for b1
          digitalWrite(TL, HIGH);
          laser_state = true;
        } else if (laser_button == i + 1 && laser_mode == 2) {
          digitalWrite(TL, laser_state ? LOW : HIGH);
          laser_state = !laser_state;
        } else {
          hc05.println("ILP+B" + (String)(i+1) + "=0");
          Serial.println("ILP+B" + (String)(i+1) + "=0");
        }
        break;
      case B_RELEASED:
        if (((i == 0 && !is_connected) || (laser_button == i + 1 && laser_mode == 1)) && laser_state) { // If not connected use mode 1 for b1
          digitalWrite(TL, LOW);
          laser_state = false;
        } else if (laser_button == i + 1 && laser_mode == 2) {
        } else {
          hc05.println("ILP+B" + (String)(i+1) + "=1");
          Serial.println("ILP+B" + (String)(i+1) + "=1");
        }
        break;
    }
  }

  if (hc05.available())
  {
    handle_commands(&hc05);
  }
  
  if (Serial.available())
  {
    handle_commands(&Serial);
  }

  if (is_connected && joy_tick_counter > 10 && (getJoyValue(joyX) != 0 || getJoyValue(joyY) != 0)) {
    hc05.print("ILP+JOYSTICK=");
    hc05.print(getJoyValue(joyX));
    hc05.print(",");
    hc05.println(getJoyValue(joyY));

    Serial.print("ILP+JOYSTICK=");
    Serial.print(getJoyValue(joyX));
    Serial.print(",");
    Serial.println(getJoyValue(joyY));
    joy_tick_counter = 0;
  }
  joy_tick_counter++;

  delay(10);
}
