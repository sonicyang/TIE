import sys

# Register
from account import register

# Sensor
from sensor import list_sensor

def parse_argument():
	# Detect if argv
	len_argv = len(sys.argv)

	if len_argv == 0:
		return 0

	list_argv = []
	list_argv = sys.argv
	counter = 0

	for index in list_argv:
		if index == "-l":
			list_sensor()
			return 1
		if index == "-r":
			register(list_argv[counter+1], list_argv[counter+2])
			return 1
		if index == "-h":
			global SITE
			SITE = list_argv[counter+1]
		if index == "-a":
			global ADDR_DEVICE
			ADDR_DEVICE = list_argv[counter+1]
		if index == "--help":
			print "Registration :"
			print "Make sure the <sensor MAC address> have already in hrv-web server"
			print "python dev-bluetooth.py -r <USER NAME> <sensor MAC address>"
			print ""
			print "Transform bio-data :"
			print "usage with custom argument: python dev-bluetooth.py -h <http://IP> -a <sensor MAC address>"
			print "usage with default argument: python dev-bluetooth.py"
			print ""
			print "List sensor"
			print "usage: python dev-bluetooth.py -l"
			
			return 1

		counter += 1

	return 0

