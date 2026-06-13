AI Generated Code

Prompt 1 – Create complete architecture
---------------------------------------------------------------------------------
Analyze this .NET Console project and create a clean architecture for an AI Audio Enhancer application.

Requirements:

1. Input video path from console.
2. Extract audio from video using FFmpeg.
3. Support multiple enhancement profiles:
   - Fast (FFmpeg filters)
   - Balanced (RNNoise)
   - Studio (DeepFilterNet)
4. Save enhanced audio separately.
5. Play enhanced audio preview using NAudio.
6. Ask user for approval.
7. If approved, replace original audio in video.
8. Save final enhanced video.
9. Use dependency injection.
10. Use async/await.
11. Create interfaces and implementations.

Create all folders, classes and interfaces.
Do not implement methods yet.

Prompt 2 – Generate FFmpeg service
---------------------------------------------------------------------------------
Implement IVideoService.

Functions required:

Task<string> ExtractAudioAsync(string videoPath)

Task<string> ReplaceAudioAsync(
    string originalVideo,
    string enhancedAudio,
    string outputVideo)

Use FFmpeg command line.

Store temporary files inside a Temp folder.

Add robust error handling and logging.

Use ProcessStartInfo.

Prompt 3 – Generate Audio Preview Service
---------------------------------------------------------------------------------
Implement IAudioPreviewService using NAudio.

Requirements:

Play audio file.

Pause audio.

Stop audio.

Show duration.

Allow user to replay audio multiple times.

Dispose resources correctly.

Prompt 4 – Generate FFmpeg Fast Enhancement
---------------------------------------------------------------------------------
Implement FastEnhancementService.

Use FFmpeg audio filters:

highpass=f=80
lowpass=f=12000
afftdn
loudnorm

Input:
wav file

Output:
enhanced wav file

Return output path.

Add logging and error handling.

Prompt 5 – Generate DeepFilterNet Integration
---------------------------------------------------------------------------------
Implement DeepFilterNetEnhancementService.

Requirements:

1. Call DeepFilterNet executable from .NET.
2. Accept input wav.
3. Produce enhanced wav.
4. Capture stdout and stderr.
5. Return enhanced file path.
6. Throw meaningful exceptions.
7. Use async execution.

Assume DeepFilterNet executable path is provided through appsettings.json.

Prompt 6 – Generate RNNoise Integration
---------------------------------------------------------------------------------
Implement RNNoiseEnhancementService.

Requirements:

1. Execute RNNoise command line tool.
2. Accept wav file.
3. Produce enhanced wav file.
4. Capture console output.
5. Handle failures gracefully.
6. Return output path.

Prompt 7 – Generate Menu System
---------------------------------------------------------------------------------
Create interactive console menu.

Show:

1. Fast Enhancement
2. Balanced Enhancement
3. Studio Enhancement
4. Exit

After enhancement:

Play preview?

Y/N

Approve result?

Y/N

If No:
Allow selecting another enhancement profile.

If Yes:
Continue to final video generation.

Prompt 8 – Generate Orchestration Service
---------------------------------------------------------------------------------
Create AudioEnhancementWorkflowService.

Flow:

Video Path
→ Extract Audio
→ Enhancement Selection
→ Enhance Audio
→ Preview Audio
→ User Approval
→ Replace Video Audio
→ Save Output Video

Implement full orchestration.

Use dependency injection.

Use async/await.

Provide progress messages

Prompt 9 – Add Configuration
---------------------------------------------------------------------------------
Add strongly typed configuration.

appsettings.json should contain:

FFmpegPath

DeepFilterNetPath

RNNoisePath

TempFolder

OutputFolder

Create options classes and register them with dependency injection.

Prompt 10 – Production Improvements
---------------------------------------------------------------------------------
Review the entire solution.

Add:

1. CancellationToken support.
2. Structured logging.
3. Retry policy.
4. Progress reporting.
5. Temporary file cleanup.
6. Validation of paths.
7. Validation of FFmpeg installation.
8. Unit-testable design.
9. XML comments.
10. Error recovery.

One final "super prompt"
---------------------------------------------------------------------------------

After all code is generated:

Act as a senior .NET architect.

Review the entire solution.

Refactor for production readiness.

Goals:

- SOLID principles
- Clean Architecture
- Dependency Injection
- Testability
- Performance
- Large video support
- Memory efficiency
- Proper disposal
- Detailed logging

Provide all code changes necessary.
