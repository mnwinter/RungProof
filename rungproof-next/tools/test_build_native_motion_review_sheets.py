"""Offline synthetic evidence validation; no Godot/UI invocation."""
import json
from pathlib import Path
import tempfile
import unittest
from PIL import Image
from tools.build_native_motion_review_sheets import build, crop_bounds


class SheetChecks(unittest.TestCase):
    def fixture(self):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        root = Path(folder.name)
        records = [dict(kind='setup')]
        for i in range(3):
            records.append(dict(kind='frame', renderedFrame=i, reviewFrame=i, timeSeconds=i*.02,
                                scan=i, loadScreens=[dict(id='box_1', x=1000, y=600)]))
        records.append(dict(kind='end', completed=True, frames=3))
        (root/'trace.jsonl').write_text('\n'.join(json.dumps(row) for row in records))
        (root/'capture.log').write_text('NATIVE_MOTION_REVIEW_COMPLETE frames=3\n')
        for i in range(4):
            Image.new('RGB', (2400,1350), 'red' if i<2 else 'blue').save(root/f'frame{i:08d}.png')
        return root, records

    def write_trace(self, root, records):
        (root/'trace.jsonl').write_text('\n'.join(json.dumps(row) for row in records))

    def test_every_png_maps_trace_plus_extra_and_exact_duplicates(self):
        root,_ = self.fixture()
        result=build(root,root/'sheets',(650,400,1880,900))
        self.assertEqual(result['pngFrameCount'],4)
        self.assertEqual([r['id'] for r in result['sources']],list(range(4)))
        self.assertEqual([g['mappedFrames'] for g in result['scenePixelIdenticalGroups']],[[0,1],[2,3]])
        self.assertTrue(all(g['exactRgbByteEqualityVerified'] for g in result['scenePixelIdenticalGroups']))
        self.assertEqual(sum(s['tileCount'] for s in result['loadSheets']),4)
        self.assertIsNone(result['sources'][-1]['traceFrame'])
        self.assertIsNone(result['sources'][-1]['scan'])
        self.assertEqual(result['sources'][-1]['loadCrops'][0]['centerSource'],'LAST_TRACE_FOR_EXTRA_FRAME')
        self.assertFalse(result['accepted'])
        self.assertEqual(result['status'],'GENERATED_NOT_YET_VISUALLY_REVIEWED')
        # Native RGB pixels survive tile assembly without resampling.
        with Image.open(root/'sheets/loads-00000.png') as image:
            self.assertEqual(image.getpixel((100,132)),(255,0,0))

    def test_rectangular_native_tiles_and_custom_grid(self):
        root,_=self.fixture()
        result=build(root,root/'sheets',(650,400,1880,900),tile_width=300,tile_height=240,columns=5,rows=5)
        self.assertEqual(result['loadSheets'][0]['dimensions'],[1500,272])
        bounds=result['sources'][0]['loadCrops'][0]['sourceBoundsExclusiveRightBottom']
        self.assertEqual(bounds[2]-bounds[0],300)
        self.assertEqual(bounds[3]-bounds[1],240)

    def test_nonconsecutive_source_ids_rejected(self):
        root,_=self.fixture()
        (root/'frame00000001.png').rename(root/'frame00000005.png')
        with self.assertRaisesRegex(ValueError,'PNG IDs'):build(root,root/'sheets',(0,0,100,100))

    def test_missing_trace_frame_rejected(self):
        root,records=self.fixture()
        del records[2]
        self.write_trace(root,records)
        with self.assertRaisesRegex(ValueError,'COMPLETE trace'):build(root,root/'sheets',(0,0,100,100))

    def test_nonconsecutive_rendered_trace_rejected(self):
        root,records=self.fixture()
        records[2]['renderedFrame']=4
        self.write_trace(root,records)
        with self.assertRaisesRegex(ValueError,'continuous from zero'):build(root,root/'sheets',(0,0,100,100))

    def test_missing_load_coordinates_rejected(self):
        root,records=self.fixture()
        del records[2]['loadScreens']
        self.write_trace(root,records)
        with self.assertRaisesRegex(ValueError,'loadScreens'):build(root,root/'sheets',(0,0,100,100))

    def test_crop_near_edge_shifted_without_padding_or_pixel_clipping(self):
        bounds,shifted=crop_bounds(5,5)
        self.assertEqual(bounds,(0,0,400,400))
        self.assertTrue(shifted)
        with self.assertRaisesRegex(ValueError,'outside native'):crop_bounds(-1,100)
        root,_=self.fixture()
        with self.assertRaisesRegex(ValueError,'ROI'):build(root,root/'sheets',(-1,0,100,100))

    def test_incomplete_capture_rejected(self):
        root,records=self.fixture()
        records[-1]['completed']=False
        self.write_trace(root,records)
        with self.assertRaisesRegex(ValueError,'COMPLETE'):build(root,root/'sheets',(0,0,100,100))

    def test_unreadable_or_wrong_dimension_frame_rejected(self):
        root,_=self.fixture()
        Image.new('RGB',(1200,675)).save(root/'frame00000002.png')
        with self.assertRaisesRegex(ValueError,'native PNG'):build(root,root/'sheets',(0,0,100,100))


if __name__=='__main__':unittest.main()
