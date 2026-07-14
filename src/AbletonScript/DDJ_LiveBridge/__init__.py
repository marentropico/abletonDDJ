# Ableton Live 12 Remote Script
import sys
from .DDJ_LiveBridge import DDJ_LiveBridge

def create_instance(c_instance):
    return DDJ_LiveBridge(c_instance)
