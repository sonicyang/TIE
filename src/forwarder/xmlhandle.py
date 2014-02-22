#!/usr/bin/env python
#
# Copyright (C) 2013 AcoMo Technology.
# All rights reserved.
#
# Authored by Jyun-Yu Huang <yillkid@acomotech.com>
#
# This is a bluetooth forwarder.
#
# AcoMo XML handler
# 
# Check/Handle AcoMo XML format

# XML
from  xml.dom  import  minidom
from xml.dom.minidom import parseString

def check_acomo_xml_format(data):
	if data == "":
		return 1

	if  data.count("<B>") != 1 or data.count("</B>") != 1:
		return 1

	return 0

def append_elem_to_root(data, tag_name, tag_value):
	doc = parseString(data)
	root = doc.documentElement

	tag_user = doc.createElement(tag_name)
	value = tag_value
	nameN = doc.createTextNode(value)

	tag_user.appendChild(nameN)
	root.appendChild(tag_user)

	# Header "<?xml version="1.0"?>" skip from char 22
	return doc.toxml()[22:]
