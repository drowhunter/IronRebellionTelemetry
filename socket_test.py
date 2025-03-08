import socket
import struct
import sys

# Configuration
UDP_IP = "127.0.0.1"
UDP_PORT = 6969
BUFFER_SIZE = 1024
EXPECTED_BYTES = 51  # 10 floats (40 bytes) + 7 bools (7 bytes) = 51 bytes

def print_values(
    velX, velY, velZ, angX, angY, angZ, 
    rotX, rotY, rotZ, tilt, lean,
    isFlying, isRunning, isHit, weaponFired, stomped, landed, jumped
):
    """
    Print telemetry data in place by overwriting previous lines.
    """
    # Move cursor up 13 lines (number of print statements + separator line)
    sys.stdout.write("\033[F" * 13)

    # Overwrite each line
    sys.stdout.write(f"Velocity: ({velX:.2f}, {velY:.2f}, {velZ:.2f})\033[K\n")
    sys.stdout.write(f"Angular: ({angX:.2f}, {angY:.2f}, {angZ:.2f})\033[K\n")
    sys.stdout.write(f"Rotation: ({rotX:.2f}, {rotY:.2f}, {rotZ:.2f})\033[K\n")
    sys.stdout.write(f"Tilt: {tilt:.2f}\033[K\n")
    sys.stdout.write(f"Lean: {lean:.2f}\033[K\n")
    sys.stdout.write(f"Flying: {'Yes' if isFlying else 'No'}\033[K\n")
    sys.stdout.write(f"Running: {'Yes' if isRunning else 'No'}\033[K\n")
    sys.stdout.write(f"Hit: {'Yes' if isHit else 'No'}\033[K\n")
    sys.stdout.write(f"Weapon Fired: {'Yes' if weaponFired else 'No'}\033[K\n")
    sys.stdout.write(f"Stomped: {'Yes' if stomped else 'No'}\033[K\n")
    sys.stdout.write(f"Landed: {'Yes' if landed else 'No'}\033[K\n")
    sys.stdout.write(f"Jumped: {'Yes' if jumped else 'No'}\033[K\n")
    sys.stdout.write("=" * 30 + "\033[K\n")  # Separator line

    sys.stdout.flush()  # Ensure immediate printing

def main():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((UDP_IP, UDP_PORT))
    print(f"Listening on {UDP_IP}:{UDP_PORT}...\n" + "=" * 30)

    # Print empty lines to reserve space for the first output
    print("\n" * 12)

    try:
        while True:
            data, addr = sock.recvfrom(BUFFER_SIZE)

            if len(data) == EXPECTED_BYTES:
                # Unpack 10 floats (40 bytes)
                telemetry = struct.unpack("11f", data[:44])  
                telemetry = tuple(round(value, 2) for value in telemetry)

                # Unpack 7 booleans (7 bytes)
                isFlying, isRunning, isHit, weaponFired, stomped, landed, jumped = struct.unpack("7b", data[44:51])

                # Convert byte booleans (0 or 1) to Python booleans
                bools = tuple(bool(b) for b in [isFlying, isRunning, isHit, weaponFired, stomped, landed, jumped])

                # Print the values 
                print_values(*telemetry, *bools)

            else:
                print(f"\nUnexpected data size: {len(data)} bytes from {addr}. Skipping.")

    except KeyboardInterrupt:
        print("\nExiting...")
    except Exception as e:
        print(f"\nError: {e}")

    finally:
        sock.close()
        print("Socket closed.")

if __name__ == "__main__":
    main()
