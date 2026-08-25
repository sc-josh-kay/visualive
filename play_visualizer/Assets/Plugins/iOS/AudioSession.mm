#import <AVFoundation/AVFoundation.h>

// Sets the app's audio session to "Playback" so music plays even when the phone's mute switch
// is on (like a music app), instead of Unity's default mute-switch-respecting session.
extern "C" void _SetAudioSessionPlayback()
{
    NSError *error = nil;
    AVAudioSession *session = [AVAudioSession sharedInstance];
    [session setCategory:AVAudioSessionCategoryPlayback error:&error];
    [session setActive:YES error:&error];
}
