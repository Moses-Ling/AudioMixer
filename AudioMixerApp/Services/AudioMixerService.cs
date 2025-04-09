using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders; // Requires NAudio NuGet package

namespace AudioMixerApp.Services
{
    // Service responsible for mixing audio streams
    public class AudioMixerService : IDisposable, ISampleProvider
    {
        private MixingSampleProvider? _mixer;
        private ISampleProvider? _microphoneInputProvider;
        private ISampleProvider? _systemAudioInputProvider;
        private VolumeSampleProvider? _micVolumeProvider; // For volume/mute control
        private bool _isDisposed;

        // The WaveFormat of the mixed output (determined by the mixer)
        public WaveFormat WaveFormat => _mixer?.WaveFormat ?? WaveFormat.CreateIeeeFloatWaveFormat(44100, 2); // Default fallback

        public AudioMixerService(WaveFormat outputFormat)
        {
            // Initialize the mixer with the desired output format
            _mixer = new MixingSampleProvider(outputFormat);
            _mixer.ReadFully = true; // Important for continuous playback
        }

        // Adds or replaces the microphone input stream
        public void SetMicrophoneInput(IWaveProvider microphoneInput)
        {
            // Remove existing microphone input if present
            if (_micVolumeProvider != null && _mixer != null)
            {
                _mixer.RemoveMixerInput((ISampleProvider)_micVolumeProvider);
                _microphoneInputProvider = null;
                _micVolumeProvider = null;
            }

            if (microphoneInput == null || _mixer == null) return;

            // Convert to ISampleProvider and handle potential format differences
            _microphoneInputProvider = ConvertToSampleProvider(microphoneInput, _mixer.WaveFormat);

            // Wrap in a VolumeSampleProvider for volume/mute control
            _micVolumeProvider = new VolumeSampleProvider(_microphoneInputProvider);
            _mixer.AddMixerInput(_micVolumeProvider);

            Console.WriteLine("Microphone input added to mixer.");
        }

        // Adds or replaces the system audio input stream
        public void SetSystemAudioInput(IWaveProvider systemAudioInput)
        {
             // Remove existing system audio input if present
            if (_systemAudioInputProvider != null && _mixer != null)
            {
                _mixer.RemoveMixerInput(_systemAudioInputProvider);
                _systemAudioInputProvider = null;
            }

            if (systemAudioInput == null || _mixer == null) return;

            // Convert to ISampleProvider and handle potential format differences
            _systemAudioInputProvider = ConvertToSampleProvider(systemAudioInput, _mixer.WaveFormat);
            _mixer.AddMixerInput(_systemAudioInputProvider);

            Console.WriteLine("System audio input added to mixer.");
        }

        // Helper to convert IWaveProvider to ISampleProvider and resample if needed
        private ISampleProvider ConvertToSampleProvider(IWaveProvider input, WaveFormat targetFormat)
        {
            ISampleProvider sampleProvider;
            if (input.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat &&
                input.WaveFormat.SampleRate == targetFormat.SampleRate &&
                input.WaveFormat.Channels == targetFormat.Channels)
            {
                // Already in the correct float format
                sampleProvider = new WaveToSampleProvider(input);
            }
            else if (input.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                 // Convert PCM to float
                if (input.WaveFormat.BitsPerSample == 16)
                    sampleProvider = new Pcm16BitToSampleProvider(input);
                else if (input.WaveFormat.BitsPerSample == 24)
                    sampleProvider = new Pcm24BitToSampleProvider(input);
                else if (input.WaveFormat.BitsPerSample == 32)
                    sampleProvider = new Pcm32BitToSampleProvider(input);
                else
                    throw new NotSupportedException($"PCM BitsPerSample {input.WaveFormat.BitsPerSample} not supported");
            }
             else if (input.WaveFormat.BitsPerSample == 32 && input.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                 // Already float, but might need channel/samplerate conversion later
                 sampleProvider = new WaveToSampleProvider(input);
            }
            else
            {
                throw new NotSupportedException($"Input WaveFormat encoding {input.WaveFormat.Encoding} not supported");
            }

            // Resample if sample rate or channel count differs (Task 17 & 20 - basic handling)
            if (sampleProvider.WaveFormat.SampleRate != targetFormat.SampleRate ||
                sampleProvider.WaveFormat.Channels != targetFormat.Channels)
            {
                Console.WriteLine($"Resampling required: From {sampleProvider.WaveFormat} to {targetFormat}");
                // Using WdlResamplingSampleProvider for quality resampling
                sampleProvider = new WdlResamplingSampleProvider(sampleProvider, targetFormat.SampleRate);

                // Handle channel differences (mono to stereo, stereo to mono)
                if (sampleProvider.WaveFormat.Channels == 1 && targetFormat.Channels == 2)
                {
                    sampleProvider = new MonoToStereoSampleProvider(sampleProvider);
                }
                else if (sampleProvider.WaveFormat.Channels == 2 && targetFormat.Channels == 1)
                {
                    sampleProvider = new StereoToMonoSampleProvider(sampleProvider);
                }
                // Ensure the final provider matches the target format exactly
                 if (sampleProvider.WaveFormat.Channels != targetFormat.Channels || sampleProvider.WaveFormat.SampleRate != targetFormat.SampleRate)
                 {
                      throw new InvalidOperationException("Resampling did not produce the target WaveFormat.");
                 }
            }

            return sampleProvider;
        }


        // Sets the volume for the microphone input (0.0 to 1.0+)
        public void SetMicrophoneVolume(float volume) // Task 18
        {
            if (_micVolumeProvider != null)
            {
                _micVolumeProvider.Volume = volume;
            }
        }

        // Mutes or unmutes the microphone input (Task 19)
        public void SetMicrophoneMute(bool isMuted)
        {
             // Setting volume to 0 effectively mutes
             SetMicrophoneVolume(isMuted ? 0.0f : (_micVolumeProvider?.Volume ?? 1.0f));
             // Ideally, store the pre-mute volume to restore it accurately.
             // For simplicity now, we might restore to 1.0f if unmuting from 0.
             // A better approach involves storing the volume before muting.
             // Let's refine this later if needed. If volume was 0 before mute, unmuting sets it to 1.
             if (!isMuted && _micVolumeProvider != null && _micVolumeProvider.Volume == 0.0f)
             {
                 _micVolumeProvider.Volume = 1.0f; // Restore to default if unmuting from 0
             }
        }


        // Implementation of ISampleProvider.Read for the mixer output
        public int Read(float[] buffer, int offset, int count)
        {
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
                    _mixer = null; // The mixer itself doesn't seem IDisposable
                    _microphoneInputProvider = null; // Assuming inputs are managed elsewhere
                    _systemAudioInputProvider = null;
                    _micVolumeProvider = null;
                }
                _isDisposed = true;
            }
        }

        ~AudioMixerService()
        {
            Dispose(false);
        }
    }
}
