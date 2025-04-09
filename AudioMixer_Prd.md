# Windows Microphone Audio Mixer
## Product Requirements Document

## 1. Introduction

### 1.1 Purpose
The Windows Microphone Audio Mixer is a desktop application designed to combine microphone input with system audio output and route the mixed audio to a user-selectable playback device. This application addresses the need for a simple, lightweight solution for users who require microphone audio to be heard alongside system audio during conferencing scenarios.

### 1.2 Background
Existing solutions for combining microphone input with system audio often involve complex third-party mixing software with steep learning curves and inconsistent results. The Windows Microphone Audio Mixer aims to provide a focused, user-friendly alternative specifically optimized for conferencing scenarios.

### 1.3 Scope
This document outlines the requirements for the initial release of the Windows Microphone Audio Mixer application. It details the core functionality, user interface, technical implementation, and development approach.

## 2. Product Overview

### 2.1 Product Description
The Windows Microphone Audio Mixer is a lightweight Windows desktop application that allows users to:
- Select a microphone input device
- Select an audio output device
- Control microphone volume level via a slider
- Mute/unmute the microphone via a checkbox
- Monitor microphone input levels visually
- Mix microphone input with system audio in real-time
- Output the combined audio to the selected playback device

### 2.2 Target Users
- Individuals participating in online conferences, meetings, and webinars
- Users who need to share both their voice and system audio simultaneously
- Anyone seeking a simple solution for mixing microphone input with system audio

### 2.3 User Needs
- Simple, intuitive interface without complex mixing controls
- Low latency audio processing suitable for real-time communication
- Reliable operation across standard Windows audio devices
- Minimal setup and configuration requirements

## 3. Requirements

### 3.1 Functional Requirements

#### 3.1.1 Audio Device Management
- **FR1.1:** Enumerate and display all available audio input devices (microphones)
- **FR1.2:** Enumerate and display all available audio output devices
- **FR1.3:** Allow user selection of microphone input device
- **FR1.4:** Allow user selection of audio output device
- **FR1.5:** (Initial Release) Attempt to maintain device connections on startup if previously selected devices are available. Full dynamic handling deferred.
- **FR1.6:** Remember last used input and output devices between sessions

#### 3.1.2 Audio Processing
- **FR2.1:** Capture audio from the selected microphone input
- **FR2.2:** Capture system audio output via WASAPI loopback
- **FR2.3:** Mix microphone and system audio streams in real-time
- **FR2.4:** Apply volume adjustment to microphone stream based on slider position
- **FR2.5:** Apply mute state to microphone stream based on checkbox state
- **FR2.6:** Output mixed audio to selected playback device
- **FR2.7:** Handle transitions when changing input or output devices
- **FR2.8:** Maintain audio synchronization between streams

#### 3.1.3 User Interface
- **FR3.1:** Provide dropdown for microphone input device selection
- **FR3.2:** Provide dropdown for audio output device selection
- **FR3.3:** Include slider control for microphone volume adjustment (0-100%)
- **FR3.4:** Include checkbox for microphone mute/unmute
- **FR3.5:** Display visual meter showing current microphone input level
- **FR3.6:** Show status indicator for active mixing state
- **FR3.7:** Support system tray operation with context menu

### 3.2 Non-Functional Requirements

#### 3.2.1 Performance
- **NFR1.1:** Achieve audio latency less than 200ms under normal conditions
- **NFR1.2:** Maintain stable audio processing during extended operation
- **NFR1.3:** Optimize memory usage for audio buffers
- **NFR1.4:** Minimize CPU utilization during operation

#### 3.2.2 Compatibility
- **NFR2.1:** Support Windows 10 as primary target OS
- **NFR2.2:** Ensure forward compatibility with Windows 11
- **NFR2.3:** Work with standard Windows audio drivers and devices
- **NFR2.4:** Support common audio formats and sample rates (44.1kHz, 48kHz)

#### 3.2.3 Usability
- **NFR3.1:** Provide English-only user interface
- **NFR3.2:** Ensure intuitive operation without technical audio knowledge
- **NFR3.3:** Support minimizing to system tray for background operation
- **NFR3.4:** Require minimal setup and configuration

#### 3.2.4 Reliability
- **NFR4.1:** (Initial Release) Basic handling of device availability at startup. Robust hot-plugging deferred.
- **NFR4.2:** (Initial Release) May require application restart if selected audio devices change or become unavailable during operation. Full recovery deferred.
- **NFR4.3:** Provide fallback to default devices if selected devices become unavailable *at startup*.

### 3.3 Constraints and Limitations
- **C1:** No equalization (EQ) or audio effects processing
- **C2:** Focus on simplicity rather than comprehensive audio mixing features
- **C3:** English-only interface in initial release

## 4. Technical Specifications

### 4.1 Technology Stack
- **TS1:** .NET 6.0+ with C# programming language
- **TS2:** Windows Presentation Foundation (WPF) for user interface
- **TS3:** NAudio library for audio processing
- **TS4:** Windows Core Audio API (WASAPI) for device interaction
- **TS5:** Visual Studio 2022 development environment

### 4.2 Architecture
- **AR1:** Modular design with separation of concerns
- **AR2:** MVVM (Model-View-ViewModel) pattern for UI implementation
- **AR3:** Background threading for audio processing
- **AR4:** Event-driven design for device and UI interactions

### 4.3 Audio Processing Pipeline
- **AP1:** WasapiCapture for microphone input
- **AP2:** WasapiLoopbackCapture for system audio
- **AP3:** BufferedWaveProvider for handling timing differences
- **AP4:** MixingSampleProvider for combining audio streams
- **AP5:** WasapiOut for audio output to selected device
- **AP6:** Format conversion as needed between devices

## 5. User Interface Design

### 5.1 Main Window
- Simple, clean interface with minimal controls
- Logical grouping of related controls
- Clear visual feedback of system state
- Minimize to tray functionality

### 5.2 UI Controls
- Microphone input device dropdown
- Audio output device dropdown
- Horizontal volume slider with percentage display
- Mute checkbox with visual indicator
- LED-style level meter for microphone
- Status indicator showing active state

### 5.3 System Tray Integration
- Minimize to tray option
- Context menu with basic controls
- Notification area for status changes

## 6. Implementation Plan

### 6.1 Development Phases
1. **Phase 1: Core Audio Implementation** (1-2 weeks)
   - Implement basic device enumeration
   - Set up audio capture for both sources
   - Create simple mixing functionality
   - Test basic audio pipeline

2. **Phase 2: User Interface Development** (1-2 weeks)
   - Develop main application window
   - Implement device selection controls
   - Add volume slider and mute functionality
   - Create visual feedback elements
   - Implement input/output device selection

3. **Phase 3: Refinement and Optimization** (1-2 weeks)
   - Optimize audio processing for lower latency
   - Implement error handling and recovery
   - Add system tray functionality
   - Create settings persistence

4. **Phase 4: Testing and Packaging** (1 week)
   - Comprehensive testing across devices
   - Performance optimization
   - User experience improvements
   - Installation package creation

### 6.2 Fallback Plan
If significant issues are encountered with the audio redirection approach, a pivot to a virtual audio device approach may be necessary:

1. Research virtual audio device creation libraries or drivers
2. Implement a custom audio driver (potentially using existing solutions like VB-Cable)
3. Modify the application to output to the virtual device
4. Provide instructions for users to select the virtual device

## 7. Testing Requirements

### 7.1 Test Scenarios
- Multiple microphone devices (USB, built-in, virtual)
- Various output devices (speakers, headphones, virtual)
- Device hot-plugging and audio configuration changes
- Extended operation testing for stability
- Different audio content types (voice, music, mixed)

### 7.2 Performance Testing
- Latency measurement under various conditions
- CPU and memory usage monitoring
- Long-duration stability testing

## 8. Future Considerations

While not included in the initial version, these features could be considered for future updates:
- Multi-language support
- Additional audio processing features
- Support for multiple input configurations
- Expanded system tray functionality
- Installation package with proper driver signing
- Robust handling of audio device hot-plugging and configuration changes during runtime

---

*Document Version: 1.0*  
*Last Updated: April 7, 2025*
