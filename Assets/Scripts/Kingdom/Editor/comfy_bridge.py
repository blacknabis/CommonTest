import urllib.request
import urllib.parse
import json
import time
import sys
import os
import random

COMFY_URL = "http://127.0.0.1:8188"
OUTPUT_DIR = "Assets/Resources/UI/Title"

def get_model_name():
    try:
        url = f"{COMFY_URL}/object_info/CheckpointLoaderSimple"
        print(f"Fetching models from {url}...")
        with urllib.request.urlopen(url) as response:
            data = json.loads(response.read())
            if "CheckpointLoaderSimple" in data:
                data = data["CheckpointLoaderSimple"]
            if "input" in data and "required" in data["input"] and "ckpt_name" in data["input"]["required"]:
                ckpt_list = data["input"]["required"]["ckpt_name"][0]
                if ckpt_list:
                    print(f"Found models: {ckpt_list}")
                    for model in ckpt_list:
                        if "v1-5" in model: return model
                    return ckpt_list[0]
            print("Could not parse model list, using default.")
            return "v1-5-pruned-emaonly.ckpt" 
    except Exception as e:
        print(f"Failed to fetch models: {e}")
        return "v1-5-pruned-emaonly.ckpt"

def queue_prompt(workflow):
    p = {"prompt": workflow}
    data = json.dumps(p).encode('utf-8')
    req = urllib.request.Request(f"{COMFY_URL}/prompt", data=data, headers={'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req) as response:
            return json.loads(response.read())['prompt_id']
    except urllib.error.HTTPError as e:
        print(f"HTTP Error {e.code}: {e.reason}")
        print(e.read().decode('utf-8'))
        raise e

def get_history(prompt_id):
    with urllib.request.urlopen(f"{COMFY_URL}/history/{prompt_id}") as response:
        return json.loads(response.read())

def get_image(filename, subfolder, folder_type):
    data = {"filename": filename, "subfolder": subfolder, "type": folder_type}
    url_values = urllib.parse.urlencode(data)
    with urllib.request.urlopen(f"{COMFY_URL}/view?{url_values}") as response:
        return response.read()

def generate(prefix, prompt, negative, model_name, width=512, height=512, use_rembg=False):
    print(f"Generating {prefix} ({width}x{height}, RemBG: {use_rembg})...")
    seed = random.randint(1, 100000000)
    
    workflow = {
        "3": { "class_type": "KSampler", "inputs": { "seed": seed, "steps": 20, "cfg": 8, "sampler_name": "euler", "scheduler": "normal", "denoise": 1, "model": ["4", 0], "positive": ["6", 0], "negative": ["7", 0], "latent_image": ["5", 0] } },
        "4": { "class_type": "CheckpointLoaderSimple", "inputs": { "ckpt_name": model_name } },
        "5": { "class_type": "EmptyLatentImage", "inputs": { "width": width, "height": height, "batch_size": 1 } },
        "6": { "class_type": "CLIPTextEncode", "inputs": { "text": prompt, "clip": ["4", 1] } },
        "7": { "class_type": "CLIPTextEncode", "inputs": { "text": negative, "clip": ["4", 1] } },
        "8": { "class_type": "VAEDecode", "inputs": { "samples": ["3", 0], "vae": ["4", 2] } },
    }
    
    last_image_node = "8"
    
    if use_rembg:
        workflow["10"] = { 
            "class_type": "Image Remove Background (rembg)", 
            "inputs": { "image": [last_image_node, 0] } 
        }
        last_image_node = "10"

    workflow["9"] = { "class_type": "SaveImage", "inputs": { "filename_prefix": prefix, "images": [last_image_node, 0] } }

    try:
        prompt_id = queue_prompt(workflow)
        print(f"Queued {prefix}: {prompt_id}")
        
        while True:
            history = get_history(prompt_id)
            if prompt_id in history:
                run_data = history[prompt_id]
                if 'outputs' not in run_data or '9' not in run_data['outputs']:
                    print(f"Error: Node 9 output missing. History dump: {json.dumps(run_data, indent=2)}")
                    break
                
                out = run_data['outputs']['9']['images'][0]
                img_data = get_image(out['filename'], out['subfolder'], out['type'])
                
                if not os.path.exists(OUTPUT_DIR):
                    os.makedirs(OUTPUT_DIR)
                    
                path = f"{OUTPUT_DIR}/{prefix}.png"
                with open(path, "wb") as f:
                    f.write(img_data)
                print(f"Saved to {path}")
                break
            time.sleep(1)
    except Exception as e:
        print(f"Failed {prefix}: {e}")

def main():
    model = get_model_name()
    print(f"Using model: {model}")
    
    # ======================================================
    # NOTE: SD1.5는 텍스트를 생성할 수 없음.
    # 로고와 버튼은 장식 요소만 생성하고,
    # 실제 텍스트는 Unity TextMeshPro로 표시함.
    # ======================================================
    
    # 1. Background — 배경 (full scene, no rembg)
    generate("Title_Background",
             "fantasy medieval kingdom panorama, kingdom rush style, hand painted, "
             "vibrant colors, 2d game art, rolling green hills, stone castle in distance, "
             "blue sky with fluffy clouds, colorful towers, bright sunny day, cartoon style",
             "text, letters, words, watermark, low quality, 3d render, photorealistic, dark, gloomy, realistic",
             model, width=768, height=512, use_rembg=False)
             
    # 2. Logo Emblem — 엠블렘만 (텍스트 없음, 배경 제거)
    generate("Title_Logo",
             "medieval fantasy shield emblem, golden ornate frame, royal crest, "
             "kingdom coat of arms, crossed swords, crown on top, "
             "hand painted game art style, centered, symmetrical, "
             "isolated on solid black background, no text, no letters, no words",
             "text, letters, words, writing, font, typography, "
             "low quality, blurry, asymmetric, complex background, people, realistic",
             model, width=512, height=512, use_rembg=True)
             
    # 3. Button Frame — 나무 버튼 틀 (텍스트 없음, 배경 제거)
    generate("Title_BtnStart",
             "wooden game ui button frame, fantasy style, horizontal rectangle, "
             "wood plank texture, metal rivets on corners, carved wood border, "
             "empty center, no text, no letters, no words, "
             "hand painted game art, isolated on solid black background",
             "text, letters, words, writing, circle, round, "
             "low quality, complex background, realistic, people",
             model, width=512, height=256, use_rembg=True)

if __name__ == "__main__":
    main()
