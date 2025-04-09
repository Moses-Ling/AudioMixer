using System;
using System.Collections.ObjectModel; // For ObservableCollection
using System.ComponentModel; // For INotifyPropertyChanged
using System.Linq; // For LINQ operations like FirstOrDefault
using System.Runtime.CompilerServices; // For CallerMemberName
using System.Threading.Tasks; // For Task
using System.Windows.Input; // For ICommand
using AudioMixerApp.Helpers; // For RelayCommand
using AudioMixerApp.Services; // To reference AudioDevice etc.
using NAudio.Wave; // For WaveFormat
using Microsoft.Win32; // For Registry access
using System.Reflection; // For Assembly to get executable path
using System.Windows; // For MessageBox

namespace AudioMixerApp.ViewModels
{
    // Base class implementing INotifyPropertyChanged
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // The main ViewModel for the application window
    public class MainViewModel : ViewModelBase
    {
        // Services
        private readonly SettingsService _settingsService;
        private readonly AudioDeviceService _audioDeviceService;
        private readonly MicrophoneCaptureService _microphoneCaptureService;
        private readonly SystemAudioCaptureService _systemAudioCaptureService;
        private readonly AudioMixerService _audioMixerService;
        private readonly AudioOutputService _audioOutputService;

        // Buffers for audio data between capture and mixer
        private BufferedWaveProvider? _micBuffer;
        private BufferedWaveProvider? _sysBuffer;

        // Properties for UI Binding (Examples - will be expanded)

        private ObservableCollection<AudioDevice> _inputDevices = new();
        public ObservableCollection<AudioDevice> InputDevices
        {
            get => _inputDevices;
            set => SetProperty(ref _inputDevices, value);
        }

        private AudioDevice? _selectedInputDevice;
        public AudioDevice? SelectedInputDevice
        {
            get => _selectedInputDevice;
            set
            {
                if (SetProperty(ref _selectedInputDevice, value))
                {
                    // Logic to handle input device change will go here
                    Console.WriteLine($"Input device selected: {value?.Name ?? "None"}");
                    // Trigger saving settings when selection changes
                    _ = SaveSettingsAsync();
                    // TODO: Restart capture/mixing process with the new device
                }
            }
        }

        private ObservableCollection<AudioDevice> _outputDevices = new();
        public ObservableCollection<AudioDevice> OutputDevices
        {
            get => _outputDevices;
            set => SetProperty(ref _outputDevices, value);
        }

         private AudioDevice? _selectedOutputDevice;
        public AudioDevice? SelectedOutputDevice
        {
            get => _selectedOutputDevice;
            set
            {
                 if (SetProperty(ref _selectedOutputDevice, value))
                {
                    // Logic to handle output device change will go here
                    Console.WriteLine($"Output device selected: {value?.Name ?? "None"}");
                    // Trigger saving settings when selection changes
                    _ = SaveSettingsAsync();
                    // TODO: Restart output process with the new device
                }
            }
        }

        private double _microphoneVolumePercent = 75.0;
        public double MicrophoneVolumePercent
        {
            get => _microphoneVolumePercent;
            set
            {
                if (SetProperty(ref _microphoneVolumePercent, value))
                {
                    // Logic to update mixer volume will go here
                    _audioMixerService?.SetMicrophoneVolume((float)(value / 100.0)); // Convert percentage to 0.0-1.0 scale
                    Console.WriteLine($"Volume set to: {value:F0}%");
                    // Trigger saving settings when volume changes
                    _ = SaveSettingsAsync();
                }
            }
        }

         private bool _isMicrophoneMuted = false;
        public bool IsMicrophoneMuted
        {
            get => _isMicrophoneMuted;
            set
            {
                 if (SetProperty(ref _isMicrophoneMuted, value))
                 {
                    // Logic to update mixer mute state will go here
                    _audioMixerService?.SetMicrophoneMute(value);
                    Console.WriteLine($"Mute set to: {value}");
                    // Trigger saving settings when mute state changes
                    _ = SaveSettingsAsync();
                    // If muted, reset the level meter immediately and change color
                    if (value)
                    {
                        MicrophoneLevel = 0;
                        LevelMeterColor = "LightGray"; // Color when muted
                    }
                    else
                    {
                        LevelMeterColor = "DodgerBlue"; // Restore default color when unmuted
                    }
                 }
            }
        }

         private double _microphoneLevel = 0; // 0-100 for ProgressBar
        public double MicrophoneLevel
        {
            get => _microphoneLevel;
            set => SetProperty(ref _microphoneLevel, value); // Updated frequently by capture service
        }

        // Status properties (Example)
        private string _statusText = "Idle";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _statusColor = "Gray"; // Use color names or hex codes
        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        private string _levelMeterColor = "DodgerBlue"; // Default progress bar color
        public string LevelMeterColor
        {
            get => _levelMeterColor;
            set => SetProperty(ref _levelMeterColor, value);
        }

        private bool _startWithWindows = false;
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (SetProperty(ref _startWithWindows, value))
                {
                    SetStartup(value); // Call registry update method
                }
            }
        }


        // Constructor
        public MainViewModel()
        {
            // Initialize services (basic example, proper DI is better)
            _settingsService = new SettingsService();
            _audioDeviceService = new AudioDeviceService();
            _microphoneCaptureService = new MicrophoneCaptureService();
            _systemAudioCaptureService = new SystemAudioCaptureService();
            _audioOutputService = new AudioOutputService();

            // Define a common output format (e.g., 48kHz, 32-bit float, stereo)
            // This should ideally match the preferred format of the output device or be configurable.
            var outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
            _audioMixerService = new AudioMixerService(outputFormat);


            // Load devices and settings asynchronously
            // We don't await here to avoid blocking the constructor,
            // but handle potential race conditions if UI is accessed before load completes.
            _ = InitializeAsync();


            // Initialize Commands (Task 33)
            StartStopCommand = new RelayCommand(ExecuteStartStop, CanExecuteStartStop);
            // Add other commands later (e.g., for system tray)

            // Read initial startup state from registry
            _startWithWindows = IsStartupEnabled();
            OnPropertyChanged(nameof(StartWithWindows)); // Notify UI of initial state
        }

        // Asynchronous initialization
        private async Task InitializeAsync()
        {
            LoadAudioDevices();
            await LoadSettingsAsync(); // Load and apply settings

            // Wire up event handlers
            _microphoneCaptureService.DataAvailable += MicrophoneDataAvailable;
            _systemAudioCaptureService.DataAvailable += SystemAudioDataAvailable;
        }

        // Method to load audio devices into the collections
        private void LoadAudioDevices()
        {
            InputDevices.Clear();
            var inputs = _audioDeviceService.GetInputDevices();
            foreach (var device in inputs) InputDevices.Add(device);
            // Select default or last used device later

            OutputDevices.Clear();
            var outputs = _audioDeviceService.GetOutputDevices();
             foreach (var device in outputs) OutputDevices.Add(device);
             // Select default or last used device later

             Console.WriteLine($"Loaded {InputDevices.Count} input devices and {OutputDevices.Count} output devices.");
        }

        // Load settings from storage and apply them (Task 35)
        private async Task LoadSettingsAsync()
        {
            Console.WriteLine("Loading settings...");
            var settings = await _settingsService.LoadSettingsAsync();

            // Apply settings to ViewModel properties
            // Use FirstOrDefault to find matching device by ID
            SelectedInputDevice = InputDevices.FirstOrDefault(d => d.Id == settings.LastInputDeviceId);
            SelectedOutputDevice = OutputDevices.FirstOrDefault(d => d.Id == settings.LastOutputDeviceId);

            // Fallback to default if last used device not found or not set
            if (SelectedInputDevice == null) SelectedInputDevice = _audioDeviceService.GetDefaultInputDevice();
            if (SelectedOutputDevice == null) SelectedOutputDevice = _audioDeviceService.GetDefaultOutputDevice();

            MicrophoneVolumePercent = settings.LastVolumePercent;
            IsMicrophoneMuted = settings.LastMuteState;

            Console.WriteLine("Settings loaded and applied.");
        }

        // Save current settings to storage (Task 35)
        private async Task SaveSettingsAsync()
        {
            Console.WriteLine("Saving settings...");
            var settings = new AppSettings
            {
                LastInputDeviceId = SelectedInputDevice?.Id,
                LastOutputDeviceId = SelectedOutputDevice?.Id,
                LastVolumePercent = MicrophoneVolumePercent,
                LastMuteState = IsMicrophoneMuted
            };
            await _settingsService.SaveSettingsAsync(settings);
            Console.WriteLine("Settings saved.");
        }

        // --- Commands --- (Task 33)

        public ICommand StartStopCommand { get; }

        private bool _isProcessing = false; // Flag to track if audio processing is active

        // TODO: Implement the actual logic to start/stop audio capture, mixing, and output
        private void ExecuteStartStop()
        {
            if (_isProcessing)
            {
                // --- Stop Logic ---
                Console.WriteLine("Stopping audio processing...");
                _audioOutputService?.Stop();
                _microphoneCaptureService?.StopCapture();
                _systemAudioCaptureService?.StopCapture();
                // Clear mixer inputs? Depends on desired behavior.
                // _audioMixerService?.SetMicrophoneInput(null);
                // _audioMixerService?.SetSystemAudioInput(null);

                StatusText = "Idle";
                StatusColor = "Gray";
                _isProcessing = false;
            }
            else
            {
                // --- Start Logic ---
                Console.WriteLine("Starting audio processing...");
                 if (SelectedInputDevice == null || SelectedOutputDevice == null)
                 {
                     Console.WriteLine("Error: Input or Output device not selected.");
                     MessageBox.Show("Please select both an input and an output device before starting.", "Device Selection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                     StatusText = "Error: Select Devices";
                     StatusColor = "Red";
                     return;
                 }

                // Initialize output with the mixer as the source
                 if (!_audioOutputService.Init(_audioMixerService, SelectedOutputDevice.Id))
                 {
                      Console.WriteLine("Error: Failed to initialize output device.");
                      MessageBox.Show($"Failed to initialize the selected output device: {SelectedOutputDevice.Name}", "Output Device Error", MessageBoxButton.OK, MessageBoxImage.Error);
                      StatusText = "Error: Output Init Failed";
                      StatusColor = "Red";
                      return;
                 }

                // Start microphone capture
                 if (!_microphoneCaptureService.StartCapture(SelectedInputDevice.Id))
                 {
                      Console.WriteLine("Error: Failed to start microphone capture.");
                      MessageBox.Show($"Failed to start capturing from the selected microphone: {SelectedInputDevice.Name}", "Microphone Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                      StatusText = "Error: Mic Capture Failed";
                      StatusColor = "Red";
                      _audioOutputService.Stop(); // Stop output if mic fails
                      return;
                 }
                // Create buffer for microphone input and connect to mixer
                if (_microphoneCaptureService.WaveFormat != null)
                 {
                     _micBuffer = new BufferedWaveProvider(_microphoneCaptureService.WaveFormat)
                     {
                         BufferDuration = TimeSpan.FromMilliseconds(100), // Reduced buffer to 100ms
                         DiscardOnBufferOverflow = true // Prevent buffer from growing indefinitely
                     };
                     _audioMixerService.SetMicrophoneInput(_micBuffer);
                }
                else
                {
                     Console.WriteLine("Error: Microphone capture WaveFormat is null.");
                     StatusText = "Error: Mic Format";
                     StatusColor = "Red";
                     _microphoneCaptureService.StopCapture();
                     _audioOutputService.Stop();
                     return;
                }


                // Start system audio capture
                if (!_systemAudioCaptureService.StartCapture())
                {
                     Console.WriteLine("Warning: Failed to start system audio capture. Continuing without it.");
                     // Optionally show warning to user
                     // StatusText = "Warning: System Audio Failed";
                     // StatusColor = "Orange";
                     // Don't stop everything, just proceed without system audio
                     _sysBuffer = null; // Ensure buffer is null if capture fails
                     _audioMixerService.SetSystemAudioInput(null); // Ensure mixer doesn't use old buffer
                }
                else
                {
                    // Create buffer for system audio input and connect to mixer
                    if (_systemAudioCaptureService.WaveFormat != null)
                     {
                          _sysBuffer = new BufferedWaveProvider(_systemAudioCaptureService.WaveFormat)
                          {
                              BufferDuration = TimeSpan.FromMilliseconds(100), // Reduced buffer to 100ms
                              DiscardOnBufferOverflow = true
                          };
                          _audioMixerService.SetSystemAudioInput(_sysBuffer);
                    }
                     else
                    {
                        Console.WriteLine("Warning: System audio capture WaveFormat is null. Continuing without system audio.");
                        _sysBuffer = null;
                        _audioMixerService.SetSystemAudioInput(null);
                    }
                }


                // Start playback
                _audioOutputService.Play();

                // Apply initial volume/mute from UI
                _audioMixerService.SetMicrophoneVolume((float)(MicrophoneVolumePercent / 100.0));
                _audioMixerService.SetMicrophoneMute(IsMicrophoneMuted);


                StatusText = "Mixing Active";
                StatusColor = "Green";
                _isProcessing = true;
            }
            // Notify command system that CanExecute might have changed
             CommandManager.InvalidateRequerySuggested();
        }

        // Determine if the Start/Stop command can execute
        private bool CanExecuteStartStop()
        {
            // Can always execute for now, logic might depend on device selection later
            return true;
            // Example: return SelectedInputDevice != null && SelectedOutputDevice != null;
        }


        // --- Event Handlers for Audio Data ---

        private void MicrophoneDataAvailable(object? sender, WaveInEventArgs e)
        {
            // If muted, don't process level or add to buffer
            if (IsMicrophoneMuted || _micBuffer == null)
            {
                 // Ensure level stays 0 if muted
                 if (IsMicrophoneMuted && MicrophoneLevel != 0) MicrophoneLevel = 0;
                 return;
            }

            // Add captured data to the buffer
            _micBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);

            // Calculate peak level for the UI meter (Task 29 update)
            float max = 0;
            // Interpret buffer based on WaveFormat (assuming float for WasapiCapture)
            if (_micBuffer.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                var buffer = new WaveBuffer(e.Buffer);
                int samples = e.BytesRecorded / 4; // 4 bytes per float sample
                for (int i = 0; i < samples; i++)
                {
                    var sample = Math.Abs(buffer.FloatBuffer[i]);
                    if (sample > max) max = sample;
                }
            }
            // Add handling for other formats if necessary

            // Update the UI property (scale 0.0-1.0 to 0-100)
            // Use Dispatcher for thread safety if needed, but OnPropertyChanged usually handles it
            MicrophoneLevel = max * 100.0;
        }

        private void SystemAudioDataAvailable(object? sender, WaveInEventArgs e)
        {
             if (_sysBuffer == null) return;
            // Add captured data to the buffer
            _sysBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        // Cleanup
        public void Cleanup()
        {
            // Stop and dispose all audio services
            _audioOutputService?.Stop(); // Stop output first
            _microphoneCaptureService?.StopCapture();
            _systemAudioCaptureService?.StopCapture();

            _audioOutputService?.Dispose();
            _audioMixerService?.Dispose();
            _microphoneCaptureService?.Dispose();
            _systemAudioCaptureService?.Dispose();
            _audioDeviceService?.Dispose();
            // Settings service doesn't need disposal currently

            // Save settings one last time on cleanup
            _ = SaveSettingsAsync();

            Console.WriteLine("ViewModel cleaned up.");
        }


        // --- Startup with Windows Logic (Task 39) ---

        private const string AppRegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppRegistryValueName = "AudioMixerApp"; // Choose a unique name

        private bool IsStartupEnabled()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(AppRegistryKey, false);
                return key?.GetValue(AppRegistryValueName) != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading startup registry key: {ex.Message}");
                return false; // Assume disabled if error occurs
            }
        }

        private void SetStartup(bool enable)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(AppRegistryKey, true);
                if (key == null)
                {
                    Console.WriteLine($"Error: Could not open registry key HKEY_CURRENT_USER\\{AppRegistryKey} for writing.");
                    return;
                }

                string? executablePath = Assembly.GetExecutingAssembly().Location;
                // For .NET Core/5+, Location might be the .dll. Need the host executable (.exe).
                // A common way is to replace .dll with .exe if applicable.
                if (!string.IsNullOrEmpty(executablePath) && executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                     executablePath = System.IO.Path.ChangeExtension(executablePath, ".exe");
                }

                if (string.IsNullOrEmpty(executablePath))
                {
                    Console.WriteLine("Error: Could not determine application executable path.");
                    return;
                }


                if (enable)
                {
                    // Add the value to run on startup
                    // Enclose path in quotes if it contains spaces
                    key.SetValue(AppRegistryValueName, $"\"{executablePath}\"");
                    Console.WriteLine($"Added '{AppRegistryValueName}' to startup.");
                }
                else
                {
                    // Remove the value
                    if (key.GetValue(AppRegistryValueName) != null)
                    {
                        key.DeleteValue(AppRegistryValueName, false);
                        Console.WriteLine($"Removed '{AppRegistryValueName}' from startup.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error modifying startup registry key: {ex.Message}");
                // Optionally inform the user via UI
            }
        }
    }
}
