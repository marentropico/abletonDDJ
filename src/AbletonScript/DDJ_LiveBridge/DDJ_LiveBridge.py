import Live
from ableton.v2.control_surface import ControlSurface, MIDI_CC_TYPE
from ableton.v2.control_surface.elements import ButtonElement

class DDJ_LiveBridge(ControlSurface):
    def __init__(self, c_instance):
        super(DDJ_LiveBridge, self).__init__(c_instance)
        self._c_instance = c_instance
        self._browser_focused = False
        
        self.log_message("DDJ_LiveBridge Inicializado em modo Puro MIDI (Canal 16).")
        
        with self.component_guard():
            self._setup_listeners()

    def log_message(self, message):
        print("[DDJ-LiveBridge] %s" % message)

    def _setup_listeners(self):
        # API Canal 16 (0-indexed = 15 no Python)
        
        # 1. Transporte (CC 10)
        self._btn_play = ButtonElement(True, MIDI_CC_TYPE, 15, 10)
        self.register_disconnectable(self._btn_play)
        self._btn_play.add_value_listener(self._do_play_toggle)
        
        # 2. View & Focus Toggles (CC 43, 44, 45, 46)
        self._btn_focus = ButtonElement(True, MIDI_CC_TYPE, 15, 43)
        self.register_disconnectable(self._btn_focus)
        self._btn_focus.add_value_listener(self._do_focus_toggle)
        
        self._btn_view = ButtonElement(True, MIDI_CC_TYPE, 15, 44)
        self.register_disconnectable(self._btn_view)
        self._btn_view.add_value_listener(self._do_view_toggle)
        
        self._btn_solo = ButtonElement(True, MIDI_CC_TYPE, 15, 45)
        self.register_disconnectable(self._btn_solo)
        self._btn_solo.add_value_listener(self._do_solo_toggle)

        self._btn_mute = ButtonElement(True, MIDI_CC_TYPE, 15, 46)
        self.register_disconnectable(self._btn_mute)
        self._btn_mute.add_value_listener(self._do_mute_toggle)
        
        # 3. EQs & Filtros (8 Knobs de controle do Plugin selecionado)
        self._knob_eq_hi_l = ButtonElement(True, MIDI_CC_TYPE, 15, 22)
        self.register_disconnectable(self._knob_eq_hi_l)
        self._knob_eq_hi_l.add_value_listener(lambda v: self._set_device_knob_param(0, v))

        self._knob_eq_mid_l = ButtonElement(True, MIDI_CC_TYPE, 15, 23)
        self.register_disconnectable(self._knob_eq_mid_l)
        self._knob_eq_mid_l.add_value_listener(lambda v: self._set_device_knob_param(1, v))

        self._knob_eq_lo_l = ButtonElement(True, MIDI_CC_TYPE, 15, 24)
        self.register_disconnectable(self._knob_eq_lo_l)
        self._knob_eq_lo_l.add_value_listener(lambda v: self._set_device_knob_param(2, v))

        self._knob_filter_l = ButtonElement(True, MIDI_CC_TYPE, 15, 25)
        self.register_disconnectable(self._knob_filter_l)
        self._knob_filter_l.add_value_listener(lambda v: self._set_device_knob_param(3, v))

        self._knob_eq_hi_r = ButtonElement(True, MIDI_CC_TYPE, 15, 32)
        self.register_disconnectable(self._knob_eq_hi_r)
        self._knob_eq_hi_r.add_value_listener(lambda v: self._set_device_knob_param(4, v))

        self._knob_eq_mid_r = ButtonElement(True, MIDI_CC_TYPE, 15, 33)
        self.register_disconnectable(self._knob_eq_mid_r)
        self._knob_eq_mid_r.add_value_listener(lambda v: self._set_device_knob_param(5, v))

        self._knob_eq_lo_r = ButtonElement(True, MIDI_CC_TYPE, 15, 34)
        self.register_disconnectable(self._knob_eq_lo_r)
        self._knob_eq_lo_r.add_value_listener(lambda v: self._set_device_knob_param(6, v))

        self._knob_filter_r = ButtonElement(True, MIDI_CC_TYPE, 15, 35)
        self.register_disconnectable(self._knob_filter_r)
        self._knob_filter_r.add_value_listener(lambda v: self._set_device_knob_param(7, v))

        # 4. Volumes (Trim Left controla volume da track selecionada, Trim Right controla Master volume)
        self._knob_trim_l = ButtonElement(True, MIDI_CC_TYPE, 15, 21)
        self.register_disconnectable(self._knob_trim_l)
        self._knob_trim_l.add_value_listener(self._set_selected_track_volume)

        self._knob_trim_r = ButtonElement(True, MIDI_CC_TYPE, 15, 31)
        self.register_disconnectable(self._knob_trim_r)
        self._knob_trim_r.add_value_listener(self._set_master_track_volume)

        # 5. Wildcard Parameter Control (Knob LevelDepth - CC 47)
        self._knob_wildcard = ButtonElement(True, MIDI_CC_TYPE, 15, 47)
        self.register_disconnectable(self._knob_wildcard)
        self._knob_wildcard.add_value_listener(self._do_wildcard_parameter)

    def _do_play_toggle(self, value):
        if value > 0:
            self.song.is_playing = not self.song.is_playing

    def _do_focus_toggle(self, value):
        if value > 0:
            self._browser_focused = not self._browser_focused
            if self._browser_focused:
                self.application.view.focus_view('Browser')
            else:
                if self.application.view.is_view_visible('Session'):
                    self.application.view.focus_view('Session')
                else:
                    self.application.view.focus_view('Arranger')

    def _do_view_toggle(self, value):
        if value > 0:
            if self.application.view.is_view_visible('Session'):
                self.application.view.hide_view('Session')
            else:
                self.application.view.show_view('Session')

    def _do_solo_toggle(self, value):
        if value > 0:
            track = self.song.view.selected_track
            if track and track != self.song.master_track:
                track.solo = not track.solo

    def _do_mute_toggle(self, value):
        if value > 0:
            track = self.song.view.selected_track
            if track and track != self.song.master_track:
                track.mute = not track.mute

    def _set_selected_track_volume(self, value):
        track = self.song.view.selected_track
        if track:
            self._set_param(track.mixer_device.volume, value)

    def _set_master_track_volume(self, value):
        self._set_param(self.song.master_track.mixer_device.volume, value)

    def _is_boolean_parameter(self, p):
        if p.is_quantized:
            # Se tem itens de valor (listas), checa se tem 2 ou menos opções (booleano)
            if hasattr(p, "value_items") and p.value_items:
                return len(p.value_items) <= 2
            # Se não tem value_items, mas é quantizado com apenas 2 estados (min=0, max=1)
            if abs(p.max - p.min) <= 1.0:
                return True
        return False

    def _get_device_parameter_mapping(self, device):
        if not device:
            return [None] * 8
            
        params = []
        for p in device.parameters:
            if p.name.lower() == "device on":
                continue
            if self._is_boolean_parameter(p):
                continue
            params.append(p)
            
        mapping = [None] * 8
        used_indices = set()
        
        def find_param_by_keywords(keywords):
            for i, p in enumerate(params):
                if i in used_indices:
                    continue
                p_name_lower = p.name.lower()
                for kw in keywords:
                    if kw in p_name_lower:
                        return i, p
            return None, None

        # 1. Map labeled parameters: High, Mid, Low, Filter
        hi_idx, hi_param = find_param_by_keywords(["gainhi", "high", "hi"])
        if hi_param:
            mapping[0] = hi_param
            used_indices.add(hi_idx)
            hi2_idx, hi2_param = find_param_by_keywords(["gainhi", "high", "hi"])
            if hi2_param:
                mapping[4] = hi2_param
                used_indices.add(hi2_idx)

        mid_idx, mid_param = find_param_by_keywords(["gainmid", "mid"])
        if mid_param:
            mapping[1] = mid_param
            used_indices.add(mid_idx)
            mid2_idx, mid2_param = find_param_by_keywords(["gainmid", "mid"])
            if mid2_param:
                mapping[5] = mid2_param
                used_indices.add(mid2_idx)

        lo_idx, lo_param = find_param_by_keywords(["gainlo", "low", "lo"])
        if lo_param:
            mapping[2] = lo_param
            used_indices.add(lo_idx)
            lo2_idx, lo2_param = find_param_by_keywords(["gainlo", "low", "lo"])
            if lo2_param:
                mapping[6] = lo2_param
                used_indices.add(lo2_idx)

        fl_idx, fl_param = find_param_by_keywords(["freq", "frequency", "filter", "cutoff"])
        if fl_param:
            mapping[3] = fl_param
            used_indices.add(fl_idx)
            fl2_idx, fl2_param = find_param_by_keywords(["freq", "frequency", "filter", "cutoff"])
            if fl2_param:
                mapping[7] = fl2_param
                used_indices.add(fl2_idx)

        # 2. Fill remaining slots with unmatched parameters in order
        for i, p in enumerate(params):
            if i in used_indices:
                continue
            for slot in range(8):
                if mapping[slot] is None:
                    mapping[slot] = p
                    used_indices.add(i)
                    break
                    
        return mapping

    def _set_device_knob_param(self, knob_idx, value):
        track = self.song.view.selected_track
        if not track:
            return
        device = track.view.selected_device
        if not device:
            return
            
        mapping = self._get_device_parameter_mapping(device)
        param = mapping[knob_idx]
        if param:
            self._set_param(param, value)

    def _do_wildcard_parameter(self, value):
        param = self.song.view.selected_parameter
        if param:
            self._set_param(param, value)

    def _set_param(self, param, midi_value):
        if param and param.is_enabled:
            norm_val = midi_value / 127.0
            param.value = (norm_val * (param.max - param.min)) + param.min

    def disconnect(self):
        self.log_message("DDJ_LiveBridge desconectado.")
        super(DDJ_LiveBridge, self).disconnect()
