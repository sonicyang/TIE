# Copyright (C) 2013 AcoMo Technology.
# All rights reserved.
#
# Authored by Jyun-Yu Huang <yillkid@acomotech.com>
#
# Unit test
# Send 100 golden sample to server

# System
import sys
import os

# URL
import urllib2

# XML-RPC
import xmlrpclib

# Path and name define
sys.path.insert(0, '../..')
from acomo.log.log_handler import get_log_name, append_log
from acomo.project.path_define import *
from acomo.project.name_define import *

import time

# Define
DEBUG = 1
FILE_SAMPLE = '../../testrunner/samples/ecg_'
COUNTS_FILE = 100
NAME_TEST = "send_samples"
DESCRIPTION = "Send golden sample files to server"
VALUE_TIMEOUT = 20

def send_ecg_golden_samples():
	# Description
	path_log = get_log_name(NAME_TEST)
	append_log(PRE_LOG_NORMAL, path_log, "--- --- TEST : " + DESCRIPTION + " --- ---", DEBUG)

	# Clean all old data
	try:
		link = urllib2.urlopen(SITE_CLEAN, timeout = VALUE_TIMEOUT)

	except urllib2.URLError, e:
		append_log(PRE_LOG_ERROR, path_log, "Connection timeout!", DEBUG)

	# Start to send
	counts_loop = 0

	append_log(PRE_LOG_NORMAL, path_log, "Sending sample files ... ... ", DEBUG)

	for index in range(COUNTS_FILE):
		# Set sample file name
		path_file =  FILE_SAMPLE + str(counts_loop) + ".xml"

		if(os.path.exists(path_file) == False):
			append_log(PRE_LOG_ERROR, path_log, "File not found ... ... ", DEBUG)
			continue

		# Read sample file
		file_sample = open(path_file, "r")
		content_file_sample = file_sample.readlines()

		# Send
		rpc_srv = xmlrpclib.ServerProxy(SITE_RECEIVE, allow_none=True, verbose=False)
		result = rpc_srv.raw_handle(content_file_sample[0])

		counts_loop += 1

		if DEBUG == 1:
			append_log(PRE_LOG_NORMAL, path_log, "Send sample file: " + path_file + " (" + str(counts_loop) + "/" + str(COUNTS_FILE) + ")", DEBUG)

		# Hand shake
		try:
			response = urllib2.urlopen(SITE_CHECK, timeout = VALUE_TIMEOUT)
			value = response.read()

			if(cmp(value, content_file_sample[0]) != 0):
				append_log(PRE_LOG_ERROR, path_log, "Server-side data do not match.", DEBUG)

		except urllib2.URLError, e:
			append_log(PRE_LOG_ERROR, path_log, "Connection timeout!", DEBUG)
			return "Fail"

#		time.sleep(0)

		file_sample.close()

	append_log(PRE_LOG_NORMAL, path_log, "Pass", DEBUG)
	return "Pass"

# Testing
print "Start: " + DESCRIPTION  + " ... ...\t" + send_ecg_golden_samples()
