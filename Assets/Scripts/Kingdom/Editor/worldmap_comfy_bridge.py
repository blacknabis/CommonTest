import urllib.request
import urllib.parse
import json
import time
import os
import random

COMFY_URL = "http://127.0.0.1:8188"
OUTPUT_PATH = "Assets/Resources/UI/Sprites/WorldMap/WorldMap_Background.png"


def get_model_name():
    try:
        with urllib.request.urlopen(f"{COMFY_URL}/object_info/CheckpointLoaderSimple") as response:
            data = json.loads(response.read())
            if "CheckpointLoaderSimple" in data:
                data = data["CheckpointLoaderSimple"]
            ckpt_list = data["input"]["required"]["ckpt_name"][0]
            for model in ckpt_list:
                if "v1-5" in model:
                    return model
            return ckpt_list[0]
    except Exception:
        return "v1-5-pruned-emaonly.ckpt"


def queue_prompt(workflow):
    payload = json.dumps({"prompt": workflow}).encode("utf-8")
    req = urllib.request.Request(
        f"{COMFY_URL}/prompt",
        data=payload,
        headers={"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(req) as response:
        return json.loads(response.read())["prompt_id"]


def get_history(prompt_id):
    with urllib.request.urlopen(f"{COMFY_URL}/history/{prompt_id}") as response:
        return json.loads(response.read())


def get_image(filename, subfolder, folder_type):
    data = urllib.parse.urlencode({"filename": filename, "subfolder": subfolder, "type": folder_type})
    with urllib.request.urlopen(f"{COMFY_URL}/view?{data}") as response:
        return response.read()


def generate_worldmap():
    model = get_model_name()
    seed = random.randint(1, 2_147_483_647)

    prompt = (
        "fantasy kingdom world map background, kingdom rush inspired, hand painted, "
        "bright colorful valleys and hills, winding road path, distant castle, "
        "clean 2d game art, no characters, no ui text, no logo, panoramic"
    )
    negative = (
        "text, letters, words, watermark, logo, blurry, low quality, "
        "photorealistic, 3d render, dark, grim"
    )

    workflow = {
        "3": {
            "class_type": "KSampler",
            "inputs": {
                "seed": seed,
                "steps": 24,
                "cfg": 7.5,
                "sampler_name": "euler",
                "scheduler": "normal",
                "denoise": 1,
                "model": ["4", 0],
                "positive": ["6", 0],
                "negative": ["7", 0],
                "latent_image": ["5", 0],
            },
        },
        "4": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": model}},
        "5": {"class_type": "EmptyLatentImage", "inputs": {"width": 1536, "height": 864, "batch_size": 1}},
        "6": {"class_type": "CLIPTextEncode", "inputs": {"text": prompt, "clip": ["4", 1]}},
        "7": {"class_type": "CLIPTextEncode", "inputs": {"text": negative, "clip": ["4", 1]}},
        "8": {"class_type": "VAEDecode", "inputs": {"samples": ["3", 0], "vae": ["4", 2]}},
        "9": {"class_type": "SaveImage", "inputs": {"filename_prefix": "WorldMap_Background", "images": ["8", 0]}},
    }

    prompt_id = queue_prompt(workflow)
    print(f"Queued prompt: {prompt_id}")

    while True:
        history = get_history(prompt_id)
        if prompt_id in history:
            run_data = history[prompt_id]
            out = run_data["outputs"]["9"]["images"][0]
            img_data = get_image(out["filename"], out["subfolder"], out["type"])

            out_dir = os.path.dirname(OUTPUT_PATH)
            if out_dir and not os.path.exists(out_dir):
                os.makedirs(out_dir)

            with open(OUTPUT_PATH, "wb") as f:
                f.write(img_data)

            print(f"Saved: {OUTPUT_PATH}")
            return

        time.sleep(1)


if __name__ == "__main__":
    generate_worldmap()
