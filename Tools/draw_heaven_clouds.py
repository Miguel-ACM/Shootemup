"""Draw a seamless cloud relief atlas: height, two slopes, and fine wisps.
No generated artwork; all four channels come from a seeded periodic spectrum.
"""
from pathlib import Path
import numpy as np
from PIL import Image

root = Path(__file__).resolve().parents[1]
size = 512
rng = np.random.default_rng(51126)
f = np.fft.fftfreq(size)
kx, ky = np.meshgrid(f, f)
frequency = np.sqrt(kx*kx + ky*ky)
noise = rng.normal(size=(size,size))
power = np.maximum(frequency,1/size)**-1.75
power[0,0] = 0
height = np.fft.ifft2(np.fft.fft2(noise)*power).real
height = (height-height.mean())/height.std()
height = np.clip(.5+height*.145,0,1)
dx=(np.roll(height,-1,1)-np.roll(height,1,1))*14
dy=(np.roll(height,-1,0)-np.roll(height,1,0))*14
wisps=np.fft.ifft2(np.fft.fft2(noise)*np.exp(-(frequency/.13)**2)).real
wisps=np.clip(.5+wisps/(wisps.std()*5),0,1)
atlas=np.stack((height,np.clip(.5+dx,0,1),np.clip(.5+dy,0,1),wisps),axis=-1)
out=root/'Assets/Resources/HeavenCloudRelief.png'
Image.fromarray(np.uint8(atlas*255),'RGBA').save(out)
print(out)
