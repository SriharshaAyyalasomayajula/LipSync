Kutumb — Mini Unity Demo
=========================

What this contains
- Simple Login scene with a "Sign in with Apple" button that simulates success/failure.
- Humanoid scene with a character, a "Play Reaction" and "Play Dialogue" button and a ReactionController that plays Smile/Sad sequence and lip-syncs a local audio file.

Key scripts (Assets/Scripts)
- `LoginManager.cs` — handles sign-in button and loads `HumanoidScene` on success.
- `ReactionController.cs` — plays Smile→Sad→Smile→Sad, coordinates audio and lip-sync.
- `LipSyncFake.cs` — simple mouth open/close faux lip-sync using blendshapes or an Animator bool.

