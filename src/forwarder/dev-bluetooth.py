#!/usr/bin/env python
#
# Copyright (C) 2013 AcoMo Technology.
# All rights reserved.
#
# Authored by Jyun-Yu Huang <yillkid@acomotech.com>
#
# This is a bluetooth forwarder.
#
# AcoMo forwarder bridge sensor and server, the protocol of client (sensor) is bluetooth RFCOMM,
# and the protocol of server is XML-RPC.
# 
# TODO: Restore data before upload.

# Receiver
from bluetooth import *
import bluetooth

import urllib2

# XML-RPC
import sys
import xmlrpclib

# Acomo format check
from xmlhandle import check_acomo_xml_format, append_elem_to_root

# Information
from info import parse_argument

# Account management
from account import get_username, search_user

# Quality test
#from test_forwarder import test

# Environment variable
#import sys

import time

# Timestamp flag : 
# The flag will be hang up "ONE TIME" when a user start to testing,
# teh other hand the range of ECG data load from DB for NN file calculate
# are [latest to flag = 1], it's a short-term test.
#flag_timestamp = 0

# User list :
# When a registered user device queried by forwarder,
# the username will be appended into the list,
# and the "TIMESTAMP" tag value will be seted to "1" ONE TIME
# the other hand the range of ECG data load from DB for NN file calculate
# are [latest to TIMESTAMP = 1], it's a short-term test
list_user = []

# Forwarder flag :
# When the flag is hang up:	continue to transfer,
# 		   down	  :	stop transfer.
flag_forwarder = 1

# Define
DEBUG = 0
CHANNEL = 6
BUFFERSIZE = 1024
ADDR_DEVICE = "00:11:67:E9:56:03"
VALUE_TIMEOUT = 10#1

# Site
SITE_BASE = 'hrv.acomotech.com'
SITE = "http://" + SITE_BASE +  "/xmldata/forwarder"
SITE_CHECK = "http://" + SITE_BASE + "/xmldata/latestecg"
SITE_UPDATE_USER = "http://" + SITE_BASE + "/account/update"

# Custom tags
TAG_ACOMO_USER = 'USER'
TAG_ACOMO_TIMESTAMP = 'TIMESTAMP'
TAG_ACOMO_MAC = 'MAC'

def forwarder():

	# Parsing argument
	if(parse_argument() == 1):
		return

	# Start to forwarder:
	#   1. devices query
	#   2. server connect detect
	#   3. data transfer

	while(flag_forwarder == 1):
	
		# Device query
		print "Performing inquiry..."
		nearby_devices = bluetooth.discover_devices(lookup_names = True)

		print "Found %d devices" % len(nearby_devices)

		# Continue to find
		if 0 == len(nearby_devices):
			continue

		# Set proxy for server site
		rpc_srv = xmlrpclib.ServerProxy(SITE, allow_none=True, verbose=False)

		# Collect devices done, check if these device have registered
		for addr, name in nearby_devices:
			username = get_username(addr)

			if(username == ""):
				continue

			# Device connecting
			print "Name:%s, Address:%s connecting" % (name, addr)

			client_socket=BluetoothSocket( RFCOMM )
			client_socket.connect((addr, CHANNEL))

			data = ""

			# Data receiveing
			try:
				data = client_socket.recv(BUFFERSIZE)
			except IOError:
				pass

			# XML RPC
			if 0 == check_acomo_xml_format(data):
				# Append username to data
				username = get_username(addr)

				data = append_elem_to_root(data, TAG_ACOMO_USER, username)
				data = append_elem_to_root(data, TAG_ACOMO_MAC, addr)
				
				# First appear ?
				if(not search_user(username, list_user)):
					list_user.append(username)
					data = append_elem_to_root(data, TAG_ACOMO_TIMESTAMP, '1')
				else:
					data = append_elem_to_root(data, TAG_ACOMO_TIMESTAMP, '0')

				print("data == " + data)

				result = rpc_srv.raw_handle(data)

				# Hand shake
				try:
					url_site_check = SITE_CHECK + "?user=" + username
					response = urllib2.urlopen(url_site_check, timeout = VALUE_TIMEOUT)
					value = response.read()

					if(cmp(value, data) != 0):
						print "Error: Server-side data do not match."

				except urllib2.URLError, e:
					print "Error: Connection timeout!"

				# Update users and devices
				try:
					url_site_update_user = SITE_UPDATE_USER + "?user=" + username + "&" + "device=" + name
					response = urllib2.urlopen(url_site_update_user)
					value = response.read()

				except urllib2.URLError, e:
					print "Error: Connection timeout!"

			client_socket.close()

forwarder()
