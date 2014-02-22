from xml.dom import minidom
#from lxml import objectify
import struct
import base64
import binascii
from xml.etree.ElementTree import parse
import datetime
import time

hrvlist = "1.0;2.01;3.02;4.05;3.02394"
hrvstr = hrvlist.split(';')
def getVLFp():
	VLFp = ((float(hrvstr[1]) + float(hrvstr[2])) / float(hrvstr[0])) * 100
	return VLFp
def getLFp():
	LFp  = float(hrvstr[3]) / float(hrvstr[0]) * 100
	return LFp
def getHFp():
	HFp  = float(hrvstr[4]) / float(hrvstr[0]) * 100
	return HFp
globalTime = time.time()
RRIlist = []
cnt = 0
timelist = []
def xmlParse(xmlString):
	#global RRIlist()
	rawdata, HR, tag, Rpeak, HQ, F1, F2, Y = "","","","","","","",""
	xmlDoc = minidom.parseString(xmlString)#objectify.fromstring(xml)
	try :
		HR = xmlDoc.getElementsByTagName('H')
		if HR != "":
			print (HR[0].firstChild.data)
			
	except :
		HR = "0"
	modulename = xmlDoc.getElementsByTagName('M') 
	rawdata = xmlDoc.getElementsByTagName('D')
	tag = xmlDoc.getElementsByTagName('T')
	Rpeak = xmlDoc.getElementsByTagName('P')
	HQ = xmlDoc.getElementsByTagName('S')
	F1 = xmlDoc.getElementsByTagName('F1')
	F2 = xmlDoc.getElementsByTagName('F2')
	Y = xmlDoc.getElementsByTagName('Y')
	smaplerate = xmlDoc.getElementsByTagName('R')
#	print (HQ[0].firstChild.data)
	if HQ[0].firstChild.data == "1":
		RRI = ""
		try:
			RRI = xmlDoc.getElementsByTagName('I')
			if RRI != "":
				dataRRI = RRI[0].firstChild.data.split(',')
				curTime = time.time()
				for i in dataRRI:
					global globalTime
					#print (curTime-globalTime)
					RRIlist.append(i)
					timelist.append(str(round((curTime-globalTime)/1000,6)))
				
		#		print(dataRRI)
		except:
			 RRI = ""
#print(getVLFp(),getLFp(),getHFp())
string = "<B><E><M>B57A7</M><R>255</R><D>goKBgYKCgICBgYGBg4SDg4SFhIOEhYOBgoSEg4SGhoWFh4qIh4eIh4aGiIiHhoiIh4eJiYiIiYqJiYqMjI2Oj4+OjY2Mi4qKi4mJiYmIiImKiYmJiomIiYqLiomKi4uNkpulscDMz8WxlHRiYGdvdXp/goOFh4qKi4yOjo6PkJGPj5CRkZCSlJOUlpeXl5iZmZmZmpubnJ6gn5+foJ6dnZ+cmpqamZeXl5mXlpaWlZKRkZCOjI6Oi4qKiomIiImIh4aJiIeFh4iHh4mKiIeIioiHiIqJh4aIiIaFhoaDgYGDg4GCg4GAfn9+fHx/fnx6fH17ent8enh5e3p7fH59</D><S>1</S><Z>4</Z><T>856</T><H>85</H><I>196,193</I><P>­91,102</P></E><USER>5207588497703702855</USER><TIMESTAMP>1</TIMESTAMP></B>"
xmlParse(string)
print (RRIlist)
print (timelist)
#target = "goKBgYKCgICBgYGBg4SDg4SFhIOEhYOBgoSEg4SGhoWFh4qIh4eIh4aGiIiHhoiIh4eJiYiIiYqJiYqMjI2Oj4+OjY2Mi4qKi4mJiYmIiImKiYmJiomIiYqLiomKi4uNkpulscDMz8WxlHRiYGdvdXp/goOFh4qKi4yOjo6PkJGPj5CRkZCSlJOUlpeXl5iZmZmZmpubnJ6gn5+foJ6dnZ+cmpqamZeXl5mXlpaWlZKRkZCOjI6Oi4qKiomIiImIh4aJiIeFh4iHh4mKiIeIioiHiIqJh4aIiIaFhoaDgYGDg4GCg4GAfn9+fHx/fnx6fH17ent8enh5e3p7fH59"

#c , d = hello(string)
#print (c,d)
#dtgt = binascii.a2b_base64(target)
#for i in dtgt:
#	print (str(dtgt[i]))
#print (dtgt.("ASCII"))
#format = ">ff"
#for i in range(100):
#    print (struct.unpack_from(format,dtgt,8*i))
