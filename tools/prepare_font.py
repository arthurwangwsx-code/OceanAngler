#!/usr/bin/env python3
"""Build/verify OceanUI from the pinned OFL-licensed Noto Sans SC source."""
import argparse,hashlib,json,urllib.request
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
COMMIT='a85815a42757630ce188fdad368c2dfc444d4773'
SOURCE_HASH='a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da'
LICENSE_HASH='1c05c68c34f9708415aada51f17e1b0092d2cea709bf4a94cd38114f9e73d7d9'
BASE='https://raw.githubusercontent.com/google/fonts/'+COMMIT+'/ofl/notosanssc/'
DEST=ROOT/'game/Assets/OceanAngler/Resources/UIFont.ttf'
NOTICE=ROOT/'art/licenses/NotoSansSC-OFL.txt'
RECORD=ROOT/'art/licenses/font-source.json'

def digest(data):return hashlib.sha256(data).hexdigest()
def characters():
    text=''.join(p.read_text() for p in (ROOT/'game/Assets/OceanAngler').rglob('*') if p.suffix in ('.cs','.json'))
    text+=NOTICE.read_text();return set(range(32,127))|{ord(c) for c in text if ord(c)>127}
def fetch(path,url,expected):
    if path.exists() and digest(path.read_bytes())==expected:return path.read_bytes()
    with urllib.request.urlopen(url,timeout=60) as response:data=response.read()
    if digest(data)!=expected:raise RuntimeError('Pinned upstream hash mismatch')
    path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data);return data

def main():
    from fontTools.ttLib import TTFont
    from fontTools import subset
    from fontTools.varLib.instancer import instantiateVariableFont
    parser=argparse.ArgumentParser();parser.add_argument('--verify',action='store_true');args=parser.parse_args()
    if args.verify:
        record=json.loads(RECORD.read_text());font=TTFont(DEST);missing=characters()-set(font.getBestCmap())
        if missing or digest(DEST.read_bytes())!=record['subsetSha256']:raise RuntimeError('Font coverage or hash mismatch')
        print(json.dumps(dict(status='PASS',characters=len(characters()),license='OFL-1.1')));return
    source=ROOT/'.local/fonts/NotoSansSC.ttf'
    fetch(source,BASE+'NotoSansSC%5Bwght%5D.ttf',SOURCE_HASH);license_bytes=fetch(NOTICE,BASE+'OFL.txt',LICENSE_HASH)
    font=TTFont(source,recalcTimestamp=False)
    if 'fvar' in font:font=instantiateVariableFont(font,{'wght':500},inplace=True)
    options=subset.Options();options.layout_features=['*'];engine=subset.Subsetter(options=options);engine.populate(unicodes=characters());engine.subset(font)
    for name in font['name'].names:
        if name.nameID in (1,4,6):name.string='OceanUI'.encode(name.getEncoding())
    DEST.parent.mkdir(parents=True,exist_ok=True);font.save(DEST)
    bundled=DEST.parent/'Legal/NotoSansSC-OFL.txt';bundled.parent.mkdir(exist_ok=True);bundled.write_bytes(license_bytes)
    record=dict(source='https://github.com/google/fonts',commit=COMMIT,license='SIL Open Font License 1.1',modified='Weight 500 subset for application strings; renamed OceanUI',sourceSha256=SOURCE_HASH,subsetSha256=digest(DEST.read_bytes()),licenseSha256=LICENSE_HASH,bundledNoticeResource=str(bundled.relative_to(ROOT)),characters=len(characters()),bytes=DEST.stat().st_size)
    RECORD.write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(record))
if __name__=='__main__':main()
