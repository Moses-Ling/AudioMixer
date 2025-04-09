using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders; // Requires NAudio NuGet package

namespace AudioMixerApp.Services
{
    // Service responsible for mixing audio streams using ISampleProvider inputs
    public class AudioMixerService : IDisposable, ISampleProvider
    {
        private readonly MixingSampleProvider _mixer;
        private ISampleProvider? _microphoneInputProvider; // The raw or AEC-processed mic input
        private ISampleProvider? _systemAudioInputProvider;  // The raw system audio input
        private VolumeSampleProvider? _micVolumeProvider;    // Wrapper for volume/mute control
        private bool _isDisposed;

        // Note: The UseEchoCancellation property was removed as the decision
        // to use AEC is now handled in the ViewModel before setting the input.
        // The mixer just accepts whatever ISampleProvider it's given for the mic.

        // The WaveFormat of the mixed output (determined by the mixer)
        public WaveFormat WaveFormat => _mixer.WaveFormat;

        public AudioMixerService(WaveFormat outputFormat)
        {
            // Initialize the mixer with the desired output format
            _mixer = new MixingSampleProvider(outputFormat);
            _mixer.ReadFully = true; // Important for continuous playback
        }

        // Adds or replaces the microphone input stream (expects ISampleProvider)
        public void SetMicrophoneInput(ISampleProvider? microphoneInputProvider)
        {
            // Remove existing microphone input chain if present
            if (_micVolumeProvider != null)
            {
                _mixer.RemoveMixerInput(_micVolumeProvider);
                _micVolumeProvider = null; // Allow GC
            }
            _microphoneInputProvider = microphoneInputProvider; // Store the (potentially AEC processed) input

            if (_microphoneInputProvider != null)
            {
                // Wrap the final microphone input (raw or AEC) in a VolumeSampleProvider
                _micVolumeProvider = new VolumeSampleProvider(_microphoneInputProvider);
                // Apply current volume/mute state
                // Retrieve volume before mute if possible, or use a stored value.
                // For now, just apply based on current state (might restore to 1.0f if unmuted from 0).
                float currentVolume = _micVolumeProvider.Volume; // Get current volume before potentially muting
                SetMicrophoneVolume(currentVolume); // Re-apply volume (handles mute)

                _mixer.AddMixerInput(_micVolumeProvider);
                Console.WriteLine("Microphone input set/updated in mixer.");
            }
            else
            {
                Console.WriteLine("Microphone input cleared from mixer.");
            }
        }

        // Adds or replaces the system audio input stream (expects ISampleProvider)
        public void SetSystemAudioInput(ISampleProvider? systemAudioInputProvider)
        {
             // Remove existing system audio input if present
            if (_systemAudioInputProvider != null)
            {
                _mixer.RemoveMixerInput(_systemAudioInputProvider);
            }

            _systemAudioInputProvider = systemAudioInputProvider;

            if (_systemAudioInputProvider != null)
            {
                 // Add the new system audio input directly to the mixer
                 _mixer.AddMixerInput(_systemAudioInputProvider);
                 Console.WriteLine("System audio input set/updated in mixer.");
            }
             else
             {
                 Console.WriteLine("System audio input cleared from mixer.");
             }
             // Note: No need to reconfigure mic input here anymore,
             // AEC decision is made in ViewModel before calling SetMicrophoneInput.
        }

        // Sets the volume for the microphone input (0.0 to 1.0+)
        public void SetMicrophoneVolume(float volume)
        {
            if (_micVolumeProvider != null)
            {
                // Store the volume internally if needed for unmute, or handle in ViewModel
                _micVolumeProvider.Volume = volume;
                Console.WriteLine($"Mixer mic volume set to: {volume:F2}");
            }
        }

        // Mutes or unmutes the microphone input (Task 19)
        public void SetMicrophoneMute(bool isMuted)
        {
             if (_micVolumeProvider != null)
             {
                 // A simple mute implementation by setting volume to 0.
                 // A more robust implementation would store the volume before muting.
                 // The ViewModel currently handles restoring volume when unmuting.
                 _micVolumeProvider.Volume = isMuted ? 0.0f : (_micVolumeProvider.Volume > 0.0f ? _micVolumeProvider.Volume : 1.0f); // Restore or set to 1.0f
                 Console.WriteLine($"Mixer mic mute set to: {isMuted} (Volume: {_micVolumeProvider.Volume:F2})");
             }
        }


        // Implementation of ISampleProvider.Read for the mixer output
        public int Read(float[] buffer, int offset, int count)
        {
            // Read from the internal mixer
            return _mixer?.Read(buffer, offset, count) ?? 0;
        }

        // Dispose pattern
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
                    // Remove inputs explicitly to release references if mixer holds them strongly
                    if (_micVolumeProvider != null) _mixer?.RemoveMixerInput(_micVolumeProvider);
                    if (_systemAudioInputProvider != null) _mixer?.RemoveMixerInput(_systemAudioInputProvider);

                    _microphoneInputProvider = null;
                    _systemAudioInputProvider = null;
                    _micVolumeProvider = null;
                    // Note: _echoCancellationService is managed and disposed by the ViewModel now
                }
                _isDisposed = true;
                Console.WriteLine("AudioMixerService disposed.");
            }
        }

        ~AudioMixerService()
        {
            Dispose(false);
        }
    }
}
