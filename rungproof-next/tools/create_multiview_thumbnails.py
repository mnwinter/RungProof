"""Create context-free 2x2 model thumbnails from four independent review renders."""
from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageOps


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--size", type=int, default=720)
    args = parser.parse_args()

    count = 0
    cell = args.size // 2
    for asset_dir in sorted(path for path in args.source.iterdir() if path.is_dir()):
        review_dir = asset_dir / "review"
        images = sorted(review_dir.glob("*_0[1-4].png"))
        if len(images) != 4:
            continue
        canvas = Image.new("RGB", (args.size, args.size), (24, 28, 31))
        for index, path in enumerate(images):
            with Image.open(path) as source:
                view = ImageOps.fit(source.convert("RGB"), (cell, cell), method=Image.Resampling.LANCZOS)
            x = (index % 2) * cell
            y = (index // 2) * cell
            canvas.paste(view, (x, y))
        # Fine separators retain the four-view reading without adding identity context.
        for offset in (-1, 0, 1):
            for y in range(args.size):
                canvas.putpixel((cell + offset, y), (12, 14, 16))
            for x in range(args.size):
                canvas.putpixel((x, cell + offset), (12, 14, 16))
        canvas.save(asset_dir / "thumbnail.png", optimize=True)
        count += 1

    print("MULTIVIEW_THUMBNAILS", count)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
