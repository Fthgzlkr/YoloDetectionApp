import sys
import os
import cv2
import numpy as np
from ultralytics import YOLO
import random

# Sınıf ID -> İsim eşleşmesi
CLASS_NAMES = {
    0: "plane",
    1: "ship",
    2: "large-vehicle",
    3: "small-vehicle"
}

# Rastgele renk ataması
def generate_class_colors(num_classes):
    return {i: (random.randint(0, 255), random.randint(0, 255), random.randint(0, 255)) for i in range(num_classes)}

CLASS_COLORS = generate_class_colors(len(CLASS_NAMES))


def split_and_detect(image_path, output_dir, model_path, tile_size=640):
    os.makedirs(output_dir, exist_ok=True)

    model = YOLO(model_path)

    image = cv2.imread(image_path)
    if image is None:
        print(f"Error: Unable to load image at {image_path}")
        return

    height, width, _ = image.shape
    result_image = image.copy()

    if height > tile_size or width > tile_size:
        pad_h = (tile_size - height % tile_size) % tile_size
        pad_w = (tile_size - width % tile_size) % tile_size
        padded_image = cv2.copyMakeBorder(image, 0, pad_h, 0, pad_w, cv2.BORDER_CONSTANT, value=(0, 0, 0))
        padded_height, padded_width, _ = padded_image.shape

        for y in range(0, padded_height, tile_size):
            for x in range(0, padded_width, tile_size):
                tile = padded_image[y:y+tile_size, x:x+tile_size]
                results = model.predict(source=tile, save=False)

                for result in results:
                    for box in result.boxes:
                        x1, y1, x2, y2 = box.xyxy[0].cpu().numpy()
                        class_id = int(box.cls[0].cpu().numpy())
                        conf = box.conf[0].cpu().numpy()

                        x1 += x
                        x2 += x
                        y1 += y
                        y2 += y

                        color = CLASS_COLORS.get(class_id, (0, 255, 0))
                        label = CLASS_NAMES.get(class_id, f"class {class_id}")
                        text_y = int(y1) - 10 if y1 > 20 else int(y1) + 20

                        cv2.rectangle(result_image, (int(x1), int(y1)), (int(x2), int(y2)), color, 2)
                        cv2.putText(result_image, label, (int(x1), text_y), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 2)
    else:
        results = model.predict(source=image, save=False)

        for result in results:
            for box in result.boxes:
                x1, y1, x2, y2 = box.xyxy[0].cpu().numpy()
                class_id = int(box.cls[0].cpu().numpy())
                conf = box.conf[0].cpu().numpy()

                color = CLASS_COLORS.get(class_id, (0, 255, 0))
                label = CLASS_NAMES.get(class_id, f"class {class_id}")
                text_y = int(y1) - 10 if y1 > 20 else int(y1) + 20

                cv2.rectangle(result_image, (int(x1), int(y1)), (int(x2), int(y2)), color, 2)
                cv2.putText(result_image, label, (int(x1), text_y), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 2)

    # Çıktı dosya yolunu oluştur (aynı uzantı ve isim)
    output_filename = os.path.basename(image_path)
    output_path = os.path.join(output_dir, output_filename)

    # Formatı belirle ve kaydet
    ext = os.path.splitext(output_filename)[1].lower()
    if ext in [".jpg", ".jpeg"]:
        cv2.imwrite(output_path, result_image, [int(cv2.IMWRITE_JPEG_QUALITY), 95])
    else:
        cv2.imwrite(output_path, result_image)

    print(f"[Python] ✅ Çıktı oluşturuldu: {output_path}")


# === Ana giriş noktası ===
if len(sys.argv) < 2:
    print("[Python] ❌ Hata: Resim yolu eksik!")
    sys.exit(1)

image_path = sys.argv[1]
output_dir = os.path.join(os.path.dirname(image_path), "outputs")
model_path = os.path.join(os.path.dirname(__file__), "last.pt")

split_and_detect(image_path, output_dir, model_path)
