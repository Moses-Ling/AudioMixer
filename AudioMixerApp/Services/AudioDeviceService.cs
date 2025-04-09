using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi; // Requires NAudio NuGet package

namespace AudioMixerApp.Services
{
    // Represents an audio device with its ID and friendly name
    public record AudioDevice(string Id, string Name);

    // Service for managing audio input and output devices
    public class AudioDeviceService
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;

        public AudioDeviceService()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
        }

        // Gets a list of available audio input devices (microphones)
        public IEnumerable<AudioDevice> GetInputDevices()
        {
            try
            {
                return _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                                        .Select(device => new AudioDevice(device.ID, device.FriendlyName));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enumerating input devices: {ex.Message}");
                return Enumerable.Empty<AudioDevice>(); // Return empty list on error
            }
        }

        // Gets a list of available audio output devices (speakers, headphones)
        public IEnumerable<AudioDevice> GetOutputDevices()
        {
            try
            {
                return _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                                        .Select(device => new AudioDevice(device.ID, device.FriendlyName));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enumerating output devices: {ex.Message}");
                return Enumerable.Empty<AudioDevice>(); // Return empty list on error
            }
        }

        // Gets the default audio input device
        public AudioDevice? GetDefaultInputDevice()
        {
            try
            {
                if (!_deviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Capture, Role.Console))
                {
                    return null; // No default device found
                }
                var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                return new AudioDevice(defaultDevice.ID, defaultDevice.FriendlyName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting default input device: {ex.Message}");
                return null;
            }
        }

        // Gets the default audio output device
        public AudioDevice? GetDefaultOutputDevice()
        {
            try
            {
                 if (!_deviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Console))
                {
                    return null; // No default device found
                }
                var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                return new AudioDevice(defaultDevice.ID, defaultDevice.FriendlyName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting default output device: {ex.Message}");
                return null;
            }
        }

        // Placeholder for getting MMDevice by ID (needed for capture/playback)
        public MMDevice? GetDeviceById(string id)
        {
             try
            {
                return _deviceEnumerator.GetDevice(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting device by ID '{id}': {ex.Message}");
                return null;
            }
        }

        // Dispose of the enumerator when done (implement IDisposable if needed later)
        public void Dispose()
        {
            _deviceEnumerator?.Dispose();
        }
    }
}
