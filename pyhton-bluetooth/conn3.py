from bluetooth import *
import bluetooth

DEBUG = 0
CHANNEL = 6
BUFFERSIZE =128
ADDR_DEVICE = "8C:DE:52:0F:EE:0D"
VALUE_TIMEOUT = 10

def main():

    try:
        client_socket=BluetoothSocket( RFCOMM )
        client_socket.connect((ADDR_DEVICE, CHANNEL))
 
        data = ""
        while True:
            # Data receiveing
            data = client_socket.recv(BUFFERSIZE)
            print(data.decode("ASCII"))
    except IOError:
        pass


    client_socket.close()


main()
