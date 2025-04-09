using System;
using NAudio.CoreAudioApi;
using NAudio.Wave; // Requires NAudio NuGet package

namespace AudioMixerApp.Services
{
    // Service for capturing system audio output (loopback)
    public class SystemAudioCaptureService : IDisposable
    {
        private WasapiLoopbackCapture? _captureDevice;
        private bool _isDisposed;

        // Event to notify subscribers when new audio data is available
        public event EventHandler<WaveInEventArgs>? DataAvailable;

        // Property to expose the WaveFormat of the captured audio
        public WaveFormat? WaveFormat => _captureDevice?.WaveFormat;

        // Starts capturing system audio loopback
        // Note: WasapiLoopbackCapture captures from the *default* render device.
        // If the user changes the default device while capturing, behavior might be unexpected.
        // Capturing from a specific *non-default* output device is more complex.
        public bool StartCapture()
        {
            StopCapture(); // Ensure any previous capture is stopped

            try
            {
                // WasapiLoopbackCapture captures the default system output device
                _captureDevice = new WasapiLoopbackCapture();

                _captureDevice.DataAvailable += OnDataAvailable;
                _captureDevice.RecordingStopped += OnRecordingStopped;

                _captureDevice.StartRecording();
                Console.WriteLine($"Started capturing system audio loopback (Default Render Device)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting system audio capture: {ex.Message}");
                // Common error: No audio playing, or device in exclusive mode.
                StopCapture(); // Clean up on error
                return false;
            }
        }

        // Stops the current audio capture
        public void StopCapture()
        {
            _captureDevice?.StopRecording();
            // _captureDevice is disposed in OnRecordingStopped handler
        }

        // Handles the DataAvailable event from the capture device
        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            // Forward the event to external subscribers
            DataAvailable?.Invoke(this, e);
        }

        // Handles the RecordingStopped event from the capture device
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            _captureDevice?.Dispose();
            _captureDevice = null;

            Console.WriteLine("System audio capture stopped.");
            if (e.Exception != null)
            {
                Console.WriteLine($"System audio capture stopped due to error: {e.Exception.Message}");
            }
        }

        // Dispose pattern implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    StopCapture(); // Ensure capture is stopped and device disposed
                }
                _isDisposed = true;
            }
        }

        // Finalizer (optional)
        ~SystemAudioCaptureService()
        {
            Dispose(false);
        }
    }
}
