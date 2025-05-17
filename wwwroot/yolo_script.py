import sys
import os
import cv2
import numpy as np
from ultralytics import YOLO
import random
from collections import defaultdict
import json


CLASS_COLORS = {
    0: (0, 123, 255),    # plane - mavi
    1: (220, 53, 69),    # ship - kırmızı
    2: (40, 167, 69),    # large-vehicle - yeşil
    3: (255, 193, 7)     # small-vehicle - sarı
}


def split_and_detect(image_path, output_dir, model_path, tile_size=640):
    os.makedirs(output_dir, exist_ok=True)

    model = YOLO(model_path)
    class_counts = defaultdict(int)  # ⬅️ Sayım bu fonksiyonun içinde olmalı

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
                        class_counts[class_id] += 1

                        x1 += x
                        x2 += x
                        y1 += y
                        y2 += y

                        color = CLASS_COLORS.get(class_id, (0, 255, 0))
                        text_y = int(y1) - 10 if y1 > 20 else int(y1) + 20

                        cv2.rectangle(result_image, (int(x1), int(y1)), (int(x2), int(y2)), color, 2)
                        cv2.putText(result_image, str(class_id), (int(x1), text_y), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 2)
    else:
        results = model.predict(source=image, save=False)

        for result in results:
            for box in result.boxes:
                x1, y1, x2, y2 = box.xyxy[0].cpu().numpy()
                class_id = int(box.cls[0].cpu().numpy())
                class_counts[class_id] += 1

                color = CLASS_COLORS.get(class_id, (0, 255, 0))
                text_y = int(y1) - 10 if y1 > 20 else int(y1) + 20

                cv2.rectangle(result_image, (int(x1), int(y1)), (int(x2), int(y2)), color, 2)
                cv2.putText(result_image, str(class_id), (int(x1), text_y), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 2)

    # Görseli kaydet
    output_filename = os.path.basename(image_path)
    output_path = os.path.join(output_dir, output_filename)

    ext = os.path.splitext(output_filename)[1].lower()
    if ext in [".jpg", ".jpeg"]:
        cv2.imwrite(output_path, result_image, [int(cv2.IMWRITE_JPEG_QUALITY), 95])
    else:
        cv2.imwrite(output_path, result_image)

    print(f"[Python] ✅ Görsel kaydedildi: {output_path}")

    # JSON class sayım dosyasını oluştur
    json_output_path = os.path.join(output_dir, os.path.splitext(output_filename)[0] + "_classes.json")
    with open(json_output_path, 'w') as f:
        json.dump(class_counts, f)
    print(f"[Python] ✅ Class özeti yazıldı: {json_output_path}")

# === Ana giriş noktası ===
if len(sys.argv) < 2:
    print("[Python] ❌ Hata: Resim yolu eksik!")
    sys.exit(1)

image_path = sys.argv[1]
output_dir = os.path.join(os.path.dirname(image_path), "outputs")
model_path = os.path.join(os.path.dirname(__file__), "last.pt")

split_and_detect(image_path, output_dir, model_path)
