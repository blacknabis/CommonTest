import json
import os
import random
import time
import urllib.parse
import urllib.request

COMFY_URL = "http://127.0.0.1:8188"
HERO_ID = "DefaultHero"

PORTRAIT_DIR = "Assets/Resources/UI/Sprites/Heroes/Portraits"
INGAME_DIR = "Assets/Resources/UI/Sprites/Heroes/InGame"
INGAME_SEQ_DIR = os.path.join(INGAME_DIR, HERO_ID)

# Keep this small for first pass. Increase after visual QA.
ACTION_FRAME_COUNTS = {
    "idle": 4,
    "walk": 4,
    "attack": 4,
    "die": 4,
}

PROMPT_BASE = (
    "kingdom rush style, stylized 2d game character, fantasy knight hero, "
    "clear silhouette, bold outline, flat vibrant colors, readable at small size, transparent background"
)

NEGATIVE_BASE = (
    "photorealistic, realistic skin, 3d render, blurry, watermark, logo, text, "
    "deformed anatomy, extra limbs, cluttered background"
)


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


def build_workflow(model_name, prompt, negative, width, height, prefix):
    seed = random.randint(1, 2_147_483_647)
    return {
        "3": {
            "class_type": "KSampler",
            "inputs": {
                "seed": seed,
                "steps": 28,
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
        "5": {"class_type": "EmptyLatentImage", "inputs": {"width": width, "height": height, "batch_size": 1}},
        "6": {"class_type": "CLIPTextEncode", "inputs": {"text": prompt, "clip": ["4", 1]}},
        "7": {"class_type": "CLIPTextEncode", "inputs": {"text": negative, "clip": ["4", 1]}},
        "8": {"class_type": "VAEDecode", "inputs": {"samples": ["3", 0], "vae": ["4", 2]}},
        "9": {"class_type": "SaveImage", "inputs": {"filename_prefix": prefix, "images": ["8", 0]}},
    }


def generate_image(model_name, prompt, negative, width, height, prefix, out_path):
    workflow = build_workflow(model_name, prompt, negative, width, height, prefix)
    prompt_id = queue_prompt(workflow)
    print(f"[ComfyUI] queued {prefix} ({prompt_id})")

    while True:
        history = get_history(prompt_id)
        if prompt_id in history:
            run_data = history[prompt_id]
            out = run_data["outputs"]["9"]["images"][0]
            img_data = get_image(out["filename"], out["subfolder"], out["type"])
            os.makedirs(os.path.dirname(out_path), exist_ok=True)
            with open(out_path, "wb") as f:
                f.write(img_data)
            print(f"[ComfyUI] saved: {out_path}")
            return
        time.sleep(1)


def generate_portrait(model_name):
    prompt = (
        f"{PROMPT_BASE}, hero portrait icon, bust shot, centered composition, "
        "armor details readable, ui portrait, no background"
    )
    out_path = os.path.join(PORTRAIT_DIR, f"{HERO_ID}.png")
    generate_image(model_name, prompt, NEGATIVE_BASE, 1024, 1024, f"{HERO_ID}_portrait", out_path)


def generate_ingame_single(model_name):
    prompt = (
        f"{PROMPT_BASE}, top-down 3/4 full body hero idle stance, "
        "gameplay sprite, no weapon motion blur"
    )
    out_path = os.path.join(INGAME_DIR, f"{HERO_ID}.png")
    generate_image(model_name, prompt, NEGATIVE_BASE, 512, 512, f"{HERO_ID}_single", out_path)


def generate_action_frames(model_name, action, count):
    if action == "idle":
        action_prompt = "top-down 3/4 idle stance, subtle breathing pose variation"
    elif action == "walk":
        action_prompt = "top-down 3/4 walking pose, forward movement cycle frame"
    elif action == "attack":
        action_prompt = "top-down 3/4 sword slash attack pose, clear combat action"
    else:
        action_prompt = "top-down 3/4 death/fall pose frame, readable collapse motion"

    for i in range(count):
        prompt = f"{PROMPT_BASE}, {action_prompt}, frame {i+1}/{count}, consistent character identity"
        out_path = os.path.join(INGAME_SEQ_DIR, f"{action}_{i:02d}.png")
        prefix = f"{HERO_ID}_{action}_{i:02d}"
        generate_image(model_name, prompt, NEGATIVE_BASE, 512, 512, prefix, out_path)


def main():
    model = get_model_name()
    print(f"[ComfyUI] model: {model}")
    generate_portrait(model)
    generate_ingame_single(model)
    for action, count in ACTION_FRAME_COUNTS.items():
        generate_action_frames(model, action, count)
    print("[ComfyUI] Hero generation completed.")


if __name__ == "__main__":
    main()
