import threading
import time

import serial
import pyautogui

# Commands :
# ILP => OK (Handshake)
# ILP+LASERMODE? => Get laser mode
# ILP+LASERMODE=(0/1/2) => Set laser mode (0 = OFF / ILP+B1, 1 = push, 2 = toggle. If invalid, become 1)
# ILP+LASER? => Return laser state (mode 1/2 only) (0 off, 1 on)
# ILP+LASER => Toggle laser state
# ILP+DEADZONE=X => Set deadzone value (X = [0, 100]. If invalid, become 0, If out of bound become closest (0 or 100))
# ILP+DEADZONE? => Return deadzone value


# Answers :
# OK => Handshake
# ILP+B(1/2/3/4/5)=(0/1) => Button pressed (0) / Released (1)
# OK+LASERMODE=(0/1/2) => Laser mode returned by lasermode set and query
# OK+LASER=(0/1) => Laser state returned by laser set & query
# OK+DEADZONE=X => Deadzone value returned by deadzone set & query
# ILP+JOYSTICK=X,Y => Joystick position with X and Y in [-100, 100]

# While not connected, lasermode 0 default to 1 to be usable as a classic pointer

port = "COM6"

connected = False
require_stop = False


def handle_data(data):
    if data == "OK":
        print(data)
        global connected
        connected = True
    elif data.startswith("ILP+JOYSTICK"):
        pos = data.split('=')[1].split(',')
        pyautogui.moveRel(int(pos[0]), int(pos[1]))
    else:
        print('\r' + data + "\n> ", end='')


def read_from_port():
    global require_stop
    bluetooth.flushInput()
    while not require_stop:
        input_data = bluetooth.readline()
        handle_data(input_data.decode().strip())
        time.sleep(0.1)


if __name__ == '__main__':
    bluetooth = serial.Serial(port, 9600)

    print("Starting connection to ILP")

    thread = threading.Thread(target=read_from_port)
    thread.start()

    while not connected:
        bluetooth.write("ILP".encode())
        time.sleep(2)

    print("ILP Connected")
    print("Write Command without \"ILP+\" :")
    while True:
        print("> ", end="")
        command = input()
        if command == "EXIT":
            break
        bluetooth.write(("ILP+" + command).encode())

    connected = False
    require_stop = True
    bluetooth.close()
    print("\rDone")
