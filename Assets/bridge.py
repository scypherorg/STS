import asyncio
import json
import socket
import logging
import struct
import sys  # <--- Added for command line arguments
from bleak import BleakScanner, BleakClient

logging.basicConfig(level=logging.INFO)
log = logging.getLogger(__name__)

# Constants
_CHAR_CTRL   = "15172001-4947-11e9-8646-d663bd873d93"
_CHAR_RESET   = "15172006-4947-11e9-8646-d663bd873d93"
_CHAR_MEDIUM = "15172003-4947-11e9-8646-d663bd873d93"
_CMD_STOP   = bytes([0x01, 0x00, 0x15]) 
_CMD_START   = bytes([0x01, 0x01, 0x15]) 
_MEAS_FMT    = "<I fff fff"

UDP_IP = "127.0.0.1"
UDP_PORT = 5001
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# --- ARGUMENT PARSING ---
# sys.argv[0] is the script name. 
# sys.argv[1] will be the MAC address passed from Unity.
if len(sys.argv) > 1:
    target_mac = sys.argv[1]
else:
    target_mac = None
    log.error("No MAC address provided in command line arguments.")

def send_to_unity(address, data_bytes):
    try:
        ts, ax, ay, az, gx, gy, gz = struct.unpack_from(_MEAS_FMT, data_bytes)
        payload = {
            "adr": address,
            "ts": ts,
            "acc": {"x": ax, "y": ay, "z": az},
            "gyro": {"x": gx, "y": gy, "z": gz}
        }
        sock.sendto(json.dumps(payload).encode(), (UDP_IP, UDP_PORT))
    except Exception as e:
        pass

async def main():
    if not target_mac:
        log.error("No target_mac specified!")
        return

    log.info(f"Searching for device {target_mac}...")
    
    device = await BleakScanner.find_device_by_address(target_mac, timeout=10.0)
    
    if not device:
        log.error(f"Could not find device {target_mac}")
        return

    log.info(f"Connecting to {device.name}[{target_mac}]...")
    
    try:
        async with BleakClient(device, timeout=20.0) as client:
            log.info(f"Connected to {target_mac}!")
            
            await client.start_notify(_CHAR_MEDIUM, lambda h, d: send_to_unity(device.address, d))
            await asyncio.sleep(3.0)
            await client.write_gatt_char(_CHAR_CTRL, _CMD_STOP, response=False)
            await client.write_gatt_char(_CHAR_CTRL, _CMD_START, response=False)
            log.info(f"Successfully connected {target_mac}")
         #   await client.write_gatt_char(_CHAR_RESET, bytes[[0x00, 0x01]], response=False)
            
            while client.is_connected:
                await asyncio.sleep(1.0)
    except Exception as e:
        log.error(f"Connection error: {e}")

if __name__ == "__main__":
    asyncio.run(main())