"""Create labeled contact sheets for a migrated-scene visual QA pass."""
import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
THUMBNAIL = (600, 338)
LABEL_HEIGHT = 42
COLS = 4
ROWS = 2


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=ROOT / "build" / "migrated-scenes")
    args = parser.parse_args()
    source = args.source.resolve()
    output = source / "contact-sheets"
    output.mkdir(parents=True, exist_ok=True)
    files = sorted(source.glob("*.png"))
    if len(files) != 32:
        raise RuntimeError(f"Expected 32 scene captures, found {len(files)}")

    font = ImageFont.load_default(size=20)
    for page, start in enumerate(range(0, len(files), COLS * ROWS), 1):
        sheet = Image.new(
            "RGB",
            (COLS * THUMBNAIL[0], ROWS * (THUMBNAIL[1] + LABEL_HEIGHT)),
            (22, 25, 29),
        )
        draw = ImageDraw.Draw(sheet)
        for slot, path in enumerate(files[start : start + COLS * ROWS]):
            image = Image.open(path).convert("RGB")
            image.thumbnail(THUMBNAIL, Image.Resampling.LANCZOS)
            col, row = slot % COLS, slot // COLS
            x = col * THUMBNAIL[0] + (THUMBNAIL[0] - image.width) // 2
            y = row * (THUMBNAIL[1] + LABEL_HEIGHT)
            sheet.paste(image, (x, y))
            draw.text(
                (col * THUMBNAIL[0] + 10, y + THUMBNAIL[1] + 9),
                path.stem,
                fill=(235, 238, 241),
                font=font,
            )
        target = output / f"migrated-scenes-{page:02d}.png"
        sheet.save(target, optimize=True)
        print(target)


if __name__ == "__main__":
    main()
