#!/usr/bin/env python3
"""原创合成水下底音与轻柔探索主题；不读取外部音频或个人素材。"""
import argparse,array,hashlib,json,math,random,wave
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'game/Assets/OceanAngler/Resources/Audio'
RATE=22050

def bed(seconds=12):
    n=RATE*seconds;fade=RATE//2;channels=[]
    for seed in (41,73):
        rng=random.Random(seed);slow=mid=fast=0.;raw=array.array('f')
        for _ in range(n+fade):
            noise=rng.uniform(-1,1);slow+=(noise-slow)*.004;mid+=(noise-mid)*.06;fast+=(noise-fast)*.25
            raw.append(.65*slow+.25*(mid-slow)+.1*(fast-mid))
        for i in range(fade):
            t=(1-math.cos(math.pi*i/fade))*.5;raw[i]=raw[n+i]*(1-t)+raw[i]*t
        channel=array.array('f',(raw[i]*(.65+.2*math.sin(4*math.pi*i/n)+.1*math.sin(6*math.pi*i/n)) for i in range(n)))
        boundary=(channel[0]+channel[-1])*.5
        for i in range(RATE//50):
            t=i/(RATE//50-1);weight=t*t*(3-2*t)
            channel[i]=boundary*(1-weight)+channel[i]*weight;channel[-1-i]=boundary*(1-weight)+channel[-1-i]*weight
        channels.append(channel)
    return channels

def theme(seconds=32):
    n=RATE*seconds;channels=[array.array('f',[0])*n for _ in range(2)]
    notes=(60,64,67,69,67,64,62,60,57,60,64,67,64,62,60,55)
    for index,note in enumerate(notes):
        frequency=440*2**((note-69)/12);start=index*RATE*2;pan=math.sin(index*.7)*.25
        for sample in range(RATE*3):
            t=sample/RATE;phase=math.tau*frequency*t
            attack=min(1,t/.025);envelope=attack*math.exp(-2.4*t)
            value=(math.sin(phase)+.28*math.sin(phase*2.01)*math.exp(-4*t)+.08*math.sin(phase*3.99)*math.exp(-6*t))*envelope
            position=(start+sample)%n
            channels[0][position]+=value*(1-pan);channels[1][position]+=value*(1+pan)
    return channels

def write(name,channels,peak):
    maximum=max(max(abs(x) for x in channel) for channel in channels);pcm=array.array('h')
    for frame in range(len(channels[0])):
        for channel in channels:pcm.append(round(channel[frame]/maximum*peak*32767))
    if __import__('sys').byteorder!='little':pcm.byteswap()
    with wave.open(str(DEST/name),'wb') as output:
        output.setparams((2,2,RATE,len(channels[0]),'NONE','not compressed'));output.writeframes(pcm.tobytes())

def inspect(name,seconds):
    path=DEST/name
    with wave.open(str(path),'rb') as audio:
        assert (audio.getnchannels(),audio.getsampwidth(),audio.getframerate(),audio.getnframes())==(2,2,RATE,seconds*RATE)
        pcm=array.array('h',audio.readframes(audio.getnframes()))
    if __import__('sys').byteorder!='little':pcm.byteswap()
    peak=max(abs(x) for x in pcm)/32768;rms=math.sqrt(sum((x/32768)**2 for x in pcm)/len(pcm));seam=max(abs(pcm[c]-pcm[-2+c])/32768 for c in (0,1))
    assert .01<rms<.3 and .1<peak<.7 and seam<.02,(name,peak,rms,seam)
    return dict(path=str(path.relative_to(ROOT)),seconds=seconds,channels=2,sampleRate=RATE,peak=peak,rms=rms,seamDelta=seam,sha256=hashlib.sha256(path.read_bytes()).hexdigest())

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--verify',action='store_true');args=parser.parse_args()
    if not args.verify:
        DEST.mkdir(parents=True,exist_ok=True);write('OceanBed.wav',bed(),.35);write('ExploreTheme.wav',theme(),.42)
    print(json.dumps(dict(status='PASS',scope='PCM structure/levels/loop-boundary; not listening acceptance',clips=[inspect('OceanBed.wav',12),inspect('ExploreTheme.wav',32)]),indent=2))
if __name__=='__main__':main()
