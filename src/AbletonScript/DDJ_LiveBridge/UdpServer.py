import socket
import threading
import json
import traceback

class UdpServer(object):
    def __init__(self, host='127.0.0.1', port=8000, message_callback=None):
        self.host = host
        self.port = port
        self.message_callback = message_callback
        self.sock = None
        self.thread = None
        self.running = False

    def start(self):
        try:
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            # Permite reutilizar o endereço para não travar ao reiniciar o script no Ableton
            self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.sock.bind((self.host, self.port))
            self.sock.settimeout(0.1) # Timeout curto para permitir o shutdown seguro
            self.running = True
            
            self.thread = threading.Thread(target=self._listen)
            self.thread.daemon = True
            self.thread.start()
        except Exception as e:
            if self.message_callback:
                self.message_callback({"error": str(e)})

    def _listen(self):
        while self.running:
            try:
                data, addr = self.sock.recvfrom(4096)
                if data: print("[DDJ-LiveBridge] UDP Recv raw: %s" % data)
                if data and self.message_callback:
                    try:
                        msg = json.loads(data.decode('utf-8'))
                        # Passamos para o callback que adicionará à fila Thread-Safe do main
                        self.message_callback(msg)
                    except Exception as parse_err:
                        pass # Ignora pacotes JSON malformados
            except socket.timeout:
                continue
            except Exception as e:
                break

    def stop(self):
        self.running = False
        if self.thread:
            self.thread.join(timeout=1.0)
        if self.sock:
            try:
                self.sock.close()
            except:
                pass

