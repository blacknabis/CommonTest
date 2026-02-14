import json
import os
import random
import time
import urllib.parse
import urllib.request

COMFY_URL = "http://127.0.0.1:8188"
OUTPUT_DIR = "Assets/Resources/UI/Sprites/WorldMap"

ASSETS = [
    {
        "name": "UIStageNode_SelectedHighlight",
        "width": 512,
        "height": 512,
        "prompt": (
            "fantasy game ui selection ring, warm golden magical glow, "
            "subtle blue inner light, circular badge aura, centered, "
            "clean outline, hand painted 2d, isolated on solid black background"
        ),
        "negative": (
            "text, letters, logo, watermark, character, scenery, "
            "photorealistic, 3d, noisy, blurry"
        ),
    },
    {
        "name": "UIStageNode_LockIcon",
        "width": 384,
        "height": 384,
        "prompt": (
            "fantasy ui lock icon, medieval metal padlock with rivets, "
            "slight gold trim, front view, centered, high contrast, "
            "hand painted 2d icon, isolated on solid black background"
        ),
        "negative": (
            "text, letters, logo, watermark, key, chain clutter, "
            "photorealistic, 3d render, blurry"
        ),
    },
    {
        "name": "UIStageNode_NotificationDot",
        "width": 256,
        "height": 256,
        "prompt": (
            "fantasy ui notification badge, red gem dot with tiny gold rim, "
            "glossy highlight, centered, simple silhouette, "
            "hand painted 2d icon, isolated on solid black background"
        ),
        "negative": (
            "text, letters, logo, watermark, realistic plastic, "
            "3d, blurry, complex background"
        ),
    },
    {
        "name": "UIStageNode_Star",
        "width": 256,
        "height": 256,
        "prompt": (
            "fantasy game ui golden star icon, five-point star, "
            "polished metal with soft glow, centered, clear outline, "
            "hand painted 2d icon, isolated on solid black background"
        ),
        "negative": (
            "text, letters, logo, watermark, realistic photo, "
            "3d render, noisy, blurry, asymmetry"
        ),
    },
]


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


def build_workflow(model_name, item):
    seed = random.randint(1, 2_147_483_647)
    return {
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
        "4": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": model_name}},
        "5": {"class_type": "EmptyLatentImage", "inputs": {"width": item["width"], "height": item["height"], "batch_size": 1}},
        "6": {"class_type": "CLIPTextEncode", "inputs": {"text": item["prompt"], "clip": ["4", 1]}},
        "7": {"class_type": "CLIPTextEncode", "inputs": {"text": item["negative"], "clip": ["4", 1]}},
        "8": {"class_type": "VAEDecode", "inputs": {"samples": ["3", 0], "vae": ["4", 2]}},
        "10": {"class_type": "Image Remove Background (rembg)", "inputs": {"image": ["8", 0]}},
        "9": {"class_type": "SaveImage", "inputs": {"filename_prefix": item["name"], "images": ["10", 0]}},
    }


def generate_asset(model_name, item):
    workflow = build_workflow(model_name, item)
    prompt_id = queue_prompt(workflow)
    print(f"[ComfyUI] queued {item['name']} ({prompt_id})")

    while True:
        history = get_history(prompt_id)
        if prompt_id in history:
            run_data = history[prompt_id]
            out = run_data["outputs"]["9"]["images"][0]
            img_data = get_image(out["filename"], out["subfolder"], out["type"])
            os.makedirs(OUTPUT_DIR, exist_ok=True)
            out_path = os.path.join(OUTPUT_DIR, f"{item['name']}.png")
            with open(out_path, "wb") as f:
                f.write(img_data)
            print(f"[ComfyUI] saved: {out_path}")
            return
        time.sleep(1)


def main():
    model = get_model_name()
    print(f"[ComfyUI] model: {model}")
    for item in ASSETS:
        generate_asset(model, item)


if __name__ == "__main__":
    main()

