from xml.dom import minidom
import struct
import base64
import binascii
from xml.etree.ElementTree import parse
import datetime
import time
from bluetooth import *
import bluetooth
import numpy as np
import scipy
from scipy import signal
import matplotlib.pyplot as plt
import subprocess

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
timelist = []

def xmlParse(xmlString):
	#global RRIlist()
	rawdata, HR, tag, Rpeak, HQ, F1, F2, Y = "","","","","","","",""
	xmlDoc = minidom.parseString(xmlString)
	try :
		HR = xmlDoc.getElementsByTagName('H')
			
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
	if HQ[0].firstChild.data == "1":
		RRI = ""
		try:
			RRI = xmlDoc.getElementsByTagName('I')
			if RRI != "":
				dataRRI = RRI[0].firstChild.data.split(',')
				curTime = time.time()
				for i in dataRRI:
					global globalTime
					RRIlist.append(float(i)/100)
					timelist.append(round((curTime-globalTime),3))
		except:
			 RRI = ""


cnt = 0
def lomb(x, y):
    global cnt
    cnt += 1
    print(cnt)
    if(cnt > 100):
        cnt = 0
        f = open('fft', 'w', encoding = 'ASCII')
        i=0
        for x in RRIlist:
            f.write(str(timelist[i]) + " "+ str(x) + "\n")
            i+=1
        f.close()

        proc = subprocess.Popen(["./lomb", "fft"], stdout = subprocess.PIPE, stderr = subprocess.STDOUT)
        vlc = lc = hc = vl = l = h = 0
        for line in proc.stdout:
            c = line.decode("ASCII").split('\t')
            if(float(c[0]) <= 0.04):
                vlc += 1
                vl += float(c[1])
            elif(float(c[0]) > 0.04 and float(c[0]) <= 0.15):
                lc += 1
                l += float(c[1])
            elif(float(c[0]) > 0.15 and float(c[0]) <= 0.4):
                hc += 1
                h += float(c[1])

        proc.wait()

        vl /= vlc
        l /= lc
        h /= hc
        tot = vl + l + h

        return vl, l, h, tot
    else:
        return -1,-1,-1,-1

DEBUG = 0
CHANNEL = 6
BUFFERSIZE = 64
ADDR_DEVICE = "8C:DE:52:0F:EE:0D"
VALUE_TIMEOUT = 10

def main():
    try:
        print("Connecting...")
        client_socket=BluetoothSocket( RFCOMM )
        client_socket.connect((ADDR_DEVICE, CHANNEL))

        data = ""
        while True:
            data += client_socket.recv(BUFFERSIZE).decode("ASCII")
            xmls = data.split('</B>')
            for i in range(0,len(xmls)-1):
                xmls[i] += '</B>'
            if(data.endswith('</B>')):
                xmls[len(xmls)-1]+='</B>'
            if(not(xmls[0].startswith('<B>'))):
                if(len(xmls) == 1):
                    data = ""
                else:
                    xmls[0] = ""
                    data = ""
                    for x in xmls:
                        data += x
            elif(xmls[0].endswith('</B>')):
                #print(data)
                print(xmls[0])
                xmlParse(xmls[0])
                xmls[0] = ""
                data = ""
                for x in xmls:
                    data += x
                #print (RRIlist)
                #print (timelist)
                vl, l, h, tot = lomb(timelist, RRIlist)
                if (vl != -1):
                    print(vl,l,h,tot)
                    
    except IOError:
        pass


    client_socket.close()

main()
