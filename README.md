TIE
========
TIE -- Take It Easy is a mood monitor and changer base on HRV(Heart Rate Variablity) analysis.

We are using bOMDIC company's HeartWare device to collect user's Heart Informations in realtime.

We focus on user's R-R Interval, by doing frequency-domain analysis, we can obtain values related to user's mood.

After Obtaining user's mood, this Program will change screen color and music playlist accroding to user's mood.


We are using: 

  Device:
    bOMDIC HeartWave

  Python 3 & Python Library:
    PyBlueZ
  
  HTML5 and Javascript:
    d3.js
    jQuery
    
  External Program:
    RedShift
    gnome
    lomb

How to use:
  1. Build lomb.c
  2. Set your HeartWave's bluetooth MAC address in main.py
  2. run main.py to connect to your device
  3. run python -m SimpleHTTPServer under code's root dicectory
  4. open localhost:8000 in broswer
  
PS
  Youtube playlist link are hard-coded in index.html's javascript section.
  
  bOMDIC's offical site: http://www.bomdic.com/
