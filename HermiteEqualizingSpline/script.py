import matplotlib.pyplot as plt
import numpy as np
import pylab
import os
import sys
import struct
import math

xp = []
yp = []

xs = []
ys = []

with open("Output.txt","r") as f:
    xp = f.readline()[:-2].replace(',','.')
    yp = f.readline()[:-2].replace(',','.')
    xs = f.readline()[:-2].replace(',','.')
    ys = f.readline()[:-2].replace(',','.')

xp=list(map(float, xp.split(' ')))
yp=list(map(float, yp.split(' ')))
xs=list(map(float, xs.split(' ')))
ys=list(map(float, ys.split(' ')))

plt.plot(xp, yp, 'bo')
plt.plot(xp, yp, 'b', label='line')
plt.plot(xs, ys, 'r', label='spline')

plt.legend()

plt.show()