import copy
import json
import os
import random
import time
import urllib.parse
import urllib.request

COMFY_URL = "http://127.0.0.1:8188"
OUTPUT_DIR = "Assets/Resources/Audio/WorldMap"
WORKFLOW_BGM_PATH = "Assets/Scripts/Kingdom/Editor/ComfyUI/workflow_worldmap_bgm.json"
WORKFLOW_SFX_PATH = "Assets/Scripts/Kingdom/Editor/ComfyUI/workflow_worldmap_sfx.json"
SAVE_AUDIO_NODE_ID = "19"


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


def get_file(filename, subfolder, folder_type):
    query = urllib.parse.urlencode(
        {
            "filename": filename,
            "subfolder": subfolder,
            "type": folder_type,
        }
    )
    with urllib.request.urlopen(f"{COMFY_URL}/view?{query}") as response:
        return response.read()


def deep_replace_placeholders(value, placeholders):
    if isinstance(value, dict):
        return {k: deep_replace_placeholders(v, placeholders) for k, v in value.items()}
    if isinstance(value, list):
        return [deep_replace_placeholders(v, placeholders) for v in value]
    if isinstance(value, str):
        # Preserve node-link ids like "12" in ComfyUI graph links.
        if value in placeholders:
            return placeholders[value]

        result = value
        for token, token_value in placeholders.items():
            result = result.replace(token, str(token_value))
        return result

    return value


def load_workflow_template(workflow_path):
    with open(workflow_path, "r", encoding="utf-8-sig") as f:
        return json.load(f)


def build_workflow(workflow_path, name, prompt, duration_sec, cfg):
    workflow = copy.deepcopy(load_workflow_template(workflow_path))
    placeholders = {
        "%PROMPT%": prompt,
        "%DURATION%": int(duration_sec),
        "%SEED%": random.randint(1, 2_147_483_647),
        "%CFG%": float(cfg),
    }
    workflow = deep_replace_placeholders(workflow, placeholders)

    if SAVE_AUDIO_NODE_ID in workflow:
        workflow[SAVE_AUDIO_NODE_ID].setdefault("inputs", {})
        workflow[SAVE_AUDIO_NODE_ID]["inputs"]["filename_prefix"] = f"WorldMap/{name}"

    return workflow


def find_audio_output_item(run_data):
    outputs = run_data.get("outputs", {})
    for _, node_data in outputs.items():
        audio_items = node_data.get("audio", [])
        if audio_items:
            return audio_items[0]
    return None


def generate(workflow_path, name, prompt, duration_sec, cfg):
    workflow = build_workflow(workflow_path, name, prompt, duration_sec, cfg)
    prompt_id = queue_prompt(workflow)
    print(f"[ComfyAudio] queued {name}: {prompt_id}")

    for _ in range(300):
        history = get_history(prompt_id)
        if prompt_id not in history:
            time.sleep(2)
            continue

        run_data = history[prompt_id]
        status = run_data.get("status", {})
        messages = status.get("messages", [])
        for msg in messages:
            if msg and len(msg) >= 2 and msg[0] == "execution_error":
                err = msg[1].get("exception_message", "Unknown error")
                node_type = msg[1].get("node_type", "unknown")
                raise RuntimeError(f"{node_type}: {err.strip()}")

        item = find_audio_output_item(run_data)
        if item is None:
            time.sleep(2)
            continue

        data = get_file(item["filename"], item.get("subfolder", ""), item.get("type", "output"))
        os.makedirs(OUTPUT_DIR, exist_ok=True)
        _, ext = os.path.splitext(item["filename"])
        if not ext:
            ext = ".mp3"
        out_path = os.path.join(OUTPUT_DIR, f"{name}{ext}")
        with open(out_path, "wb") as f:
            f.write(data)

        print(f"[ComfyAudio] saved: {out_path}")
        return out_path

    raise TimeoutError(f"Timed out while generating {name}")


def main():
    bgm_prompt = (
        "fantasy world map background music, kingdom rush inspired, "
        "bright orchestral, playful medieval, no vocals, loop friendly"
    )
    click_prompt = (
        "short fantasy ui click sound, wood and metal texture, crisp transient, no voice"
    )

    generate(WORKFLOW_BGM_PATH, "WorldMap_BGM", bgm_prompt, 24, 6.5)
    generate(WORKFLOW_SFX_PATH, "WorldMap_Click", click_prompt, 2, 7.0)
    print("[ComfyAudio] done")


if __name__ == "__main__":
    main()
