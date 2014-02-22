#!/usr/bin/env python
#
# Copyright (C) 2013 AcoMo Technology.
# All rights reserved.
#
# Authored by Jyun-Yu Huang <yillkid@acomotech.com>
#
# Sensor query

# Receiver
from bluetooth import *
import bluetooth

def list_sensor():
	# Sensor query
	print "Performing inquiry..."
	nearby_devices = bluetooth.discover_devices(lookup_names = True)
	print "Found %d devices" % len(nearby_devices)

	if(len(nearby_devices) != 0):
		for addr, name in nearby_devices:
			print "Find a device name: " + name + "address: " + addr
