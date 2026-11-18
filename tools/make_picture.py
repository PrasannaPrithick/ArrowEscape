from PIL import Image
import json, random, sys

def make(src, name, seed=1, colors=8):
    im = Image.open(src).convert("RGB")
    w, h = im.size; s = min(w, h)
    im = im.crop(((w - s) // 2, (h - s) // 2, (w + s) // 2, (h + s) // 2))
    small = im.resize((10, 10), Image.Resampling.BOX)
    small = small.quantize(colors=colors, method=Image.Quantize.MEDIANCUT).convert("RGB")
    pixels = [["#%02x%02x%02x" % small.getpixel((x, y)) for x in range(10)] for y in range(10)]
    order = list(range(100)); random.Random(seed).shuffle(order)
    pieces = [order[i * 5:(i + 1) * 5] for i in range(20)]   # pixel index = y*10 + x
    json.dump({"name": name, "size": 10, "pixels": pixels, "pieces": pieces},
              open(f"{name}.json", "w"))
    small.resize((320, 320), Image.Resampling.NEAREST).save(f"{name}_preview.png")

if __name__ == "__main__":
    make(sys.argv[1], sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 1)
