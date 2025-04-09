using System;
using NAudio.CoreAudioApi;
using NAudio.Wave; // Requires NAudio NuGet package

namespace AudioMixerApp.Services
{
    // Service responsible for playing audio to a selected output device
    public class AudioOutputService : IDisposable
    {
        private WasapiOut? _outputDevice;
        private ISampleProvider? _audioSource; // The mixed audio stream to play
        private bool _isDisposed;

        // Initializes the output service with the audio source and target device ID
        // Returns true if initialization is successful, false otherwise.
        public bool Init(ISampleProvider audioSource, string deviceId)
        {
            Stop(); // Stop any previous playback
            DisposePlayer(); // Dispose previous player instance

            _audioSource = audioSource ?? throw new ArgumentNullException(nameof(audioSource));

            try
            {
                var deviceService = new AudioDeviceService(); // Consider injecting later
                var mmDevice = deviceService.GetDeviceById(deviceId);
                deviceService.Dispose();

                if (mmDevice == null)
                {
                    Console.WriteLine($"Output device with ID '{deviceId}' not found.");
                    return false; // Task 23: Handle device availability at startup
                }

                // Use Exclusive mode for potentially lower latency, ShareMode.Shared is safer
                _outputDevice = new WasapiOut(mmDevice, AudioClientShareMode.Shared, true, 100); // 100ms latency buffer
                _outputDevice.PlaybackStopped += OnPlaybackStopped;

                // Initialize the output device with the audio source
                _outputDevice.Init(_audioSource);

                Console.WriteLine($"Initialized output to: {mmDevice.FriendlyName}");
                return true;
            }
            catch (Exception ex) // Task 24: Basic error handling
            {
                Console.WriteLine($"Error initializing output device: {ex.Message}");
                DisposePlayer(); // Clean up on error
                return false;
            }
        }

        // Starts playback
        public void Play()
        {
            if (_outputDevice != null && _outputDevice.PlaybackState != PlaybackState.Playing)
            {
                try
                {
                    _outputDevice.Play();
                    Console.WriteLine("Output playback started.");
                }
                catch (Exception ex) // Task 24: Basic error handling
                {
                    Console.WriteLine($"Error starting playback: {ex.Message}");
                }
            }
        }

        // Stops playback
        public void Stop()
        {
            if (_outputDevice != null && _outputDevice.PlaybackState != PlaybackState.Stopped)
            {
                try
                {
                    _outputDevice.Stop();
                    // Player is disposed in OnPlaybackStopped handler
                    Console.WriteLine("Output playback stopped.");
                }
                 catch (Exception ex) // Task 24: Basic error handling
                {
                    Console.WriteLine($"Error stopping playback: {ex.Message}");
                     // Force disposal if stop fails? Consider implications.
                    DisposePlayer();
                }
            }
        }

        // Handles the PlaybackStopped event
        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            // We dispose the player here to release the audio device
            DisposePlayer();
            Console.WriteLine("Output playback stopped event received.");
             if (e.Exception != null) // Task 24: Basic error handling
            {
                Console.WriteLine($"Playback stopped unexpectedly: {e.Exception.Message}");
            }
        }

        // Helper method to dispose the WasapiOut instance
        private void DisposePlayer()
        {
            _outputDevice?.Dispose();
            _outputDevice = null;
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
                    Stop(); // Ensure playback is stopped
                    DisposePlayer(); // Ensure player is disposed
                    _audioSource = null;
                }
                _isDisposed = true;
            }
        }

        // Finalizer (optional)
        ~AudioOutputService()
        {
            Dispose(false);
        }
    }
}
