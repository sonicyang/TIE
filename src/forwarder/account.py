#!/usr/bin/env python
#
# Copyright (C) 2013 AcoMo Technology.
# All rights reserved.
#
# Authored by Jyun-Yu Huang <yillkid@acomotech.com>
#
# Account manager
#
# Management accounts with AcoMo forwarder and server

filename_account = 'account.txt'

# Register a account
def register(username, mac):
	hash_username = hash(username)

	file_account = open(filename_account,'a')
	file_account.write(mac + "," + str(hash_username) + "\n")
	file_account.close()

	print "Register " +  username + " with " + mac

# Get binding MAC username 
def get_username(addr):
	file_account = open(filename_account,'r')

	print "Reading account data ... ..."
	while 1:
		content_line = file_account.readline()

		if not content_line:
			break

		# Separate username and MAC
		list_acc = content_line.split(',')

		if(addr == list_acc[0]):
			print "Find a MAC " + list_acc[0] + " binding username " + list_acc[1]
			return list_acc[1].rstrip()

	file_account.close()

	print "The device " + addr + " not register to forwarder yet !"

	return ""

# Search if user in user list
def search_user(username, list_user):
	for user in list_user:
		if user == username:
			return True

	return False
