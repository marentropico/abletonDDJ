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
        
        # 3. EQs & Filtros - Deck Esquerdo (CC 21 a 25)
        self._knob_trim_l = ButtonElement(True, MIDI_CC_TYPE, 15, 21)
        self.register_disconnectable(self._knob_trim_l)
        self._knob_trim_l.add_value_listener(lambda v: self._set_track_volume(0, v))
        
        self._knob_eq_hi_l = ButtonElement(True, MIDI_CC_TYPE, 15, 22)
        self.register_disconnectable(self._knob_eq_hi_l)
        self._knob_eq_hi_l.add_value_listener(lambda v: self._set_eq_param(0, "GainHi", v))

        self._knob_eq_mid_l = ButtonElement(True, MIDI_CC_TYPE, 15, 23)
        self.register_disconnectable(self._knob_eq_mid_l)
        self._knob_eq_mid_l.add_value_listener(lambda v: self._set_eq_param(0, "GainMid", v))

        self._knob_eq_lo_l = ButtonElement(True, MIDI_CC_TYPE, 15, 24)
        self.register_disconnectable(self._knob_eq_lo_l)
        self._knob_eq_lo_l.add_value_listener(lambda v: self._set_eq_param(0, "GainLo", v))

        self._knob_filter_l = ButtonElement(True, MIDI_CC_TYPE, 15, 25)
        self.register_disconnectable(self._knob_filter_l)
        self._knob_filter_l.add_value_listener(lambda v: self._set_filter_param(0, v))

        # 4. EQs & Filtros - Deck Direito (CC 31 a 35)
        self._knob_trim_r = ButtonElement(True, MIDI_CC_TYPE, 15, 31)
        self.register_disconnectable(self._knob_trim_r)
        self._knob_trim_r.add_value_listener(lambda v: self._set_track_volume(1, v))
        
        self._knob_eq_hi_r = ButtonElement(True, MIDI_CC_TYPE, 15, 32)
        self.register_disconnectable(self._knob_eq_hi_r)
        self._knob_eq_hi_r.add_value_listener(lambda v: self._set_eq_param(1, "GainHi", v))

        self._knob_eq_mid_r = ButtonElement(True, MIDI_CC_TYPE, 15, 33)
        self.register_disconnectable(self._knob_eq_mid_r)
        self._knob_eq_mid_r.add_value_listener(lambda v: self._set_eq_param(1, "GainMid", v))

        self._knob_eq_lo_r = ButtonElement(True, MIDI_CC_TYPE, 15, 34)
        self.register_disconnectable(self._knob_eq_lo_r)
        self._knob_eq_lo_r.add_value_listener(lambda v: self._set_eq_param(1, "GainLo", v))

        self._knob_filter_r = ButtonElement(True, MIDI_CC_TYPE, 15, 35)
        self.register_disconnectable(self._knob_filter_r)
        self._knob_filter_r.add_value_listener(lambda v: self._set_filter_param(1, v))

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

    def _set_track_volume(self, track_idx, value):
        tracks = self.song.visible_tracks
        if len(tracks) > track_idx:
            track = tracks[track_idx]
            self._set_param(track.mixer_device.volume, value)

    def _set_eq_param(self, track_idx, param_name, value):
        tracks = self.song.visible_tracks
        if len(tracks) > track_idx:
            track = tracks[track_idx]
            if len(track.devices) > 0:
                dev = track.devices[0]
                self._set_device_param(dev, param_name, value)

    def _set_filter_param(self, track_idx, value):
        tracks = self.song.visible_tracks
        if len(tracks) > track_idx:
            track = tracks[track_idx]
            if len(track.devices) > 0:
                dev = track.devices[0]
                self._set_device_param(dev, "Freq", value)
                self._set_device_param(dev, "Frequency", value)

    def _set_param(self, param, midi_value):
        if param and param.is_enabled:
            norm_val = midi_value / 127.0
            param.value = (norm_val * (param.max - param.min)) + param.min

    def _set_device_param(self, device, param_name, midi_value):
        for p in device.parameters:
            if param_name.lower() in p.name.lower() or p.name.lower() in param_name.lower():
                self._set_param(p, midi_value)
                return

    def disconnect(self):
        self.log_message("DDJ_LiveBridge desconectado.")
        super(DDJ_LiveBridge, self).disconnect()
