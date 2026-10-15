"""Small AVI fixtures exercise boundaries, padding and completeness, not sampling."""
import io
import json
from pathlib import Path
import struct
import tempfile
import unittest
from PIL import Image
from tools.extract_native_movie_frames import extract


def chunk(kind, payload):
    return kind + struct.pack('<I', len(payload)) + payload + (b'\0' if len(payload) & 1 else b'')


def container(kind, payload):
    return chunk(b'LIST', kind + payload)


def riff(kind, payload):
    return chunk(b'RIFF', kind + payload)


def jpeg(color):
    output = io.BytesIO()
    Image.new('RGB', (8, 6), color).save(output, 'JPEG')
    return output.getvalue()


def header(total):
    main = bytearray(56)
    struct.pack_into('<I', main, 16, total)
    struct.pack_into('<II', main, 32, 8, 6)
    return container(b'hdrl', chunk(b'avih', main)
        + container(b'strl', chunk(b'strh', b'vidsMJPG' + bytes(48)))
        + container(b'strl', chunk(b'strh', b'auds' + bytes(52))))


def godot_short_index_fixture():
    movie = b''
    entries = b''
    for i in range(4):
        for kind, payload, flags in [(b'00db', jpeg('red'), 16), (b'01wb', b'audio!', 0)]:
            relative = 4 + len(movie)
            movie += chunk(kind, payload)
            entries += struct.pack('<4sIII', kind, flags, relative, len(payload) + (len(payload) & 1))
    data = bytearray(riff(b'AVI ', header(4) + container(b'movi', movie) + chunk(b'idx1', entries)))
    struct.pack_into('<I', data, 4, len(data) - 8 - 70)
    return bytes(data)


class ExtractionChecks(unittest.TestCase):
    def run_fixture(self, data, compatibility=False):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        root = Path(folder.name)
        source = root / 'movie.avi'
        source.write_bytes(data)
        result = extract(source, root / 'frames', compatibility)
        self.assertEqual(source.read_bytes(), data)
        return root, result

    def test_every_frame_original_bytes_audio_padding_and_rec_list(self):
        first, second = jpeg('red'), jpeg('blue')
        data = riff(b'AVI ', header(2) + container(b'movi',
            chunk(b'00dc', first) + chunk(b'01wb', b'\xff\xd8audio\xff\xd9')
            + chunk(b'JUNK', b'odd') + container(b'rec ', chunk(b'00dc', second))))
        root, result = self.run_fixture(data)
        self.assertEqual([f['id'] for f in result['frames']], [0, 1])
        self.assertEqual(result['frameCount'], 2)
        self.assertEqual(result['dimensions'], [8, 6])
        for frame, expected in zip(result['frames'], [first, second]):
            self.assertEqual((root / 'frames' / frame['file']).read_bytes(), expected)
            offset = frame['sourceOffset']
            self.assertEqual(data[offset:offset + frame['byteCount']], expected)
        self.assertEqual(json.loads((root / 'frames/manifest.json').read_text())['frameCount'], 2)

    def test_avix_continues_frame_ids(self):
        data = riff(b'AVI ', header(2) + container(b'movi', chunk(b'00dc', jpeg('red'))))
        data += riff(b'AVIX', container(b'movi', chunk(b'00dc', jpeg('blue'))))
        _, result = self.run_fixture(data)
        self.assertEqual([f['id'] for f in result['frames']], [0, 1])

    def test_rejects_missing_frame_even_with_valid_chunk_boundaries(self):
        data = riff(b'AVI ', header(2) + container(b'movi', chunk(b'00dc', jpeg('red'))))
        with self.assertRaisesRegex(ValueError, 'Missing/extra video frames'):
            self.run_fixture(data)

    def test_rejects_truncated_riff(self):
        data = riff(b'AVI ', header(1) + container(b'movi', chunk(b'00dc', jpeg('red'))))
        with self.assertRaisesRegex(ValueError, 'Truncated RIFF'):
            self.run_fixture(data[:-1])

    def test_rejects_nested_chunk_crossing_parent_boundary(self):
        bad = b'00dc' + struct.pack('<I', 1000) + b'abcd'
        data = riff(b'AVI ', header(1) + container(b'movi', bad))
        with self.assertRaisesRegex(ValueError, 'exceeds its LIST/RIFF boundary'):
            self.run_fixture(data)

    def test_rejects_missing_word_padding(self):
        bad = b'JUNK' + struct.pack('<I', 3) + b'odd'
        data = riff(b'AVI ', header(1) + container(b'movi', bad))
        with self.assertRaisesRegex(ValueError, 'exceeds its LIST/RIFF boundary'):
            self.run_fixture(data)

    def test_rejects_unreadable_frame(self):
        data = riff(b'AVI ', header(1) + container(b'movi', chunk(b'00dc', b'not JPEG')))
        with self.assertRaisesRegex(ValueError, 'Unreadable JPEG'):
            self.run_fixture(data)

    def test_rejects_dimension_mismatch(self):
        output = io.BytesIO()
        Image.new('RGB', (9, 6)).save(output, 'JPEG')
        data = riff(b'AVI ', header(1) + container(b'movi', chunk(b'00dc', output.getvalue())))
        with self.assertRaisesRegex(ValueError, 'advertised JPEG dimensions'):
            self.run_fixture(data)

    def test_explicit_godot_70_byte_index_compatibility(self):
        data = godot_short_index_fixture()
        with self.assertRaisesRegex(ValueError, 'exceeds its LIST/RIFF boundary'):
            self.run_fixture(data)
        _, result = self.run_fixture(data, compatibility=True)
        self.assertEqual(result['frameCount'], 4)
        self.assertEqual(result['compatibility']['indexedMovieChunks'], 8)

    def test_godot_compatibility_rejects_truncated_index(self):
        with self.assertRaises(ValueError):
            self.run_fixture(godot_short_index_fixture()[:-1], compatibility=True)

    def test_godot_compatibility_rejects_wrong_index_offset(self):
        data = bytearray(godot_short_index_fixture())
        index = data.index(b'idx1') + 8
        struct.pack_into('<I', data, index + 8, 99)
        with self.assertRaisesRegex(ValueError, 'idx1 entry 0'):
            self.run_fixture(bytes(data), compatibility=True)

    def test_godot_compatibility_rejects_missing_frame(self):
        data = bytearray(godot_short_index_fixture())
        main = data.index(b'avih') + 8
        struct.pack_into('<I', data, main + 16, 5)
        with self.assertRaisesRegex(ValueError, 'Missing/extra video frames'):
            self.run_fixture(bytes(data), compatibility=True)

    def test_godot_compatibility_rejects_truncated_movie_chunk(self):
        data = bytearray(godot_short_index_fixture())
        movie = data.index(b'movi') + 4
        struct.pack_into('<I', data, movie + 4, len(data))
        with self.assertRaisesRegex(ValueError, 'exceeds its LIST/RIFF boundary'):
            self.run_fixture(bytes(data), compatibility=True)


if __name__ == '__main__':
    unittest.main()
