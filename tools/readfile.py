f = open('fft', 'r', encoding = 'ascii')

vl = 0
vlc = 0
l = 0
lc = 0
h = 0
hc = 0

for i in range(0,608):
    c = f.readline().split('\t')
    if(float(c[0]) <= 0.04):
        vlc += 1
        vl += float(c[1])
    elif(float(c[0]) > 0.04 and float(c[0]) <= 0.15):
        lc += 1
        l += float(c[1])
    elif(float(c[0]) > 0.15 and float(c[0]) <= 0.4):
        hc += 1
        h += float(c[1])

vl /= vlc
l /= lc
h /= hc
tot = vl + l + h
print(vl,l,h,tot)
