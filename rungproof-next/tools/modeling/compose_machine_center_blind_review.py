"""Compose an unlabeled four-view inspection board for source-blind review."""
from pathlib import Path
from PIL import Image, ImageOps
import os

root = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
review = root / "assets" / "scene_core" / "enclosed_machine_center" / "review"
views = ("hero.png", "spindle_vise_tool.png", "tool_carousel_and_coolant.png", "hmi_and_estop.png")
tiles = [Image.open(review / name).convert("RGB") for name in views]
size = 720
canvas = Image.new("RGB", (size * 2 + 18, size * 2 + 18), (18, 23, 28))
for index, tile in enumerate(tiles):
    fitted = ImageOps.contain(tile, (size, size), Image.Resampling.LANCZOS)
    frame = Image.new("RGB", (size, size), (8, 12, 16))
    frame.paste(fitted, ((size - fitted.width) // 2, (size - fitted.height) // 2))
    x = 6 + (index % 2) * (size + 6)
    y = 6 + (index // 2) * (size + 6)
    canvas.paste(frame, (x, y))
canvas.save(review / "blind_review.png")
print(f"MACHINE_CENTER_BLIND_BOARD_RENDERED {review / 'blind_review.png'}")
