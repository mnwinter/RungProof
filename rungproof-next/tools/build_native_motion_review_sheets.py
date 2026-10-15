"""Build native-pixel sheets for COMPLETE PNG captures; generation is not review."""
from __future__ import annotations
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
from PIL import Image, ImageDraw, ImageFont

NATIVE_SIZE = (2400, 1350)
LABEL_HEIGHT = 32


def sha256_file(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def crop_bounds(x, y, size=400, native_size=NATIVE_SIZE):
    if not math.isfinite(x) or not math.isfinite(y) or not (0 <= x < native_size[0] and 0 <= y < native_size[1]):
        raise ValueError('Load center is outside native image or nonfinite')
    width, height = (size, size) if isinstance(size, int) else size
    if not (0 < width <= native_size[0] and 0 < height <= native_size[1]):
        raise ValueError('Load crop dimensions must fit native image')
    wanted_x, wanted_y = math.floor(x - width / 2), math.floor(y - height / 2)
    left = max(0, min(native_size[0] - width, wanted_x))
    top = max(0, min(native_size[1] - height, wanted_y))
    return (left, top, left + width, top + height), (left != wanted_x or top != wanted_y)


def validate_case(case):
    trace_path = case / 'trace.jsonl'
    records = [json.loads(line) for line in trace_path.read_text(encoding='utf-8-sig').splitlines() if line.strip()]
    frames = [row for row in records if row.get('kind') == 'frame']
    endings = [row for row in records if row.get('kind') == 'end']
    if not frames or len(endings) != 1 or endings[0].get('completed') is not True or endings[0].get('frames') != len(frames):
        raise ValueError('Requires exactly one COMPLETE trace end matching every trace frame')
    log = (case / 'capture.log').read_text(encoding='utf-8-sig')
    complete = re.findall(r'NATIVE_MOTION_REVIEW_COMPLETE frames=(\d+)', log)
    if complete != [str(len(frames))]:
        raise ValueError('Requires capture.log COMPLETE matching trace frame count')
    for i, row in enumerate(frames):
        if row.get('reviewFrame') != i or row.get('renderedFrame') != i:
            raise ValueError('Trace review/rendered frame IDs must be continuous from zero')
    names = sorted(case.glob('frame*.png'))
    if [path.name for path in names] != [f'frame{i:08d}.png' for i in range(len(frames) + 1)]:
        raise ValueError('PNG IDs must be continuous from zero with exactly trace count + 1')
    return frames, names


class Sheets:
    def __init__(self, folder, prefix, tile_size, columns, rows):
        self.folder, self.prefix = folder, prefix
        self.width, self.height = tile_size
        self.columns, self.rows = columns, rows
        self.pending = []
        self.generated = []
        font_path = Path('C:/Windows/Fonts/arial.ttf')
        self.font = ImageFont.truetype(str(font_path), 12) if font_path.exists() else ImageFont.load_default()

    def add(self, pixels, label, record):
        sheet_number = len(self.generated)
        index = len(self.pending)
        record['sheet'] = f'{self.prefix}-{sheet_number:05d}.png'
        record['tile'] = index
        self.pending.append((pixels.copy(), label, record))
        if len(self.pending) == self.columns * self.rows:
            self.flush()

    def flush(self):
        if not self.pending:
            return
        rows = math.ceil(len(self.pending) / self.columns)
        canvas = Image.new('RGB', (self.columns * self.width, rows * (self.height + LABEL_HEIGHT)), '#18232a')
        draw = ImageDraw.Draw(canvas)
        for i, (pixels, label, _) in enumerate(self.pending):
            x = (i % self.columns) * self.width
            y = (i // self.columns) * (self.height + LABEL_HEIGHT)
            draw.multiline_text((x + 4, y + 2), label, fill='white', font=self.font, spacing=0)
            canvas.paste(pixels, (x, y + LABEL_HEIGHT))  # Native pixels, no resize/resampling.
        name = f'{self.prefix}-{len(self.generated):05d}.png'
        canvas.save(self.folder / name)
        self.generated.append(dict(file=name, tileCount=len(self.pending), dimensions=list(canvas.size),
                                   visualReviewStatus='NOT_YET_VISUALLY_REVIEWED'))
        self.pending.clear()


def build(case, output, roi, loads=True, scene=True, tile_width=400, tile_height=400, columns=3, rows=3):
    case, output = Path(case).resolve(), Path(output).resolve()
    if not loads and not scene:
        raise ValueError('Select loads and/or scene')
    if len(roi) != 4 or not (0 <= roi[0] < roi[2] <= NATIVE_SIZE[0] and 0 <= roi[1] < roi[3] <= NATIVE_SIZE[1]):
        raise ValueError('Fixed geometry ROI must fit inside native image')
    if columns < 1 or rows < 1:
        raise ValueError('Sheet columns and rows must be positive')
    crop_bounds(0, 0, (tile_width, tile_height))
    trace, originals = validate_case(case)
    # Reject missing coordinates before producing partial load sheets.
    if loads:
        for row in trace:
            positions = row.get('loadScreens')
            if not isinstance(positions, list) or not positions:
                raise ValueError('Every traced frame requires loadScreens for load sheets')
            if len({p['id'] for p in positions}) != len(positions):
                raise ValueError('Duplicate load IDs in trace')
            for position in positions:
                crop_bounds(position['x'], position['y'], (tile_width, tile_height))
    if output.exists() and any(output.iterdir()):
        raise ValueError('Output must be absent or empty; preserve existing evidence')
    output.mkdir(parents=True, exist_ok=True)
    load_sheets = Sheets(output, 'loads', (tile_width, tile_height), columns, rows) if loads else None
    scene_sheets = Sheets(output, 'scene', (roi[2] - roi[0], roi[3] - roi[1]), 1, 3) if scene else None
    source_records, groups, hash_groups = [], [], {}
    for i, source in enumerate(originals):
        metadata = trace[i] if i < len(trace) else trace[-1]
        extra = i == len(trace)
        label = (f'f{i} t={metadata["timeSeconds"]:.2f}s scan={metadata["scan"]}'
                 + (' EXTRA' if extra else ''))
        record = dict(id=i, file=source.name, sha256=sha256_file(source),
                      traceFrame=None if extra else metadata['renderedFrame'],
                      timeSeconds=None if extra else metadata['timeSeconds'],
                      scan=None if extra else metadata['scan'],
                      finalExtraUntraced=extra, loadCrops=[])
        with Image.open(source) as image:
            if image.format != 'PNG' or image.size != NATIVE_SIZE:
                raise ValueError(f'{source.name} must be readable native PNG {NATIVE_SIZE}')
            image.load()
            native = image.convert('RGB')
            if loads:
                for position in metadata['loadScreens']:
                    bounds, shifted = crop_bounds(position['x'], position['y'], (tile_width, tile_height))
                    crop_record = dict(loadId=position['id'], sourceBoundsExclusiveRightBottom=list(bounds),
                                       centeredCropShiftedToImageBoundary=shifted,
                                       centerSource='LAST_TRACE_FOR_EXTRA_FRAME' if extra else 'ACTUAL_TRACE_LOAD_SCREEN')
                    load_sheets.add(native.crop(bounds), label + f'\n{position["id"]} {bounds}', crop_record)
                    record['loadCrops'].append(crop_record)
            if scene:
                crop = native.crop(roi)
                pixels = crop.tobytes()
                digest = hashlib.sha256(pixels).hexdigest()
                group_id = None
                # SHA chooses candidates only. Exact RGB equality proves every
                # duplicate mapping against a source-PNG representative.
                for candidate in hash_groups.get(digest, []):
                    representative = originals[groups[candidate]['representativeFrame']]
                    with Image.open(representative) as previous:
                        if previous.convert('RGB').crop(roi).tobytes() == pixels:
                            group_id = candidate
                            break
                if group_id is None:
                    group_id = len(groups)
                    group = dict(representativeFrame=i, mappedFrames=[], sha256=digest,
                                 exactRgbByteEqualityVerified=True, rgbByteCount=len(pixels))
                    groups.append(group)
                    hash_groups.setdefault(digest, []).append(group_id)
                    scene_sheets.add(crop, label + f'\nROI={tuple(roi)}', group)
                groups[group_id]['mappedFrames'].append(i)
                record['sceneRepresentativeFrame'] = groups[group_id]['representativeFrame']
        source_records.append(record)
    if loads:
        load_sheets.flush()
    if scene:
        scene_sheets.flush()
    manifest = dict(sourceCase=str(case), nativeDimensions=list(NATIVE_SIZE),
                    traceSha256=sha256_file(case / 'trace.jsonl'), traceFrameCount=len(trace),
                    pngFrameCount=len(originals), continuousSourceIds=True,
                    fixedGeometryRoiExclusiveRightBottom=list(roi),
                    modes=dict(loads=loads, scene=scene),
                    loadSheetLayout=dict(tileWidth=tile_width, tileHeight=tile_height, columns=columns, rows=rows, labelHeight=LABEL_HEIGHT),
                    status='GENERATED_NOT_YET_VISUALLY_REVIEWED', accepted=False,
                    method='Native RGB crops on labeled PNG sheets; no resizing; exact byte duplicate proof',
                    finalExtraFramePolicy='Final PNG is included. Its load crop uses last traced screen centers explicitly; scene ROI uses its actual native pixels. No extra time/scan is invented.',
                    sources=source_records, scenePixelIdenticalGroups=groups,
                    loadSheets=load_sheets.generated if loads else [],
                    sceneSheets=scene_sheets.generated if scene else [])
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    return manifest


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('case', type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--roi', required=True, help='Fixed full-geometry ROI x,y,x2,y2 (exclusive right/bottom)')
    parser.add_argument('--loads', action='store_true')
    parser.add_argument('--scene', action='store_true')
    parser.add_argument('--tile-width', type=int, default=400)
    parser.add_argument('--tile-height', type=int, default=400)
    parser.add_argument('--columns', type=int, default=3)
    parser.add_argument('--rows', type=int, default=3)
    args = parser.parse_args()
    roi = tuple(int(value) for value in args.roi.split(','))
    result = build(args.case, args.output or args.case / 'review-sheets', roi,
                   args.loads or not args.scene, args.scene or not args.loads,
                   args.tile_width, args.tile_height, args.columns, args.rows)
    print(json.dumps({key: result[key] for key in ['status', 'accepted', 'traceFrameCount', 'pngFrameCount', 'modes']}, indent=2))
