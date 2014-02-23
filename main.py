from xml.dom import minidom
import struct
import base64
import binascii
from xml.etree.ElementTree import parse
import datetime
import time
from bluetooth import *
import bluetooth
import subprocess

def getVLFp(vlf, tot):
	VLFp = (vlf / tot) * 100
	return VLFp
def getLFp(lv, tot):
	LFp  = lv / tot * 100
	return LFp
def getHFp(h, tot):
	HFp  = h / tot * 100
	return HFp
def getPreasure(l,h):
    Pre =  float(l) / float(l+h) * 100
    return Pre

globalTime = time.time()
RRIlist = []
timelist = []
SampleRate = 50
CHANNEL = 6
BUFFERSIZE = 64
ADDR_DEVICE = "8C:DE:52:0F:EE:0D"


def DecodeRaw(rawdata):
    raw = binascii.a2b_base64(rawdata)
    return raw, len(raw)

def xmlParse(xmlString):
    global RRIlist,timelist
    rawdata, HR, tag, Rpeak, HQ, F1, F2, Y = "","","","","","","",""
    xmlDoc = minidom.parseString(xmlString)
    try :
        HR = xmlDoc.getElementsByTagName('H')
        HR = int(HR[0].firstChild.data)
    except :
        HR = 0
    
    modulename = xmlDoc.getElementsByTagName('M') 
    rawdata = xmlDoc.getElementsByTagName('D')
    tag = xmlDoc.getElementsByTagName('T')
    Rpeak = xmlDoc.getElementsByTagName('P')
    HQ = xmlDoc.getElementsByTagName('S')
    F1 = xmlDoc.getElementsByTagName('F1')
    F2 = xmlDoc.getElementsByTagName('F2')
    Y = xmlDoc.getElementsByTagName('Y')
    smaplerate = xmlDoc.getElementsByTagName('R')
    
    raw, cnt = DecodeRaw(rawdata[0].firstChild.data)

    curTime = time.time()
    global globaltime
    dt = round((curTime-globalTime),3) 

    if HQ[0].firstChild.data == "1":
        RRI = ""
        try:
            RRI = xmlDoc.getElementsByTagName('I')
            if RRI != "":
                dataRRI = RRI[0].firstChild.data.split(',')
                for i in dataRRI:
                    RRIlist.append(float(i)/100)
                    timelist.append(dt)
                return True, raw, cnt, HR
        except:
            RRI = ""
    return False, raw, cnt, HR
    #Rate set to cnt/1 for convience

def OutputData():
    global RRIlist,timelist
    f = open('fft', 'w', encoding = 'ASCII')
    i=0
    for x in RRIlist:
        f.write(str(timelist[i]) + " "+ str(x) + "\n")
        i+=1
    f.close()

def lomb():
    global RRIlist,timelist
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

def DataReconstruct(xmls):
    xmls[0] = ""
    data = ""
    for x in xmls:
        data += x
    return data

def SplitData(data):
    xmls = data.split('</B>')
    for i in range(0,len(xmls)-1):
        xmls[i] += '</B>'
    if(data.endswith('</B>')):
        xmls[len(xmls)-1]+='</B>'
    return xmls

def CheckData(xmls, data):
    if(not(xmls[0].startswith('<B>'))):
        if(len(xmls) == 1):
            data = ""
        else:
            data = DataReconstruct(xmls)
    elif(xmls[0].endswith('</B>')):
        return True, data
    return False, data

def initFile():
    f = open("data/raw","w", encoding = "ASCII")
    f.write("")
    f.close()
    f = open("data/preasure","w", encoding = "ASCII")
    f.write("")
    f.close()

def WritePreasure(Pre):
    f = open("data/preasure", "w", encoding = "ASCII")
    f.write(str(Pre))
    f.close()

def WriteHR(HR):
    f = open("data/hr", "w", encoding = "ASCII")
    f.write(str(HR))
    f.close()


def AppendRaw(raw, rate, old_raw):
    out = ""
    x = 1
    for b in raw:
        out += ("{ \"x\": " +  str(x) + ",   \"y\": " + str(b) + "},")
        x+=1
    for b in old_raw:
        out += ("{ \"x\": " +  str(x) + ",   \"y\": " + str(b) + "},")
        x+=1
    out = out[:-1]

    f = open("data/raw", "w", encoding = "ASCII")
    f.write("[" + out + "]")
    f.close()
    f = open("data/rate", "w", encoding = "ASCII")
    f.write(str(rate))
    f.close()

def main():
    global RRIlist,timelist
    initFile()

    try:
        print("Connecting...")
        client_socket=BluetoothSocket( RFCOMM )
        client_socket.connect((ADDR_DEVICE, CHANNEL))

        data = ""
        R_cnt = 0
        old_raw = ""
        raw = ""

        while True:
            data += client_socket.recv(BUFFERSIZE).decode("ASCII")
            xmls = SplitData(data)
            ret, data = CheckData(xmls, data)
            if(ret):
                old_raw = raw

                print(xmls[0])

                RExist, raw, rate, HR = xmlParse(xmls[0])
                AppendRaw(raw, rate, old_raw)
                print(HR)
                if(HR > 0):
                    WriteHR(HR)
                if(RExist):
                    R_cnt += 1
                    print(R_cnt)
                if(R_cnt > SampleRate):
                    vl, l, h, tot = lomb()
                    print(vl,l,h,tot)
                    R_cnt = 0
                    RRIlist = RRIlist[SampleRate:]
                    timelist = timelist[SampleRate:]
                    WritePreasure(getPreasure(l,h))
                    print(getPreasure(l,h)) 

                data = DataReconstruct(xmls)
                    
    except IOError:
        pass
    client_socket.close()

main()
