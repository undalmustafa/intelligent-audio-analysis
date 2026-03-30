"""
Intelligent Audio Analysis - Audio Processing Module
Applies noise reduction using bandpass filtering and spectral gating.
"""

import numpy as np
from scipy.signal import butter, sosfilt
import soundfile as sf
import noisereduce as nr
import logging
import os

logger = logging.getLogger(__name__)

# ==============================
# Filter Configuration
# ==============================
DEFAULT_LOW_CUTOFF = 300       # Hz — remove low-frequency rumble (AC hum, wind)
DEFAULT_HIGH_CUTOFF = 3400     # Hz — remove high-frequency hiss
FILTER_ORDER = 5               # Butterworth filter order


def bandpass_filter(audio_data: np.ndarray, sample_rate: int,
                    low_cutoff: int = DEFAULT_LOW_CUTOFF,
                    high_cutoff: int = DEFAULT_HIGH_CUTOFF,
                    order: int = FILTER_ORDER) -> np.ndarray:
    """
    Apply a Butterworth bandpass filter to remove low and high frequency noise.
    
    Args:
        audio_data: Input audio signal as numpy array
        sample_rate: Sample rate in Hz
        low_cutoff: Lower frequency bound (Hz) — frequencies below are removed
        high_cutoff: Upper frequency bound (Hz) — frequencies above are removed
        order: Filter order (higher = sharper cutoff)
    
    Returns:
        Filtered audio signal as numpy array
    """
    nyquist = sample_rate / 2.0

    # Clamp cutoff frequencies to valid range
    low = max(low_cutoff, 20) / nyquist
    high = min(high_cutoff, nyquist - 1) / nyquist

    if low >= high:
        logger.warning("Invalid filter range, skipping bandpass filter")
        return audio_data

    sos = butter(order, [low, high], btype='band', output='sos')
    filtered = sosfilt(sos, audio_data)

    logger.info(f"Bandpass filter applied: {low_cutoff}Hz - {high_cutoff}Hz")
    return filtered


def spectral_gate(audio_data: np.ndarray, sample_rate: int) -> np.ndarray:
    """
    Apply spectral gating noise reduction.
    Uses the noisereduce library which estimates noise profile
    from the signal and subtracts it.
    
    Args:
        audio_data: Input audio signal as numpy array
        sample_rate: Sample rate in Hz
    
    Returns:
        Noise-reduced audio signal as numpy array
    """
    reduced = nr.reduce_noise(
        y=audio_data,
        sr=sample_rate,
        prop_decrease=0.75,     # How much to reduce noise (0.0 - 1.0)
        stationary=False,       # Use non-stationary noise reduction
        n_fft=2048,
        hop_length=512,
    )

    logger.info("Spectral gating noise reduction applied")
    return reduced


def normalize_audio(audio_data: np.ndarray) -> np.ndarray:
    """
    Normalize audio to prevent clipping after processing.
    Scales audio to [-1.0, 1.0] range.
    """
    max_val = np.max(np.abs(audio_data))
    if max_val > 0:
        return audio_data / max_val * 0.95  # Leave 5% headroom
    return audio_data


def process_audio_file(input_path: str, output_path: str,
                       low_cutoff: int = DEFAULT_LOW_CUTOFF,
                       high_cutoff: int = DEFAULT_HIGH_CUTOFF) -> dict:
    """
    Full processing pipeline: read audio → bandpass filter → spectral gate → normalize → save.
    
    Args:
        input_path: Path to input WAV file
        output_path: Path to save processed WAV file
        low_cutoff: Bandpass filter lower bound (Hz)
        high_cutoff: Bandpass filter upper bound (Hz)
    
    Returns:
        Dictionary with processing metadata
    """
    logger.info(f"Processing audio: {input_path}")

    # Read audio file
    audio_data, sample_rate = sf.read(input_path, dtype='float64')

    # If stereo, convert to mono
    if len(audio_data.shape) > 1:
        audio_data = np.mean(audio_data, axis=1)
        logger.info("Converted stereo to mono")

    original_rms = np.sqrt(np.mean(audio_data ** 2))

    # Step 1: Bandpass filter (remove rumble + hiss)
    filtered = bandpass_filter(audio_data, sample_rate, low_cutoff, high_cutoff)

    # Step 2: Spectral gating (adaptive noise reduction)
    denoised = spectral_gate(filtered, sample_rate)

    # Step 3: Normalize
    final = normalize_audio(denoised)

    processed_rms = np.sqrt(np.mean(final ** 2))

    # Save processed audio
    sf.write(output_path, final, sample_rate, subtype='PCM_16')

    result = {
        'sample_rate': sample_rate,
        'duration_seconds': round(len(audio_data) / sample_rate, 2),
        'original_rms': round(float(original_rms), 6),
        'processed_rms': round(float(processed_rms), 6),
        'noise_reduction_db': round(20 * np.log10(processed_rms / original_rms + 1e-10), 2),
        'input_file': os.path.basename(input_path),
        'output_file': os.path.basename(output_path),
    }

    logger.info(f"Processing complete: {result}")
    return result
