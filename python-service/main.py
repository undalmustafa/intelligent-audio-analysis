"""
Intelligent Audio Analysis - Python Noise Reduction Service
Flask REST API that receives audio, applies noise reduction, and returns processed audio.
"""

from flask import Flask, request, jsonify, send_file
from flask_cors import CORS
from processing import process_audio_file
import subprocess
import os
import uuid
import logging

# ==============================
# App Configuration
# ==============================
app = Flask(__name__)
CORS(app)  # Enable CORS for frontend health checks
app.config['UPLOAD_FOLDER'] = os.path.join(os.path.dirname(__file__), 'temp')
app.config['MAX_CONTENT_LENGTH'] = 50 * 1024 * 1024  # 50 MB max

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s [%(levelname)s] %(name)s: %(message)s'
)
logger = logging.getLogger(__name__)

# Ensure temp directory exists
os.makedirs(app.config['UPLOAD_FOLDER'], exist_ok=True)


def convert_to_wav(input_path: str, output_path: str) -> bool:
    """
    Convert any audio format to WAV using ffmpeg subprocess.
    Works with WebM, MP3, OGG, etc.
    """
    try:
        result = subprocess.run(
            ['ffmpeg', '-y', '-i', input_path, '-ar', '16000', '-ac', '1', output_path],
            capture_output=True,
            text=True,
            timeout=30
        )
        if result.returncode != 0:
            logger.error(f"ffmpeg error: {result.stderr}")
            return False
        return True
    except FileNotFoundError:
        logger.error("ffmpeg not found. Please install ffmpeg.")
        return False
    except subprocess.TimeoutExpired:
        logger.error("ffmpeg conversion timed out")
        return False


# ==============================
# Health Check
# ==============================
@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint for monitoring."""
    # Also check if ffmpeg is available
    ffmpeg_available = True
    try:
        subprocess.run(['ffmpeg', '-version'], capture_output=True, timeout=5)
    except (FileNotFoundError, subprocess.TimeoutExpired):
        ffmpeg_available = False

    return jsonify({
        'status': 'healthy',
        'service': 'python-audio-processor',
        'version': '1.0.0',
        'ffmpeg_available': ffmpeg_available
    }), 200


# ==============================
# Process Audio Endpoint
# ==============================
@app.route('/process', methods=['POST'])
def process_audio():
    """
    Receives raw audio data, applies noise reduction, and returns processed audio.
    
    Expected: multipart/form-data with 'file' field containing audio data.
    Returns: Processed WAV audio file.
    """
    if 'file' not in request.files:
        return jsonify({'error': 'No audio file provided'}), 400

    audio_file = request.files['file']

    if audio_file.filename == '':
        return jsonify({'error': 'Empty filename'}), 400

    logger.info(f"Received audio file: {audio_file.filename}, "
                f"Content-Type: {audio_file.content_type}")

    # Generate unique filenames for temp storage
    file_id = str(uuid.uuid4())
    temp_dir = app.config['UPLOAD_FOLDER']
    input_raw_path = os.path.join(temp_dir, f"{file_id}_raw_input")
    input_wav_path = os.path.join(temp_dir, f"{file_id}_input.wav")
    output_wav_path = os.path.join(temp_dir, f"{file_id}_processed.wav")

    try:
        # Save the uploaded file
        audio_file.save(input_raw_path)
        file_size = os.path.getsize(input_raw_path)
        logger.info(f"Saved raw input: {file_size} bytes")

        # Check if file is already WAV (ESP32 sends WAV directly)
        is_wav = False
        with open(input_raw_path, 'rb') as f:
            header = f.read(4)
            is_wav = header == b'RIFF'

        if is_wav:
            # Already WAV — use directly, no ffmpeg needed
            input_wav_path = input_raw_path
            logger.info("File is already WAV format, skipping conversion")
        else:
            # Non-WAV (e.g., WebM) — convert with ffmpeg
            if not convert_to_wav(input_raw_path, input_wav_path):
                return jsonify({
                    'error': 'Audio format conversion failed. Make sure ffmpeg is installed.',
                }), 422
            logger.info("Converted to WAV successfully")

        # Apply noise reduction pipeline
        metadata = process_audio_file(input_wav_path, output_wav_path)

        logger.info(f"Processing complete. Noise reduction: {metadata['noise_reduction_db']} dB")

        # Return the processed WAV file
        return send_file(
            output_wav_path,
            mimetype='audio/wav',
            as_attachment=True,
            download_name=f'processed_{file_id}.wav'
        )

    except Exception as e:
        logger.error(f"Processing error: {e}", exc_info=True)
        return jsonify({
            'error': 'Audio processing failed',
            'detail': str(e)
        }), 500

    finally:
        # Cleanup temp files
        for path in [input_raw_path, input_wav_path, output_wav_path]:
            try:
                if os.path.exists(path):
                    os.remove(path)
            except OSError:
                pass


# ==============================
# Entry Point
# ==============================
if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5002))
    logger.info(f"Starting Python Audio Processor on port {port}")
    app.run(host='0.0.0.0', port=port, debug=True)
