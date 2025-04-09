using System;
using NAudio.CoreAudioApi;
using NAudio.Wave; // Requires NAudio NuGet package

namespace AudioMixerApp.Services
{
    // Service for capturing audio from a selected microphone input device
    public class MicrophoneCaptureService : IDisposable
    {
        private WasapiCapture? _captureDevice;
        private BufferedWaveProvider? _bufferedWaveProvider;
        private bool _isDisposed;

        // Event to notify subscribers when new audio data is available
        public event EventHandler<WaveInEventArgs>? DataAvailable;

        // Property to expose the WaveFormat of the captured audio
        public WaveFormat? WaveFormat => _captureDevice?.WaveFormat;

        // Starts capturing audio from the device specified by its ID
        public bool StartCapture(string deviceId)
        {
            StopCapture(); // Ensure any previous capture is stopped

            try
            {
                var deviceService = new AudioDeviceService(); // Consider injecting this dependency later
                var mmDevice = deviceService.GetDeviceById(deviceId);
                deviceService.Dispose(); // Dispose temporary service

                if (mmDevice == null)
                {
                    Console.WriteLine($"Capture device with ID '{deviceId}' not found.");
                    return false;
                }

                _captureDevice = new WasapiCapture(mmDevice);
                _captureDevice.DataAvailable += OnDataAvailable;
                _captureDevice.RecordingStopped += OnRecordingStopped;

                // Optional: Create a buffer if needed for further processing/mixing
                // _bufferedWaveProvider = new BufferedWaveProvider(_captureDevice.WaveFormat);
                // _bufferedWaveProvider.BufferDuration = TimeSpan.FromMilliseconds(200); // Example buffer size

                _captureDevice.StartRecording();
                Console.WriteLine($"Started capturing from: {mmDevice.FriendlyName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting capture: {ex.Message}");
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
            // If using a buffer:
            // _bufferedWaveProvider?.AddSamples(e.Buffer, 0, e.BytesRecorded);

            // Forward the event to external subscribers
            DataAvailable?.Invoke(this, e);
        }

        // Handles the RecordingStopped event from the capture device
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            _captureDevice?.Dispose();
            _captureDevice = null;
            // _bufferedWaveProvider?.ClearBuffer(); // Clear buffer if used

            Console.WriteLine("Capture stopped.");
            if (e.Exception != null)
            {
                Console.WriteLine($"Capture stopped due to error: {e.Exception.Message}");
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
                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _isDisposed = true;
            }
        }

        // Finalizer (optional)
        ~MicrophoneCaptureService()
        {
            Dispose(false);
        }
    }
}
