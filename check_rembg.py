import urllib.request
import json
import sys

COMFY_URL = "http://127.0.0.1:8188"

def check_node(node_name):
    try:
        url = f"{COMFY_URL}/object_info/{node_name}"
        with urllib.request.urlopen(url) as response:
            if response.getcode() == 200:
                print(f"Node '{node_name}' found.")
                return True
    except:
        pass
    print(f"Node '{node_name}' NOT found.")
    return False

def check_any_rembg():
    try:
        url = f"{COMFY_URL}/object_info"
        print(f"Fetching all nodes from {url}...")
        with urllib.request.urlopen(url) as response:
            data = json.loads(response.read())
            
            rembg_nodes = []
            for key in data.keys():
                if "rembg" in key.lower() or "remove background" in key.lower():
                    rembg_nodes.append(key)
            
            if rembg_nodes:
                print(f"Found RemBG-related nodes: {rembg_nodes}")
                return True
            else:
                print("No RemBG nodes found.")
                return False
    except Exception as e:
        print(f"Failed to connect: {e}")
        return False

if __name__ == "__main__":
    # Common RemBG node names
    # "Image Rembg (Remove Background)" (Jordach)
    # "RemBGSession+" (Essentials)
    # "RemBG" (WAS Node Suite)
    check_any_rembg()
