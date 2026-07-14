# DDJ-LiveBridge Remote Script
# Este script intercepta mensagens MIDI específicas (CCs) da porta loopMIDI
# e as traduz para ações nativas do Ableton Live via API Python.

import Live

class DDJ_LiveBridge:
    def __init__(self, c_instance):
        self._c_instance = c_instance
        self._app = Live.Application.get_application()
        self.log_message("DDJ_LiveBridge Remote Script carregado com sucesso!")

    def log_message(self, message):
        self._c_instance.log_message(f"[DDJ-LiveBridge] {message}")

    def build_midi_map(self, midi_map_handle):
        # Aqui vamos mapear os CCs enviados pelo C# para acionar funções
        pass

    def receive_midi(self, midi_bytes):
        # Parse manual de bytes MIDI se necessário
        status_byte = midi_bytes[0]
        data_byte1 = midi_bytes[1]
        data_byte2 = midi_bytes[2]
        
        self.log_message(f"MIDI Recebido: {status_byte}, {data_byte1}, {data_byte2}")
        
    def disconnect(self):
        self.log_message("DDJ_LiveBridge desconectado.")
