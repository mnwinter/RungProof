"""Extract native MJPEG AVI frames unchanged; validate RIFF structure and counts."""
from __future__ import annotations
import argparse
from dataclasses import dataclass
import hashlib
import io
import json
from pathlib import Path
import struct
from typing import BinaryIO, Iterator
from PIL import Image


@dataclass(frozen=True)
class Chunk:
    kind: bytes
    offset: int
    size: int
    parents: tuple[bytes, ...]
    movi_base: int | None = None


def _read(stream: BinaryIO, offset: int, count: int) -> bytes:
    stream.seek(offset)
    data = stream.read(count)
    if len(data) != count:
        raise ValueError(f"Truncated data at offset {offset}: expected {count}, got {len(data)}")
    return data


def _chunks(stream: BinaryIO, start: int, end: int, parents: tuple[bytes, ...], movi_base: int | None = None, short_index_end: int | None = None) -> Iterator[Chunk]:
    cursor = start
    while cursor < end:
        if end - cursor < 8:
            raise ValueError(f"Truncated chunk header at {cursor}")
        kind, size = struct.unpack('<4sI', _read(stream, cursor, 8))
        payload = cursor + 8
        padded_end = payload + size + (size & 1)
        if padded_end > end:
            # Explicit compatibility for the observed finalized Godot artifact:
            # only its trailing idx1 crosses RIFF by exactly 70 bytes. Nested
            # LIST/movi boundaries remain strict, including every video frame.
            if not (short_index_end is not None and parents == (b'AVI ',)
                    and kind == b'idx1' and size % 16 == 0
                    and padded_end == short_index_end and padded_end - end == 70):
                raise ValueError(f"Chunk {kind!r} at {cursor} exceeds its LIST/RIFF boundary")
        if kind == b'LIST':
            if size < 4:
                raise ValueError(f"LIST missing its type at {cursor}")
            subtype = _read(stream, payload, 4)
            yield from _chunks(stream, payload + 4, payload + size, parents + (subtype,),
                               payload if subtype == b'movi' else movi_base)
        else:
            yield Chunk(kind, payload, size, parents, movi_base)
        # RIFF word padding is structural, never searched for JPEG markers.
        if size & 1:
            _read(stream, payload + size, 1)
        cursor = padded_end


def avi_chunks(stream: BinaryIO, length: int, allow_godot_short_riff_index: bool = False) -> Iterator[Chunk]:
    cursor = 0
    segment = 0
    while cursor < length:
        if length - cursor < 12:
            raise ValueError(f"Truncated RIFF segment at {cursor}")
        kind, size, subtype = struct.unpack('<4sI4s', _read(stream, cursor, 12))
        if kind != b'RIFF' or subtype != (b'AVI ' if segment == 0 else b'AVIX') or size < 4:
            raise ValueError(f"Unsupported RIFF segment at {cursor}: {kind!r}/{subtype!r}")
        end = cursor + 8 + size
        padded_end = end + (size & 1)
        if padded_end > length:
            raise ValueError(f"Truncated RIFF segment at {cursor}: declared end {padded_end}, file {length}")
        compatible_end = length if (allow_godot_short_riff_index and segment == 0
                                    and length - padded_end == 70) else None
        last = None
        for item in _chunks(stream, cursor + 12, end, (subtype,), short_index_end=compatible_end):
            last = item
            yield item
        if compatible_end is not None:
            if last is None or last.kind != b'idx1' or last.offset + last.size != length:
                raise ValueError('Godot compatibility requires an exact trailing idx1 boundary')
            padded_end = length
        if size & 1:
            _read(stream, end, 1)
        cursor = padded_end
        segment += 1
    if not segment:
        raise ValueError('No AVI RIFF segment')


def extract(source: Path, destination: Path, allow_godot_short_riff_index: bool = False) -> dict:
    source = source.resolve()
    destination = destination.resolve()
    before = source.stat()
    expected = None
    open_dml_count = None
    video_streams = []
    stream_number = -1
    dimensions = None
    with source.open('rb') as stream:
        indexed_chunks = []
        index = None
        for chunk in avi_chunks(stream, before.st_size, allow_godot_short_riff_index):
            if b'movi' in chunk.parents and chunk.kind[2:] in (b'dc', b'db', b'wb'):
                indexed_chunks.append(chunk)
            if chunk.kind == b'idx1':
                index = chunk
            if chunk.kind == b'avih':
                if chunk.size < 40:
                    raise ValueError('Truncated AVI main header')
                data = _read(stream, chunk.offset, 40)
                expected = struct.unpack_from('<I', data, 16)[0]
                dimensions = struct.unpack_from('<II', data, 32)
            elif chunk.kind == b'dmlh':
                if chunk.size < 4:
                    raise ValueError('Truncated OpenDML header')
                open_dml_count = struct.unpack('<I', _read(stream, chunk.offset, 4))[0]
            elif chunk.kind == b'strh' and b'strl' in chunk.parents:
                stream_number += 1
                if chunk.size < 8:
                    raise ValueError('Truncated stream header')
                kind, codec = struct.unpack('<4s4s', _read(stream, chunk.offset, 8))
                if kind == b'vids':
                    if codec.upper() not in (b'MJPG', b'JPEG'):
                        raise ValueError(f'Unsupported video codec {codec!r}')
                    video_streams.append(stream_number)
        if len(video_streams) != 1:
            raise ValueError(f'Expected exactly one MJPEG video stream, found {video_streams}')
        expected = open_dml_count or expected
        if not expected or not dimensions or min(dimensions) <= 0:
            raise ValueError('Missing finalized positive frame count/dimensions')
        if allow_godot_short_riff_index:
            if index is None or index.size != len(indexed_chunks) * 16:
                raise ValueError('Godot compatibility requires complete video/audio idx1 entries')
            entries = _read(stream, index.offset, index.size)
            for i, actual in enumerate(indexed_chunks):
                kind, flags, relative, size = struct.unpack_from('<4sIII', entries, i * 16)
                if (kind != actual.kind or size != actual.size + (actual.size & 1) or actual.movi_base is None
                        or relative != actual.offset - 8 - actual.movi_base):
                    raise ValueError(f'idx1 entry {i} does not match actual movie chunk offset/size')
        destination.mkdir(parents=True, exist_ok=True)
        if any(destination.glob('frame*.jpg')) or (destination / 'manifest.json').exists():
            raise ValueError('Destination already contains extracted frames/manifest')
        frames = []
        video_prefix = f'{video_streams[0]:02d}'.encode('ascii')
        for chunk in avi_chunks(stream, before.st_size, allow_godot_short_riff_index):
            if b'movi' not in chunk.parents:
                continue
            if len(chunk.kind) != 4 or chunk.kind[2:] not in (b'dc', b'db'):
                continue  # Audio/JUNK/index/palette chunks are not video frames.
            if chunk.kind[:2] != video_prefix:
                raise ValueError(f'Unexpected video stream chunk {chunk.kind!r}')
            payload = _read(stream, chunk.offset, chunk.size)
            try:
                with Image.open(io.BytesIO(payload)) as image:
                    if image.format != 'JPEG' or image.size != dimensions:
                        raise ValueError(f'Frame {len(frames)} differs from advertised JPEG dimensions {dimensions}')
                    image.load()  # Validate readability only; write original bytes below.
            except (OSError, SyntaxError) as exc:
                raise ValueError(f'Unreadable JPEG frame {len(frames)} at {chunk.offset}') from exc
            name = f'frame{len(frames):08d}.jpg'
            (destination / name).write_bytes(payload)
            frames.append(dict(id=len(frames), file=name, sourceOffset=chunk.offset,
                               byteCount=chunk.size, sha256=hashlib.sha256(payload).hexdigest(),
                               width=dimensions[0], height=dimensions[1]))
        if len(frames) != expected:
            raise ValueError(f'Missing/extra video frames: header declares {expected}, parsed {len(frames)}')
    after = source.stat()
    if (before.st_size, before.st_mtime_ns) != (after.st_size, after.st_mtime_ns):
        raise ValueError('Source changed during extraction; capture must be finalized')
    digest = hashlib.sha256()
    with source.open('rb') as stream:
        for data in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(data)
    final = source.stat()
    if (before.st_size, before.st_mtime_ns) != (final.st_size, final.st_mtime_ns):
        raise ValueError('Source changed during hashing')
    report = dict(source=str(source), sourceByteCount=before.st_size, sourceSha256=digest.hexdigest(),
                  method='RIFF AVI/AVIX LIST/movi chunk parsing; original JPEG payload bytes, no recompression',
                  compatibility=dict(allowGodotShortRiffIndex=allow_godot_short_riff_index,
                      indexedMovieChunks=len(indexed_chunks),
                      note='Explicit 70-byte trailing idx1 RIFF discrepancy; every video/audio index offset and padded chunk size validated' if allow_godot_short_riff_index else 'Strict RIFF boundaries'),
                  advertisedFrameCount=expected, frameCount=len(frames), continuousIds=True,
                  dimensions=list(dimensions), frames=frames)
    (destination / 'manifest.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('destination', type=Path)
    parser.add_argument('--allow-godot-short-riff-index', action='store_true',
                        help='Allow only observed 70-byte trailing idx1 discrepancy after complete index validation')
    args = parser.parse_args()
    result = extract(args.source, args.destination, args.allow_godot_short_riff_index)
    print(json.dumps({k: v for k, v in result.items() if k != 'frames'}, indent=2))
