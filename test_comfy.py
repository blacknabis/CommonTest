import urllib.request
import urllib.error

url = "http://127.0.0.1:8188/object_info"
print(f"Testing connection to {url}...")

try:
    with urllib.request.urlopen(url, timeout=5) as response:
        print(f"Success! Status: {response.status}")
        print(f"Data length: {len(response.read())}")
except urllib.error.URLError as e:
    print(f"Error: {e}")
except Exception as e:
    print(f"Exception: {e}")
